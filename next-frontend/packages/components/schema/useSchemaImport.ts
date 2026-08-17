import type { ComputedRef, Ref } from 'vue'
import type { ListFieldSchema } from './types'
import { computed, ref } from 'vue'
import { i18n } from '~/locales'
import { downloadText } from './useSchemaExport'

/** 本地化便捷封装：保持 i18n.global 的 this 绑定 */
function t(key: string, named?: Record<string, unknown>): string {
  return named ? i18n.global.t(key, named) : i18n.global.t(key)
}

/**
 * Schema 同步导入（CSV → 预校验 → 逐行 create）。
 * 模板列、解析校验均由 importable 字段派生（toImportFields），页面零额外配置。
 */

/** 行级错误（row 为数据行号，从 1 开始，不含表头/说明行） */
export interface ImportRowError {
  row: number
  /** 出错字段标题（文件级错误无此项） */
  field?: string
  message: string
}

/** 解析后的数据行 */
export interface ImportParsedRow {
  /** 数据行号（从 1 开始） */
  row: number
  /** 原始单元格（字段 key → 原文），用于失败行回写下载 */
  raw: Record<string, string>
  /** 校验转换后的记录（field.key → 归一化值；校验失败时为 undefined） */
  record?: Record<string, unknown>
  /** 校验/创建错误（创建失败在导入阶段追加） */
  errors: ImportRowError[]
}

/** 导入结果汇总 */
export interface ImportSummary {
  /** 文件内数据行总数 */
  total: number
  /** 创建成功行数 */
  success: number
  /** 失败行数（校验失败 + 创建失败） */
  failed: number
}

/** CSV 单元格转义（与导出一致） */
function csvCell(value: string): string {
  return /[",\r\n]/.test(value) ? `"${value.replace(/"/g, '""')}"` : value
}

/**
 * 解析 CSV 文本为单元格矩阵：支持双引号包裹、内嵌逗号/换行、"" 转义，兼容 CRLF/LF，剥离 BOM。
 */
export function parseCsv(text: string): string[][] {
  const source = text.replace(/^\uFEFF/, '')
  const rows: string[][] = []
  let cells: string[] = []
  let cell = ''
  let inQuotes = false
  let index = 0

  const pushCell = () => {
    cells.push(cell)
    cell = ''
  }
  const pushRow = () => {
    pushCell()
    // 跳过完全空行
    if (cells.some(value => value.trim() !== '')) {
      rows.push(cells)
    }
    cells = []
  }

  while (index < source.length) {
    const char = source[index]!
    if (inQuotes) {
      if (char === '"') {
        if (source[index + 1] === '"') {
          cell += '"'
          index += 2
          continue
        }
        inQuotes = false
        index += 1
        continue
      }
      cell += char
      index += 1
      continue
    }
    if (char === '"' && cell === '') {
      inQuotes = true
      index += 1
      continue
    }
    if (char === ',') {
      pushCell()
      index += 1
      continue
    }
    if (char === '\n') {
      pushRow()
      index += 1
      continue
    }
    if (char === '\r') {
      pushRow()
      index += source[index + 1] === '\n' ? 2 : 1
      continue
    }
    cell += char
    index += 1
  }
  if (cell !== '' || cells.length > 0) {
    pushRow()
  }
  return rows
}

/** 字段的取值提示（模板说明行 / 校验错误提示共用） */
function fieldHint(field: ListFieldSchema): string {
  if (field.options?.length) {
    return t('component.schema_import.hint_options', { values: field.options.map(o => o.label).join('/') })
  }
  switch (field.dataType) {
    case 'boolean':
      return t('component.schema_import.hint_boolean')
    case 'number':
    case 'money':
    case 'percent':
      return t('component.schema_import.hint_number')
    case 'date':
      return t('component.schema_import.hint_date')
    case 'datetime':
      return t('component.schema_import.hint_datetime')
    case 'json':
      return t('component.schema_import.hint_json')
    default:
      return t('component.schema_import.hint_text')
  }
}

/**
 * 生成导入模板 CSV：表头行（字段标题）+ 以 # 开头的说明行（必填/取值提示，导入时自动跳过）。
 */
export function buildImportTemplate(fields: ListFieldSchema[]): string {
  const header = fields.map(f => csvCell(f.title)).join(',')
  const hints = fields
    .map((f, i) => {
      const flag = f.required ? t('component.schema_import.flag_required') : t('component.schema_import.flag_optional')
      return csvCell(`${i === 0 ? '#' : ''}${t('component.schema_import.template_cell', { flag, hint: fieldHint(f) })}`)
    })
    .join(',')
  return `\uFEFF${header}\r\n${hints}`
}

const TRUE_TOKENS = new Set(['是', 'true', '1', 'y', 'yes'])
const FALSE_TOKENS = new Set(['否', 'false', '0', 'n', 'no'])

/** 单元格 → 归一化值（按 dataType / options 转换；失败返回错误文案） */
function convertCell(field: ListFieldSchema, text: string): { value?: unknown, error?: string } {
  const trimmed = text.trim()
  if (trimmed === '') {
    return field.required ? { error: t('component.schema_import.err_required') } : {}
  }
  // 有选项的字段（enum/tag/带选项的 boolean 等）：按 label 或 value 反查
  if (field.options?.length) {
    const matched = field.options.find(o => o.label === trimmed || String(o.value) === trimmed)
    if (!matched) {
      return { error: t('component.schema_import.err_invalid_option', { value: trimmed, hint: fieldHint(field) }) }
    }
    if (field.dataType === 'boolean') {
      return { value: Boolean(Number(matched.value)) }
    }
    return { value: matched.value }
  }
  switch (field.dataType) {
    case 'boolean': {
      const token = trimmed.toLowerCase()
      if (TRUE_TOKENS.has(token)) {
        return { value: true }
      }
      if (FALSE_TOKENS.has(token)) {
        return { value: false }
      }
      return { error: t('component.schema_import.err_invalid_boolean', { value: trimmed }) }
    }
    case 'number':
    case 'money':
    case 'percent': {
      const num = Number(trimmed.replace(/,/g, ''))
      return Number.isNaN(num) ? { error: t('component.schema_import.err_invalid_number', { value: trimmed }) } : { value: num }
    }
    case 'date':
    case 'datetime':
      return Number.isNaN(Date.parse(trimmed))
        ? { error: t('component.schema_import.err_invalid_date', { value: trimmed }) }
        : { value: trimmed }
    case 'json':
      try {
        JSON.parse(trimmed)
        return { value: trimmed }
      }
      catch {
        return { error: t('component.schema_import.err_invalid_json') }
      }
    default:
      return { value: trimmed }
  }
}

/** 从异常中提取后端错误文案 */
function errorMessage(error: unknown): string {
  if (error instanceof Error && error.message) {
    return error.message
  }
  return typeof error === 'string' && error ? error : t('component.schema_import.err_create_failed')
}

export interface UseSchemaImportOptions {
  /** 导入字段（已按权限过滤；建议传入字典选项已注入的 resolved 字段） */
  fields: () => ListFieldSchema[]
  /** 模板/失败行文件名前缀（通常为 pageCode） */
  fileName: () => string
  /** 创建单条（来自 resource.create） */
  create: (record: Record<string, unknown>) => Promise<unknown>
  /** 并发创建数（默认 4） */
  concurrency?: number
}

export interface UseSchemaImport {
  /** 阶段：idle 待选文件 / ready 已解析待导入 / importing 导入中 / done 完成 */
  phase: Ref<'done' | 'idle' | 'importing' | 'ready'>
  /** 解析后的数据行 */
  rows: Ref<ImportParsedRow[]>
  /** 文件级错误（缺少必填列等，阻断导入） */
  fileErrors: Ref<string[]>
  /** 校验通过、可导入的行 */
  validRows: ComputedRef<ImportParsedRow[]>
  /** 含错误的行（校验失败 + 创建失败） */
  errorRows: ComputedRef<ImportParsedRow[]>
  /** 已处理行数（导入进度） */
  progress: Ref<number>
  /** 导入结果（done 阶段有值） */
  summary: Ref<ImportSummary | null>
  /** 源文件名（loadFile 后有值，供留痕上报） */
  sourceFileName: Ref<string>
  /** 下载导入模板 */
  downloadTemplate: () => void
  /** 解析并校验文件 */
  loadFile: (file: File) => Promise<void>
  /** 执行导入（仅校验通过的行） */
  run: () => Promise<ImportSummary>
  /** 下载失败行（原始单元格 + 错误原因，修正后可重新导入） */
  downloadErrors: () => void
  /** 重置（关闭弹窗/重新选择文件） */
  reset: () => void
}

export function useSchemaImport(options: UseSchemaImportOptions): UseSchemaImport {
  const phase = ref<'done' | 'idle' | 'importing' | 'ready'>('idle')
  const rows = ref<ImportParsedRow[]>([])
  const fileErrors = ref<string[]>([])
  const progress = ref(0)
  const summary = ref<ImportSummary | null>(null)
  const sourceFileName = ref('')

  const validRows = computed(() => rows.value.filter(r => r.errors.length === 0 && r.record))
  const errorRows = computed(() => rows.value.filter(r => r.errors.length > 0))

  function reset(): void {
    phase.value = 'idle'
    rows.value = []
    fileErrors.value = []
    progress.value = 0
    summary.value = null
    sourceFileName.value = ''
  }

  function downloadTemplate(): void {
    downloadText(`${options.fileName()}${t('component.schema_import.template_filename_suffix')}.csv`, buildImportTemplate(options.fields()))
  }

  async function loadFile(file: File): Promise<void> {
    reset()
    sourceFileName.value = file.name
    const fields = options.fields()
    const matrix = parseCsv(await file.text())
    // 跳过以 # 开头的说明行
    const meaningful = matrix.filter(cells => !cells[0]?.trim().startsWith('#'))
    if (meaningful.length === 0) {
      fileErrors.value = [t('component.schema_import.err_empty_file')]
      phase.value = 'ready'
      return
    }
    // 表头映射：按字段标题（优先）或字段 key 匹配；无法识别的列忽略
    const header = meaningful[0]!
    const columnFields: Array<ListFieldSchema | undefined> = header.map((cell) => {
      const name = cell.trim()
      return fields.find(f => f.title === name) ?? fields.find(f => f.key === name)
    })
    const mappedKeys = new Set(columnFields.filter(f => !!f).map(f => f.key))
    const missingRequired = fields.filter(f => f.required && !mappedKeys.has(f.key))
    const errors: string[] = []
    if (mappedKeys.size === 0) {
      errors.push(t('component.schema_import.err_no_columns'))
    }
    if (missingRequired.length > 0) {
      errors.push(t('component.schema_import.err_missing_columns', { columns: missingRequired.map(f => f.title).join(t('component.schema_import.column_join')) }))
    }
    fileErrors.value = errors

    const parsed: ImportParsedRow[] = []
    for (let i = 1; i < meaningful.length; i += 1) {
      const cells = meaningful[i]!
      const row: ImportParsedRow = { row: i, raw: {}, errors: [] }
      const record: Record<string, unknown> = {}
      columnFields.forEach((field, col) => {
        if (!field) {
          return
        }
        const text = cells[col] ?? ''
        row.raw[field.key] = text
        const { value, error } = convertCell(field, text)
        if (error) {
          row.errors.push({ row: row.row, field: field.title, message: error })
        }
        else if (value !== undefined) {
          record[field.key] = value
        }
      })
      // 文件级错误阻断导入：所有行均不可导入，但仍展示行级校验结果辅助修正
      if (row.errors.length === 0 && errors.length === 0) {
        row.record = record
      }
      parsed.push(row)
    }
    rows.value = parsed
    phase.value = 'ready'
  }

  async function run(): Promise<ImportSummary> {
    const targets = validRows.value
    phase.value = 'importing'
    progress.value = 0
    let success = 0
    const queue = [...targets]
    const concurrency = Math.max(1, options.concurrency ?? 4)
    const worker = async (): Promise<void> => {
      while (queue.length > 0) {
        const row = queue.shift()!
        try {
          await options.create(row.record!)
          success += 1
        }
        catch (error) {
          row.errors.push({ row: row.row, message: errorMessage(error) })
        }
        progress.value += 1
      }
    }
    await Promise.all(Array.from({ length: Math.min(concurrency, queue.length || 1) }, () => worker()))
    const result: ImportSummary = {
      total: rows.value.length,
      success,
      failed: rows.value.length - success,
    }
    summary.value = result
    phase.value = 'done'
    return result
  }

  function downloadErrors(): void {
    const fields = options.fields()
    const failed = errorRows.value
    if (failed.length === 0) {
      return
    }
    const header = [...fields.map(f => csvCell(f.title)), t('component.schema_import.error_reason_column')].join(',')
    const body = failed
      .map((row) => {
        const cells = fields.map(f => csvCell(row.raw[f.key] ?? ''))
        const reason = row.errors.map(e => (e.field ? `${e.field}: ${e.message}` : e.message)).join(t('component.schema_import.error_reason_join'))
        return [...cells, csvCell(reason)].join(',')
      })
      .join('\r\n')
    downloadText(`${options.fileName()}${t('component.schema_import.errors_filename_suffix')}.csv`, `\uFEFF${header}\r\n${body}`)
  }

  return {
    phase,
    rows,
    fileErrors,
    validRows,
    errorRows,
    progress,
    summary,
    sourceFileName,
    downloadTemplate,
    loadFile,
    run,
    downloadErrors,
    reset,
  }
}
