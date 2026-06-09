import { computed, ref } from 'vue'

/**
 * 灵动岛（Dynamic Island）。
 *
 * 不止是提醒：支持「进行中/成功/失败/信息」状态、确定性进度条、岛内交互按钮、
 * 整条点击跳转、常驻状态（网络/连接等），以及点击展开的活动面板（活动 + 最近历史）。
 *
 * 模块级单例状态，可在 store / composable / 组件任意处调用，由全局挂载的
 * DynamicIsland 组件订阅渲染。`islandStart` 兼容旧用法 `islandStart(id, label)`。
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
  /** 进度 0-100；省略表示不确定（转圈） */
  progress?: number
  /** 岛内交互按钮 */
  actions?: IslandAction[]
  /** 整条点击回调（如跳转/查看） */
  onClick?: () => void
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
}

export interface IslandHistoryItem {
  id: string
  label: string
  detail?: string
  state: IslandState
  order: number
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

const SUCCESS_LINGER = 1600
const ERROR_LINGER = 3200
const INFO_LINGER = 2400
const HISTORY_CAP = 20

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
    { id: task.id, label: task.label, detail: task.detail, state: task.state, order: ++orderSeq },
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

function upsert(id: string, label: string, init: IslandTaskInit, kind: 'event' | 'status'): void {
  const index = tasks.value.findIndex(item => item.id === id)
  const base: Partial<IslandTask> = index >= 0 ? tasks.value[index]! : {}
  const next: IslandTask = {
    id,
    kind: (base.kind as IslandTask['kind']) ?? kind,
    label,
    detail: init.detail ?? base.detail,
    icon: init.icon ?? base.icon,
    state: init.state ?? base.state ?? 'loading',
    progress: init.progress ?? base.progress,
    actions: init.actions ?? base.actions,
    onClick: init.onClick ?? base.onClick,
    persistent: init.persistent ?? base.persistent,
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

/**
 * 开始一个灵动岛任务（默认进行中态）。返回句柄推进终态。
 * @param id    任务键；同 id 复用同一条
 * @param label 文案
 * @param init  可选：detail/icon/state/progress/actions/onClick/persistent
 */
export function islandStart(id: string, label: string, init: IslandTaskInit = {}): IslandHandle {
  clearTimer(id)
  upsert(id, label, { ...init, state: init.state ?? 'loading' }, 'event')
  return makeHandle(id)
}

/**
 * 常驻状态指示（不自动消失，用于网络/实时连接/后台运行等）。
 * 通过返回句柄的 success/info/dismiss 收尾。
 */
export function islandStatus(id: string, label: string, init: IslandTaskInit = {}): IslandHandle {
  clearTimer(id)
  upsert(id, label, { ...init, state: init.state ?? 'info', persistent: true }, 'status')
  return makeHandle(id)
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
