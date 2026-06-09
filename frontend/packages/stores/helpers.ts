import { nextTick, watch } from 'vue'
import { islandStart } from '~/composables/useDynamicIsland'
import { STORAGE_PREFIX } from '~/constants'
import { LocalStorage } from '~/utils'
import { useAppContext } from './app-context'

const BUILD_TIME_KEY = `${STORAGE_PREFIX}build_time`

/**
 * 偏好设置在 PagePreference 中的保留页面码（全局，按当前用户维度）。
 * 与收藏夹 [[favorites]] 同源：后端不解释 Payload 语义，仅按 (UserId, PageCode) 存取，零后端改动。
 */
const PREFERENCES_PAGE_CODE = '__app:preferences__'
/** PagePreference 载荷分区名（payload = {"preferences":{ storageKey: value, ... }}） */
const PREFERENCES_SECTION = 'preferences'

/**
 * 所有经 bindPersist 注册的偏好 (storageKey → ref)。
 * 仅 theme / layout / preferences 三个 slice 走 bindPersist，故此注册表恰好等于「偏好设置」全集，
 * 不含 token / 用户信息 / 收藏夹 / 标签等（它们不经 bindPersist），可安全整体上行后端。
 */
const registry = new Map<string, { value: unknown }>()
/** 各偏好的默认值 (storageKey → default)，由 bindPersist 第三参登记，供「重置偏好」恢复默认值使用 */
const defaults = new Map<string, unknown>()
/** 后端回写开关：仅登录并完成水合后开启，避免登录页 / 离线时产生无谓的失败请求 */
let backendSyncEnabled = false
/** 一次会话仅水合一次（登录或刷新后），退出登录时重置 */
let hydrated = false
let syncTimer: ReturnType<typeof setTimeout> | null = null

/**
 * BUILD_TIME 缓存失效：
 * 当 `__APP_BUILD_TIME__` 变化时清除所有带前缀的偏好缓存，
 * 强制使用新默认值，解决"用户本地偏好锁死新功能"的问题。
 */
export function invalidateCacheIfBuildTimeChanged() {
  const currentBuildTime = typeof __APP_BUILD_TIME__ === 'string' ? __APP_BUILD_TIME__ : ''
  if (!currentBuildTime)
    return

  const savedBuildTime = LocalStorage.get<string>(BUILD_TIME_KEY)
  if (savedBuildTime === currentBuildTime)
    return

  const keysToRemove: string[] = []
  for (let i = 0; i < localStorage.length; i++) {
    const key = localStorage.key(i)
    if (key?.startsWith(STORAGE_PREFIX) && key !== BUILD_TIME_KEY) {
      keysToRemove.push(key)
    }
  }
  keysToRemove.forEach(key => localStorage.removeItem(key))
  LocalStorage.set(BUILD_TIME_KEY, currentBuildTime)
}

/** 防抖把整份偏好快照上行后端（尽力而为，失败静默） */
function scheduleBackendSync() {
  if (!backendSyncEnabled)
    return
  if (syncTimer) {
    clearTimeout(syncTimer)
  }
  syncTimer = setTimeout(() => {
    const snapshot: Record<string, unknown> = {}
    registry.forEach((source, key) => {
      snapshot[key] = source.value
    })
    const payload = JSON.stringify({ [PREFERENCES_SECTION]: snapshot })
    const task = islandStart('pref:save', '正在同步偏好设置…')
    void useAppContext()
      .apis
      .pagePreferenceApi
      .save({ pageCode: PREFERENCES_PAGE_CODE, payload })
      .then(() => task.success('偏好设置已同步'))
      .catch(() => task.error('偏好设置同步失败'))
  }, 800)
}

/**
 * 监听 ref 变化并自动写入 localStorage；同时登记进偏好注册表，
 * 任一偏好变化触发整份快照防抖上行后端（开启回写后）。
 */
export function bindPersist<T>(key: string, source: { value: T }, defaultValue?: T) {
  registry.set(key, source as { value: unknown })
  if (defaultValue !== undefined) {
    defaults.set(key, defaultValue)
  }
  watch(source, (value) => {
    LocalStorage.set(key, value)
    scheduleBackendSync()
  })
}

/**
 * 重置所有「偏好设置」为默认值。
 * 仅作用于经 bindPersist 登记了默认值的偏好（恰为偏好设置全集），不含 token / 用户信息 / 收藏夹 / 标签。
 * 直接改写内存 ref → 经各自 watch 同步落地 localStorage 并防抖上行后端；不触发整页刷新，因此不会影响登录态。
 */
export function resetRegisteredPreferences(): void {
  registry.forEach((source, key) => {
    if (defaults.has(key)) {
      source.value = defaults.get(key)
    }
  })
}

/** 立即更新 ref 并写入 localStorage（ref 变化亦会经 bindPersist 的 watch 触发后端同步） */
export function save<T>(key: string, target: { value: T }, value: T) {
  target.value = value
  LocalStorage.set(key, value)
}

/**
 * 登录 / 刷新恢复会话后：拉取后端偏好并覆盖本地（应用到已注册的偏好 ref）。
 * - localStorage 仍是即时事实源，后端为跨端覆盖层；端点未就绪 / 无记录时静默保留本地。
 * - 一次会话仅执行一次；退出登录后由 resetPreferenceBackendSync 重置。
 * - 后端无记录时以当前本地偏好播种后端（首端首次登录即落库）。
 */
export async function hydratePreferencesFromBackend(options?: { showIsland?: boolean }): Promise<void> {
  if (hydrated)
    return
  hydrated = true
  // 登录流程由登录灵动岛统一覆盖，此处不重复提示；刷新恢复会话时则独立提示
  const task = options?.showIsland === false ? null : islandStart('pref:hydrate', '正在同步偏好设置…')
  // 应用远端期间暂停回写，避免把刚拉取的远端值原样回传
  backendSyncEnabled = false
  let needSeed = false
  try {
    const dto = await useAppContext().apis.pagePreferenceApi.get(PREFERENCES_PAGE_CODE)
    const parsed = dto?.payload ? (JSON.parse(dto.payload) as Record<string, unknown>) : null
    const remote = parsed ? parsed[PREFERENCES_SECTION] : null
    if (remote && typeof remote === 'object') {
      const map = remote as Record<string, unknown>
      registry.forEach((source, key) => {
        if (key in map && map[key] !== undefined) {
          source.value = map[key]
        }
      })
    }
    else {
      // 后端无偏好记录：登录后以本地当前偏好播种
      needSeed = true
    }
    task?.success('偏好设置已同步')
  }
  catch {
    // 静默：保留本地
    task?.dismiss()
  }
  finally {
    // 等本轮 watch 全部 flush（写本地、跳过回写）后再开启回写
    await nextTick()
    backendSyncEnabled = true
    if (needSeed) {
      scheduleBackendSync()
    }
  }
}

/** 退出登录：停止后端回写、清空待发请求，允许下次登录重新水合 */
export function resetPreferenceBackendSync(): void {
  backendSyncEnabled = false
  hydrated = false
  if (syncTimer) {
    clearTimeout(syncTimer)
    syncTimer = null
  }
}
