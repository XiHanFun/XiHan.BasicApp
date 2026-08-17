/**
 * 模板驱动的模拟打印数据模型。
 * 职责：从 hiprint 标准字段绑定提取表单结构，并安全归一、克隆和读写仅存于浏览器内存的模拟数据。
 */
import type {
  PrintFieldDefinition,
  PrintFieldKind,
  PrintSampleDataSet,
  PrintSampleFormField,
  PrintSampleFormSchema,
  PrintSampleInputType,
  PrintTableColumn,
} from './types'
import { getPrintDataSource, requirePrintDataSource } from './data-source-registry'

const DANGEROUS_PATH_SEGMENTS = new Set(['__proto__', 'constructor', 'prototype'])

/** hiprint 元素绑定及其视觉排序信息。 */
interface PrintElementBinding {
  columns?: readonly PrintTableColumn[]
  elementIndex: number
  field: string
  inputType?: PrintSampleInputType
  kind: PrintFieldKind
  label: string
  left: number
  panelIndex: number
  placeholder?: string
  top: number
}

/**
 * 从模板的标准 field/tid 绑定生成模拟数据表单结构。
 * @param template hiprint 根模板对象。
 * @param dataSourceCode 当前模板可选的代码数据源编码；为空时按模板自身元数据推断。
 * @returns 按面板、纵坐标和横坐标排序，并按字段路径去重的表单结构。
 * @throws 非空数据源未注册、模板根结构无效或字段路径包含原型污染片段。
 * @example
 * const schema = extractPrintSampleFormSchema(templateJson, 'system.print-demo')
 * // schema.fields 将只包含实际绑定到模板元素的业务字段。
 */
export function extractPrintSampleFormSchema(
  template: unknown,
  dataSourceCode: null | string,
): PrintSampleFormSchema {
  const normalizedDataSourceCode = dataSourceCode?.trim() || null
  const source = normalizedDataSourceCode
    ? requirePrintDataSource(normalizedDataSourceCode)
    : undefined
  const panels = readTemplatePanels(template)
  const bindings = panels.flatMap((panel, panelIndex) => readPanelBindings(panel, panelIndex, source?.code ?? null))
    .sort(compareBindings)
  const registeredFields = new Map((source?.fields ?? []).map(field => [field.key, field]))
  const seenFields = new Set<string>()
  const warnings: string[] = []
  const fields: PrintSampleFormField[] = []

  for (const binding of bindings) {
    validatePrintSamplePath(binding.field)
    if (seenFields.has(binding.field))
      continue
    seenFields.add(binding.field)

    const registered = registeredFields.get(binding.field)
    if (registered) {
      fields.push(toRegisteredFormField(registered))
      continue
    }

    // 自由字段从元素标准元数据推断；已选数据源时也允许额外字段，但明确提示契约未覆盖。
    fields.push(toInferredFormField(binding))
    if (source)
      warnings.push(`模板字段“${binding.field}”未在数据源 ${source.code} 中注册，已按模板元数据生成输入项。`)
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
 * @param template hiprint 根模板对象，用于自由模式提取字段。
 * @param dataSourceCode 可选代码数据源；存在时优先调用其样例工厂。
 * @returns 与数据源样例一致的单对象/对象数组，或按自由字段生成的单个空白对象。
 * @throws 数据源未注册、样例结构非法、模板损坏或字段路径不安全。
 */
export async function createDefaultPrintSampleData(
  template: unknown,
  dataSourceCode: null | string,
): Promise<Record<string, unknown> | Record<string, unknown>[]> {
  const schema = extractPrintSampleFormSchema(template, dataSourceCode)
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

/** 读取并校验 hiprint 根模板面板。 */
function readTemplatePanels(template: unknown): Record<string, unknown>[] {
  if (!isRecord(template) || !Array.isArray(template.panels) || !template.panels.every(isRecord))
    throw new TypeError('打印模板根对象必须包含有效的 panels 数组。')
  return template.panels
}

/** 从一个面板提取带视觉坐标的标准字段绑定。 */
function readPanelBindings(
  panel: Record<string, unknown>,
  panelIndex: number,
  dataSourceCode: null | string,
): PrintElementBinding[] {
  if (!Array.isArray(panel.printElements))
    return []
  return panel.printElements.flatMap((element, elementIndex) => {
    if (!isRecord(element))
      return []
    const field = readElementField(element, dataSourceCode)
    if (!field)
      return []
    const options = isRecord(element.options) ? element.options : {}
    return [{
      ...readElementMetadata(element, field),
      panelIndex,
      elementIndex,
      field,
      top: readCoordinate(options.top, element.top),
      left: readCoordinate(options.left, element.left),
    }]
  })
}

/** 按面板、纵向、横向和原始数组顺序生成稳定视觉阅读顺序。 */
function compareBindings(left: PrintElementBinding, right: PrintElementBinding): number {
  return left.panelIndex - right.panelIndex
    || left.top - right.top
    || left.left - right.left
    || left.elementIndex - right.elementIndex
}

/** 从 hiprint 标准字段、元素类型字段或 XiHan provider tid 读取绑定路径。 */
function readElementField(element: Record<string, unknown>, dataSourceCode: null | string): string | null {
  const options = isRecord(element.options) ? element.options : {}
  const printElementType = isRecord(element.printElementType) ? element.printElementType : {}
  const explicitField = readNonEmptyString(options.field)
    ?? readNonEmptyString(printElementType.field)
  if (explicitField)
    return explicitField

  const tid = readNonEmptyString(element.tid)
    ?? readNonEmptyString(options.tid)
    ?? readNonEmptyString(printElementType.tid)
  return readFieldFromTid(tid, dataSourceCode)
}

/** 只识别 XiHan 动态 provider 的 tid；默认组件 tid 不代表业务字段。 */
function readFieldFromTid(tid: string | null, dataSourceCode: null | string): string | null {
  if (!dataSourceCode)
    return null
  const prefix = `xihan.${dataSourceCode}.`
  if (!tid?.startsWith(prefix))
    return null
  return tid.slice(prefix.length).trim() || null
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

/** 把模板元素标准元数据复制成自由字段表单项，不把模板对象引用暴露给 UI。 */
function toInferredFormField(binding: PrintElementBinding): PrintSampleFormField {
  return Object.freeze({
    key: binding.field,
    label: binding.label,
    kind: binding.kind,
    inputType: binding.inputType,
    placeholder: binding.placeholder,
    columns: binding.columns,
    registered: false,
  })
}

/** 从元素类型、标题、列配置和可选输入提示推断自由字段表单元数据。 */
function readElementMetadata(
  element: Record<string, unknown>,
  field: string,
): Omit<PrintElementBinding, 'elementIndex' | 'field' | 'left' | 'panelIndex' | 'top'> {
  const options = isRecord(element.options) ? element.options : {}
  const printElementType = isRecord(element.printElementType) ? element.printElementType : {}
  const kind = readElementKind(element, options, printElementType)
  const inputType = readSampleInputType(options.inputType)
    ?? readSampleInputType(printElementType.inputType)
  const placeholder = readNonEmptyString(options.placeholder)
    ?? readNonEmptyString(printElementType.placeholder)
    ?? undefined
  const columns = kind === 'table'
    ? readElementTableColumns(element, options, printElementType)
    : undefined

  return {
    kind,
    label: readNonEmptyString(printElementType.title)
      ?? readNonEmptyString(options.title)
      ?? field,
    inputType,
    placeholder,
    columns,
  }
}

/** 按 hiprint 的 type/textType 约定识别图片、条码、二维码与表格，未知类型安全回退文本。 */
function readElementKind(
  element: Record<string, unknown>,
  options: Record<string, unknown>,
  printElementType: Record<string, unknown>,
): PrintFieldKind {
  const textType = (readNonEmptyString(options.textType)
    ?? readNonEmptyString(printElementType.textType)
    ?? readNonEmptyString(element.textType))?.toLowerCase()
  if (textType === 'barcode' || textType === 'qrcode')
    return textType

  const type = (readNonEmptyString(options.type)
    ?? readNonEmptyString(printElementType.type)
    ?? readNonEmptyString(element.type))?.toLowerCase()
  if (type === 'image' || type === 'table' || type === 'barcode' || type === 'qrcode')
    return type

  const tid = (readNonEmptyString(element.tid)
    ?? readNonEmptyString(options.tid)
    ?? readNonEmptyString(printElementType.tid))?.toLowerCase()
  if (tid?.endsWith('.image'))
    return 'image'
  if (tid?.endsWith('.barcode'))
    return 'barcode'
  if (tid?.endsWith('.qrcode'))
    return 'qrcode'
  if (tid?.toLowerCase().includes('table'))
    return 'table'
  return 'text'
}

/** 读取模板中已保存的明细列；列字段缺失时忽略该列，避免生成不可写表单。 */
function readElementTableColumns(
  element: Record<string, unknown>,
  options: Record<string, unknown>,
  printElementType: Record<string, unknown>,
): readonly PrintTableColumn[] | undefined {
  const candidate = options.columns ?? printElementType.columns ?? element.columns
  const columns = flattenColumnRecords(candidate).flatMap((column) => {
    const columnOptions = isRecord(column.options) ? column.options : {}
    const columnField = readNonEmptyString(column.field) ?? readNonEmptyString(columnOptions.field)
    if (!columnField)
      return []
    validatePrintSamplePath(columnField)
    const width = typeof column.width === 'number' && Number.isFinite(column.width)
      ? column.width
      : undefined
    return [Object.freeze({
      field: columnField,
      title: readNonEmptyString(column.title) ?? readNonEmptyString(columnOptions.title) ?? columnField,
      width,
      inputType: readSampleInputType(column.inputType) ?? readSampleInputType(columnOptions.inputType),
      placeholder: readNonEmptyString(column.placeholder)
        ?? readNonEmptyString(columnOptions.placeholder)
        ?? undefined,
    })]
  })
  return columns.length > 0 ? Object.freeze(columns) : undefined
}

/** 递归展开 hiprint 常见的二维列数组，同时忽略非对象叶子。 */
function flattenColumnRecords(value: unknown): Record<string, unknown>[] {
  if (Array.isArray(value))
    return value.flatMap(flattenColumnRecords)
  return isRecord(value) ? [value] : []
}

/** 只接受模拟表单已支持的显式输入类型，未知模板扩展值由样例值继续推断。 */
function readSampleInputType(value: unknown): PrintSampleInputType | undefined {
  return value === 'boolean'
    || value === 'date'
    || value === 'datetime'
    || value === 'number'
    || value === 'text'
    || value === 'textarea'
    ? value
    : undefined
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

/** 读取有效坐标；缺失或无效值排到当前面板末尾。 */
function readCoordinate(primary: unknown, fallback: unknown): number {
  if (typeof primary === 'number' && Number.isFinite(primary))
    return primary
  if (typeof fallback === 'number' && Number.isFinite(fallback))
    return fallback
  return Number.MAX_SAFE_INTEGER
}

/** 读取去除首尾空白后的字符串。 */
function readNonEmptyString(value: unknown): string | null {
  return typeof value === 'string' && value.trim() ? value.trim() : null
}

/** 判断普通 JSON 风格对象。 */
function isRecord(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === 'object' && !Array.isArray(value)
}
