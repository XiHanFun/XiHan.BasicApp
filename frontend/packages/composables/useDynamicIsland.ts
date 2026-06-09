import { computed, ref } from 'vue'

/**
 * 灵动岛（Dynamic Island）进度提示。
 *
 * 用于「轻量耗时、且会同步页面样式」的后台行为的过程反馈：登录、同步偏好设置、
 * 同步表格 / 搜索设置等。模块级单例状态，可在 store / composable / 组件任意处调用，
 * 由全局挂载的 DynamicIsland 组件订阅渲染。
 */

export type IslandState = 'loading' | 'success' | 'error'

export interface IslandTask {
  /** 任务唯一键（同 id 复用同一条，避免同类行为刷屏） */
  id: string
  /** 展示文案 */
  label: string
  /** 状态：进行中 / 成功 / 失败 */
  state: IslandState
  /** 内部排序序号（越大越新，决定当前展示哪条） */
  order: number
}

/** 任务句柄：开始后用于推进到成功 / 失败 / 更新文案 */
export interface IslandHandle {
  /** 更新进行中的文案 */
  update: (label: string) => void
  /** 标记成功（短暂停留后自动消失） */
  success: (label?: string) => void
  /** 标记失败（稍长停留后自动消失） */
  error: (label?: string) => void
  /** 立即移除（无终态停留） */
  dismiss: () => void
}

/** 成功 / 失败终态停留时长（ms） */
const SUCCESS_LINGER = 1600
const ERROR_LINGER = 3200

const tasks = ref<IslandTask[]>([])
const timers = new Map<string, ReturnType<typeof setTimeout>>()
let orderSeq = 0

/** 当前展示的任务：取最近一次活动（order 最大）的那条 */
const current = computed<IslandTask | null>(() => {
  if (tasks.value.length === 0) {
    return null
  }
  return tasks.value.reduce((latest, item) => (item.order > latest.order ? item : latest))
})

/** 进行中任务数（>1 时组件可提示并发数量） */
const loadingCount = computed(() => tasks.value.filter(item => item.state === 'loading').length)

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
  timers.set(id, setTimeout(() => removeTask(id), delay))
}

function upsert(id: string, patch: Partial<Omit<IslandTask, 'id' | 'order'>>): void {
  const index = tasks.value.findIndex(item => item.id === id)
  if (index >= 0) {
    const next = [...tasks.value]
    next[index] = { ...next[index]!, ...patch, order: ++orderSeq }
    tasks.value = next
  }
  else {
    tasks.value = [
      ...tasks.value,
      { id, label: patch.label ?? '', state: patch.state ?? 'loading', order: ++orderSeq },
    ]
  }
}

/**
 * 开始一个灵动岛任务（进行中态）。返回句柄用于推进终态。
 * @param id    任务键；同 id 重复调用会复用同一条（适合防抖型同步）
 * @param label 进行中文案
 */
export function islandStart(id: string, label: string): IslandHandle {
  clearTimer(id)
  upsert(id, { label, state: 'loading' })
  return {
    update(nextLabel: string) {
      upsert(id, { label: nextLabel, state: 'loading' })
    },
    success(nextLabel?: string) {
      upsert(id, nextLabel ? { label: nextLabel, state: 'success' } : { state: 'success' })
      scheduleRemoval(id, SUCCESS_LINGER)
    },
    error(nextLabel?: string) {
      upsert(id, nextLabel ? { label: nextLabel, state: 'error' } : { state: 'error' })
      scheduleRemoval(id, ERROR_LINGER)
    },
    dismiss() {
      removeTask(id)
    },
  }
}

/** 组件订阅入口：当前展示任务 + 进行中数量 */
export function useDynamicIsland() {
  return { current, loadingCount }
}
