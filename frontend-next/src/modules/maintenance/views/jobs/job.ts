import type { Tone } from '@xihan-ui/core'
import type { SchemaQueryParams, SchemaResource, SchemaSelectOption } from '~/components'
import type { JobRetryBackoff, JobRunState, JobScheduleKind, JobSource, JobTrigger, PageQueryMapping, PageRequest, RequestOf, ResponseOf, TriggerState } from '~/contracts'
import { jobRuns as jobRunsApi, jobs as jobsApi, toPageRequest, toSchemaPageResult } from '~/contracts'
import { ApiError } from '~/request'
import { formatSeconds } from '../caches/cache'

export type Translate = (key: string, values?: Record<string, unknown>) => string

/** 一个作业（列表项与 jobs.get 同形） */
export type JobResponse = ResponseOf<'jobs.list'>['items'][number]
export type JobTriggerInfo = JobResponse['triggers'][number]
export type JobRunRow = ResponseOf<'job-runs.list'>['items'][number]
/** jobs.validate-schedule 的结果：规范写法、计划种类与接下来的运行时间（UTC） */
export type SchedulePreview = ResponseOf<'jobs.validate-schedule'>
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

/** 登记来源：代码登记、运行时登记，或代码登记后在运行时改过计划（重启后代码定义没变就保留这次修改） */
export function sourceLabel(t: Translate, source: JobSource): string {
  switch (source) {
    case 'Runtime': return t('maintenance.jobs.sources.runtime')
    case 'Overridden': return t('maintenance.jobs.sources.overridden')
    default: return t('maintenance.jobs.sources.code')
  }
}

/** 登记来源的语气：代码登记中性、运行时登记蓝、已改计划黄（与代码定义不一致，提醒可恢复） */
export function sourceTone(source: JobSource): Tone {
  switch (source) {
    case 'Runtime': return 'info'
    case 'Overridden': return 'warning'
    default: return 'neutral'
  }
}

/** 登记来源的搜索选项：后端按 `source eq` 匹配 */
export function sourceOptions(t: Translate): { label: string, value: JobSource }[] {
  return (['Code', 'Runtime', 'Overridden'] as const).map(source => ({ label: sourceLabel(t, source), value: source }))
}

/** 作业类型的搜索选项：取自 jobs.types（已登记的类型键，按序数排序），键即显示文本；多选时后端按 `jobType in` 匹配 */
export async function loadJobTypeOptions(): Promise<SchemaSelectOption<string>[]> {
  const { items } = await jobsApi.types({})
  return items.map(type => ({ label: type, value: type }))
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

/** 一次运行怎么触发的：按计划、手动（立即执行）、节点失联后的恢复或失败后的重试 */
export function runTriggerLabel(t: Translate, trigger: JobTrigger): string {
  switch (trigger) {
    case 'Manual': return t('maintenance.jobs.trigger_modes.manual')
    case 'Recovery': return t('maintenance.jobs.trigger_modes.recovery')
    case 'Retry': return t('maintenance.jobs.trigger_modes.retry')
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

/** 批量暂停 / 恢复 / 恢复代码计划只回失败项的错误码：认得的翻成文案，其余原样 */
export function batchFailureMessage(t: Translate, code: string): string {
  switch (code) {
    case 'MAINTENANCE_NOTFOUND_001': return t('maintenance.jobs.job_not_found')
    // 恢复代码计划：作业没有代码定义（运行时登记的作业没有可恢复的定义）
    case 'SCHEDULING_NOTFOUND_005': return t('maintenance.jobs.no_code_definition')
    // 删除：代码登记的作业（含改过计划的）不能删
    case 'MAINTENANCE_CONFLICT_004': return t('maintenance.jobs.code_job_undeletable')
    default: return code
  }
}

/** 批量结果里有失败项即抛错：逐项原因去重后拼成一条 */
export function assertBatch(t: Translate, result: { failed: readonly { code: string }[] }): void {
  if (result.failed.length > 0)
    throw new Error([...new Set(result.failed.map(failure => batchFailureMessage(t, failure.code)))].join('; '))
}

/**
 * 列表的后端筛选：关键字匹配作业名、显示名与说明；触发类型、启停与登记来源精确匹配；作业类型多选（`in`，逗号分隔）；
 * 运行状态不是后端字段，由 runStatusFilters 换成 running / paused 两条。
 */
export const JOB_QUERY: PageQueryMapping = {
  filters: { kind: 'eq', paused: 'eq', source: 'eq' },
  multiples: ['jobType'],
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

/** 计划触发器的个数：框架为立即执行（manual-）与故障恢复（recovery-）排进的临时触发器不算，改计划时后端也保留它们 */
export function activeTriggerCount(job: Pick<JobResponse, 'triggers'>): number {
  return job.triggers.filter(trigger => !trigger.name.startsWith('manual-') && !trigger.name.startsWith('recovery-')).length
}

/** 能否修改计划：后端只换唯一一个计划触发器的计划，多个即 MAINTENANCE_CONFLICT_002（没有触发器的按缺省名登记一个） */
export function canReschedule(job: Pick<JobResponse, 'triggers'>): boolean {
  return activeTriggerCount(job) <= 1
}

/** 能否恢复代码计划：只有代码登记后在运行时改过计划的作业（已改计划）有可恢复的代码定义 */
export function canRestore(job: Pick<JobResponse, 'source'>): boolean {
  return job.source === 'Overridden'
}

/** 修改计划弹窗的表单：计划（Cron 表达式或计划写法）与时区（只对 Cron 表达式生效，空即 UTC） */
export interface ScheduleForm {
  expression: string
  timeZone: string
}

/** 弹窗的初值：Cron 触发器拆成表达式与时区，其余取计划的文本写法（every … / once … / manual），没有触发器为空 */
export function scheduleFormOf(job: Pick<JobResponse, 'triggers'>): ScheduleForm {
  const trigger = job.triggers[0]
  if (!trigger)
    return { expression: '', timeZone: '' }
  if (trigger.kind === 'Cron' && trigger.cron)
    return { expression: trigger.cron, timeZone: trigger.timeZone ?? '' }
  return { expression: trigger.schedule, timeZone: '' }
}

/** 计划去空白；时区去空白后留空即不填（后端按 UTC） */
function scheduleParts(form: ScheduleForm): { expression: string, timeZone: null | string } {
  const timeZone = form.timeZone.trim()
  return { expression: form.expression.trim(), timeZone: timeZone || null }
}

/** 修改计划弹窗的表单：计划与时区，加上重试与时限（后端按请求整体替换） */
export type RescheduleForm = ScheduleForm & RetryForm

/** jobs.reschedule 的请求：作业名进路由值 `{id}`，请求体是计划、时区、重试与时限 */
export function toRescheduleRequest(name: string, form: RescheduleForm): RequestOf<'jobs.reschedule'> {
  return { id: name, ...scheduleParts(form), ...retryParts(form) }
}

/** 修改计划、重试与时限：其余登记与暂停状态不变，返回改后的作业 */
export async function rescheduleJob(name: string, form: RescheduleForm): Promise<JobRow> {
  return toJobRow(await jobsApi.reschedule(toRescheduleRequest(name, form)))
}

/** 预览列出的下次运行个数（4.x 的 Cron 构建器同为 5 次） */
export const SCHEDULE_PREVIEW_COUNT = 5

/** 校验计划并列出接下来的运行时间（不改任何作业）；写错即 MAINTENANCE_VALIDATION_002 */
export async function previewSchedule(form: ScheduleForm): Promise<SchedulePreview> {
  return jobsApi.validateSchedule({ ...scheduleParts(form), count: SCHEDULE_PREVIEW_COUNT })
}

/** 恢复代码计划（按作业名批量）：丢弃运行时改过的计划、按代码定义重新登记，暂停状态保留；逐项失败翻成文案抛出 */
export async function restoreJobs(t: Translate, names: readonly string[]): Promise<void> {
  assertBatch(t, await jobsApi.restore({ ids: [...names] }))
}

/** 间隔秒数写成计划的文本写法：`every [d.]hh:mm:ss`（后端按 TimeSpan 的 c 格式解析） */
export function intervalExpression(seconds: number): string {
  const total = Math.max(1, Math.floor(seconds))
  const days = Math.floor(total / 86400)
  const pad = (value: number) => String(value).padStart(2, '0')
  const clock = `${pad(Math.floor(total / 3600) % 24)}:${pad(Math.floor(total / 60) % 60)}:${pad(total % 60)}`
  return `every ${days > 0 ? `${days}.` : ''}${clock}`
}

/** 计划文本里的间隔秒数：不是 `every …` 的写法为空 */
export function intervalSecondsOf(expression: string): number | null {
  const match = /^every\s+(?:(\d+)\.)?(\d{1,2}):(\d{2}):(\d{2})(?:\.\d+)?(?:\s|$)/.exec(expression.trim())
  if (!match)
    return null
  const [, days, hours, minutes, seconds] = match
  return Number(days ?? 0) * 86400 + Number(hours) * 3600 + Number(minutes) * 60 + Number(seconds)
}

// ── 重试与执行时限（4.x 的最大重试次数与超时时间，6.0 另有重试间隔与退避） ─────────

/** 重试次数上限（与框架一致） */
export const MAX_RETRIES = 100

/** 重试等待与执行时限的秒数上限：30 天（与后端一致） */
export const MAX_RETRY_SECONDS = 2_592_000

/** 新设重试时的缺省间隔（与后端请求的缺省一致） */
export const DEFAULT_RETRY_DELAY_SECONDS = 60

/** 重试与执行时限的表单：时限与重试次数留空即不限时、不重试；间隔、退避与等待上限只在重试时用 */
export interface RetryForm {
  timeoutSeconds: null | number
  maxRetries: null | number
  retryDelaySeconds: null | number
  retryBackoff: JobRetryBackoff
  retryMaxDelaySeconds: null | number
}

/** 新建时的初值：不限时、不重试 */
export function blankRetry(): RetryForm {
  return { timeoutSeconds: null, maxRetries: null, retryDelaySeconds: DEFAULT_RETRY_DELAY_SECONDS, retryBackoff: 'Fixed', retryMaxDelaySeconds: null }
}

/** 修改弹窗的初值：作业现在的重试与时限（int64 统一按 Number 读）；不重试时间隔取缺省，便于勾上重试 */
export function retryFormOf(job: Pick<JobResponse, 'retry' | 'timeoutSeconds'>): RetryForm {
  const retry = job.retry
  const seconds = (value: null | number | undefined) => (value === null || value === undefined ? null : Number(value))
  return {
    timeoutSeconds: seconds(job.timeoutSeconds),
    maxRetries: retry ? retry.maxRetries : null,
    retryDelaySeconds: retry ? Number(retry.delaySeconds) : DEFAULT_RETRY_DELAY_SECONDS,
    retryBackoff: retry?.backoff ?? 'Fixed',
    retryMaxDelaySeconds: seconds(retry?.maxDelaySeconds),
  }
}

/** 设了重试次数却没填间隔：保存按钮据此禁用 */
export function retryIncomplete(form: RetryForm): boolean {
  return form.maxRetries !== null && form.retryDelaySeconds === null
}

/** 请求里的重试与时限：不重试时只发时限与空的次数；等待上限只对指数退避有意义，固定间隔不发 */
function retryParts(form: RetryForm): Pick<RequestOf<'jobs.create'>, 'maxRetries' | 'retryBackoff' | 'retryDelaySeconds' | 'retryMaxDelaySeconds' | 'timeoutSeconds'> {
  if (form.maxRetries === null || form.retryDelaySeconds === null)
    return { timeoutSeconds: form.timeoutSeconds, maxRetries: null }
  return {
    timeoutSeconds: form.timeoutSeconds,
    maxRetries: form.maxRetries,
    retryDelaySeconds: form.retryDelaySeconds,
    retryBackoff: form.retryBackoff,
    retryMaxDelaySeconds: form.retryBackoff === 'Exponential' ? form.retryMaxDelaySeconds : null,
  }
}

/** 等待方式的文案 */
export function backoffLabel(t: Translate, backoff: JobRetryBackoff): string {
  return backoff === 'Exponential' ? t('maintenance.jobs.retry_backoffs.exponential') : t('maintenance.jobs.retry_backoffs.fixed')
}

/** 等待方式的选项 */
export function backoffOptions(t: Translate): { label: string, value: JobRetryBackoff }[] {
  return (['Fixed', 'Exponential'] as const).map(value => ({ label: backoffLabel(t, value), value }))
}

/** 详情里的重试策略：不重试；最多几次、每次等多久；或最多几次、从多久起翻倍（有上限时带上） */
export function retryText(t: Translate, job: Pick<JobResponse, 'retry'>): string {
  const retry = job.retry
  if (!retry)
    return t('maintenance.jobs.no_retry')
  const delay = formatSeconds(t, Number(retry.delaySeconds))
  if (retry.backoff !== 'Exponential')
    return t('maintenance.jobs.retry_fixed_value', { count: retry.maxRetries, delay })
  if (retry.maxDelaySeconds === null || retry.maxDelaySeconds === undefined)
    return t('maintenance.jobs.retry_exponential_value', { count: retry.maxRetries, delay })
  return t('maintenance.jobs.retry_exponential_capped_value', { count: retry.maxRetries, delay, max: formatSeconds(t, Number(retry.maxDelaySeconds)) })
}

/** 详情里的执行时限：不限或可读时长 */
export function timeoutText(t: Translate, job: Pick<JobResponse, 'timeoutSeconds'>): string {
  return job.timeoutSeconds === null || job.timeoutSeconds === undefined ? t('maintenance.jobs.no_timeout') : formatSeconds(t, Number(job.timeoutSeconds))
}

/** 新建作业的表单：作业名、类型、计划与时区、参数（JSON 对象的文本）、并发、重试与时限及说明 */
export interface JobCreateForm extends ScheduleForm, RetryForm {
  name: string
  jobType: string
  data: string
  allowConcurrent: boolean
  description: string
}

/** jobs.create 的请求：空的参数与说明不填 */
export function toCreateRequest(form: JobCreateForm): RequestOf<'jobs.create'> {
  const data = form.data.trim()
  const description = form.description.trim()
  return {
    name: form.name.trim(),
    jobType: form.jobType,
    ...scheduleParts(form),
    data: data || null,
    allowConcurrent: form.allowConcurrent,
    ...retryParts(form),
    description: description || null,
  }
}

/** 新建运行时作业 */
export async function createJob(form: JobCreateForm): Promise<JobResponse> {
  return jobsApi.create(toCreateRequest(form))
}

/** 删除一个运行时作业（DELETE jobs?ids=）；代码登记的与不存在的翻成文案抛出 */
export async function deleteJob(t: Translate, name: string): Promise<void> {
  assertBatch(t, await jobsApi.delete({ ids: [name] }))
}

/** 能否删除：只有运行时登记的作业 */
export function canDelete(job: Pick<JobResponse, 'source'>): boolean {
  return job.source === 'Runtime'
}

/** 修改计划的常用预设（沿用 4.x Cron 构建器的六个；周按 Unix 约定，1 是周一） */
export function schedulePresets(t: Translate): { label: string, value: string }[] {
  return [
    { label: t('maintenance.jobs.presets.per_minute'), value: '0 * * * * ?' },
    { label: t('maintenance.jobs.presets.per_5min'), value: '0 0/5 * * * ?' },
    { label: t('maintenance.jobs.presets.hourly'), value: '0 0 * * * ?' },
    { label: t('maintenance.jobs.presets.daily'), value: '0 0 0 * * ?' },
    { label: t('maintenance.jobs.presets.weekly'), value: '0 0 0 ? * 1' },
    { label: t('maintenance.jobs.presets.monthly'), value: '0 0 0 1 * ?' },
  ]
}

/** 执行日志「是否出错」的选项：后端按 `failed eq true / false`（有无错误码）筛 */
export function runFailedOptions(t: Translate): { label: string, value: string }[] {
  return [
    { label: t('maintenance.jobs.log_failed_options.yes'), value: 'true' },
    { label: t('maintenance.jobs.log_failed_options.no'), value: 'false' },
  ]
}

/**
 * 执行日志抽屉的查询：按作业过滤，状态单选，是否出错（有无错误码）、触发器名与节点精确匹配，
 * 关键字匹配触发器名、节点与错误（替 4.x 的批次号）；后端按计划时间倒序，总数为真实总数。
 */
export interface JobRunsQuery {
  job: string
  page: number
  pageSize: number
  state: JobRunState | null
  failed: boolean | null
  triggerName: string
  node: string
  keyword: string
}

export function toJobRunsRequest(query: JobRunsQuery): PageRequest {
  const filters: PageFilter[] = [{ field: 'job', operator: 'eq', value: query.job }]
  if (query.state)
    filters.push({ field: 'state', operator: 'eq', value: query.state })
  if (query.failed !== null)
    filters.push({ field: 'failed', operator: 'eq', value: String(query.failed) })
  const triggerName = query.triggerName.trim()
  if (triggerName)
    filters.push({ field: 'triggerName', operator: 'eq', value: triggerName })
  const node = query.node.trim()
  if (node)
    filters.push({ field: 'node', operator: 'eq', value: node })
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
