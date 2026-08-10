/**
 * 打印数据源注册表。
 * 职责：维护代码注册的数据源与字段目录，并在重复编码或无效字段进入设计器前立即失败。
 */
import type { PrintDataSourceDefinition, PrintFieldDefinition } from './types'

const dataSources = new Map<string, PrintDataSourceDefinition<unknown>>()
const SUPPORTED_SAMPLE_INPUT_TYPES = new Set(['boolean', 'date', 'datetime', 'number', 'text', 'textarea'])

/**
 * 注册一个打印数据源；重复编码立即抛错，避免后注册模块静默覆盖已有字段契约。
 * @param definition 数据源定义。
 * @returns 只读规范化定义。
 * @throws 数据源编码、名称、字段或样例工厂无效，或编码已注册。
 */
export function registerPrintDataSource<T>(
  definition: PrintDataSourceDefinition<T>,
): Readonly<PrintDataSourceDefinition<T>> {
  if (!definition || typeof definition !== 'object')
    throw new TypeError('打印数据源定义不能为空。')

  const code = normalizeCode(definition.code, '打印数据源编码')
  if (dataSources.has(code))
    throw new Error(`打印数据源编码已注册：${code}`)
  if (!definition.name?.trim())
    throw new Error(`打印数据源 ${code} 的名称不能为空。`)
  if (typeof definition.createSampleData !== 'function')
    throw new TypeError(`打印数据源 ${code} 必须提供 createSampleData。`)

  const fieldKeys = new Set<string>()
  const fields = definition.fields.map((field, index) => normalizeField(code, field, index, fieldKeys))
  if (fields.length === 0)
    throw new Error(`打印数据源 ${code} 至少需要一个字段。`)

  const normalized = Object.freeze({
    ...definition,
    code,
    name: definition.name.trim(),
    fields: Object.freeze(fields),
  })
  dataSources.set(code, normalized as PrintDataSourceDefinition<unknown>)
  return normalized
}

/**
 * 读取已注册数据源。
 * @param code 数据源编码。
 * @returns 数据源；未注册时返回 undefined。
 */
export function getPrintDataSource<T = Record<string, unknown>>(
  code: null | string | undefined,
): Readonly<PrintDataSourceDefinition<T>> | undefined {
  const normalized = code?.trim()
  if (!normalized)
    return undefined
  return dataSources.get(normalized) as Readonly<PrintDataSourceDefinition<T>> | undefined
}

/**
 * 确认数据源已注册。
 * @param code 模板声明的数据源编码。
 * @returns 已注册数据源。
 * @throws 编码无效或数据源未注册。
 */
export function requirePrintDataSource<T = Record<string, unknown>>(
  code: string,
): Readonly<PrintDataSourceDefinition<T>> {
  const normalized = normalizeCode(code, '打印数据源编码')
  const definition = getPrintDataSource<T>(normalized)
  if (!definition)
    throw new Error(`打印数据源未注册：${normalized}。请先调用 registerPrintDataSource。`)
  return definition
}

/**
 * 列出当前会话全部数据源。
 * @returns 按编码排序的只读快照。
 */
export function listPrintDataSources(): readonly Readonly<PrintDataSourceDefinition<unknown>>[] {
  return [...dataSources.values()].sort((left, right) => left.code.localeCompare(right.code))
}

/**
 * 生成字段对应的稳定 hiprint tid。
 * @param dataSourceCode 数据源编码。
 * @param fieldKey 字段路径。
 * @returns provider 与拖拽元素共用的 tid。
 */
export function getPrintFieldTid(dataSourceCode: string, fieldKey: string): string {
  return `xihan.${dataSourceCode}.${fieldKey}`
}

/** 规范化并校验编码。 */
function normalizeCode(value: string, label: string): string {
  const normalized = value?.trim()
  if (!normalized)
    throw new Error(`${label}不能为空。`)
  if (normalized.length > 100 || /\s/u.test(normalized))
    throw new Error(`${label}不能超过 100 个字符且不能包含空白字符。`)
  return normalized
}

/** 校验字段以及 table 列契约。 */
function normalizeField(
  dataSourceCode: string,
  field: PrintFieldDefinition,
  index: number,
  fieldKeys: Set<string>,
): Readonly<PrintFieldDefinition> {
  const key = normalizeCode(field?.key, `打印数据源 ${dataSourceCode} 第 ${index + 1} 个字段编码`)
  if (fieldKeys.has(key))
    throw new Error(`打印数据源 ${dataSourceCode} 存在重复字段：${key}`)
  fieldKeys.add(key)
  if (!field.label?.trim())
    throw new Error(`打印字段 ${key} 的名称不能为空。`)

  const kind = field.kind ?? 'text'
  if (!['barcode', 'image', 'qrcode', 'table', 'text'].includes(kind))
    throw new Error(`打印字段 ${key} 的类型无效：${kind}`)
  if (kind === 'table' && (!field.columns || field.columns.length === 0))
    throw new Error(`明细表字段 ${key} 至少需要一列。`)

  const columns = kind === 'table'
    ? Object.freeze(field.columns!.map((column) => {
        const columnField = normalizeCode(column.field, `明细表 ${key} 列字段`)
        if (!column.title?.trim())
          throw new Error(`明细表 ${key} 的列标题不能为空。`)
        return Object.freeze({
          ...column,
          field: columnField,
          title: column.title.trim(),
          inputType: normalizeSampleInputType(column.inputType, `明细表 ${key} 列 ${columnField}`),
          placeholder: normalizePlaceholder(column.placeholder),
        })
      }))
    : undefined

  return Object.freeze({
    ...field,
    key,
    label: field.label.trim(),
    kind,
    columns,
    inputType: normalizeSampleInputType(field.inputType, `打印字段 ${key}`),
    placeholder: normalizePlaceholder(field.placeholder),
  })
}

/** 校验可选模拟数据控件类型，防止注册时引入 UI 无法识别的字符串。 */
function normalizeSampleInputType<T extends string>(value: T | undefined, label: string): T | undefined {
  if (value === undefined)
    return undefined
  if (!SUPPORTED_SAMPLE_INPUT_TYPES.has(value))
    throw new Error(`${label} 的模拟数据控件类型无效：${value}`)
  return value
}

/** 规范化可选占位文案；纯空白等同于未配置。 */
function normalizePlaceholder(value: string | undefined): string | undefined {
  return value?.trim() || undefined
}
