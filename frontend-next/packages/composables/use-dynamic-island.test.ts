/**
 * useDynamicIsland 灵动岛单元测试。
 * 职责：锁定「消息堆叠而非排队」「按状态各自倒计时」「面板展开暂停计时、收起重排」
 * 「消息条数上限 5 / 历史上限 20」「只有 success/error 入历史」
 * 「服务端进行中任务与历史写 sessionStorage、刷新恢复且序号续接」
 * 「灵动岛关闭时终态由轻提示接管、常驻状态整体静默」这几条约定，以及计时器卸载清理。
 */
import type { IslandTask } from './useDynamicIsland'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { nextTick } from 'vue'

const STORAGE_KEY = 'xihan_island_state'

/** 每个用例一份全新模块状态：tasks / history / timers / orderSeq 都是模块级的 */
async function loadModule() {
  vi.resetModules()
  return import('./useDynamicIsland')
}

afterEach(() => {
  vi.useRealTimers()
})

describe('useDynamicIsland 任务生命周期', () => {
  it('islandStart 建一条进行中任务并成为折叠态展示项', async () => {
    const { islandStart, useDynamicIsland } = await loadModule()

    islandStart('t1', '正在导出')

    const island = useDynamicIsland()
    expect(island.current.value?.label).toBe('正在导出')
    expect(island.current.value?.state).toBe('loading')
    expect(island.loadingCount.value).toBe(1)
  })

  it('同 id 复用同一条任务，不产生两条', async () => {
    const { islandStart, useDynamicIsland } = await loadModule()

    islandStart('t1', '第一次')
    islandStart('t1', '第二次')

    const island = useDynamicIsland()
    expect(island.activeTasks.value).toHaveLength(1)
    expect(island.current.value?.label).toBe('第二次')
  })

  it('update 只换文案并保持进行中态', async () => {
    const { islandStart, useDynamicIsland } = await loadModule()

    const task = islandStart('t1', '开始')
    task.update('进行中 50%')

    expect(useDynamicIsland().current.value?.label).toBe('进行中 50%')
    expect(useDynamicIsland().current.value?.state).toBe('loading')
  })

  it('setProgress 把进度夹在 0-100 之间', async () => {
    const { islandStart, useDynamicIsland } = await loadModule()
    const task = islandStart('t1', '导出')

    task.setProgress(-20)
    expect(useDynamicIsland().current.value?.progress).toBe(0)

    task.setProgress(999)
    expect(useDynamicIsland().current.value?.progress).toBe(100)

    task.setProgress(37.5)
    expect(useDynamicIsland().current.value?.progress).toBe(37.5)
  })

  it('patch 合并字段，未传的字段沿用原值', async () => {
    const { islandStart, useDynamicIsland } = await loadModule()
    const task = islandStart('t1', '导出', { detail: '准备中', icon: 'lucide:download' })

    task.patch({ detail: '写入文件' })

    const current = useDynamicIsland().current.value
    expect(current?.label).toBe('导出')
    expect(current?.detail).toBe('写入文件')
    expect(current?.icon).toBe('lucide:download')
  })

  it('dismiss 立即移除且不入历史', async () => {
    const { islandStart, useDynamicIsland } = await loadModule()
    const task = islandStart('t1', '导出')

    task.dismiss()

    const island = useDynamicIsland()
    expect(island.activeTasks.value).toEqual([])
    expect(island.history.value).toEqual([])
  })

  it('成功终态在停留 1600ms 后消失，并留下一条历史', async () => {
    vi.useFakeTimers()
    const { islandStart, useDynamicIsland } = await loadModule()
    const island = useDynamicIsland()

    islandStart('t1', '导出').success('导出完成')

    expect(island.current.value?.state).toBe('success')

    vi.advanceTimersByTime(1599)
    expect(island.activeTasks.value).toHaveLength(1)

    vi.advanceTimersByTime(1)
    expect(island.activeTasks.value).toEqual([])
    expect(island.history.value.map(item => item.label)).toEqual(['导出完成'])
  })

  it('失败终态停留更久（3200ms），给用户看清错误的时间', async () => {
    vi.useFakeTimers()
    const { islandStart, useDynamicIsland } = await loadModule()
    const island = useDynamicIsland()

    islandStart('t1', '导出').error('导出失败')

    vi.advanceTimersByTime(3199)
    expect(island.activeTasks.value).toHaveLength(1)

    vi.advanceTimersByTime(1)
    expect(island.activeTasks.value).toEqual([])
  })

  it('信息态停留 2400ms', async () => {
    vi.useFakeTimers()
    const { islandStart, useDynamicIsland } = await loadModule()
    const island = useDynamicIsland()

    islandStart('t1', '提示').info('已知悉')

    vi.advanceTimersByTime(2399)
    expect(island.activeTasks.value).toHaveLength(1)

    vi.advanceTimersByTime(1)
    expect(island.activeTasks.value).toEqual([])
  })

  it('信息态只在 success/error 入历史的规则外，因此不留历史', async () => {
    vi.useFakeTimers()
    const { islandStart, useDynamicIsland } = await loadModule()
    const island = useDynamicIsland()

    islandStart('t1', '提示').info('已知悉')
    vi.advanceTimersByTime(2400)

    expect(island.activeTasks.value).toEqual([])
    expect(island.history.value).toEqual([])
  })

  it('终态未指定文案时沿用原标签', async () => {
    const { islandStart, useDynamicIsland } = await loadModule()

    islandStart('t1', '正在导出').success()

    expect(useDynamicIsland().current.value?.label).toBe('正在导出')
  })
})

describe('useDynamicIsland 消息堆叠', () => {
  it('多条终态消息同时存活并按新到旧排序，而不是排队逐条展示', async () => {
    vi.useFakeTimers()
    const { islandStart, useDynamicIsland } = await loadModule()
    const island = useDynamicIsland()

    islandStart('a', 'A').success()
    islandStart('b', 'B').success()
    islandStart('c', 'C').success()

    expect(island.current.value?.label).toBe('C')
    expect(island.pendingCount.value).toBe(2)
    expect(island.pendingTasks.value.map((item: IslandTask) => item.label)).toEqual(['B', 'A'])
  })

  it('消息条数上限 5，第 6 条到来时丢掉最旧的一条', async () => {
    vi.useFakeTimers()
    const { islandStart, useDynamicIsland } = await loadModule()
    const island = useDynamicIsland()

    for (let i = 1; i <= 6; i++) {
      islandStart(`m${i}`, `消息${i}`).success()
    }

    expect(island.activeTasks.value).toHaveLength(5)
    expect(island.activeTasks.value.map((item: IslandTask) => item.label))
      .toEqual(['消息6', '消息5', '消息4', '消息3', '消息2'])
  })

  it('进行中任务不参与消息堆叠限长，不会被消息刷掉', async () => {
    vi.useFakeTimers()
    const { islandStart, useDynamicIsland } = await loadModule()
    const island = useDynamicIsland()

    islandStart('job', '后台任务进行中')
    for (let i = 1; i <= 6; i++) {
      islandStart(`m${i}`, `消息${i}`).success()
    }

    expect(island.activeTasks.value.some((item: IslandTask) => item.id === 'job')).toBe(true)
  })

  it('无消息时折叠态回落到最近一条进行中/常驻状态', async () => {
    const { islandStart, islandStatus, useDynamicIsland } = await loadModule()
    const island = useDynamicIsland()

    islandStatus('net', '网络已连接')
    islandStart('job', '同步中')

    expect(island.current.value?.label).toBe('同步中')
  })

  it('有消息时消息优先于常驻状态占据折叠态', async () => {
    vi.useFakeTimers()
    const { islandStart, islandStatus, useDynamicIsland } = await loadModule()
    const island = useDynamicIsland()

    islandStatus('net', '网络已连接')
    islandStart('m', '保存成功').success()

    expect(island.current.value?.label).toBe('保存成功')
  })

  it('没有任何任务时折叠态为 null', async () => {
    const { useDynamicIsland } = await loadModule()

    expect(useDynamicIsland().current.value).toBeNull()
  })
})

describe('useDynamicIsland 常驻状态', () => {
  it('islandStatus 建的是常驻条目，不自动消失', async () => {
    vi.useFakeTimers()
    const { islandStatus, useDynamicIsland } = await loadModule()
    const island = useDynamicIsland()

    islandStatus('net', '网络已断开', { state: 'error' })
    vi.advanceTimersByTime(60_000)

    expect(island.activeTasks.value).toHaveLength(1)
    expect(island.activeTasks.value[0]?.persistent).toBe(true)
  })

  it('常驻状态默认信息态', async () => {
    const { islandStatus, useDynamicIsland } = await loadModule()

    islandStatus('net', '实时连接中')

    expect(useDynamicIsland().current.value?.state).toBe('info')
  })

  it('常驻条目 success 收尾后清掉常驻标记并开始倒计时', async () => {
    vi.useFakeTimers()
    const { islandStatus, useDynamicIsland } = await loadModule()
    const island = useDynamicIsland()

    islandStatus('net', '网络已断开', { state: 'error' }).success('网络已恢复')

    expect(island.current.value?.persistent).toBe(false)

    vi.advanceTimersByTime(1600)

    expect(island.activeTasks.value).toEqual([])
  })

  it('常驻条目转信息态时默认保持常驻，不会悄悄消失', async () => {
    vi.useFakeTimers()
    const { islandStatus, useDynamicIsland } = await loadModule()
    const island = useDynamicIsland()

    islandStatus('net', '连接中').info('已重连')
    vi.advanceTimersByTime(10_000)

    expect(island.activeTasks.value).toHaveLength(1)
    expect(island.current.value?.label).toBe('已重连')
  })
})

describe('useDynamicIsland 面板展开与计时', () => {
  it('面板展开时暂停全部消息计时，收起后重新计时', async () => {
    vi.useFakeTimers()
    const { islandStart, useDynamicIsland } = await loadModule()
    const island = useDynamicIsland()

    islandStart('m', '保存成功').success()

    island.expanded.value = true
    await nextTick()

    vi.advanceTimersByTime(10_000)
    expect(island.activeTasks.value).toHaveLength(1)

    island.expanded.value = false
    await nextTick()

    vi.advanceTimersByTime(1600)
    expect(island.activeTasks.value).toEqual([])
  })

  it('面板已展开时新产生的终态不计时，避免眼皮底下消失', async () => {
    vi.useFakeTimers()
    const { islandStart, useDynamicIsland } = await loadModule()
    const island = useDynamicIsland()

    islandStart('x', '占位')
    island.expanded.value = true
    await nextTick()

    islandStart('m', '保存成功').success()
    vi.advanceTimersByTime(10_000)

    expect(island.activeTasks.value.some((item: IslandTask) => item.id === 'm')).toBe(true)
  })

  it('无可展开内容时 expand 不生效', async () => {
    const { useDynamicIsland } = await loadModule()
    const island = useDynamicIsland()

    island.expand()

    expect(island.hasPanel.value).toBe(false)
    expect(island.expanded.value).toBe(false)
  })

  it('有任务时 expand 生效，collapse 收起', async () => {
    const { islandStart, useDynamicIsland } = await loadModule()
    const island = useDynamicIsland()
    islandStart('t', '任务')

    island.expand()
    expect(island.expanded.value).toBe(true)

    island.collapse()
    expect(island.expanded.value).toBe(false)
  })

  it('toggleExpand 在无可展开内容时始终保持收起', async () => {
    const { useDynamicIsland } = await loadModule()
    const island = useDynamicIsland()

    island.toggleExpand()
    island.toggleExpand()

    expect(island.expanded.value).toBe(false)
  })

  it('仅剩历史记录时面板仍可展开', async () => {
    vi.useFakeTimers()
    const { islandStart, useDynamicIsland } = await loadModule()
    const island = useDynamicIsland()

    islandStart('m', '保存成功').success()
    vi.advanceTimersByTime(1600)

    expect(island.activeTasks.value).toEqual([])
    expect(island.hasPanel.value).toBe(true)
  })

  it('清空历史后面板不再可展开', async () => {
    vi.useFakeTimers()
    const { islandStart, useDynamicIsland } = await loadModule()
    const island = useDynamicIsland()

    islandStart('m', '保存成功').success()
    vi.advanceTimersByTime(1600)

    island.clearHistory()

    expect(island.history.value).toEqual([])
    expect(island.hasPanel.value).toBe(false)
  })

  it('面板内手动关闭任务不写历史', async () => {
    const { islandStart, useDynamicIsland } = await loadModule()
    const island = useDynamicIsland()

    islandStart('m', '任务')
    island.dismissTask('m')

    expect(island.activeTasks.value).toEqual([])
    expect(island.history.value).toEqual([])
  })
})

describe('useDynamicIsland 历史上限', () => {
  it('历史最多保留 20 条，最新在前', async () => {
    vi.useFakeTimers()
    const { islandStart, useDynamicIsland } = await loadModule()
    const island = useDynamicIsland()

    for (let i = 1; i <= 22; i++) {
      islandStart(`m${i}`, `消息${i}`).success()
      vi.advanceTimersByTime(1600)
    }

    expect(island.history.value).toHaveLength(20)
    expect(island.history.value[0]?.label).toBe('消息22')
    expect(island.history.value.at(-1)?.label).toBe('消息3')
  })

  it('被限长挤掉的旧消息同样计入历史', async () => {
    vi.useFakeTimers()
    const { islandStart, useDynamicIsland } = await loadModule()
    const island = useDynamicIsland()

    for (let i = 1; i <= 6; i++) {
      islandStart(`m${i}`, `消息${i}`).success()
    }

    expect(island.history.value.map(item => item.label)).toEqual(['消息1'])
  })
})

describe('useDynamicIsland 会话级持久化', () => {
  it('只持久化服务端的进行中任务，本地任务不入存储', async () => {
    const { islandStart, applyServerTaskProgress } = await loadModule()

    islandStart('local', '本地任务')
    applyServerTaskProgress({ taskId: 'job-1', label: '服务端任务' })
    await nextTick()

    const saved = JSON.parse(sessionStorage.getItem(STORAGE_KEY) ?? '{}') as {
      tasks: Array<{ id: string, label: string }>
    }
    expect(saved.tasks.map(item => item.id)).toEqual(['server:job-1'])
  })

  it('刷新后恢复服务端进行中任务与历史', async () => {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify({
      tasks: [{ id: 'server:job-1', label: '恢复的任务', progress: 40, startedAt: 1000 }],
      history: [{ id: 'h1', label: '历史项', state: 'success', order: 7, time: 1 }],
    }))

    const { useDynamicIsland } = await loadModule()
    const island = useDynamicIsland()

    expect(island.activeTasks.value.map((item: IslandTask) => item.id)).toEqual(['server:job-1'])
    expect(island.activeTasks.value[0]?.progress).toBe(40)
    expect(island.activeTasks.value[0]?.startedAt).toBe(1000)
    expect(island.history.value.map(item => item.label)).toEqual(['历史项'])
  })

  it('恢复历史后序号续接，新任务不会与恢复项撞 order', async () => {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify({
      tasks: [],
      history: [{ id: 'h1', label: '历史项', state: 'success', order: 7, time: 1 }],
    }))

    const { islandStart, useDynamicIsland } = await loadModule()
    islandStart('new', '新任务')

    expect(useDynamicIsland().current.value?.order).toBeGreaterThan(7)
  })

  it('损坏的持久化载荷被丢弃，不影响启动', async () => {
    sessionStorage.setItem(STORAGE_KEY, '{不是合法 JSON')

    const { useDynamicIsland } = await loadModule()

    expect(useDynamicIsland().activeTasks.value).toEqual([])
    expect(useDynamicIsland().history.value).toEqual([])
  })

  it('历史超过上限的持久化载荷在恢复时被截断到 20 条', async () => {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify({
      tasks: [],
      history: Array.from({ length: 30 }, (_, i) => ({
        id: `h${i}`,
        label: `历史${i}`,
        state: 'success',
        order: i,
        time: i,
      })),
    }))

    const { useDynamicIsland } = await loadModule()

    expect(useDynamicIsland().history.value).toHaveLength(20)
  })
})

describe('useDynamicIsland 服务端任务进度', () => {
  it('服务端任务 id 带 server: 前缀，同 taskId 复用同一条', async () => {
    const { applyServerTaskProgress, useDynamicIsland } = await loadModule()

    applyServerTaskProgress({ taskId: 'job-1', label: '导入中', progress: 10 })
    applyServerTaskProgress({ taskId: 'job-1', label: '导入中', progress: 80 })

    const island = useDynamicIsland()
    expect(island.activeTasks.value).toHaveLength(1)
    expect(island.activeTasks.value[0]?.id).toBe('server:job-1')
    expect(island.activeTasks.value[0]?.progress).toBe(80)
  })

  it('缺 taskId 或缺 label 的载荷被忽略', async () => {
    const { applyServerTaskProgress, useDynamicIsland } = await loadModule()

    applyServerTaskProgress({ taskId: '', label: '有标签' })
    applyServerTaskProgress({ taskId: 'job-1', label: '' })

    expect(useDynamicIsland().activeTasks.value).toEqual([])
  })

  it('未知 state 一律按进行中处理', async () => {
    const { applyServerTaskProgress, useDynamicIsland } = await loadModule()

    applyServerTaskProgress({ taskId: 'job-1', label: '任务', state: 'weird' })

    expect(useDynamicIsland().activeTasks.value[0]?.state).toBe('loading')
  })

  it('进行中的服务端任务带服务器图标，终态时移除该图标', async () => {
    vi.useFakeTimers()
    const { applyServerTaskProgress, useDynamicIsland } = await loadModule()

    applyServerTaskProgress({ taskId: 'job-1', label: '任务' })
    expect(useDynamicIsland().activeTasks.value[0]?.icon).toBe('lucide:server')

    applyServerTaskProgress({ taskId: 'job-1', label: '任务完成', state: 'success' })
    expect(useDynamicIsland().activeTasks.value[0]?.state).toBe('success')
  })

  it('终态服务端任务照常倒计时消失并入历史', async () => {
    vi.useFakeTimers()
    const { applyServerTaskProgress, useDynamicIsland } = await loadModule()
    const island = useDynamicIsland()

    applyServerTaskProgress({ taskId: 'job-1', label: '任务失败', state: 'error' })
    vi.advanceTimersByTime(3200)

    expect(island.activeTasks.value).toEqual([])
    expect(island.history.value.map(item => item.label)).toEqual(['任务失败'])
  })

  it('null 的 detail / progress / link 被规整为 undefined，不写进任务', async () => {
    const { applyServerTaskProgress, useDynamicIsland } = await loadModule()

    applyServerTaskProgress({ taskId: 'job-1', label: '任务', detail: null, progress: null, link: null })

    const task = useDynamicIsland().activeTasks.value[0]
    expect(task?.detail).toBeUndefined()
    expect(task?.progress).toBeUndefined()
    expect(task?.link).toBeUndefined()
  })
})

describe('useDynamicIsland 关闭时的兜底接管', () => {
  it('关闭后 islandStart 不进岛，终态改由轻提示接管', async () => {
    const { configureDynamicIsland, islandStart, useDynamicIsland } = await loadModule()
    const message = vi.fn()
    configureDynamicIsland({ isEnabled: () => false, message })

    islandStart('t', '正在保存').success('保存成功')

    expect(useDynamicIsland().activeTasks.value).toEqual([])
    expect(message).toHaveBeenCalledWith('success', '保存成功')
  })

  it('关闭后进行中态静默，不产生任何轻提示', async () => {
    const { configureDynamicIsland, islandStart } = await loadModule()
    const message = vi.fn()
    configureDynamicIsland({ isEnabled: () => false, message })

    const task = islandStart('t', '正在保存')
    task.update('还在保存')
    task.setProgress(50)

    expect(message).not.toHaveBeenCalled()
  })

  it('关闭后终态未带文案时沿用最后一次 update 的文案', async () => {
    const { configureDynamicIsland, islandStart } = await loadModule()
    const message = vi.fn()
    configureDynamicIsland({ isEnabled: () => false, message })

    const task = islandStart('t', '初始文案')
    task.update('最新文案')
    task.error()

    expect(message).toHaveBeenCalledWith('error', '最新文案')
  })

  it('关闭后文案为空白时不发轻提示，避免空气泡', async () => {
    const { configureDynamicIsland, islandStart } = await loadModule()
    const message = vi.fn()
    configureDynamicIsland({ isEnabled: () => false, message })

    islandStart('t', '   ').success()

    expect(message).not.toHaveBeenCalled()
  })

  it('关闭后常驻状态整体静默，不降级成消息噪音', async () => {
    const { configureDynamicIsland, islandStatus, useDynamicIsland } = await loadModule()
    const message = vi.fn()
    configureDynamicIsland({ isEnabled: () => false, message })

    const status = islandStatus('net', '网络已断开')
    status.error('仍然断开')
    status.success('网络已恢复')

    expect(message).not.toHaveBeenCalled()
    expect(useDynamicIsland().activeTasks.value).toEqual([])
  })

  it('关闭后服务端任务的进行中态静默、终态走轻提示', async () => {
    const { configureDynamicIsland, applyServerTaskProgress, useDynamicIsland } = await loadModule()
    const message = vi.fn()
    configureDynamicIsland({ isEnabled: () => false, message })

    applyServerTaskProgress({ taskId: 'job-1', label: '导入中' })
    expect(message).not.toHaveBeenCalled()

    applyServerTaskProgress({ taskId: 'job-1', label: '导入完成', state: 'success' })
    expect(message).toHaveBeenCalledWith('success', '导入完成')
    expect(useDynamicIsland().activeTasks.value).toEqual([])
  })

  it('未注入启用判定时默认按启用处理', async () => {
    const { islandStart, useDynamicIsland } = await loadModule()

    islandStart('t', '正在保存')

    expect(useDynamicIsland().activeTasks.value).toHaveLength(1)
  })
})
