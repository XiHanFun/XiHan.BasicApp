import type { FavoriteItem } from '~/types'
import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { settingSyncIsland, settingSyncRemoteApplied } from '~/composables/useSettingSyncIsland'
import { bindSyncedSetting } from '~/composables/useSyncedSetting'
import { FAVORITES_KEY, FAVORITES_SETTING_KEY, USER_SETTING_CLIENT_ID, UserSettingScene } from '~/constants'
import { i18n } from '~/locales'
import { useAppContext } from '~/stores/app-context'
import { isFavoritesSyncEnabled } from '~/stores/helpers'
import { SetupStoreId } from '~/stores/store-ids'
import { LocalStorage } from '~/utils'

/**
 * 收藏夹状态：收藏常用菜单，支持增删、排序，跨端同步。
 *
 * 设计与「表格设置 / 搜索设置」一致，拉取、推送与回写走 bindSyncedSetting：
 * - 交互即时写本地，后端「尽力而为」防抖落库，端点未就绪 / 离线时静默回退本地；
 * - 开着同步时以后端那份为准：ready 之前界面不摆收藏（本地那份可能是别的设备改动之前的旧值），超时先按本地摆；
 *   后端那份与其它设备推来的都写回本地。
 */

/** 后端载荷归一成收藏列表；认不出返回 null */
function parseFavorites(value: unknown): FavoriteItem[] | null {
  if (!Array.isArray(value)) {
    return null
  }
  return value
    .filter((x): x is FavoriteItem => Boolean(x) && typeof (x as FavoriteItem).path === 'string')
    .map(x => ({ key: x.path, title: x.title, path: x.path, icon: x.icon }))
}
export const useFavoritesStore = defineStore(SetupStoreId.Favorites, () => {
  const favorites = ref<FavoriteItem[]>(LocalStorage.get<FavoriteItem[]>(FAVORITES_KEY) ?? [])
  /**
   * 收藏能否摆出来：关着同步立即为真；开着同步等后端那份回来（超时也先置真）。
   * 只升不降：会话里再次水合（布局重新挂载、进仪表盘）时后台刷新，已摆出的收藏不缩回去
   */
  const ready = ref(!isFavoritesSyncEnabled())
  let inflight: Promise<void> | null = null
  let saveTimer: ReturnType<typeof setTimeout> | null = null

  const count = computed(() => favorites.value.length)

  function has(path: string): boolean {
    return favorites.value.some(item => item.path === path)
  }

  /** 持久化：立即写本地 + 防抖落库（失败静默，不阻塞交互） */
  function persist(): void {
    LocalStorage.set(FAVORITES_KEY, favorites.value)
    // 未开启收藏夹同步：仅本地存储，不上行后端
    if (!isFavoritesSyncEnabled()) {
      return
    }
    if (saveTimer) {
      clearTimeout(saveTimer)
    }
    saveTimer = setTimeout(() => {
      const settingValue = JSON.stringify(favorites.value)
      const task = settingSyncIsland('favorites:save', i18n.global.t('island.sync.name.favorites'))
      void useAppContext()
        .apis
        .userSettingApi
        .save({ scene: UserSettingScene.Preference, settingKey: FAVORITES_SETTING_KEY, settingValue, clientId: USER_SETTING_CLIENT_ID })
        .then(() => task.success())
        .catch(() => task.error())
    }, 600)
  }

  /** 新增收藏；已存在则忽略。返回是否真正新增。 */
  function add(item: FavoriteItem): boolean {
    if (!item.path || has(item.path)) {
      return false
    }
    favorites.value = [...favorites.value, { ...item, key: item.path }]
    persist()
    return true
  }

  /** 移除收藏 */
  function remove(path: string): void {
    const next = favorites.value.filter(item => item.path !== path)
    if (next.length === favorites.value.length) {
      return
    }
    favorites.value = next
    persist()
  }

  /** 切换收藏状态。返回切换后是否为「已收藏」。 */
  function toggle(item: FavoriteItem): boolean {
    if (has(item.path)) {
      remove(item.path)
      return false
    }
    return add(item)
  }

  /** 拖拽排序：把 fromIndex 项移动到 toIndex 位置 */
  function move(fromIndex: number, toIndex: number): void {
    if (
      fromIndex < 0
      || toIndex < 0
      || fromIndex >= favorites.value.length
      || toIndex >= favorites.value.length
      || fromIndex === toIndex
    ) {
      return
    }
    const next = [...favorites.value]
    const [moved] = next.splice(fromIndex, 1)
    if (!moved) {
      return
    }
    next.splice(toIndex, 0, moved)
    favorites.value = next
    persist()
  }

  /** 清空收藏 */
  function clear(): void {
    if (!favorites.value.length) {
      return
    }
    favorites.value = []
    persist()
  }

  /** 拉取后端收藏；失败或无记录 resolve undefined，保留本地 */
  async function loadRemote(): Promise<FavoriteItem[] | undefined> {
    const task = settingSyncIsland('favorites:hydrate', i18n.global.t('island.sync.name.favorites'))
    try {
      const dto = await useAppContext().apis.userSettingApi.get({ scene: UserSettingScene.Preference, settingKey: FAVORITES_SETTING_KEY })
      const list = parseFavorites(dto?.settingValue ? JSON.parse(dto.settingValue) : null)
      task.success()
      return list ?? undefined
    }
    catch {
      task.dismiss()
      return undefined
    }
  }

  /**
   * 从后端拉取并覆盖本地（尽力而为；端点未就绪 / 离线 / 无记录时静默保留本地）。
   * 以 in-flight 去重，允许布局重新挂载（如切换用户）时重新水合。
   */
  function hydrate(): Promise<void> {
    if (inflight) {
      return inflight
    }
    const binding = bindSyncedSetting<FavoriteItem[]>(
      { enabled: isFavoritesSyncEnabled, load: loadRemote },
      {
        // 空数组也是用户存下的（收藏全删了），同样采用
        accept: (value): value is FavoriteItem[] => Array.isArray(value),
        apply: (list) => {
          favorites.value = list
        },
        writeLocal: list => LocalStorage.set(FAVORITES_KEY, list),
      },
      {
        onReady: () => {
          ready.value = true
        },
      },
    )
    inflight = binding.settled.finally(() => {
      inflight = null
    })
    return inflight
  }

  /**
   * 应用来自其它在线设备的收藏夹变更推送（SignalR UserSettingChanged，scene=Preference / settingKey=favorites）。
   * 直接覆盖内存与本地，不再回环上行。
   */
  function applyRemote(settingValue?: null | string): void {
    if (!settingValue || !isFavoritesSyncEnabled()) {
      return
    }
    let list: FavoriteItem[] | null
    try {
      list = parseFavorites(JSON.parse(settingValue))
    }
    catch {
      return
    }
    if (!list) {
      return
    }
    favorites.value = list
    LocalStorage.set(FAVORITES_KEY, list)
    settingSyncRemoteApplied('favorites:remote', i18n.global.t('island.sync.name.favorites'))
  }

  return { favorites, ready, count, has, add, remove, toggle, move, clear, hydrate, applyRemote }
})
