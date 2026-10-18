/**
 * 模拟打印数据模型。
 * 职责：按代码数据源的字段目录生成模拟数据表单结构，并安全归一、克隆和读写仅存于浏览器内存的模拟数据。
 */
import type {
  PrintFieldDefinition,
  PrintFieldKind,
  PrintSampleDataSet,
  PrintSampleFormField,
  PrintSampleFormSchema,
  PrintSampleInputType,
} from './types'
import { getPrintDataSource, requirePrintDataSource } from './data-source-registry'

const DANGEROUS_PATH_SEGMENTS = new Set(['__proto__', 'constructor', 'prototype'])

/**
 * 按模板绑定的代码数据源生成模拟数据表单结构。
 * @param dataSourceCode 模板可选的代码数据源编码；为空表示自由模板。
 * @returns 按数据源字段顺序排列的表单结构；自由模板没有登记字段，表单为空并带一条提示。
 * @throws 非空数据源未注册或字段路径包含原型污染片段。
 * @example
 * const schema = extractPrintSampleFormSchema('system.print-demo')
 * // schema.fields 为数据源登记的全部字段。
 */
export function extractPrintSampleFormSchema(dataSourceCode: null | string): PrintSampleFormSchema {
  const normalizedDataSourceCode = dataSourceCode?.trim() || null
  if (!normalizedDataSourceCode) {
    return Object.freeze({
      fields: Object.freeze([]),
      warnings: Object.freeze(['自由模板没有登记字段，样例数据按空记录打印；需要填写样例时请给模板选择数据源。']),
    })
  }
  const source = requirePrintDataSource(normalizedDataSourceCode)
  const fields = source.fields.map((field) => {
    validatePrintSamplePath(field.key)
    return toRegisteredFormField(field)
  })
  return Object.freeze({
    fields: Object.freeze(fields),
    warnings: Object.freeze([]),
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
 * @throws 数据源未注册、样例结构非法或字段路径不安全。
 */
export async function createDefaultPrintSampleData(
  dataSourceCode: null | string,
): Promise<Record<string, unknown> | Record<string, unknown>[]> {
  const schema = extractPrintSampleFormSchema(dataSourceCode)
  const source = getPrintDataSource(dataSourceCode)
  const sample = source
    ? await source.createSampleData()
    : createBlankPrintSampleRecord(schema.fields)
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
