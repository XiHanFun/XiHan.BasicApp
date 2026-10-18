/**
 * 模拟打印数据模型。
 * 职责：按模板引用的字段与代码数据源的字段目录生成模拟数据表单结构，并安全归一、克隆和读写仅存于浏览器内存的模拟数据。
 */
import type { PrintDocument } from '@xihan-print/core'
import type { DataField } from '@xihan-ui/designer-kit'
import type {
  PrintFieldDefinition,
  PrintFieldKind,
  PrintSampleDataSet,
  PrintSampleFormField,
  PrintSampleFormSchema,
  PrintSampleInputType,
  PrintTableColumn,
} from './types'
import { collectDocumentFields } from '@xihan-print/core'
import { requirePrintDataSource } from './data-source-registry'

const DANGEROUS_PATH_SEGMENTS = new Set(['__proto__', 'constructor', 'prototype'])

/**
 * 按模板引用的字段生成模拟数据表单结构。
 * @param document 打印模板。
 * @param dataSourceCode 模板可选的代码数据源编码；为空表示自由模板。
 * @returns 按字段在模板里首次出现的顺序排列、按路径去重的表单结构：数据源登记过的字段用登记的名称与控件，
 * 其余按模板的引用推断（明细表取它下面的列，其它按文本），绑定了数据源时每个未登记字段带一条提示。
 * @throws 非空数据源未注册或字段路径包含原型污染片段。
 * @example
 * const schema = extractPrintSampleFormSchema(document, 'system.print-demo')
 * // schema.fields 只包含模板实际引用的字段。
 */
export function extractPrintSampleFormSchema(document: PrintDocument, dataSourceCode: null | string): PrintSampleFormSchema {
  const normalizedDataSourceCode = dataSourceCode?.trim() || null
  const source = normalizedDataSourceCode ? requirePrintDataSource(normalizedDataSourceCode) : undefined
  const registeredFields = new Map((source?.fields ?? []).map(field => [field.key, field]))
  const fields: PrintSampleFormField[] = []
  const warnings: string[] = []

  for (const used of flattenUsedFields(collectDocumentFields(document), '')) {
    validatePrintSamplePath(used.key)
    const registered = registeredFields.get(used.key)
    if (registered) {
      fields.push(toRegisteredFormField(registered))
      continue
    }
    fields.push(Object.freeze(used))
    if (source)
      warnings.push(`模板字段“${used.key}”未在数据源 ${source.code} 中注册，已按模板的引用生成输入项。`)
  }

  return Object.freeze({
    fields: Object.freeze(fields),
    warnings: Object.freeze(warnings),
  })
}

/**
 * 把代码数据源样例归一为至少一条可编辑记录。
 * @param sample createSampleData 返回的单对象或对象数组。
 * @returns 深克隆后的记录集合；空数组会生成一条空白记录，同时保留数组输出语义。
 * @throws 样例不是普通对象/对象数组，或包含无法结构化克隆的值。
 */
export function normalizePrintSampleData(sample: unknown): PrintSampleDataSet {
  if (Array.isArray(sample)) {
    if (!sample.every(isRecord))
      throw new TypeError('模拟打印数据数组中的每一项都必须是对象。')
    return {
      isCollection: true,
      records: sample.length > 0
        ? sample.map(record => clonePrintSampleRecord(record))
        : [{}],
    }
  }
  if (!isRecord(sample))
    throw new TypeError('模拟打印数据必须是对象或对象数组。')
  return {
    isCollection: false,
    records: [clonePrintSampleRecord(sample)],
  }
}

/**
 * 创建只包含当前模板表单字段的空白记录。
 * @param fields 已提取的模板表单字段。
 * @returns 可直接用于新增多记录的普通对象。
 * @throws 字段路径无效或包含危险片段。
 */
export function createBlankPrintSampleRecord(
  fields: readonly PrintSampleFormField[],
): Record<string, unknown> {
  const record: Record<string, unknown> = {}
  for (const field of fields) {
    const value = field.kind === 'table'
      ? []
      : field.inputType === 'boolean'
        ? false
        : field.inputType === 'number'
          ? null
          : ''
    setPrintSampleValue(record, field.key, value)
  }
  return record
}

/**
 * 为直打等无需人工填写的入口创建默认模拟数据。
 * @param dataSourceCode 可选代码数据源；存在时调用其样例工厂。
 * @returns 与数据源样例一致的单对象/对象数组；自由模板为一个空白对象。
 * @throws 数据源未注册或样例结构非法。
 */
export async function createDefaultPrintSampleData(
  dataSourceCode: null | string,
): Promise<Record<string, unknown> | Record<string, unknown>[]> {
  const source = dataSourceCode?.trim() ? requirePrintDataSource(dataSourceCode.trim()) : undefined
  const sample = source ? await source.createSampleData() : {}
  const dataSet = normalizePrintSampleData(sample)
  return createPrintSamplePayload(dataSet.records, dataSet.isCollection)
}

/**
 * 深克隆一条模拟打印记录，确保页签和默认样例之间不存在共享引用。
 * @param record 原始记录。
 * @returns 独立可编辑副本。
 * @throws 记录包含函数、DOM 节点等不可结构化克隆值。
 */
export function clonePrintSampleRecord(
  record: Record<string, unknown>,
): Record<string, unknown> {
  try {
    return structuredClone(record)
  }
  catch (error) {
    throw new TypeError('模拟打印数据包含无法复制的值，请仅返回可结构化克隆的数据。', { cause: error })
  }
}

/**
 * 读取点路径字段值。
 * @param record 模拟打印记录。
 * @param path 例如 customer.name 的字段路径。
 * @returns 路径不存在时返回 undefined。
 * @throws 路径无效或包含原型污染片段。
 */
export function getPrintSampleValue(record: Record<string, unknown>, path: string): unknown {
  const segments = validatePrintSamplePath(path)
  let current: unknown = record
  for (const segment of segments) {
    if (!isRecord(current))
      return undefined
    current = current[segment]
  }
  return current
}

/**
 * 安全写入点路径字段值。
 * @param record 模拟打印记录；本方法会就地更新，便于 Vue 代理保持响应式。
 * @param path 例如 customer.name 的字段路径。
 * @param value 新值。
 * @returns 无返回值。
 * @throws 路径无效或包含原型污染片段。
 */
export function setPrintSampleValue(
  record: Record<string, unknown>,
  path: string,
  value: unknown,
): undefined {
  const segments = validatePrintSamplePath(path)
  let current = record
  for (const segment of segments.slice(0, -1)) {
    const next = current[segment]
    if (!isRecord(next))
      current[segment] = {}
    current = current[segment] as Record<string, unknown>
  }
  current[segments.at(-1)!] = value
  return undefined
}

/**
 * 推断普通字段或明细列应使用的控件类型。
 * @param configured 注册表显式配置；存在时优先使用。
 * @param sampleValue 当前记录的样例值。
 * @returns 稳定的模拟数据控件类型；日期字符串不会被猜测，避免误判业务编码。
 */
export function inferPrintSampleInputType(
  configured: PrintSampleInputType | undefined,
  sampleValue: unknown,
): PrintSampleInputType {
  if (configured)
    return configured
  if (typeof sampleValue === 'number')
    return 'number'
  if (typeof sampleValue === 'boolean')
    return 'boolean'
  return 'text'
}

/**
 * 把编辑记录转换为公共预览 API 所需的对象或对象数组。
 * @param records UI 内存中的记录数据，不包含组件内部 UUID。
 * @param isCollection 是否保持顶层数组语义。
 * @returns 独立克隆的预览载荷。
 * @throws 记录为空或包含不可克隆值。
 */
export function createPrintSamplePayload(
  records: readonly Record<string, unknown>[],
  isCollection: boolean,
): Record<string, unknown> | Record<string, unknown>[] {
  if (records.length === 0)
    throw new RangeError('模拟打印数据至少需要一条记录。')
  const cloned = records.map(record => clonePrintSampleRecord(record))
  return isCollection ? cloned : cloned[0]!
}

/** 把注册字段复制为不可变表单字段，避免 UI 改写全局数据源元数据。 */
function toRegisteredFormField(field: Readonly<PrintFieldDefinition>): PrintSampleFormField {
  return Object.freeze({
    key: field.key,
    label: field.label,
    kind: (field.kind ?? 'text') as PrintFieldKind,
    inputType: field.inputType,
    placeholder: field.placeholder,
    columns: field.columns,
    registered: true,
  })
}

/**
 * 把模板引用的字段树摊成表单项：对象展开成点路径，数组是明细表、它下面的值字段是列（列路径相对一行），其余按文本。
 * @param fields collectDocumentFields 给出的字段树。
 * @param prefix 上层对象的点路径。
 * @returns 未登记的表单项，registered 为 false。
 */
function flattenUsedFields(fields: readonly DataField[], prefix: string): PrintSampleFormField[] {
  return fields.flatMap((field): PrintSampleFormField[] => {
    const key = prefix ? `${prefix}.${field.name}` : field.name
    if (field.kind === 'object')
      return flattenUsedFields(field.children ?? [], key)
    if (field.kind === 'list')
      return [{ key, label: key, kind: 'table', columns: Object.freeze(listColumns(field.children ?? [], '')), registered: false }]
    return [{ key, label: key, kind: 'text', registered: false }]
  })
}

/** 明细表一行里引用的值字段，嵌套对象展开成点路径；行里再套的数组不出列。 */
function listColumns(fields: readonly DataField[], prefix: string): PrintTableColumn[] {
  return fields.flatMap((field): PrintTableColumn[] => {
    const path = prefix ? `${prefix}.${field.name}` : field.name
    if (field.kind === 'object')
      return listColumns(field.children ?? [], path)
    return field.kind === 'value' ? [{ field: path, title: path }] : []
  })
}

/** 校验并拆分点路径，阻止通过用户模板字段污染对象原型。 */
function validatePrintSamplePath(path: string): string[] {
  const normalized = path?.trim()
  if (!normalized)
    throw new TypeError('模拟打印字段路径不能为空。')
  const segments = normalized.split('.').map(segment => segment.trim())
  if (segments.some(segment => !segment))
    throw new TypeError(`模拟打印字段路径无效：${normalized}`)
  const dangerous = segments.find(segment => DANGEROUS_PATH_SEGMENTS.has(segment))
  if (dangerous)
    throw new TypeError(`模拟打印字段路径包含不安全片段：${dangerous}`)
  return segments
}

/** 判断普通 JSON 风格对象。 */
function isRecord(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === 'object' && !Array.isArray(value)
}
