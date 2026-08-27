import { nextTick, ref, watch } from 'vue'
import { settingSyncIsland, settingSyncRemoteApplied } from '~/composables/useSettingSyncIsland'
import { FAVORITES_SYNC_KEY, PREFERENCE_SETTING_KEY, PREFERENCE_SYNC_KEY, SEARCH_SYNC_KEY, STORAGE_PREFIX, TABLE_SYNC_KEY, THEME_ANIMATION_ENABLED_KEY, THEME_AUTO, THEME_MODE_KEY, USER_SETTING_CLIENT_ID, UserSettingScene, WIDGETS_SYNC_KEY } from '~/constants'
import { i18n } from '~/locales'
import { LocalStorage, runThemeTransition } from '~/utils'
import { useAppContext } from './app-context'

const BUILD_TIME_KEY = `${STORAGE_PREFIX}build_time`

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
 * 偏好草稿模式：偏好设置抽屉打开期间为 true，此时偏好变更只更新内存 ref（实时预览），
 * 不落地 localStorage / 不上行后端；点击「保存」才提交，关闭抽屉若未保存则还原。
 */
let persistSuspended = false
/** 草稿基线快照（storageKey → 进入草稿/最近一次保存时的值），用于「取消」还原 */
let draftSnapshot: Map<string, unknown> | null = null
/** 草稿模式下是否存在未保存变更（响应式，供「保存」按钮启用判定） */
export const preferenceDraftDirty = ref(false)

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

/**
 * 同步开关本身为「设备本地」维度：仅本地存储，不随偏好快照上行后端、也不被远端覆盖。
 */
const DEVICE_LOCAL_KEYS = new Set<string>([
  PREFERENCE_SYNC_KEY,
  FAVORITES_SYNC_KEY,
  SEARCH_SYNC_KEY,
  TABLE_SYNC_KEY,
  WIDGETS_SYNC_KEY,
])

/**
 * 下次上行时随变更携带的动画起点，形如 'x,y'（视口百分比）。
 * 存百分比而非像素：各端视口尺寸不同，只有相对位置才能在对端还原出「同一个位置」。
 * 由主题切换登记，上行一次即清空——它描述的是那一次点击，不该粘到后续任何变更上。
 */
let pendingOrigin: null | string = null

/** 登记本次变更的动画起点（视口百分比）。传 null 表示无起点（如键盘/程序触发） */
export function setPendingPreferenceOrigin(origin: null | string): void {
  pendingOrigin = origin
}

/** 读注册表里某条偏好的当前值（未注册时回退 localStorage） */
function readLocalPreference<T>(key: string): T | undefined {
  const source = registry.get(key)
  return source ? (source.value as T) : (LocalStorage.get<T>(key) ?? undefined)
}

/**
 * 把发起端携带的视口百分比起点还原为本端坐标。
 * 各端视口尺寸不同，按百分比换算才能落在「相对位置相同」的点上。
 */
function parseTransitionOrigin(origin?: null | string): { clientX: number, clientY: number } | undefined {
  if (!origin) {
    return undefined
  }
  const [xRaw, yRaw] = origin.split(',')
  const xPercent = Number(xRaw)
  const yPercent = Number(yRaw)
  if (!Number.isFinite(xPercent) || !Number.isFinite(yPercent)) {
    return undefined
  }
  return {
    clientX: (xPercent / 100) * window.innerWidth,
    clientY: (yPercent / 100) * window.innerHeight,
  }
}

/** 当前实际呈现的明暗（auto 取系统主题） */
function isCurrentlyDark(): boolean {
  const mode = readLocalPreference<string>(THEME_MODE_KEY)
  if (mode === THEME_AUTO) {
    return window.matchMedia?.('(prefers-color-scheme: dark)').matches ?? false
  }
  return mode === 'dark'
}

/** 远端快照应用后实际呈现的明暗；快照未携带主题时返回 null */
function resolveRemoteEffectiveDark(remote: Record<string, unknown>): boolean | null {
  const mode = remote[THEME_MODE_KEY]
  if (typeof mode !== 'string') {
    return null
  }
  if (mode === THEME_AUTO) {
    return window.matchMedia?.('(prefers-color-scheme: dark)').matches ?? false
  }
  return mode === 'dark'
}

/** 偏好后端同步是否启用（用户开关，默认开启=上行后端并实时推送多端）。读自注册表中的同步开关偏好。 */
export function isPreferenceSyncEnabled(): boolean {
  return registry.get(PREFERENCE_SYNC_KEY)?.value === true
}

/** 收藏夹后端同步是否启用（默认开启；关闭后仅本地存储） */
export function isFavoritesSyncEnabled(): boolean {
  return registry.get(FAVORITES_SYNC_KEY)?.value === true
}

/** 搜索设置后端同步是否启用（默认开启；关闭后仅本地存储） */
export function isSearchSyncEnabled(): boolean {
  return registry.get(SEARCH_SYNC_KEY)?.value === true
}

/** 表格/视图设置后端同步是否启用（默认开启；关闭后仅本地存储） */
export function isTableSyncEnabled(): boolean {
  return registry.get(TABLE_SYNC_KEY)?.value === true
}

/** 小组件看板后端同步是否启用（默认开启；关闭后仅本地存储） */
export function isWidgetsSyncEnabled(): boolean {
  return registry.get(WIDGETS_SYNC_KEY)?.value === true
}

/** 构建整份偏好快照（排除同步开关本身——设备本地维度，不上行） */
function buildPreferenceSnapshot(): string {
  const snapshot: Record<string, unknown> = {}
  registry.forEach((source, key) => {
    if (DEVICE_LOCAL_KEYS.has(key)) {
      return
    }
    snapshot[key] = source.value
  })
  return JSON.stringify(snapshot)
}

/** 立即把整份偏好快照上行后端（尽力而为，失败静默；仅在已开启同步时执行） */
function pushPreferencesToBackend(): void {
  if (!isPreferenceSyncEnabled()) {
    return
  }
  const settingValue = buildPreferenceSnapshot()
  const origin = pendingOrigin
  pendingOrigin = null
  const task = settingSyncIsland('pref:save', i18n.global.t('island.sync.name.preferences'))
  void useAppContext()
    .apis
    .userSettingApi
    .save({ scene: UserSettingScene.Preference, settingKey: PREFERENCE_SETTING_KEY, settingValue, clientId: USER_SETTING_CLIENT_ID, origin })
    .then(() => task.success())
    .catch(() => task.error())
}

/** 防抖把整份偏好快照上行后端（尽力而为，失败静默） */
function scheduleBackendSync() {
  // 草稿模式 / 会话回写未就绪 / 用户未开启后端同步：不上行
  if (persistSuspended || !backendSyncEnabled || !isPreferenceSyncEnabled())
    return
  if (syncTimer) {
    clearTimeout(syncTimer)
  }
  syncTimer = setTimeout(() => {
    syncTimer = null
    // 防抖期间三道门都可能关闭，触发时必须与入口同样复查：
    // 只查同步开关会让「排队后才进入草稿」的场景把草稿预览值随整份快照上行并推给其它设备。
    // 此处丢弃这一次上行是安全的：快照是全量的，保存草稿 / 下一次任意偏好变更都会把最新值补上。
    if (persistSuspended || !backendSyncEnabled || !isPreferenceSyncEnabled())
      return
    pushPreferencesToBackend()
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
    // 草稿模式：仅内存预览，不落地；标记存在未保存变更
    if (persistSuspended) {
      preferenceDraftDirty.value = true
      return
    }
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

/**
 * 进入偏好草稿模式（偏好设置抽屉打开时调用）。
 * 暂停落地（变更仅内存预览），并快照当前已保存值作为「取消」还原的基线。
 */
export function beginPreferenceDraft(): void {
  draftSnapshot = new Map()
  registry.forEach((source, key) => draftSnapshot!.set(key, source.value))
  preferenceDraftDirty.value = false
  persistSuspended = true
}

/**
 * 保存草稿（点击「保存」时调用）。
 * 将当前内存值落地 localStorage，并立即按开关上行后端；更新还原基线，dirty 复位。
 */
export function commitPreferenceDraft(): void {
  if (!draftSnapshot) {
    return
  }
  registry.forEach((source, key) => {
    LocalStorage.set(key, source.value)
    draftSnapshot!.set(key, source.value)
  })
  preferenceDraftDirty.value = false
  pushPreferencesToBackend()
}

/**
 * 放弃草稿（关闭抽屉时调用）。
 * 还原到基线（最近一次保存或打开时的值），结束草稿模式恢复正常落地。
 * 由于 commit 会更新基线，已保存的变更不会被还原。
 */
export function discardPreferenceDraft(): void {
  const snapshot = draftSnapshot
  draftSnapshot = null
  preferenceDraftDirty.value = false
  if (!snapshot) {
    persistSuspended = false
    return
  }
  let reverted = false
  snapshot.forEach((original, key) => {
    const source = registry.get(key)
    if (source && source.value !== original) {
      source.value = original
      reverted = true
    }
  })
  // 还原触发的 watch 回调在下一微任务才 flush；需等其跑完（此时 persistSuspended 仍为 true，不落地）
  // 再解除暂停，避免把还原值又写回 localStorage / 上行后端
  if (reverted) {
    void nextTick(() => {
      persistSuspended = false
      // 还原本身也会经 bindPersist 的 watch 在暂停期把 dirty 顶回 true，
      // 故在 watch flush 完成后再复位一次，避免「已取消却仍提示未保存」
      preferenceDraftDirty.value = false
    })
  }
  else {
    persistSuspended = false
  }
}

/** 立即更新 ref 并写入 localStorage（ref 变化亦会经 bindPersist 的 watch 触发后端同步） */
export function save<T>(key: string, target: { value: T }, value: T) {
  target.value = value
  // 草稿模式：仅预览（dirty 由 bindPersist 的 watch 标记），不落地
  if (persistSuspended) {
    return
  }
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
  // 未开启后端同步：本地模式，不拉取后端；仍开启回写门以便用户随后启用同步时即时生效
  if (!isPreferenceSyncEnabled()) {
    backendSyncEnabled = true
    return
  }
  // 登录流程由登录灵动岛统一覆盖，此处不重复提示；刷新恢复会话时则独立提示
  const task = options?.showIsland === false ? null : settingSyncIsland('pref:hydrate', i18n.global.t('island.sync.name.preferences'))
  // 应用远端期间暂停回写，避免把刚拉取的远端值原样回传
  backendSyncEnabled = false
  let needSeed = false
  try {
    const dto = await useAppContext().apis.userSettingApi.get({ scene: UserSettingScene.Preference, settingKey: PREFERENCE_SETTING_KEY })
    const remote = dto?.settingValue ? (JSON.parse(dto.settingValue) as Record<string, unknown>) : null
    if (remote && typeof remote === 'object') {
      const map = remote as Record<string, unknown>
      registry.forEach((source, key) => {
        // 同步开关为设备本地维度，不受远端覆盖
        if (DEVICE_LOCAL_KEYS.has(key)) {
          return
        }
        if (key in map && map[key] !== undefined) {
          source.value = map[key]
        }
      })
    }
    else {
      // 后端无偏好记录：登录后以本地当前偏好播种
      needSeed = true
    }
    task?.success()
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

/**
 * 应用来自其它在线设备的偏好变更推送（SignalR UserSettingChanged，scene=Preference / settingKey=global）。
 * 仅在已开启偏好同步、非草稿编辑中、会话回写已就绪（已水合）时应用；
 * 应用期间暂停回写：watch 照常落地 localStorage，但不把远端值回环上行。
 * 草稿模式（偏好抽屉打开）期间忽略远端推送，避免编辑中界面跳变——按「最后保存者赢」收敛。
 */
export async function applyRemotePreferenceSnapshot(settingValue?: null | string, origin?: null | string): Promise<void> {
  if (!settingValue || !isPreferenceSyncEnabled() || persistSuspended || !backendSyncEnabled) {
    return
  }
  let remote: Record<string, unknown>
  try {
    remote = JSON.parse(settingValue) as Record<string, unknown>
  }
  catch {
    return
  }
  if (!remote || typeof remote !== 'object') {
    return
  }
  backendSyncEnabled = false
  const applyRemoteValues = () => {
    registry.forEach((source, key) => {
      // 同步开关为设备本地维度，不受远端覆盖
      if (DEVICE_LOCAL_KEYS.has(key)) {
        return
      }
      if (key in remote && remote[key] !== undefined) {
        source.value = remote[key]
      }
    })
  }

  // 明暗真的翻转时走与本机点击切换同一条圆形扩散动画；
  // 其余偏好变更本机也是直接生效，这里同样直接应用
  const toDark = resolveRemoteEffectiveDark(remote)
  if (toDark !== null && toDark !== isCurrentlyDark()) {
    await runThemeTransition({
      toDark,
      // 发起端带了点击位置就从对应相对位置扩散，否则从视口中心
      origin: parseTransitionOrigin(origin),
      enabled: readLocalPreference<boolean>(THEME_ANIMATION_ENABLED_KEY) ?? true,
      commit: async () => {
        applyRemoteValues()
        await nextTick()
      },
    })
  }
  else {
    applyRemoteValues()
  }
  // 等本轮 watch flush（写本地、上行被门挡住）后恢复回写
  await nextTick()
  backendSyncEnabled = true
  settingSyncRemoteApplied('pref:remote', i18n.global.t('island.sync.name.preferences'))
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
