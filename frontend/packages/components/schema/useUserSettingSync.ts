/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { SyncedSettingBinding, SyncedSettingTarget } from '~/composables/useSyncedSetting'
import { settingSyncIsland, settingSyncRemoteApplied } from '~/composables/useSettingSyncIsland'
import { bindSyncedSetting } from '~/composables/useSyncedSetting'
import { USER_SETTING_CLIENT_ID, UserSettingScene } from '~/constants'
import { i18n } from '~/locales'
import { isPresetSyncEnabled, isSearchSyncEnabled, isTableSyncEnabled, isWidgetsSyncEnabled, onSettingSessionReset, useAppContext } from '~/stores'

/** 分区 → i18n 名称键（用于灵动岛同步提示） */
const SECTION_NAME_KEYS: Record<string, string> = {
  table: 'island.sync.name.table',
  search: 'island.sync.name.search',
  views: 'island.sync.name.views',
  board: 'island.sync.name.widgets',
}

function sectionName(section: string): string {
  return i18n.global.t(SECTION_NAME_KEYS[section] ?? 'island.sync.name.page')
}

/**
 * 分区是否开启后端同步：搜索、表格、条件预设、看板各看对应开关，其余缺省开启
 * 关闭时只走本地（localStorage 由各调用方维护），不上行也不拉取
 */
function isSectionSyncEnabled(section: string): boolean {
  if (section === 'search') {
    return isSearchSyncEnabled()
  }
  if (section === 'table') {
    return isTableSyncEnabled()
  }
  if (section === 'views') {
    return isPresetSyncEnabled()
  }
  if (section === 'board') {
    return isWidgetsSyncEnabled()
  }
  return true
}

/**
 * 页面设置后端同步（跨端）
 *
 * 后端用户设置以 (用户 × 场景 × 设置键) 存一条 JSON 载荷；页面设置场景固定 scene=Page、settingKey=pageCode，
 * 前端在该载荷中按「分区」(section) 读写：
 * - 列设置 → section `table`
 * - 条件预设 → section `views`
 *
 * 设计为「尽力而为」：localStorage 仍是同步事实源；后端加载成功则覆盖本地，保存失败只在灵动岛提示，不阻塞交互
 * 一次保存传的是整页载荷：没拿到后端那份（拉取失败或还在拉）之前不上行，否则只带一个分区的载荷会把其它分区抹掉
 */
interface UserSettingApiShape {
  get: (input: { scene: number, settingKey: string }) => Promise<{ scene: number, settingKey: string, settingValue?: null | string }>
  save: (input: { scene: number, settingKey: string, settingValue?: null | string, clientId?: string }) => Promise<{ scene: number, settingKey: string, settingValue?: null | string }>
}

let sharedApi: UserSettingApiShape | null = null

/** pageCode → 后端那份叠上本机还没传上去的改动（拉取成功或收到推送之后才有，分区读写共享同一条后端记录） */
const cache = new Map<string, Record<string, unknown>>()
/** pageCode → 本机改过、还没确认传上去的分区：拿到后端那份时盖在上面，传成功才去掉 */
const unsent = new Map<string, Record<string, unknown>>()
const loading = new Map<string, Promise<Record<string, unknown> | null>>()
const saveTimers = new Map<string, ReturnType<typeof setTimeout>>()
/** 会话代次：退出登录或切换身份之后，还在路上的拉取与保存回来不再动缓存 */
let generation = 0

function resolveApi(): UserSettingApiShape {
  sharedApi ??= useAppContext().apis.userSettingApi as UserSettingApiShape
  return sharedApi
}

function withUnsent(pageCode: string, payload: Record<string, unknown>): Record<string, unknown> {
  const local = unsent.get(pageCode)
  return local
    ? {
        ...payload,
        ...local,
      }
    : payload
}

/** 拉后端那份；失败 resolve null 且不留结果，下次读或保存时重拉 */
async function loadPayload(pageCode: string): Promise<Record<string, unknown> | null> {
  let pending = loading.get(pageCode)
  if (!pending) {
    // 后台读取：灵动岛只在拉取期间显示「同步中」活动态，完成即静默消失（不弹成功提示，避免每次进页面刷屏）
    const task = settingSyncIsland('page-load', i18n.global.t('island.sync.name.page'))
    const session = generation
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
        if (session !== generation) {
          return null
        }
        const payload = withUnsent(pageCode, parsed)
        cache.set(pageCode, payload)
        return payload
      })
      .catch(() => {
        if (session === generation) {
          loading.delete(pageCode)
        }
        return null
      })
      .finally(() => task.dismiss())
    loading.set(pageCode, pending)
  }
  return pending
}

/** 拉取完成后读缓存里这个分区此刻的值（含本会话保存与其它设备推来的） */
async function loadSection(pageCode: string, section: string): Promise<unknown> {
  await loadPayload(pageCode)
  return cache.get(pageCode)?.[section]
}

function saveSection(pageCode: string, section: string, value: unknown): void {
  // 该分区未开启同步：仅本地（由调用方写 localStorage），不更新合并缓存、不上行后端
  if (!isSectionSyncEnabled(section)) {
    return
  }
  const local = unsent.get(pageCode) ?? {}
  local[section] = value
  unsent.set(pageCode, local)
  const known = cache.get(pageCode)
  if (known) {
    known[section] = value
  }

  const existing = saveTimers.get(pageCode)
  if (existing) {
    clearTimeout(existing)
  }
  saveTimers.set(
    pageCode,
    setTimeout(() => {
      saveTimers.delete(pageCode)
      void flushPage(pageCode, section)
    }, 600),
  )
}

/** 把这页整条载荷传上去：还没拿到后端那份先拉，拉不到就不传，改动留在本机等下一次保存 */
async function flushPage(pageCode: string, section: string): Promise<void> {
  const task = settingSyncIsland(`page:${pageCode}`, sectionName(section))
  const session = generation
  const payload = cache.get(pageCode) ?? await loadPayload(pageCode)
  if (session !== generation) {
    task.dismiss()
    return
  }
  if (!payload) {
    task.error()
    return
  }
  const sent = {
    ...unsent.get(pageCode),
  }
  try {
    await resolveApi().save({
      scene: UserSettingScene.Page,
      settingKey: pageCode,
      settingValue: JSON.stringify(payload),
      clientId: USER_SETTING_CLIENT_ID,
    })
  }
  catch {
    task.error()
    return
  }
  // 传的途中又改了的分区留着，等下一次
  const local = unsent.get(pageCode)
  if (local && session === generation) {
    for (const [key, value] of Object.entries(sent)) {
      if (local[key] === value) {
        delete local[key]
      }
    }
    if (Object.keys(local).length === 0) {
      unsent.delete(pageCode)
    }
  }
  task.success()
}

// ── 远端实时推送（SignalR UserSettingChanged，scene=Page）────────
/** 页面设置远端变更监听者（pageCode → section → 监听者集合） */
const remoteListeners = new Map<string, Map<string, Set<(value: unknown) => void>>>()

/** 订阅某页某分区的远端变更（其它设备保存后），调用方须在作用域销毁时退订 */
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
 * 应用其它设备推来的页面设置：覆盖该页缓存，按分区分发给监听者
 * 载荷是整页全量而一次保存只改一个分区，只分发变化的分区，免得重复应用、一次推送弹多条提示
 */
export function applyRemotePageSetting(pageCode: string, settingValue?: null | string): void {
  let payload: Record<string, unknown>
  try {
    payload = settingValue
      ? JSON.parse(settingValue) as Record<string, unknown>
      : {}
  }
  catch {
    return
  }
  if (!payload || typeof payload !== 'object') {
    return
  }
  const previous = cache.get(pageCode)
  const local = unsent.get(pageCode)
  cache.set(pageCode, withUnsent(pageCode, payload))
  const sections = remoteListeners.get(pageCode)
  if (!sections) {
    return
  }
  sections.forEach((listeners, section) => {
    // 本机还没传上去的改动比推来的新
    if (listeners.size === 0 || !isSectionSyncEnabled(section) || (local && section in local)) {
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

/** 清掉本会话已拉取的页面设置（退出登录、切换身份后不能沿用上一个人的那份，上一个人还没发出的回写也不能带着新令牌写进新身份） */
export function resetPageSettingCache(): void {
  generation++
  cache.clear()
  unsent.clear()
  loading.clear()
  saveTimers.forEach(timer => clearTimeout(timer))
  saveTimers.clear()
}
onSettingSessionReset(resetPageSettingCache)

export interface UserSettingSync {
  /** 读取指定分区的远端值（首次触发后端加载，后续走缓存） */
  hydrate: <T>(section: string) => Promise<T | undefined>
  /** 写入指定分区（合并入整条载荷，防抖落库；没拿到后端那份或保存失败时灵动岛提示失败） */
  save: (section: string, value: unknown) => void
  /** 订阅该分区的远端实时变更（返回退订函数） */
  subscribeRemote: (section: string, listener: (value: unknown) => void) => () => void
  /**
   * 按 bindSyncedSetting 的口径接一个分区：开着同步以后端为准、超时先按本地、拉取与推送都写回本地
   * 调用方先从本地恢复，再把 ready 接到渲染上
   */
  bindSection: <T>(section: string, target: SyncedSettingTarget<T>) => SyncedSettingBinding
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
      return await loadSection(pageCode, section) as T | undefined
    },
    save: (section: string, value: unknown) => saveSection(pageCode, section, value),
    subscribeRemote: (section: string, listener: (value: unknown) => void) =>
      subscribeRemotePageSetting(pageCode, section, listener),
    bindSection: <T>(section: string, target: SyncedSettingTarget<T>): SyncedSettingBinding => bindSyncedSetting({
      enabled: () => isSectionSyncEnabled(section),
      // 本会话已拉过这页（或收到过推送、本机保存过）：直接采用缓存，页面来回切换不再等
      peek: () => {
        const cached = cache.get(pageCode)
        return cached
          ? { value: cached[section] }
          : undefined
      },
      load: () => loadSection(pageCode, section),
      subscribe: listener => subscribeRemotePageSetting(pageCode, section, listener),
    }, target),
  }
}
