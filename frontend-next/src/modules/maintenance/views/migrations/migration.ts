import type { Translate } from '../jobs/job'
import type { LogDetailSchema } from '@/modules/auditing/components/log-detail.types'
import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { PageQueryMapping, PageRequest, ResponseOf } from '~/contracts'
import type { QueryFilter } from '~/types/contracts'
import { booleanTone } from '@/modules/auditing/components/log-tones'
import { migrations as migrationsApi, toPageRequest, toSchemaPageResult } from '~/contracts'
import { QueryOperator } from '~/types/contracts'

/** 升级脚本的一次执行记录 */
export type MigrationRow = ResponseOf<'migrations.list'>['items'][number]
type PageFilter = PageRequest['filters'][number]

/** 执行时间的区间筛选另行换算（后端收 `ge` / `lt`，PageQueryMapping 的区间拆成的是 `ge` / `le`） */
const EXECUTED_AT = 'executedAt'

/**
 * 列表的后端筛选：关键字匹配版本、脚本名与节点；版本、脚本名与节点按包含匹配，执行结果精确匹配。
 * 缺省（不排序）由后端按执行时间倒序。
 */
export const MIGRATION_QUERY: PageQueryMapping = {
  filters: { version: 'contains', scriptName: 'contains', nodeName: 'contains', success: 'eq' },
}

/** 执行时间的区间 → `ge 起点` 与 `lt 终点后 1 毫秒`：区间选择器给的终点是含在内的那一刻 */
export function executedAtFilters(condition: QueryFilter): PageFilter[] {
  const [start, end] = (condition.values ?? []).map(value => String(value))
  const endAt = end ? Date.parse(end) : Number.NaN
  if (condition.operator !== QueryOperator.Between || !start || Number.isNaN(endAt))
    throw new Error(`执行时间的筛选只认区间：${JSON.stringify(condition)}`)
  return [
    { field: EXECUTED_AT, operator: 'ge', value: start },
    { field: EXECUTED_AT, operator: 'lt', value: new Date(endAt + 1).toISOString() },
  ]
}

/** SchemaPage 的查询 → migrations.list 的分页请求 */
export function toMigrationsRequest(params: SchemaQueryParams): PageRequest {
  const filters = { ...params.filters }
  // 区间的原始值已由 conditionFilters 下发
  delete filters[EXECUTED_AT]
  const conditions = params.conditionFilters ?? []
  const request = toPageRequest({ ...params, filters, conditionFilters: conditions.filter(condition => condition.field !== EXECUTED_AT) }, MIGRATION_QUERY)
  for (const condition of conditions.filter(item => item.field === EXECUTED_AT))
    request.filters.push(...executedAtFilters(condition))
  return request
}

export function successLabel(t: Translate, success: boolean): string {
  return success ? t('maintenance.migrations.success_yes') : t('maintenance.migrations.success_no')
}

/** 执行结果的搜索选项：后端按 `success eq true / false` 匹配 */
export function successOptions(t: Translate): { label: string, value: string }[] {
  return [
    { label: successLabel(t, true), value: 'true' },
    { label: successLabel(t, false), value: 'false' },
  ]
}

/** SchemaPage 的资源：只有分页（升级记录由升级执行器写入，只读） */
export function migrationResource(): SchemaResource<MigrationRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(await migrationsApi.list(toMigrationsRequest(params)), params),
  }
}

/** 详情：按记录 ID 取（含失败原因） */
export function getMigration(id: string): Promise<MigrationRow> {
  return migrationsApi.get({ id })
}

/**
 * 详情抽屉的定义（与审计包的日志详情同一套写法）：抬头是版本 · 脚本、执行结果与耗时，副信息是执行时间与节点；
 * 记录 ID 可复制，失败原因排在最后。
 */
export function migrationDetailSchema(t: Translate): LogDetailSchema {
  return {
    header: {
      title: ['version', 'scriptName'],
      status: 'success',
      badges: ['durationMs'],
      meta: ['executedAt', 'nodeName'],
    },
    fields: [
      { key: 'version', label: t('maintenance.migrations.version') },
      { key: 'scriptName', label: t('maintenance.migrations.script_name') },
      { key: 'success', label: t('maintenance.migrations.success'), type: 'boolean', trueText: t('maintenance.migrations.success_yes'), falseText: t('maintenance.migrations.success_no'), tone: booleanTone },
      { key: 'executedAt', label: t('maintenance.migrations.executed_time'), type: 'date' },
      { key: 'durationMs', label: t('maintenance.migrations.duration'), type: 'duration' },
      { key: 'nodeName', label: t('maintenance.migrations.node_name') },
      { key: 'id', label: t('maintenance.migrations.record_id'), group: 'trace', mono: true },
      { key: 'errorMessage', label: t('maintenance.migrations.error_message'), type: 'code' },
    ],
  }
}
