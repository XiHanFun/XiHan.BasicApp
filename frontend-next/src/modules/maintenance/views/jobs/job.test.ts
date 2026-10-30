import type { JobResponse, JobRunRow } from './job'
import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { QueryOperator } from '~/types/contracts'
import {
  activeTriggerCount,
  assertBatch,
  batchFailureMessage,
  canReschedule,
  canRestore,
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
  loadJobTypeOptions,
  previewSchedule,
  rescheduleJob,
  restoreJobs,
  runFailedOptions,
  runStateLabel,
  runStateTone,
  runStatusFilters,
  runStatusLabel,
  runStatusOptions,
  runStatusTone,
  runTriggerLabel,
  scheduleFormOf,
  schedulePresets,
  sourceLabel,
  sourceOptions,
  sourceTone,
  statusOptions,
  toJobRow,
  toJobRunsRequest,
  toJobsRequest,
  toRescheduleRequest,
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

describe('任务调度：登记来源与作业类型筛选', () => {
  it('登记来源三种（已改计划即代码登记后在运行时改过计划），语气中性 / 蓝 / 黄；搜索按 source eq', () => {
    expect(sourceOptions(t)).toEqual([
      { label: 'maintenance.jobs.sources.code', value: 'Code' },
      { label: 'maintenance.jobs.sources.runtime', value: 'Runtime' },
      { label: 'maintenance.jobs.sources.overridden', value: 'Overridden' },
    ])
    expect(sourceLabel(t, 'Overridden')).toBe('maintenance.jobs.sources.overridden')
    expect((['Code', 'Runtime', 'Overridden'] as const).map(sourceTone)).toEqual(['neutral', 'info', 'warning'])
    expect(toJobsRequest({ page: 1, pageSize: 20, filters: { source: 'Overridden' } }).filters).toEqual([{ field: 'source', operator: 'eq', value: 'Overridden' }])
  })

  it('作业类型的候选取自 GET jobs:types，登记键即显示文本', async () => {
    const calls = stub({ items: ['auditing.retention', 'files.expire-uploads'] })
    expect(await loadJobTypeOptions()).toEqual([
      { label: 'auditing.retention', value: 'auditing.retention' },
      { label: 'files.expire-uploads', value: 'files.expire-uploads' },
    ])
    expect(calls[0]).toMatchObject({ method: 'GET', url: '/api/jobs:types', body: undefined })
  })

  it('作业类型多选合成 jobType in（逗号分隔），单选一个也走 in；多选的原始值不再按 eq 重复下发', () => {
    const request = toJobsRequest({
      page: 1,
      pageSize: 20,
      filters: { jobType: ['auditing.retention', 'files.expire-uploads'] },
      conditionFilters: [{ field: 'jobType', operator: QueryOperator.In, values: ['auditing.retention', 'files.expire-uploads'] }],
    })
    expect(request.filters).toEqual([{ field: 'jobType', operator: 'in', value: 'auditing.retention,files.expire-uploads' }])
    expect(toJobsRequest({ page: 1, pageSize: 20, filters: {}, conditionFilters: [{ field: 'jobType', operator: QueryOperator.In, values: ['auditing.retention'] }] }).filters)
      .toEqual([{ field: 'jobType', operator: 'in', value: 'auditing.retention' }])
  })
})

describe('任务调度：修改计划', () => {
  const INTERVAL = { name: 'default', schedule: 'every 00:05:00', kind: 'Interval' as const, cron: null, intervalSeconds: 300, timeZone: null, state: 'Waiting' as const, nextFireAt: null, previousFireAt: null, fireCount: 0, error: null }

  it('只能改唯一一个计划触发器的计划；恢复代码计划只对已改计划的作业', () => {
    // 立即执行排进的临时触发器（manual-）不算
    expect(activeTriggerCount(job())).toBe(1)
    expect(canReschedule(job())).toBe(true)
    expect(canReschedule(job({ triggers: [INTERVAL, { ...INTERVAL, name: 'nightly' }] }))).toBe(false)
    expect(canReschedule(job({ triggers: [INTERVAL] }))).toBe(true)
    expect(canReschedule(job({ triggers: [] }))).toBe(true)
    expect(canRestore(job({ source: 'Overridden' }))).toBe(true)
    expect(canRestore(job({ source: 'Code' }))).toBe(false)
    expect(canRestore(job({ source: 'Runtime' }))).toBe(false)
  })

  it('弹窗初值：Cron 触发器拆成表达式与时区，其余取计划写法，没有触发器为空', () => {
    const cron = { ...INTERVAL, schedule: 'cron 0 0 3 * * ? tz=Asia/Shanghai', kind: 'Cron' as const, cron: '0 0 3 * * ?', intervalSeconds: null, timeZone: 'Asia/Shanghai' }
    expect(scheduleFormOf(job({ triggers: [cron] }))).toEqual({ expression: '0 0 3 * * ?', timeZone: 'Asia/Shanghai' })
    expect(scheduleFormOf(job({ triggers: [{ ...cron, timeZone: null }] }))).toEqual({ expression: '0 0 3 * * ?', timeZone: '' })
    expect(scheduleFormOf(job({ triggers: [INTERVAL] }))).toEqual({ expression: 'every 00:05:00', timeZone: '' })
    expect(scheduleFormOf(job({ triggers: [] }))).toEqual({ expression: '', timeZone: '' })
  })

  it('请求：作业名进路由值，计划去空白，时区留空即 null（后端按 UTC）', () => {
    expect(toRescheduleRequest('auditing.retention', { expression: ' 0 0 3 * * ? ', timeZone: ' Asia/Shanghai ' })).toEqual({ id: 'auditing.retention', expression: '0 0 3 * * ?', timeZone: 'Asia/Shanghai' })
    expect(toRescheduleRequest('auditing.retention', { expression: 'every 00:05:00', timeZone: '  ' })).toEqual({ id: 'auditing.retention', expression: 'every 00:05:00', timeZone: null })
  })

  it('保存：POST jobs/{id}:reschedule，请求体只有计划与时区，返回改后的作业（已改计划）', async () => {
    const changed = job({ source: 'Overridden', triggers: [{ ...INTERVAL, schedule: 'cron 0 0 3 * * ? tz=Asia/Shanghai', kind: 'Cron', cron: '0 0 3 * * ?', intervalSeconds: null, timeZone: 'Asia/Shanghai' }] })
    const calls = stub(changed)
    const row = await rescheduleJob('auditing.retention', { expression: '0 0 3 * * ?', timeZone: 'Asia/Shanghai' })
    expect(calls[0]).toEqual({ method: 'POST', url: '/api/jobs/auditing.retention:reschedule', body: { expression: '0 0 3 * * ?', timeZone: 'Asia/Shanghai' } })
    expect(row).toMatchObject({ name: 'auditing.retention', source: 'Overridden', kind: 'Cron', group: 'auditing' })
  })

  it('保存失败（计划写错）原样抛出后端的问题详情，由弹窗落到计划框下', async () => {
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async () => new Response(JSON.stringify({ status: 400, code: 'MAINTENANCE_VALIDATION_002', detail: '计划写错了' }), { status: 400, headers: { 'Content-Type': 'application/problem+json' } })) as unknown as typeof fetch,
    })
    await expect(rescheduleJob('auditing.retention', { expression: 'nope', timeZone: '' })).rejects.toMatchObject({ code: 'MAINTENANCE_VALIDATION_002', detail: '计划写错了' })
  })

  it('预览：GET jobs:validate-schedule 带计划、时区与个数 5；时区留空不进查询串', async () => {
    const calls = stub(
      { schedule: 'cron 0 0 3 * * ? tz=Asia/Shanghai', kind: 'Cron', nextRuns: ['2026-10-30T19:00:00Z'] },
      { schedule: 'every 00:05:00', kind: 'Interval', nextRuns: [] },
    )
    expect(await previewSchedule({ expression: ' 0 0 3 * * ? ', timeZone: 'Asia/Shanghai' })).toEqual({ schedule: 'cron 0 0 3 * * ? tz=Asia/Shanghai', kind: 'Cron', nextRuns: ['2026-10-30T19:00:00Z'] })
    await previewSchedule({ expression: 'every 00:05:00', timeZone: ' ' })
    const first = new URL(calls[0]!.url, 'http://host')
    expect(calls[0]!.method).toBe('GET')
    expect(first.pathname).toBe('/api/jobs:validate-schedule')
    expect(Object.fromEntries(first.searchParams)).toEqual({ expression: '0 0 3 * * ?', timeZone: 'Asia/Shanghai', count: '5' })
    expect(Object.fromEntries(new URL(calls[1]!.url, 'http://host').searchParams)).toEqual({ expression: 'every 00:05:00', count: '5' })
  })

  it('常用预设沿用 4.x 的六个（周按 Unix 约定，1 是周一）', () => {
    expect(schedulePresets(t).map(preset => preset.value)).toEqual(['0 * * * * ?', '0 0/5 * * * ?', '0 0 * * * ?', '0 0 0 * * ?', '0 0 0 ? * 1', '0 0 0 1 * ?'])
  })
})

describe('任务调度：恢复代码计划', () => {
  it('按作业名批量 POST jobs:restore；全部成功不抛错', async () => {
    const calls = stub({ succeeded: ['auditing.retention'], failed: [] })
    await restoreJobs(t, ['auditing.retention'])
    expect(calls[0]).toEqual({ method: 'POST', url: '/api/jobs:restore', body: { ids: ['auditing.retention'] } })
  })

  it('没有代码定义（SCHEDULING_NOTFOUND_005）与作业不存在翻成文案抛出；未知码原样', async () => {
    stub({ succeeded: [], failed: [{ id: 'runtime.job', code: 'SCHEDULING_NOTFOUND_005' }, { id: 'gone.job', code: 'MAINTENANCE_NOTFOUND_001' }] })
    await expect(restoreJobs(t, ['runtime.job', 'gone.job'])).rejects.toThrow('maintenance.jobs.no_code_definition; maintenance.jobs.job_not_found')
    expect(batchFailureMessage(t, 'SCHEDULING_NOTFOUND_005')).toBe('maintenance.jobs.no_code_definition')
    expect(batchFailureMessage(t, 'X_9')).toBe('X_9')
  })
})

describe('任务调度：执行日志', () => {
  const NO_FILTERS = { state: null, failed: null, triggerName: '', node: '', keyword: '' } as const

  it('按作业过滤，状态单选，关键字（替 4.x 的批次号）去空白，留空不下发', () => {
    expect(toJobRunsRequest({ ...NO_FILTERS, job: 'auditing.retention', page: 3, pageSize: 10, state: 'Failed', keyword: ' node-a ' })).toEqual({
      page: { index: 3, size: 10 },
      filters: [{ field: 'job', operator: 'eq', value: 'auditing.retention' }, { field: 'state', operator: 'eq', value: 'Failed' }],
      sorts: [],
      keyword: 'node-a',
    })
    expect(toJobRunsRequest({ ...NO_FILTERS, job: 'a.b', page: 1, pageSize: 10, keyword: ' ' })).toMatchObject({ filters: [{ field: 'job', operator: 'eq', value: 'a.b' }], keyword: null })
  })

  it('是否出错按 failed eq true / false 下发，触发器名与节点去空白后精确匹配，留空不下发', () => {
    expect(toJobRunsRequest({ ...NO_FILTERS, job: 'a.b', page: 1, pageSize: 10, failed: true, triggerName: ' manual-2 ', node: ' node-a ' }).filters).toEqual([
      { field: 'job', operator: 'eq', value: 'a.b' },
      { field: 'failed', operator: 'eq', value: 'true' },
      { field: 'triggerName', operator: 'eq', value: 'manual-2' },
      { field: 'node', operator: 'eq', value: 'node-a' },
    ])
    expect(toJobRunsRequest({ ...NO_FILTERS, job: 'a.b', page: 1, pageSize: 10, failed: false, triggerName: '  ', node: '' }).filters).toEqual([
      { field: 'job', operator: 'eq', value: 'a.b' },
      { field: 'failed', operator: 'eq', value: 'false' },
    ])
    expect(runFailedOptions(t).map(option => option.value)).toEqual(['true', 'false'])
  })

  it('分页：POST job-runs:list 带上全部筛选，总数取后端给的真实总数并换成数字', async () => {
    const calls = stub({ items: [RUN], total: '41', page: { index: 2, size: 10 } })
    const result = await listJobRuns({ job: 'auditing.retention', page: 2, pageSize: 10, state: 'Succeeded', failed: false, triggerName: 'default', node: 'node-a', keyword: '' })
    expect(calls[0]).toMatchObject({
      method: 'POST',
      url: '/api/job-runs:list',
      body: {
        page: { index: 2, size: 10 },
        filters: [
          { field: 'job', operator: 'eq', value: 'auditing.retention' },
          { field: 'state', operator: 'eq', value: 'Succeeded' },
          { field: 'failed', operator: 'eq', value: 'false' },
          { field: 'triggerName', operator: 'eq', value: 'default' },
          { field: 'node', operator: 'eq', value: 'node-a' },
        ],
        keyword: null,
      },
    })
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
