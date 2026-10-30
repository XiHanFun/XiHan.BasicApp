import type { Tone } from '@xihan-ui/core'
import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { JobRunState, JobScheduleKind, JobSource, JobTrigger, PageQueryMapping, PageRequest, ResponseOf, TriggerState } from '~/contracts'
import { jobRuns as jobRunsApi, jobs as jobsApi, toPageRequest, toSchemaPageResult } from '~/contracts'
import { ApiError } from '~/request'

export type Translate = (key: string, values?: Record<string, unknown>) => string

/** 一个作业（列表项与 jobs.get 同形） */
export type JobResponse = ResponseOf<'jobs.list'>['items'][number]
export type JobTriggerInfo = JobResponse['triggers'][number]
export type JobRunRow = ResponseOf<'job-runs.list'>['items'][number]
type PageFilter = PageRequest['filters'][number]

/** 任务的运行状态（4.x 的 RunTaskStatus 去掉「已停止」）：6.0 没有这一列，由运行中次数、暂停与上次结果推出来 */
export type JobRunStatus = 'Running' | 'Paused' | 'Failed' | 'Success' | 'Pending'

/**
 * 列表行：作业本身加上 4.x 列表直接给、6.0 要推出来的几列。
 * 分组取作业名第一段（登记它的包），触发类型取第一个触发器，已执行是各触发器触发次数之和，上次执行取上次结果的开始时间。
 */
export type JobRow = JobResponse & {
  group: null | string
  kind: JobScheduleKind | null
  runState: JobRunStatus
  executedCount: number
  lastRunAt: null | string
}

/** 运行状态的推法：正在运行优先；否则暂停即「已暂停」；再看上次结果，没运行过为「待执行」 */
export function deriveRunStatus(job: Pick<JobResponse, 'last' | 'paused' | 'runningCount'>): JobRunStatus {
  if (job.runningCount > 0)
    return 'Running'
  if (job.paused)
    return 'Paused'
  if (!job.last)
    return 'Pending'
  return job.last.succeeded ? 'Success' : 'Failed'
}

/** 任务分组：作业名的第一段（如 auditing.retention 归 auditing）；没有点分即没有分组 */
export function jobGroup(name: string): null | string {
  const dot = name.indexOf('.')
  return dot > 0 ? name.slice(0, dot) : null
}

export function toJobRow(job: JobResponse): JobRow {
  return {
    ...job,
    group: jobGroup(job.name),
    kind: job.triggers[0]?.kind ?? null,
    runState: deriveRunStatus(job),
    executedCount: job.triggers.reduce((sum, trigger) => sum + trigger.fireCount, 0),
    lastRunAt: job.last?.startedAt ?? null,
  }
}

/** 正在运行：不能再立即执行、也不能改启停（4.x 同；暂停的作业仍可立即执行） */
export function isRunning(job: Pick<JobResponse, 'runningCount'>): boolean {
  return job.runningCount > 0
}

// 枚举值是契约字面量，文案键静态写出以便 i18n 门禁核对

/** 触发类型（计划的种类）沿用 4.x 的四个文案：手动即立即执行、一次性即定时执行、间隔即循环执行 */
export function kindLabel(t: Translate, kind: JobScheduleKind | null | undefined): string {
  switch (kind) {
    case 'Manual': return t('maintenance.jobs.trigger_immediate')
    case 'Once': return t('maintenance.jobs.trigger_schedule')
    case 'Interval': return t('maintenance.jobs.trigger_recurring')
    case 'Cron': return t('maintenance.jobs.trigger_cron')
    default: return '-'
  }
}

/** 触发类型的搜索选项（4.x 的顺序）：后端按 `kind eq` 匹配任一触发器 */
export function kindOptions(t: Translate): { label: string, value: JobScheduleKind }[] {
  return (['Manual', 'Once', 'Interval', 'Cron'] as const).map(kind => ({ label: kindLabel(t, kind), value: kind }))
}

export function runStatusLabel(t: Translate, status: JobRunStatus): string {
  switch (status) {
    case 'Running': return t('maintenance.jobs.run_running')
    case 'Paused': return t('maintenance.jobs.run_paused')
    case 'Failed': return t('maintenance.jobs.run_failed')
    case 'Success': return t('maintenance.jobs.run_success')
    default: return t('maintenance.jobs.run_pending')
  }
}

/** 运行状态的语气（4.x 同）：成功绿、失败红、运行中黄、暂停中性、待执行蓝 */
export function runStatusTone(status: JobRunStatus): Tone {
  switch (status) {
    case 'Success': return 'success'
    case 'Failed': return 'danger'
    case 'Running': return 'warning'
    case 'Paused': return 'neutral'
    default: return 'info'
  }
}

/** 运行状态的搜索选项：后端只能按「正在运行」与「暂停」筛，成功 / 失败 / 待执行筛不了，不给选 */
export function runStatusOptions(t: Translate): { label: string, value: JobRunStatus }[] {
  return (['Running', 'Paused'] as const).map(status => ({ label: runStatusLabel(t, status), value: status }))
}

/** 运行状态的筛选：运行中即 `running eq true`；已暂停即暂停且不在运行（与列上推出的「已暂停」一致） */
export function runStatusFilters(value: unknown): PageFilter[] {
  if (value === undefined || value === null || value === '')
    return []
  switch (value) {
    case 'Running': return [{ field: 'running', operator: 'eq', value: 'true' }]
    case 'Paused': return [{ field: 'paused', operator: 'eq', value: 'true' }, { field: 'running', operator: 'eq', value: 'false' }]
    default: throw new Error(`运行状态 ${String(value)} 没有对应的后端筛选`)
  }
}

/** 启停：6.0 的启停即恢复 / 暂停，停用的作业不再按计划触发 */
export function enabledLabel(t: Translate, paused: boolean): string {
  return paused ? t('common.statuses.disabled') : t('common.statuses.enabled')
}

/** 启停的搜索选项：后端按 `paused eq true / false` 匹配，启用即没暂停 */
export function statusOptions(t: Translate): { label: string, value: string }[] {
  return [
    { label: t('common.statuses.enabled'), value: 'false' },
    { label: t('common.statuses.disabled'), value: 'true' },
  ]
}

export function sourceLabel(t: Translate, source: JobSource): string {
  return source === 'Runtime' ? t('maintenance.jobs.sources.runtime') : t('maintenance.jobs.sources.code')
}

/** 一次运行的状态 */
export function runStateLabel(t: Translate, state: JobRunState): string {
  switch (state) {
    case 'Running': return t('maintenance.jobs.run_running')
    case 'Succeeded': return t('maintenance.jobs.run_success')
    case 'Failed': return t('maintenance.jobs.run_failed')
    case 'Canceled': return t('maintenance.jobs.run_canceled')
    default: return t('maintenance.jobs.run_lost')
  }
}

/** 一次运行的语气：成功绿、失败与失联红、运行中黄、取消中性 */
export function runStateTone(state: JobRunState): Tone {
  switch (state) {
    case 'Succeeded': return 'success'
    case 'Failed':
    case 'Lost': return 'danger'
    case 'Running': return 'warning'
    default: return 'neutral'
  }
}

export function runStateOptions(t: Translate): { label: string, value: JobRunState }[] {
  return (['Running', 'Succeeded', 'Failed', 'Canceled', 'Lost'] as const).map(state => ({ label: runStateLabel(t, state), value: state }))
}

/** 一次运行怎么触发的：按计划、手动（立即执行）或节点失联后的恢复 */
export function runTriggerLabel(t: Translate, trigger: JobTrigger): string {
  switch (trigger) {
    case 'Manual': return t('maintenance.jobs.trigger_modes.manual')
    case 'Recovery': return t('maintenance.jobs.trigger_modes.recovery')
    default: return t('maintenance.jobs.trigger_modes.schedule')
  }
}

export function triggerStateLabel(t: Translate, state: TriggerState): string {
  switch (state) {
    case 'Paused': return t('maintenance.jobs.trigger_states.paused')
    case 'Acquired': return t('maintenance.jobs.trigger_states.acquired')
    case 'Blocked': return t('maintenance.jobs.trigger_states.blocked')
    case 'Complete': return t('maintenance.jobs.trigger_states.complete')
    case 'Error': return t('maintenance.jobs.trigger_states.error')
    default: return t('maintenance.jobs.trigger_states.waiting')
  }
}

/** 触发器状态的语气：出错红、即将触发与被阻塞黄、暂停与已结束中性、等待蓝 */
export function triggerStateTone(state: TriggerState): Tone {
  switch (state) {
    case 'Error': return 'danger'
    case 'Acquired':
    case 'Blocked': return 'warning'
    case 'Paused':
    case 'Complete': return 'neutral'
    default: return 'info'
  }
}

/** 耗时（4.x 同）：不足一秒按毫秒，否则按秒保留两位；还在运行（null）为 - */
export function formatDuration(ms: null | number | undefined): string {
  if (ms === null || ms === undefined || !Number.isFinite(ms))
    return '-'
  return ms >= 1000 ? `${(ms / 1000).toFixed(2)} s` : `${ms} ms`
}

/** 报错文案：后端的问题详情只取正文（已按请求语言本地化），其余取消息；都没有时为空串，由调用点补上兜底文案 */
export function errorText(error: unknown): string {
  if (error instanceof ApiError)
    return error.detail
  return error instanceof Error ? error.message : String(error ?? '')
}

/** 批量暂停 / 恢复只回失败项的错误码：认得的翻成文案，其余原样 */
export function batchFailureMessage(t: Translate, code: string): string {
  return code === 'MAINTENANCE_NOTFOUND_001' ? t('maintenance.jobs.job_not_found') : code
}

/** 批量结果里有失败项即抛错：逐项原因去重后拼成一条 */
export function assertBatch(t: Translate, result: { failed: readonly { code: string }[] }): void {
  if (result.failed.length > 0)
    throw new Error([...new Set(result.failed.map(failure => batchFailureMessage(t, failure.code)))].join('; '))
}

/**
 * 列表的后端筛选：关键字匹配作业名、显示名与说明；触发类型与启停精确匹配；
 * 运行状态不是后端字段，由 runStatusFilters 换成 running / paused 两条。
 */
export const JOB_QUERY: PageQueryMapping = {
  filters: { kind: 'eq', paused: 'eq' },
}

/** SchemaPage 的查询 → jobs.list 的分页请求 */
export function toJobsRequest(params: SchemaQueryParams): PageRequest {
  const { runState, ...filters } = params.filters
  const request = toPageRequest({ ...params, filters }, JOB_QUERY)
  request.filters.push(...runStatusFilters(runState))
  return request
}

/** SchemaPage 的资源：分页与启停（启用即恢复、停用即暂停；没有新增、编辑与删除——作业在代码里登记） */
export function jobResource(t: Translate): SchemaResource<JobRow> {
  return {
    page: async (params: SchemaQueryParams) => {
      const result = await jobsApi.list(toJobsRequest(params))
      return toSchemaPageResult({ ...result, items: result.items.map(toJobRow) }, params)
    },
    updateStatus: async (id, enabled) => {
      const request = { ids: [String(id)] }
      assertBatch(t, enabled ? await jobsApi.resume(request) : await jobsApi.pause(request))
    },
  }
}

/** 详情：按作业名取最新一份 */
export async function getJob(name: string): Promise<JobRow> {
  return toJobRow(await jobsApi.get({ id: name }))
}

/** 立即执行：排进一个一次性的手动触发器，由任一节点执行、不等结果；返回的触发器名即运行历史里的触发器名 */
export async function triggerJob(name: string): Promise<string> {
  return (await jobsApi.trigger({ id: name })).trigger
}

/** 执行日志抽屉的查询：按作业过滤，状态单选，关键字匹配触发器名、节点与错误（替 4.x 的批次号）；后端按计划时间倒序 */
export interface JobRunsQuery {
  job: string
  page: number
  pageSize: number
  state: JobRunState | null
  keyword: string
}

export function toJobRunsRequest(query: JobRunsQuery): PageRequest {
  const filters: PageFilter[] = [{ field: 'job', operator: 'eq', value: query.job }]
  if (query.state)
    filters.push({ field: 'state', operator: 'eq', value: query.state })
  const keyword = query.keyword.trim()
  return {
    page: { index: query.page, size: query.pageSize },
    filters,
    sorts: [],
    keyword: keyword || null,
  }
}

export async function listJobRuns(query: JobRunsQuery): Promise<{ items: JobRunRow[], total: number }> {
  const result = await jobRunsApi.list(toJobRunsRequest(query))
  return { items: result.items, total: Number(result.total) }
}
