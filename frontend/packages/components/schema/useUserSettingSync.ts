import { settingSyncIsland, settingSyncRemoteApplied } from '~/composables/useSettingSyncIsland'
import { USER_SETTING_CLIENT_ID, UserSettingScene } from '~/constants'
import { isSearchSyncEnabled, isTableSyncEnabled, useAppContext } from '~/stores'

/** 分区 → 中文名（用于灵动岛同步提示） */
const SECTION_NAMES: Record<string, string> = {
  table: '表格设置',
  search: '搜索设置',
  views: '视图',
}

function sectionName(section: string): string {
  return SECTION_NAMES[section] ?? '页面设置'
}

/**
 * 分区是否开启后端同步：搜索→「同步搜索设置」开关；表格/视图→「同步表格设置」开关；其余默认开启。
 * 关闭时仅本地（localStorage 由各调用方维护），不上行 / 不拉取后端。
 */
function isSectionSyncEnabled(section: string): boolean {
  if (section === 'search') {
    return isSearchSyncEnabled()
  }
  if (section === 'table' || section === 'views') {
    return isTableSyncEnabled()
  }
  return true
}

/**
 * 页面设置后端同步（跨端）。
 *
 * 后端用户设置以 (用户 × 场景 × 设置键) 存一条 JSON 载荷；页面设置场景固定 scene=Page、settingKey=pageCode，
 * 前端在该载荷中按「分区」(section) 读写：
 * - 列设置 → section `table`
 * - 个人视图 → section `views`
 *
 * 设计为「尽力而为」：localStorage 仍是同步事实源；后端加载成功则覆盖本地，
 * 保存失败（如端点未就绪/离线）静默忽略，不阻塞交互。
 */
interface UserSettingApiShape {
  get: (input: { scene: number, settingKey: string }) => Promise<{ scene: number, settingKey: string, settingValue?: null | string }>
  save: (input: { scene: number, settingKey: string, settingValue?: null | string, clientId?: string }) => Promise<{ scene: number, settingKey: string, settingValue?: null | string }>
}

let sharedApi: UserSettingApiShape | null = null

/** pageCode → 已合并的载荷对象（内存缓存，分区读写共享同一条后端记录） */
const cache = new Map<string, Record<string, unknown>>()
const loading = new Map<string, Promise<Record<string, unknown>>>()
const saveTimers = new Map<string, ReturnType<typeof setTimeout>>()

function resolveApi(): UserSettingApiShape {
  sharedApi ??= useAppContext().apis.userSettingApi as UserSettingApiShape
  return sharedApi
}

async function loadPayload(pageCode: string): Promise<Record<string, unknown>> {
  let pending = loading.get(pageCode)
  if (!pending) {
    // 后台读取：灵动岛只在拉取期间显示「同步中」活动态，完成即静默消失（不弹成功提示，避免每次进页面刷屏）
    const task = settingSyncIsland('page-load', '页面设置')
    pending = resolveApi()
      .get({ scene: UserSettingScene.Page, settingKey: pageCode })
      .then((dto) => {
        let parsed: Record<string, unknown> = {}
        if (dto?.settingValue) {
          try {
            parsed = JSON.parse(dto.settingValue) as Record<string, unknown>
          }
          catch {
            parsed = {}
          }
        }
        cache.set(pageCode, parsed)
        task.dismiss()
        return parsed
      })
      .catch(() => {
        cache.set(pageCode, {})
        task.dismiss()
        return {}
      })
    loading.set(pageCode, pending)
  }
  return pending
}

function saveSection(pageCode: string, section: string, value: unknown): void {
  // 该分区未开启同步：仅本地（由调用方写 localStorage），不更新合并缓存、不上行后端
  if (!isSectionSyncEnabled(section)) {
    return
  }
  const payload = cache.get(pageCode) ?? {}
  payload[section] = value
  cache.set(pageCode, payload)

  const existing = saveTimers.get(pageCode)
  if (existing) {
    clearTimeout(existing)
  }
  saveTimers.set(
    pageCode,
    setTimeout(() => {
      const task = settingSyncIsland(`page:${pageCode}`, sectionName(section))
      void resolveApi()
        .save({ scene: UserSettingScene.Page, settingKey: pageCode, settingValue: JSON.stringify(payload), clientId: USER_SETTING_CLIENT_ID })
        .then(() => task.success())
        .catch(() => task.error())
    }, 600),
  )
}

// ── 远端实时推送（SignalR UserSettingChanged，scene=Page）────────
/** 页面设置远端变更监听者（pageCode → section → 监听者集合） */
const remoteListeners = new Map<string, Map<string, Set<(value: unknown) => void>>>()

/**
 * 订阅某页面某分区的远端变更（其它设备保存后实时应用到已打开页面）。
 * 返回退订函数，调用方需在作用域销毁时退订。
 */
export function subscribeRemotePageSetting(
  pageCode: string,
  section: string,
  listener: (value: unknown) => void,
): () => void {
  let sections = remoteListeners.get(pageCode)
  if (!sections) {
    sections = new Map()
    remoteListeners.set(pageCode, sections)
  }
  let listeners = sections.get(section)
  if (!listeners) {
    listeners = new Set()
    sections.set(section, listeners)
  }
  listeners.add(listener)
  return () => {
    listeners.delete(listener)
  }
}

/**
 * 应用来自其它在线设备的页面设置推送：覆盖该页合并缓存（后续 hydrate 即取新值），
 * 并按分区分发给已打开页面的监听者（同步开关关闭的分区跳过）。
 * 推送载荷为整页全量而一次保存只改一个分区，故对照旧缓存仅分发真正变化的分区，
 * 避免未变分区被重复应用、一次推送弹出多条提示。
 */
export function applyRemotePageSetting(pageCode: string, settingValue?: null | string): void {
  let payload: Record<string, unknown>
  try {
    payload = settingValue ? JSON.parse(settingValue) as Record<string, unknown> : {}
  }
  catch {
    return
  }
  if (!payload || typeof payload !== 'object') {
    return
  }
  const previous = cache.get(pageCode)
  cache.set(pageCode, payload)
  const sections = remoteListeners.get(pageCode)
  if (!sections) {
    return
  }
  sections.forEach((listeners, section) => {
    if (listeners.size === 0 || !isSectionSyncEnabled(section)) {
      return
    }
    const value = payload[section]
    if (value === undefined) {
      return
    }
    if (previous && JSON.stringify(previous[section]) === JSON.stringify(value)) {
      return
    }
    listeners.forEach(listener => listener(value))
    settingSyncRemoteApplied(`page:${pageCode}:${section}:remote`, sectionName(section))
  })
}

export interface UserSettingSync {
  /** 读取指定分区的远端值（首次触发后端加载，后续走缓存） */
  hydrate: <T>(section: string) => Promise<T | undefined>
  /** 写入指定分区（合并入整条载荷，防抖落库；失败静默） */
  save: (section: string, value: unknown) => void
  /** 订阅该分区的远端实时变更（返回退订函数） */
  subscribeRemote: (section: string, listener: (value: unknown) => void) => () => void
}

export function useUserSettingSync(pageCode: string): UserSettingSync {
  // 在 setup 阶段提前捕获 api 引用，避免防抖回调里脱离 pinia 上下文
  resolveApi()
  return {
    hydrate: async <T>(section: string): Promise<T | undefined> => {
      // 该分区未开启同步：不拉取后端，使用本地
      if (!isSectionSyncEnabled(section)) {
        return undefined
      }
      const payload = await loadPayload(pageCode)
      return payload[section] as T | undefined
    },
    save: (section: string, value: unknown) => saveSection(pageCode, section, value),
    subscribeRemote: (section: string, listener: (value: unknown) => void) =>
      subscribeRemotePageSetting(pageCode, section, listener),
  }
}
