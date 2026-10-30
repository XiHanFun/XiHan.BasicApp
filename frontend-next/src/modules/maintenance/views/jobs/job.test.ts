import type { JobResponse, JobRunRow } from './job'
import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import {
  assertBatch,
  deriveRunStatus,
  enabledLabel,
  formatDuration,
  getJob,
  isRunning,
  jobGroup,
  jobResource,
  kindLabel,
  kindOptions,
  listJobRuns,
  runStateLabel,
  runStateTone,
  runStatusFilters,
  runStatusLabel,
  runStatusOptions,
  runStatusTone,
  runTriggerLabel,
  statusOptions,
  toJobRow,
  toJobRunsRequest,
  toJobsRequest,
  triggerJob,
  triggerStateTone,
} from './job'

/** 假翻译：键原样返回，带参数时拼上参数便于断言 */
const t = (key: string, values?: Record<string, unknown>) => (values ? `${key}:${JSON.stringify(values)}` : key)

function json(status: number, body: object): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

function stub(...responses: object[]): { method: string, url: string, body: unknown }[] {
  const calls: { method: string, url: string, body: unknown }[] = []
  configureRequest({
    baseUrl: '',
    getToken: () => 't',
    fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
      calls.push({ method: init?.method ?? 'GET', url: String(url), body: init?.body ? JSON.parse(String(init.body)) : undefined })
      return json(200, responses[calls.length - 1] ?? {})
    }) as unknown as typeof fetch,
  })
  return calls
}

function job(patch: Partial<JobResponse> = {}): JobResponse {
  return {
    name: 'auditing.retention',
    title: '审计日志清理',
    description: null,
    jobType: 'auditing.retention',
    source: 'Code',
    paused: false,
    runningCount: 0,
    allowConcurrent: false,
    runOnEveryInstance: false,
    nextRunAt: '2026-10-31T03:00:00Z',
    triggers: [
      { name: 'default', schedule: 'cron 0 3 * * *', kind: 'Cron', cron: '0 3 * * *', intervalSeconds: null, timeZone: null, state: 'Waiting', nextFireAt: '2026-10-31T03:00:00Z', previousFireAt: '2026-10-30T03:00:00Z', fireCount: 12, error: null },
      { name: 'manual-1', schedule: 'manual', kind: 'Manual', cron: null, intervalSeconds: null, timeZone: null, state: 'Complete', nextFireAt: null, previousFireAt: '2026-10-29T08:00:00Z', fireCount: 2, error: null },
    ],
    last: { runId: 'r1', trigger: 'Schedule', startedAt: '2026-10-30T03:00:00Z', completedAt: '2026-10-30T03:00:01.5Z', durationMs: 1500, succeeded: true, errorCode: null, errorMessage: null },
    updatedAt: '2026-10-01T00:00:00Z',
    ...patch,
  }
}

const FAILED = { runId: 'r2', trigger: 'Manual' as const, startedAt: '2026-10-30T04:00:00Z', completedAt: '2026-10-30T04:00:00.2Z', durationMs: 200, succeeded: false, errorCode: 'X_1', errorMessage: 'boom' }

const RUN: JobRunRow = {
  id: 'r1',
  job: 'auditing.retention',
  triggerName: 'default',
  trigger: 'Schedule',
  scheduledAt: '2026-10-30T03:00:00Z',
  startedAt: '2026-10-30T03:00:00Z',
  completedAt: '2026-10-30T03:00:01.5Z',
  durationMs: 1500,
  state: 'Succeeded',
  errorCode: null,
  errorMessage: null,
  nodeId: 'node-a',
  cancelRequested: false,
}

describe('任务调度：运行状态', () => {
  it('正在运行优先（暂停的也算运行中）；否则暂停为已暂停；再看上次结果，没运行过为待执行', () => {
    expect(deriveRunStatus(job({ runningCount: 1, paused: true }))).toBe('Running')
    expect(deriveRunStatus(job({ paused: true, last: FAILED }))).toBe('Paused')
    expect(deriveRunStatus(job({ last: FAILED }))).toBe('Failed')
    expect(deriveRunStatus(job())).toBe('Success')
    expect(deriveRunStatus(job({ last: null }))).toBe('Pending')
    expect(isRunning(job({ runningCount: 2 }))).toBe(true)
    expect(isRunning(job())).toBe(false)
  })

  it('文案与语气沿用 4.x：成功绿、失败红、运行中黄、暂停中性、待执行蓝；搜索只给后端筛得了的两项', () => {
    expect(runStatusLabel(t, 'Running')).toBe('maintenance.jobs.run_running')
    expect(runStatusLabel(t, 'Pending')).toBe('maintenance.jobs.run_pending')
    expect(['Success', 'Failed', 'Running', 'Paused', 'Pending'].map(s => runStatusTone(s as never))).toEqual(['success', 'danger', 'warning', 'neutral', 'info'])
    expect(runStatusOptions(t).map(option => option.value)).toEqual(['Running', 'Paused'])
  })
})

describe('任务调度：列表行', () => {
  it('分组取作业名第一段，触发类型取第一个触发器，已执行为各触发器触发次数之和，上次执行取上次结果的开始时间', () => {
    const row = toJobRow(job())
    expect(row).toMatchObject({ group: 'auditing', kind: 'Cron', runState: 'Success', executedCount: 14, lastRunAt: '2026-10-30T03:00:00Z' })
    expect(toJobRow(job({ triggers: [], last: null }))).toMatchObject({ kind: null, executedCount: 0, lastRunAt: null, runState: 'Pending' })
    expect(jobGroup('cleanup')).toBeNull()
    expect(jobGroup('.hidden')).toBeNull()
  })

  it('触发类型沿用 4.x 的四个文案：手动即立即执行、一次性即定时执行、间隔即循环执行', () => {
    expect(kindOptions(t)).toEqual([
      { label: 'maintenance.jobs.trigger_immediate', value: 'Manual' },
      { label: 'maintenance.jobs.trigger_schedule', value: 'Once' },
      { label: 'maintenance.jobs.trigger_recurring', value: 'Interval' },
      { label: 'maintenance.jobs.trigger_cron', value: 'Cron' },
    ])
    expect(kindLabel(t, null)).toBe('-')
    expect(enabledLabel(t, true)).toBe('common.statuses.disabled')
    expect(statusOptions(t)).toEqual([{ label: 'common.statuses.enabled', value: 'false' }, { label: 'common.statuses.disabled', value: 'true' }])
  })
})

describe('任务调度：列表筛选', () => {
  it('触发类型与启停按值精确匹配；运行中换成 running eq true；排序原样下发', () => {
    const request = toJobsRequest({
      page: 2,
      pageSize: 20,
      filters: { keyword: ' audit ', kind: 'Cron', paused: 'false', runState: 'Running' },
      sorts: [{ field: 'nextRunAt', order: 'asc' }, { field: 'lastRunAt', order: 'desc' }],
    })
    expect(request).toEqual({
      page: { index: 2, size: 20 },
      keyword: 'audit',
      filters: [
        { field: 'kind', operator: 'eq', value: 'Cron' },
        { field: 'paused', operator: 'eq', value: 'false' },
        { field: 'running', operator: 'eq', value: 'true' },
      ],
      sorts: [{ field: 'nextRunAt', descending: false }, { field: 'lastRunAt', descending: true }],
    })
  })

  it('已暂停即暂停且不在运行；没选不加条件；认不出的值直接报错而不是静默丢掉', () => {
    expect(runStatusFilters('Paused')).toEqual([
      { field: 'paused', operator: 'eq', value: 'true' },
      { field: 'running', operator: 'eq', value: 'false' },
    ])
    expect(runStatusFilters('')).toEqual([])
    expect(runStatusFilters(undefined)).toEqual([])
    expect(() => runStatusFilters('Success')).toThrow()
    expect(toJobsRequest({ page: 1, pageSize: 10, filters: { runState: null } }).filters).toEqual([])
  })
})

describe('任务调度：资源', () => {
  it('分页：POST jobs:list，结果换成列表行与 SchemaPage 的分页', async () => {
    const calls = stub({ items: [job()], total: 1, page: { index: 1, size: 20 } })
    const result = await jobResource(t).page!({ page: 1, pageSize: 20, filters: { paused: 'true' }, conditionFilters: [], sorts: [] })
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/jobs:list', body: { page: { index: 1, size: 20 }, filters: [{ field: 'paused', operator: 'eq', value: 'true' }], sorts: [], keyword: null } })
    expect(result.items[0]).toMatchObject({ name: 'auditing.retention', group: 'auditing', executedCount: 14 })
    expect(result.page.totalCount).toBe(1)
  })

  it('启用即恢复、停用即暂停（按作业名批量）；逐项失败翻成文案，未知码原样', async () => {
    const calls = stub(
      { succeeded: ['auditing.retention'], failed: [] },
      { succeeded: ['auditing.retention'], failed: [] },
      { succeeded: [], failed: [{ id: 'gone.job', code: 'MAINTENANCE_NOTFOUND_001' }] },
    )
    const resource = jobResource(t)
    await resource.updateStatus!('auditing.retention', true)
    await resource.updateStatus!('auditing.retention', false)
    await expect(resource.updateStatus!('gone.job', false)).rejects.toThrow('maintenance.jobs.job_not_found')
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/jobs:resume', body: { ids: ['auditing.retention'] } })
    expect(calls[1]).toMatchObject({ method: 'POST', url: '/api/jobs:pause', body: { ids: ['auditing.retention'] } })
    expect(calls[2]).toMatchObject({ method: 'POST', url: '/api/jobs:pause', body: { ids: ['gone.job'] } })
    expect(() => assertBatch(t, { failed: [{ code: 'X_1' }, { code: 'X_1' }] })).toThrow(/^X_1$/)
  })

  it('详情按作业名取；立即执行按作业名排进一次性触发器，返回触发器名', async () => {
    const calls = stub(job({ runningCount: 1 }), { trigger: 'manual-2' })
    const detail = await getJob('auditing.retention')
    expect(detail.runState).toBe('Running')
    expect(await triggerJob('auditing.retention')).toBe('manual-2')
    expect(calls[0]).toMatchObject({ method: 'GET', url: '/api/jobs/auditing.retention' })
    expect(calls[1]).toMatchObject({ method: 'POST', url: '/api/jobs/auditing.retention:trigger' })
  })
})

describe('任务调度：执行日志', () => {
  it('按作业过滤，状态单选，关键字（替 4.x 的批次号）去空白，留空不下发', () => {
    expect(toJobRunsRequest({ job: 'auditing.retention', page: 3, pageSize: 10, state: 'Failed', keyword: ' node-a ' })).toEqual({
      page: { index: 3, size: 10 },
      filters: [{ field: 'job', operator: 'eq', value: 'auditing.retention' }, { field: 'state', operator: 'eq', value: 'Failed' }],
      sorts: [],
      keyword: 'node-a',
    })
    expect(toJobRunsRequest({ job: 'a.b', page: 1, pageSize: 10, state: null, keyword: ' ' })).toMatchObject({ filters: [{ field: 'job', operator: 'eq', value: 'a.b' }], keyword: null })
  })

  it('分页：POST job-runs:list，总数换成数字', async () => {
    const calls = stub({ items: [RUN], total: '41', page: { index: 1, size: 10 } })
    const result = await listJobRuns({ job: 'auditing.retention', page: 1, pageSize: 10, state: null, keyword: '' })
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/job-runs:list' })
    expect(result).toEqual({ items: [RUN], total: 41 })
  })

  it('耗时同 4.x：不足一秒按毫秒、否则按秒两位，还在运行为 -；运行状态、触发方式与触发器状态的文案和语气', () => {
    expect(formatDuration(999)).toBe('999 ms')
    expect(formatDuration(1500)).toBe('1.50 s')
    expect(formatDuration(null)).toBe('-')
    expect(runStateLabel(t, 'Canceled')).toBe('maintenance.jobs.run_canceled')
    expect(runStateLabel(t, 'Lost')).toBe('maintenance.jobs.run_lost')
    expect(['Succeeded', 'Failed', 'Lost', 'Running', 'Canceled'].map(s => runStateTone(s as never))).toEqual(['success', 'danger', 'danger', 'warning', 'neutral'])
    expect(runTriggerLabel(t, 'Recovery')).toBe('maintenance.jobs.trigger_modes.recovery')
    expect(triggerStateTone('Error')).toBe('danger')
    expect(triggerStateTone('Blocked')).toBe('warning')
  })
})
