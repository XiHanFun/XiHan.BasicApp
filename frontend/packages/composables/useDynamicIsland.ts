import { computed, ref, watch } from 'vue'

/**
 * 灵动岛（Dynamic Island）。
 *
 * 不止是提醒：支持「进行中/成功/失败/信息」状态、确定性进度（进度环）、岛内交互按钮、
 * 整条点击跳转、常驻状态（网络/实时连接等）、服务端后台任务（SignalR TaskProgress 推送）
 * 以及点击展开的活动面板（活动 + 最近历史）。
 *
 * 模块级单例状态，可在 store / composable / 组件任意处调用，由全局挂载的
 * DynamicIsland 组件订阅渲染。`islandStart` 兼容旧用法 `islandStart(id, label)`。
 *
 * 常驻性：状态存活于模块作用域（跨路由不丢）；服务端任务与历史另存 sessionStorage，
 * 刷新页面后自动恢复（进行中的服务端任务等待 SignalR 续推）。
 */

export type IslandState = 'loading' | 'success' | 'error' | 'info'
export type IslandTone = 'default' | 'primary' | 'danger'

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

/** 创建 / 更新任务时可选字段 */
export interface IslandTaskInit {
  /** 副文本（展开面板内展示） */
  detail?: string
  /** 自定义前导图标（覆盖按状态推断的图标） */
  icon?: string
  /** 状态 */
  state?: IslandState
  /** 进度 0-100；省略表示不确定（旋转弧环） */
  progress?: number
  /** 岛内交互按钮 */
  actions?: IslandAction[]
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
  /** event=普通任务（终态入历史）；status=常驻状态 */
  kind: 'event' | 'status'
  /** local=前端本地任务；server=服务端推送任务（参与刷新恢复） */
  source: 'local' | 'server'
  /** 任务开始时间戳（展开面板显示耗时） */
  startedAt: number
}

export interface IslandHistoryItem {
  id: string
  label: string
  detail?: string
  state: IslandState
  order: number
  /** 终态时间戳（展开面板显示相对时间） */
  time: number
}

export interface IslandHandle {
  /** 仅更新文案（兼容旧用法） */
  update: (label: string) => void
  /** 合并更新任意字段 */
  patch: (p: Partial<IslandTaskInit> & { label?: string }) => void
  /** 设置进度 0-100 */
  setProgress: (n: number) => void
  /** 标记成功（短暂停留后自动消失，并入历史） */
  success: (label?: string, opts?: IslandTaskInit) => void
  /** 标记失败（稍长停留后自动消失，并入历史） */
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
const HISTORY_CAP = 20
const STORAGE_KEY = 'xihan_island_state'

const tasks = ref<IslandTask[]>([])
const history = ref<IslandHistoryItem[]>([])
const expanded = ref(false)
const timers = new Map<string, ReturnType<typeof setTimeout>>()
let orderSeq = 0

/** 折叠态展示的任务：取最近活动（order 最大）的那条 */
const current = computed<IslandTask | null>(() => {
  if (tasks.value.length === 0) {
    return null
  }
  return tasks.value.reduce((latest, item) => (item.order > latest.order ? item : latest))
})

/** 进行中任务数（折叠态可提示并发数量） */
const loadingCount = computed(() => tasks.value.filter(item => item.state === 'loading').length)

/** 按 order 倒序的活动任务（展开面板用） */
const activeTasks = computed(() => [...tasks.value].sort((a, b) => b.order - a.order))

/** 是否有可展开内容 */
const hasPanel = computed(() => tasks.value.length > 0 || history.value.length > 0)

function clearTimer(id: string): void {
  const timer = timers.get(id)
  if (timer) {
    clearTimeout(timer)
    timers.delete(id)
  }
}

function pushHistory(task: IslandTask): void {
  if (task.state !== 'success' && task.state !== 'error') {
    return
  }
  history.value = [
    { id: task.id, label: task.label, detail: task.detail, state: task.state, order: ++orderSeq, time: Date.now() },
    ...history.value,
  ].slice(0, HISTORY_CAP)
}

function removeTask(id: string, recordHistory = true): void {
  clearTimer(id)
  const task = tasks.value.find(item => item.id === id)
  if (task && recordHistory) {
    pushHistory(task)
  }
  tasks.value = tasks.value.filter(item => item.id !== id)
}

function scheduleRemoval(id: string, delay: number): void {
  clearTimer(id)
  timers.set(id, setTimeout(() => removeTask(id), delay))
}

function upsert(id: string, label: string, init: IslandTaskInit, kind: 'event' | 'status', source: 'local' | 'server' = 'local'): void {
  const index = tasks.value.findIndex(item => item.id === id)
  const base: Partial<IslandTask> = index >= 0 ? tasks.value[index]! : {}
  const next: IslandTask = {
    id,
    kind: (base.kind as IslandTask['kind']) ?? kind,
    source: (base.source as IslandTask['source']) ?? source,
    label,
    detail: init.detail ?? base.detail,
    icon: init.icon ?? base.icon,
    state: init.state ?? base.state ?? 'loading',
    progress: init.progress ?? base.progress,
    actions: init.actions ?? base.actions,
    onClick: init.onClick ?? base.onClick,
    link: init.link ?? base.link,
    persistent: init.persistent ?? base.persistent,
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
}

/** 终态收尾：非常驻则按状态停留后移除 */
function settle(id: string, lingerByState: IslandState): void {
  const task = tasks.value.find(item => item.id === id)
  if (!task || task.persistent) {
    return
  }
  const delay = lingerByState === 'error' ? ERROR_LINGER : lingerByState === 'info' ? INFO_LINGER : SUCCESS_LINGER
  scheduleRemoval(id, delay)
}

function makeHandle(id: string): IslandHandle {
  const apply = (label: string | undefined, state: IslandState, opts?: IslandTaskInit) => {
    const existing = tasks.value.find(item => item.id === id)
    // 终态默认清除常驻标记（如「网络已恢复」短暂展示后消失）
    const persistent = opts?.persistent ?? (state === 'info' ? existing?.persistent : false)
    upsert(id, label ?? existing?.label ?? '', { ...opts, state, persistent }, existing?.kind ?? 'event')
    if (!persistent) {
      settle(id, state)
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
      upsert(id, existing?.label ?? '', { progress: Math.max(0, Math.min(100, n)), state: 'loading' }, existing?.kind ?? 'event')
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
      removeTask(id, false)
    },
  }
}

// ── 会话级持久化：服务端任务（进行中）与历史在刷新后恢复 ────────────
interface PersistedState {
  tasks: Array<Pick<IslandTask, 'detail' | 'icon' | 'id' | 'label' | 'link' | 'progress' | 'startedAt'>>
  history: IslandHistoryItem[]
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
      history: history.value,
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
    if (Array.isArray(saved.history)) {
      history.value = saved.history.slice(0, HISTORY_CAP)
      // 序号续接：orderSeq 每次会话从 0 起，若不推进到恢复历史的最大序号，
      // 本会话同名任务会产出与恢复项相同的 (id, order)，导致列表 key 冲突
      orderSeq = Math.max(orderSeq, ...history.value.map(item => item.order ?? 0))
    }
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
watch([tasks, history], persistState, { deep: true })

// ── 关闭灵动岛时的兜底接管 ──────────────────────────────────────
// 由全局挂载的 DynamicIsland 组件在 setup 时注入：是否启用 + 关闭时由 Naive Message 接管终态。
type IslandMessageSink = (state: 'success' | 'error' | 'info', content: string) => void
let enabledResolver: (() => boolean) | null = null
let messageSink: IslandMessageSink | null = null

/**
 * 注入灵动岛启用判定与「关闭时」的消息兜底。
 * 启用时：进度/终态均由灵动岛呈现；关闭时：进行中态静默，终态（成功/失败/信息）改由 Naive Message 接管。
 */
export function configureDynamicIsland(options: { isEnabled: () => boolean, message: IslandMessageSink }): void {
  enabledResolver = options.isEnabled
  messageSink = options.message
}

function islandEnabled(): boolean {
  return enabledResolver ? enabledResolver() : true
}

/** 关闭灵动岛时的事件兜底句柄：进行中态静默，终态交给 Naive Message，其余为空操作 */
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
  return { update: noop, patch: noop, setProgress: noop, success: noop, error: noop, info: noop, dismiss: noop }
}

/**
 * 开始一个灵动岛任务（默认进行中态）。返回句柄推进终态。
 * @param id    任务键；同 id 复用同一条
 * @param label 文案
 * @param init  可选：detail/icon/state/progress/actions/onClick/link/persistent
 */
export function islandStart(id: string, label: string, init: IslandTaskInit = {}): IslandHandle {
  // 灵动岛关闭：不进岛，终态由 Naive Message 接管
  if (!islandEnabled()) {
    return fallbackEventHandle(label)
  }
  clearTimer(id)
  upsert(id, label, { ...init, state: init.state ?? 'loading' }, 'event')
  return makeHandle(id)
}

/**
 * 常驻状态指示（不自动消失，用于网络/实时连接/后台运行等）。
 * 通过返回句柄的 success/info/dismiss 收尾。
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
 * 服务端后台任务进度入口（SignalR TaskProgress 事件 → 灵动岛）。
 * 同 taskId 复用同一条；进行中任务参与会话级持久化（刷新后恢复，等待服务端续推）。
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
      messageSink?.(state === 'info' ? 'info' : state, payload.label)
    }
    return
  }

  clearTimer(id)
  upsert(id, payload.label, {
    detail: payload.detail ?? undefined,
    progress: payload.progress ?? undefined,
    link: payload.link ?? undefined,
    state,
    icon: state === 'loading' ? 'lucide:server' : undefined,
  }, 'event', 'server')

  if (state !== 'loading') {
    settle(id, state)
  }
}

/** 组件订阅入口与操作 */
export function useDynamicIsland() {
  return {
    current,
    activeTasks,
    history,
    expanded,
    loadingCount,
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
    dismissTask: (id: string) => removeTask(id, false),
    clearHistory: () => {
      history.value = []
    },
  }
}
