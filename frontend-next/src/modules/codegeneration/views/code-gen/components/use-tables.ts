import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { ResponseOf } from '~/contracts'
import { codegenTables, toPageRequest, toSchemaPageResult } from '~/contracts'

export type TableRow = ResponseOf<'codegen-tables.list'>['items'][number]
export type TableDetail = ResponseOf<'codegen-tables.get'>
export type ColumnRow = ResponseOf<'codegen-table-columns.list'>['items'][number]
export type TemplateType = TableRow['templateType']
export type DeliveryType = TableRow['deliveryType']
export type GenerationStatus = TableRow['generationStatus']
export type GenerationScope = TableRow['scope']
export type GenerationTarget = TableRow['target']
export type HtmlType = ColumnRow['htmlType']
export type QueryType = ColumnRow['queryType']
export type DictSelectorType = NonNullable<ColumnRow['dictSelectorType']>

/** 可裁剪的操作（列表与详情始终生成），与后端 CodeGenActions.All 同集同序 */
export const ALL_ACTIONS = ['create', 'update', 'delete', 'export', 'import', 'status', 'print'] as const
export type TableAction = typeof ALL_ACTIONS[number]
/** 没配置时的缺省集合（启停与打印要显式勾选），同后端 CodeGenActions.Defaults */
export const DEFAULT_ACTIONS: readonly TableAction[] = ['create', 'update', 'delete', 'export', 'import']

export const TEMPLATE_TYPES: readonly TemplateType[] = ['Single', 'Tree', 'MasterDetail']
export const DELIVERY_TYPES: readonly DeliveryType[] = ['Zip', 'Project']
export const GENERATION_STATUSES: readonly GenerationStatus[] = ['NotGenerated', 'Generated', 'Failed']
export const GENERATION_SCOPES: readonly GenerationScope[] = ['All', 'BackendOnly', 'FrontendOnly']
export const GENERATION_TARGETS: readonly GenerationTarget[] = ['NewPackage', 'ExistingPackage']
export const HTML_TYPES: readonly HtmlType[] = ['Input', 'Textarea', 'Select', 'Radio', 'Checkbox', 'DatePicker', 'DateTimePicker', 'TimePicker', 'ImageUpload', 'FileUpload', 'Editor', 'InputNumber', 'Switch', 'TreeSelect']
export const QUERY_TYPES: readonly QueryType[] = ['Equal', 'NotEqual', 'GreaterThan', 'GreaterThanOrEqual', 'LessThan', 'LessThanOrEqual', 'Like', 'LikeLeft', 'LikeRight', 'Between', 'In', 'NotIn']
export const DICT_SELECTOR_TYPES: readonly DictSelectorType[] = ['DictSelector', 'EnumSelector', 'ConstSelector', 'TableSelector', 'TreeSelector']

/** 枚举值的文案键：值是契约字面量，键按 snake_case 落在 codegeneration.code_gen.enums 下 */
export function enumKey(group: string, value: string): string {
  return `codegeneration.code_gen.enums.${group}.${value.replace(/([a-z0-9])([A-Z])/g, '$1_$2').toLowerCase()}`
}

/** 后端逗号分隔串 → 多选数组；空即缺省集合（照 4.x） */
export function parseEnabledActions(raw?: string | null): TableAction[] {
  if (!raw)
    return [...DEFAULT_ACTIONS]
  const selected = raw.split(',').map(item => item.trim().toLowerCase()).filter(Boolean)
  return ALL_ACTIONS.filter(action => selected.includes(action))
}

/** 勾选恰为缺省集合（或一个都没勾）时提交空，与没配置同义（照 4.x） */
export function serializeEnabledActions(actions: readonly TableAction[]): null | string {
  const isDefault = actions.length === 0 || (actions.length === DEFAULT_ACTIONS.length && DEFAULT_ACTIONS.every(action => actions.includes(action)))
  return isDefault ? null : ALL_ACTIONS.filter(action => actions.includes(action)).join(',')
}

/** 与后端同一条规则：导入逐行调新建，只勾导入会产出一个调不通的导入按钮 */
export function actionsConflict(actions: readonly TableAction[]): boolean {
  return actions.includes('import') && !actions.includes('create')
}

/**
 * SchemaPage 的资源：分页与删除
 * 生成模式、交付方式、生成结果与状态多选合成 in；关键字匹配表名、类名与表注释
 */
export function tablesResource(): SchemaResource<TableRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(
      await codegenTables.list(toPageRequest(params, { multiples: ['templateType', 'deliveryType', 'generationStatus', 'status'] })),
      params,
    ),
  }
}
