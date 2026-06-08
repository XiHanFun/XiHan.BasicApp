import type { FavoriteItem } from '~/types'
import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { FAVORITES_KEY } from '~/constants'
import { useAppContext } from '~/stores/app-context'
import { SetupStoreId } from '~/stores/store-ids'
import { LocalStorage } from '~/utils'

/**
 * 收藏夹在 PagePreference 中的保留页面码（全局，按当前用户维度，与具体列表页无关）。
 * 后端不解释 Payload 语义，仅按 (UserId, PageCode) 存取，故收藏夹复用此机制即可零后端改动。
 */
const FAVORITES_PAGE_CODE = '__app:favorites__'
/** PagePreference 载荷分区名（payload = {"favorites":[...]}） */
const FAVORITES_SECTION = 'favorites'

/**
 * 收藏夹状态：收藏常用菜单，支持增删、排序，跨端同步。
 *
 * 设计与「表格设置 / 搜索设置」一致：
 * - localStorage 为同步事实源，交互即时；
 * - 后端为「尽力而为」的跨端同步（防抖落库），端点未就绪 / 离线时静默回退本地，不阻塞 UI。
 */
export const useFavoritesStore = defineStore(SetupStoreId.Favorites, () => {
  const favorites = ref<FavoriteItem[]>(LocalStorage.get<FavoriteItem[]>(FAVORITES_KEY) ?? [])
  let inflight: Promise<void> | null = null
  let saveTimer: ReturnType<typeof setTimeout> | null = null

  const count = computed(() => favorites.value.length)

  function has(path: string): boolean {
    return favorites.value.some(item => item.path === path)
  }

  /** 持久化：立即写本地 + 防抖落库（失败静默，不阻塞交互） */
  function persist(): void {
    LocalStorage.set(FAVORITES_KEY, favorites.value)
    if (saveTimer) {
      clearTimeout(saveTimer)
    }
    saveTimer = setTimeout(() => {
      const payload = JSON.stringify({ [FAVORITES_SECTION]: favorites.value })
      void useAppContext()
        .apis.pagePreferenceApi.save({ pageCode: FAVORITES_PAGE_CODE, payload })
        .catch(() => {})
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

  /**
   * 从后端拉取并覆盖本地（尽力而为；端点未就绪 / 离线 / 无记录时静默保留本地）。
   * 以 in-flight 去重，允许布局重新挂载（如切换用户）时重新水合。
   */
  function hydrate(): Promise<void> {
    if (inflight) {
      return inflight
    }
    inflight = (async () => {
      try {
        const dto = await useAppContext().apis.pagePreferenceApi.get(FAVORITES_PAGE_CODE)
        if (!dto?.payload) {
          return
        }
        const parsed = JSON.parse(dto.payload) as Record<string, unknown>
        const remote = parsed[FAVORITES_SECTION]
        if (!Array.isArray(remote)) {
          return
        }
        const list: FavoriteItem[] = remote
          .filter((x): x is FavoriteItem => Boolean(x) && typeof (x as FavoriteItem).path === 'string')
          .map(x => ({ key: x.path, title: x.title, path: x.path, icon: x.icon }))
        favorites.value = list
        LocalStorage.set(FAVORITES_KEY, list)
      }
      catch {
        // 静默：保留本地
      }
    })().finally(() => {
      inflight = null
    })
    return inflight
  }

  return { favorites, count, has, add, remove, toggle, move, clear, hydrate }
})
