/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { Tone } from '@xihan-ui/core'
import type { Component } from 'vue'
import { computed, markRaw, ref, watch } from 'vue'

/**
 * 灵动岛（Dynamic Island）
 *
 * 一条活动（任务）有三种呈现，分区参照 iOS 实时活动：
 * - 胶囊：左侧 leading、中间 label、右侧 trailing
 * - 卡片：有新内容时自动短暂展开，自上而下是 leading / label + detail / trailing、内容块 blocks、操作 actions
 * - 面板：点开胶囊后列出全部活动，每条与卡片同一套分区
 *
 * 各区放什么由调用方决定：两侧放小件（IslandAccessory），正文下方放内容块（IslandBlock），
 * 都有 custom 一档交给调用方自己的组件；不写的区按状态取缺省（进度环、状态图标、百分比、已用时）
 *
 * 模块级单例状态，可在 store / composable / 组件任意处调用，由全局挂载的 DynamicIsland 组件订阅渲染
 * 状态存活于模块作用域（跨路由不丢）；服务端进行中的任务另存 sessionStorage，刷新后恢复并等 SignalR 续推
 */

export type IslandState = 'loading' | 'success' | 'error' | 'info'
/** 岛内按钮的语气：取组件库 tone 轴里的三档，缺省 neutral */
export type IslandTone = Extract<Tone, 'neutral' | 'brand' | 'danger'>

/** 交给调用方自己的组件：灵动岛只负责放进槽位，组件内的交互与样式归调用方 */
export interface IslandCustom {
  type: 'custom'
  component: Component
  props?: Record<string, unknown>
}

/**
 * 两侧小件：胶囊与展开行的 leading / trailing
 * 空间只够一个图标、一枚头像、一个小环或一小段字
 */
export type IslandAccessory
  = | { type: 'icon', icon: string, tone?: Tone }
    | { type: 'avatar', name: string, avatar?: null | string }
    /** 进度环：value 省略即不确定态（旋转弧环），环心可放图标 */
    | { type: 'ring', value?: number, max?: number, icon?: string, tone?: Tone }
    | { type: 'text', text: string, tone?: Tone }
    /** 自活动开始起的计时（m:ss） */
    | { type: 'elapsed' }
    | IslandCustom

/** 列表行：行首小件、名称、右侧取值，给了 progress 就在行下画一条进度 */
export interface IslandListItem {
  key: string
  label: string
  leading?: IslandAccessory
  value?: string
  /** 0-100 */
  progress?: number
  tone?: Tone
}

/** 网格格子：自上而下是标题、图标、取值，各格等宽 */
export interface IslandGridItem {
  key: string
  label: string
  icon?: string
  value: string
}

/** 正文下方的内容块，按数组顺序自上而下 */
export type IslandBlock
  = | { type: 'text', text: string }
    /** 大号数值：value + unit，下方 caption，右侧可挂一枚小件（如进度环） */
    | { type: 'metric', value: number | string, unit?: string, caption?: string, accessory?: IslandAccessory }
    /** 线形进度：上方 label 与 valueText，缺省取值为百分比 */
    | { type: 'progress', value: number, max?: number, label?: string, valueText?: string, tone?: Tone }
    | { type: 'list', items: IslandListItem[] }
    | { type: 'grid', items: IslandGridItem[] }
    | { type: 'media', src: string, alt: string }
    | IslandCustom

/** 岛内交互按钮 */
export interface IslandAction {
  key: string
  label: string
  icon?: string
  tone?: IslandTone
  /** 点击执行 */
  handler: () => void
  /** 点击后是否保留任务（默认点击即关闭该任务） */
  keepOpen?: boolean
}

/** 创建 / 更新任务时可选字段；省略的字段沿用这条任务原有的值 */
export interface IslandTaskInit {
  /** 副文本：卡片里最多两行，面板里完整显示 */
  detail?: string
  /** 缺省指示器上的图标：进行中画在环心，终态替换状态图标；要整个换掉指示器用 leading */
  icon?: string
  /** 状态 */
  state?: IslandState
  /** 进度 0-100；省略表示不确定（旋转弧环） */
  progress?: number
  /** 左侧小件，缺省是状态指示器（进行中是进度环，终态是状态图标） */
  leading?: IslandAccessory
  /** 右侧小件，缺省在进行中且有进度时显示百分比 */
  trailing?: IslandAccessory
  /** 正文下方的内容块 */
  blocks?: IslandBlock[]
  /** 岛内交互按钮 */
  actions?: IslandAction[]
  /** 按钮的排法：inline 一行带字的按钮（缺省），bar 底部等宽的图标栏 */
  actionLayout?: 'inline' | 'bar'
  /** 有新内容时自动短暂展开成卡片；缺省带 detail 或 blocks 时展开 */
  alert?: boolean
  /** 进行中时在面板里显示已用时，缺省显示 */
  elapsed?: boolean
  /** 面板里可手动移除，缺省非常驻的都可移除 */
  dismissible?: boolean
  /** 终态停留的毫秒数；缺省成功 1.6 秒、失败 3.2 秒、信息 2.4 秒 */
  duration?: number
  /** 整条点击回调（如跳转/查看） */
  onClick?: () => void
  /** 整条点击跳转的应用内链接（与 onClick 二选一，组件经路由跳转） */
  link?: string
  /** 常驻：不自动消失（用于网络/连接等状态指示） */
  persistent?: boolean
}

export interface IslandTask extends IslandTaskInit {
  id: string
  label: string
  state: IslandState
  order: number
  /** event=普通任务；status=常驻状态 */
  kind: 'event' | 'status'
  /** local=前端本地任务；server=服务端推送任务（参与刷新恢复） */
  source: 'local' | 'server'
  /** 任务开始时间戳（已用时从这里算） */
  startedAt: number
}

export interface IslandHandle {
  /** 仅更新文案 */
  update: (label: string) => void
  /** 合并更新任意字段 */
  patch: (p: Partial<IslandTaskInit> & { label?: string }) => void
  /** 设置进度 0-100 */
  setProgress: (n: number) => void
  /** 标记成功（短暂停留后自动消失） */
  success: (label?: string, opts?: IslandTaskInit) => void
  /** 标记失败（稍长停留后自动消失） */
  error: (label?: string, opts?: IslandTaskInit) => void
  /** 标记信息态（默认短暂停留；opts.persistent 则常驻） */
  info: (label?: string, opts?: IslandTaskInit) => void
  /** 立即移除 */
  dismiss: () => void
}

/** 服务端任务进度载荷（与后端 TaskProgress 事件约定一致） */
export interface ServerTaskProgressPayload {
  taskId: string
  label: string
  detail?: null | string
  state?: null | string
  progress?: null | number
  link?: null | string
}

const SUCCESS_LINGER = 1600
const ERROR_LINGER = 3200
const INFO_LINGER = 2400
/** 同时存活的消息条数上限：超出丢最旧的，避免刷屏时堆一长串 */
const MESSAGE_CAP = 5
const STORAGE_KEY = 'xihan_island_state'

const tasks = ref<IslandTask[]>([])
const expanded = ref(false)
const timers = new Map<string, ReturnType<typeof setTimeout>>()
let orderSeq = 0

/**
 * 终态消息（非常驻的 success/error/info），由新到旧
 *
 * 多条并存靠「同时可见的堆叠」而不是排队：每条各自计时同时倒数，最新的一条在最前完整展示，
 * 其余在后方露边，点开面板可看全部——与 Sonner / Ant Design / iOS 通知一致
 * 排队逐条展示是 Snackbar 那条路线（一次一条、不堆叠），两者不能混用：
 * 混起来会既看不到多条、新消息又要等
 */
const messageStack = computed(() => tasks.value
  .filter(item => item.state !== 'loading' && !item.persistent)
  .sort((a, b) => b.order - a.order))

/** 进行中与常驻状态：长驻，不参与消息堆叠；无消息时才回到岛上 */
const statusTasks = computed(() => tasks.value.filter(item => item.state === 'loading' || item.persistent))

/** 压在最前一条之后的消息（由新到旧；折叠态据此渲染叠层） */
const pendingTasks = computed(() => messageStack.value.slice(1))

/** 压在最前一条之后的条数 */
const pendingCount = computed(() => pendingTasks.value.length)

/** 折叠态展示的任务：最新消息优先，无消息时回落到最近的进行中/常驻状态 */
const current = computed<IslandTask | null>(() => {
  const top = messageStack.value[0]
  if (top) {
    return top
  }
  if (statusTasks.value.length === 0) {
    return null
  }
  return statusTasks.value.reduce((latest, item) => (item.order > latest.order
    ? item
    : latest))
})

/** 进行中任务数（折叠态可提示并发数量） */
const loadingCount = computed(() => tasks.value.filter(item => item.state === 'loading').length)

/** 按 order 倒序的活动任务（展开面板用） */
const activeTasks = computed(() => [...tasks.value].sort((a, b) => b.order - a.order))

/** 是否有可展开内容 */
const hasPanel = computed(() => tasks.value.length > 0)

function clearTimer(id: string): void {
  const timer = timers.get(id)
  if (timer) {
    clearTimeout(timer)
    timers.delete(id)
  }
}

function removeTask(id: string): void {
  clearTimer(id)
  tasks.value = tasks.value.filter(item => item.id !== id)
}

function scheduleRemoval(id: string, delay: number): void {
  clearTimer(id)
  timers.set(id, setTimeout(removeTask, delay, id))
}

/** 调用方的组件不进响应式：放进任务列表时标成 raw，免得被深层代理 */
function markComponents(init: IslandTaskInit): void {
  const mark = (item?: IslandAccessory | IslandBlock) => {
    if (item?.type === 'custom') {
      markRaw(item.component)
    }
  }
  mark(init.leading)
  mark(init.trailing)
  for (const block of init.blocks ?? []) {
    mark(block)
    if (block.type === 'metric') {
      mark(block.accessory)
    }
    else if (block.type === 'list') {
      block.items.forEach(item => mark(item.leading))
    }
  }
}

function upsert(id: string, label: string, init: IslandTaskInit, kind: 'event' | 'status', source: 'local' | 'server' = 'local'): void {
  const index = tasks.value.findIndex(item => item.id === id)
  const base: Partial<IslandTask> = index >= 0
    ? tasks.value[index]!
    : {}
  markComponents(init)
  // 省略或给 undefined 的字段沿用原值
  const patch = Object.fromEntries(Object.entries(init).filter(([, value]) => value !== undefined)) as IslandTaskInit
  const next: IslandTask = {
    ...base,
    ...patch,
    id,
    kind: base.kind ?? kind,
    source: base.source ?? source,
    label,
    state: patch.state ?? base.state ?? 'loading',
    startedAt: base.startedAt ?? Date.now(),
    order: ++orderSeq,
  }
  if (index >= 0) {
    const copy = [...tasks.value]
    copy[index] = next
    tasks.value = copy
  }
  else {
    tasks.value = [...tasks.value, next]
  }
  trimMessages()
}

/** 限长：一波消息涌进来时只留最近的若干条，最旧的直接丢掉，避免堆一长串 */
function trimMessages(): void {
  for (const task of messageStack.value.slice(MESSAGE_CAP)) {
    removeTask(task.id)
  }
}

function lingerOf(task: IslandTask): number {
  if (task.duration != null) {
    return task.duration
  }
  return task.state === 'error'
    ? ERROR_LINGER
    : task.state === 'info'
      ? INFO_LINGER
      : SUCCESS_LINGER
}

/** 终态收尾：非常驻的消息各自开始倒计时（面板展开时不计时，收起后统一重排） */
function settle(id: string): void {
  const task = tasks.value.find(item => item.id === id)
  if (!task || task.persistent || expanded.value) {
    return
  }
  scheduleRemoval(id, lingerOf(task))
}

/**
 * 展开面板时暂停全部消息计时，收起后重新计时
 * 否则正看着列表，消息在眼皮底下一条条消失
 */
watch(expanded, (open) => {
  for (const task of messageStack.value) {
    if (open) {
      clearTimer(task.id)
    }
    else if (!timers.has(task.id)) {
      scheduleRemoval(task.id, lingerOf(task))
    }
  }
})

function makeHandle(id: string): IslandHandle {
  const apply = (label: string | undefined, state: IslandState, opts?: IslandTaskInit) => {
    const existing = tasks.value.find(item => item.id === id)
    // 终态默认清除常驻标记（如「网络已恢复」短暂展示后消失）
    const persistent = opts?.persistent ?? (state === 'info'
      ? existing?.persistent
      : false)
    upsert(id, label ?? existing?.label ?? '', { ...opts, state, persistent }, existing?.kind ?? 'event')
    if (!persistent) {
      settle(id)
    }
  }
  return {
    update(label) {
      const existing = tasks.value.find(item => item.id === id)
      upsert(id, label, { state: 'loading' }, existing?.kind ?? 'event')
    },
    patch(p) {
      const existing = tasks.value.find(item => item.id === id)
      upsert(id, p.label ?? existing?.label ?? '', p, existing?.kind ?? 'event')
    },
    setProgress(n) {
      const existing = tasks.value.find(item => item.id === id)
      upsert(id, existing?.label ?? '', {
        progress: Math.max(0, Math.min(100, n)),
        state: 'loading',
      }, existing?.kind ?? 'event')
    },
    success(label, opts) {
      apply(label, 'success', opts)
    },
    error(label, opts) {
      apply(label, 'error', opts)
    },
    info(label, opts) {
      apply(label, 'info', opts)
    },
    dismiss() {
      removeTask(id)
    },
  }
}

// ── 会话级持久化：服务端进行中的任务在刷新后恢复 ────────────────────
interface PersistedState {
  tasks: Array<Pick<IslandTask, 'detail' | 'icon' | 'id' | 'label' | 'link' | 'progress' | 'startedAt'>>
}

function persistState(): void {
  try {
    const payload: PersistedState = {
      tasks: tasks.value
        .filter(item => item.source === 'server' && item.state === 'loading')
        .map(item => ({
          id: item.id,
          label: item.label,
          detail: item.detail,
          icon: item.icon,
          progress: item.progress,
          link: item.link,
          startedAt: item.startedAt,
        })),
    }
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(payload))
  }
  catch {
    // 存储不可用（隐私模式等）时静默放弃持久化
  }
}

function restoreState(): void {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY)
    if (!raw) {
      return
    }
    const saved = JSON.parse(raw) as Partial<PersistedState>
    for (const item of saved.tasks ?? []) {
      upsert(item.id, item.label, {
        detail: item.detail,
        icon: item.icon,
        progress: item.progress,
        link: item.link,
        state: 'loading',
      }, 'event', 'server')
      const restored = tasks.value.find(t => t.id === item.id)
      if (restored && item.startedAt) {
        restored.startedAt = item.startedAt
      }
    }
  }
  catch {
    // 损坏的持久化载荷直接丢弃
  }
}

restoreState()
watch(tasks, persistState, { deep: true })

// ── 关闭灵动岛时的兜底接管 ──────────────────────────────────────
// 由全局挂载的 DynamicIsland 组件在 setup 时注入：是否启用 + 关闭时由轻提示接管终态
type IslandMessageSink = (state: 'success' | 'error' | 'info', content: string) => void
let enabledResolver: (() => boolean) | null = null
let messageSink: IslandMessageSink | null = null

/**
 * 注入灵动岛启用判定与关闭时的消息兜底
 * 关闭时进行中态静默，终态（成功/失败/信息）改由轻提示接管
 */
export function configureDynamicIsland(options: { isEnabled: () => boolean, message: IslandMessageSink }): void {
  enabledResolver = options.isEnabled
  messageSink = options.message
}

function islandEnabled(): boolean {
  return enabledResolver
    ? enabledResolver()
    : true
}

/** 关闭灵动岛时的事件兜底句柄：进行中态静默，终态交给轻提示，其余为空操作 */
function fallbackEventHandle(initialLabel: string): IslandHandle {
  let label = initialLabel
  const emit = (state: 'success' | 'error' | 'info', next?: string) => {
    const content = (next ?? label)?.trim()
    if (content) {
      messageSink?.(state, content)
    }
  }
  return {
    update(next) {
      label = next
    },
    patch(p) {
      if (p.label != null) {
        label = p.label
      }
    },
    setProgress() {},
    success(next) {
      emit('success', next)
    },
    error(next) {
      emit('error', next)
    },
    info(next) {
      emit('info', next)
    },
    dismiss() {},
  }
}

/** 关闭灵动岛时的常驻状态兜底句柄：整体静默（不把网络等状态指示降级成消息噪音） */
function silentHandle(): IslandHandle {
  const noop = () => {}
  return {
    update: noop,
    patch: noop,
    setProgress: noop,
    success: noop,
    error: noop,
    info: noop,
    dismiss: noop,
  }
}

/**
 * 开始一个灵动岛任务（默认进行中态）。返回句柄推进终态
 * @param id    任务键；同 id 复用同一条
 * @param label 文案
 * @param init  可选字段，见 IslandTaskInit
 */
export function islandStart(id: string, label: string, init: IslandTaskInit = {}): IslandHandle {
  // 灵动岛关闭：不进岛，终态由轻提示接管
  if (!islandEnabled()) {
    return fallbackEventHandle(label)
  }
  clearTimer(id)
  upsert(id, label, { ...init, state: init.state ?? 'loading' }, 'event')
  return makeHandle(id)
}

/**
 * 常驻状态指示（网络、实时连接、后台运行等），不自动消失
 * 经句柄的 success / info / dismiss 收尾
 */
export function islandStatus(id: string, label: string, init: IslandTaskInit = {}): IslandHandle {
  // 灵动岛关闭：常驻状态整体静默（不弹消息）
  if (!islandEnabled()) {
    return silentHandle()
  }
  clearTimer(id)
  upsert(id, label, { ...init, state: init.state ?? 'info', persistent: true }, 'status')
  return makeHandle(id)
}

/**
 * 服务端后台任务进度（SignalR TaskProgress）→ 灵动岛
 * 同 taskId 复用同一条；进行中的任务参与会话级持久化，刷新后恢复并等服务端续推
 */
export function applyServerTaskProgress(payload: ServerTaskProgressPayload): void {
  if (!payload?.taskId || !payload.label) {
    return
  }

  const id = `server:${payload.taskId}`
  const state: IslandState = payload.state === 'success' || payload.state === 'error' || payload.state === 'info'
    ? payload.state
    : 'loading'

  if (!islandEnabled()) {
    if (state !== 'loading') {
      messageSink?.(state, payload.label)
    }
    return
  }

  clearTimer(id)
  upsert(id, payload.label, {
    detail: payload.detail ?? undefined,
    progress: payload.progress ?? undefined,
    link: payload.link ?? undefined,
    state,
    icon: state === 'loading'
      ? 'lucide:server'
      : undefined,
  }, 'event', 'server')

  if (state !== 'loading') {
    settle(id)
  }
}

/** 组件订阅入口与操作 */
export function useDynamicIsland() {
  return {
    current,
    activeTasks,
    expanded,
    loadingCount,
    pendingTasks,
    pendingCount,
    hasPanel,
    expand: () => {
      if (hasPanel.value) {
        expanded.value = true
      }
    },
    collapse: () => {
      expanded.value = false
    },
    toggleExpand: () => {
      expanded.value = hasPanel.value && !expanded.value
    },
    dismissTask: (id: string) => removeTask(id),
  }
}
