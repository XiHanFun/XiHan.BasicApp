import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { ResponseOf } from '~/contracts'
import { computed, reactive, ref, watch } from 'vue'
import { numberingRules as rulesApi, toPageRequest, toSchemaPageResult } from '~/contracts'
import { ApiError } from '~/request'

export type RuleRow = ResponseOf<'numbering-rules.list'>['items'][number]
export type AllocationRow = ResponseOf<'numbering-rules.allocations'>['items'][number]
export type DateFormat = RuleRow['dateFormat']
export type ResetCycle = RuleRow['resetCycle']
export type RuleStatus = RuleRow['status']

export const DATE_FORMATS: readonly DateFormat[] = ['None', 'Yyyy', 'YyyyMM', 'YyyyMMdd', 'YyMM', 'YyMMdd', 'MMdd']
export const RESET_CYCLES: readonly ResetCycle[] = ['Never', 'Daily', 'Monthly', 'Yearly']

/** 一次预览最多拼多少个号（后端 NumberingFormatter.MaxPreview） */
export const PREVIEW_MAX_COUNT = 50

/** 流水值：1 到 18 位、不以 0 开头的正整数（后端 NumberingPatterns.SequenceValue） */
const SEQUENCE_VALUE = /^[1-9]\d{0,17}$/

/** 日期格式在编号里的样子（`None` 不输出日期段，由页面给文案） */
export function datePattern(format: DateFormat): string {
  switch (format) {
    case 'Yyyy': return 'yyyy'
    case 'YyyyMM': return 'yyyyMM'
    case 'YyyyMMdd': return 'yyyyMMdd'
    case 'YyMM': return 'yyMM'
    case 'YyMMdd': return 'yyMMdd'
    case 'MMdd': return 'MMdd'
    default: return ''
  }
}

/** 弹窗表单：新建与编辑共用一份（编码只在新建时可改；发过号后格式冻结） */
export interface RuleForm {
  code: string
  name: string
  prefix: string
  separator: string
  dateFormat: DateFormat
  /** 数字框清空时为 null，提交按缺省 4 位 */
  serialLength: null | number
  resetCycle: ResetCycle
  timeZoneId: string
  allowTenantUse: boolean
  /** 数字框清空时为 null，提交按 0 */
  order: null | number
  description: string
  status: RuleStatus
}

export interface ResetForm {
  nextValue: string
  reason: string
  confirmCode: string
}

export type EditorMode = 'create' | 'edit'

function blankToNull(value: string): null | string {
  return value.trim() === '' ? null : value.trim()
}

function emptyRuleForm(): RuleForm {
  return { code: '', name: '', prefix: '', separator: '-', dateFormat: 'YyyyMMdd', serialLength: 4, resetCycle: 'Daily', timeZoneId: 'UTC', allowTenantUse: false, order: 0, description: '', status: 'Active' }
}

/** 批量删除的结果里只有失败项的错误码：逐项拒绝的原因拼成一条报错 */
function failedCodes(result: { failed: { code: string }[] }): void {
  if (result.failed.length > 0)
    throw new Error(result.failed.map(f => f.code).join('; '))
}

/** 提交失败：字段级错误挂回对应字段，其余给整条提示 */
function capture(error: unknown, fieldErrors: { value: Record<string, string[]> }, submitError: { value: null | string }) {
  if (error instanceof ApiError) {
    fieldErrors.value = error.errors ?? {}
    if (!error.hasFieldErrors)
      submitError.value = error.detail
  }
  else {
    submitError.value = String(error)
  }
}

/** 规则的格式部分（预览与更新都要整组带上） */
function formatOf(form: Pick<RuleForm, 'dateFormat' | 'prefix' | 'resetCycle' | 'separator' | 'serialLength' | 'timeZoneId'>) {
  return {
    prefix: blankToNull(form.prefix),
    separator: form.separator,
    dateFormat: form.dateFormat,
    serialLength: form.serialLength ?? 4,
    resetCycle: form.resetCycle,
    timeZoneId: form.timeZoneId.trim() || 'UTC',
  }
}

/**
 * SchemaPage 的资源：分页（平台：平台规则；租户：自己的 + 供租户使用的平台规则）、删除、启停与导入新建。
 * 搜索字段按后端白名单下发：所属 / 状态 / 重置周期精确匹配，关键字匹配编码与名称。
 */
export function numberingRulesResource(): SchemaResource<RuleRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(
      await rulesApi.list(toPageRequest(params, { filters: { owner: 'eq', status: 'eq', resetCycle: 'eq' } })),
      params,
    ),
    // 发过号的与另一侧的规则由后端逐项拒绝
    remove: async id => failedCodes(await rulesApi.delete({ ids: [String(id)] })),
    // 更新是整体覆盖：先取当前值再只改状态（格式原样带回，发过号的规则不会被判成改格式）
    updateStatus: async (id, enabled) => {
      const row = await rulesApi.get({ id: String(id) })
      return rulesApi.update({
        id: row.id,
        name: row.name,
        prefix: row.prefix,
        separator: row.separator,
        dateFormat: row.dateFormat,
        serialLength: row.serialLength,
        resetCycle: row.resetCycle,
        timeZoneId: row.timeZoneId,
        allowTenantUse: row.allowTenantUse,
        order: row.order,
        description: row.description,
        status: enabled ? 'Active' : 'Disabled',
      })
    },
    // 导入：importable 字段组装的记录 → 新建请求（缺省值与新建弹窗相同；导入的规则不供租户使用）
    create: (record) => {
      const defaults = emptyRuleForm()
      return rulesApi.create({
        code: String(record.code ?? '').trim(),
        name: String(record.name ?? '').trim(),
        prefix: blankToNull(String(record.prefix ?? '')),
        separator: typeof record.separator === 'string' ? record.separator : defaults.separator,
        dateFormat: (record.dateFormat as DateFormat | undefined) ?? defaults.dateFormat,
        serialLength: typeof record.serialLength === 'number' ? record.serialLength : 4,
        resetCycle: (record.resetCycle as ResetCycle | undefined) ?? defaults.resetCycle,
        timeZoneId: String(record.timeZoneId ?? '').trim() || defaults.timeZoneId,
        allowTenantUse: false,
        order: typeof record.order === 'number' ? record.order : 0,
        description: blankToNull(String(record.description ?? '')),
      })
    },
  }
}

/**
 * 规则的编辑弹窗（两侧都有）：新建（写在调用方这一侧）、编辑本侧规则（发过号后格式冻结，只能改名称、说明、状态与是否供租户使用）、
 * 按表单里的格式预览一个号（不发号）。只有平台规则能供租户使用；另一侧的规则只读。字段级错误挂回对应字段。
 */
export function useNumberingRuleEditor(isPlatformCaller: () => boolean) {
  const open = ref(false)
  const mode = ref<EditorMode>('create')
  const editing = ref<RuleRow | null>(null)
  const form = reactive<RuleForm>(emptyRuleForm())
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<string | null>(null)
  const submitting = ref(false)
  const previewNumber = ref('')
  const previewing = ref(false)

  /** 这条规则是否属于调用方这一侧（只有本侧的能改、能删、能重置） */
  function isOwn(row: RuleRow): boolean {
    return row.isPlatform === isPlatformCaller()
  }

  const isEdit = computed(() => mode.value === 'edit')
  /** 发过号即格式冻结：前缀、分隔符、日期格式、流水位数、重置周期与时区不能再改 */
  const formatFrozen = computed(() => isEdit.value && (editing.value?.hasAllocated ?? false))

  function reset(next: Partial<RuleForm> = {}) {
    Object.assign(form, emptyRuleForm(), next)
    fieldErrors.value = {}
    submitError.value = null
    previewNumber.value = ''
  }

  function startCreate() {
    mode.value = 'create'
    editing.value = null
    reset()
    open.value = true
  }

  function startEdit(row: RuleRow) {
    mode.value = 'edit'
    editing.value = row
    reset({
      code: row.code,
      name: row.name,
      prefix: row.prefix ?? '',
      separator: row.separator,
      dateFormat: row.dateFormat,
      serialLength: row.serialLength,
      resetCycle: row.resetCycle,
      timeZoneId: row.timeZoneId,
      allowTenantUse: row.allowTenantUse,
      order: row.order,
      description: row.description ?? '',
      status: row.status,
    })
    open.value = true
  }

  function fieldError(field: keyof RuleForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  /** 按表单里的格式拼出第一个号（不发号）；请求失败原样抛出，由页面提示 */
  async function runPreview(): Promise<void> {
    if (previewing.value)
      return
    previewing.value = true
    try {
      const result = await rulesApi.preview({ ...formatOf(form), startValue: '1', count: 1 })
      previewNumber.value = result.numbers[0] ?? ''
    }
    finally {
      previewing.value = false
    }
  }

  async function submit(): Promise<boolean> {
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      const current = editing.value
      const common = {
        name: form.name.trim(),
        ...formatOf(form),
        // 只有平台规则能供租户使用
        allowTenantUse: isPlatformCaller() && form.allowTenantUse,
        order: form.order ?? 0,
        description: blankToNull(form.description),
      }
      if (mode.value === 'edit' && current)
        await rulesApi.update({ id: current.id, ...common, status: form.status })
      else
        await rulesApi.create({ code: form.code.trim(), ...common })
      open.value = false
      return true
    }
    catch (error) {
      capture(error, fieldErrors, submitError)
      return false
    }
    finally {
      submitting.value = false
    }
  }

  return { open, mode, isEdit, editing, form, formatFrozen, fieldError, submitError, submitting, previewNumber, previewing, isOwn, startCreate, startEdit, runPreview, submit }
}

/**
 * 安全重置弹窗：下一个值要在位数范围内、大于本周期已发出的最大值（可向前跳、不能回退）；平台规则要回填规则编码确认，理由必填。
 * 缺省预填当前流水 + 1；18 位流水按字符串传输以免精度丢失。字段级错误挂回对应字段。
 */
export function useNumberingReset() {
  const open = ref(false)
  const target = ref<RuleRow | null>(null)
  const form = reactive<ResetForm>({ nextValue: '', reason: '', confirmCode: '' })
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<string | null>(null)
  const submitting = ref(false)

  /** 平台规则的重置要回填规则编码 */
  const needsConfirm = computed(() => target.value?.isPlatform ?? false)

  function start(row: RuleRow) {
    target.value = row
    Object.assign(form, { nextValue: (BigInt(row.currentValue) + 1n).toString(), reason: '', confirmCode: '' })
    fieldErrors.value = {}
    submitError.value = null
    open.value = true
  }

  function fieldError(field: keyof ResetForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  async function submit(): Promise<boolean> {
    const current = target.value
    if (!current)
      return false
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      await rulesApi.reset({
        id: current.id,
        nextValue: form.nextValue.trim(),
        reason: form.reason.trim(),
        confirmCode: needsConfirm.value ? blankToNull(form.confirmCode) : null,
      })
      open.value = false
      return true
    }
    catch (error) {
      capture(error, fieldErrors, submitError)
      return false
    }
    finally {
      submitting.value = false
    }
  }

  return { open, target, form, needsConfirm, fieldError, submitError, submitting, start, submit }
}

export type PreviewMode = 'batch' | 'single'

/** 预览结果的公共信息：周期键、日期段与示例流水区间 */
export interface PreviewMetadata {
  periodKey: string
  dateText: null | string
  startValue: string
  endValue: string
}

/** 预览执行的结果：完成、起始流水不合法、批量个数越界，或期间换了规则（旧响应作废） */
export type PreviewOutcome = 'done' | 'invalid_count' | 'invalid_start' | 'stale'

/**
 * 规则预览面板：按已保存规则的格式拼单个或连续一批号（1 到 50 个，不消耗流水）。
 * 条件一变就清掉旧结果；版本号隔离快速切换规则时的旧响应。
 */
export function useNumberingPreview() {
  /** 批量模式缺省展示十个连续编号 */
  const DEFAULT_BATCH_COUNT = 10
  const rule = ref<RuleRow | null>(null)
  const mode = ref<PreviewMode>('single')
  const startValue = ref('1')
  const count = ref<null | number>(DEFAULT_BATCH_COUNT)
  const loading = ref(false)
  const numbers = ref<string[]>([])
  const metadata = ref<PreviewMetadata | null>(null)
  let version = 0

  const rows = computed(() => numbers.value.map((number, index) => ({ sequence: index + 1, number })))

  function clear() {
    numbers.value = []
    metadata.value = null
  }

  // 输入变化后旧结果已不再对应当前条件，立即清空可避免误读
  watch([mode, startValue, count], clear)

  function load(row: null | RuleRow) {
    version++
    rule.value = row
    mode.value = 'single'
    startValue.value = '1'
    count.value = DEFAULT_BATCH_COUNT
    loading.value = false
    clear()
  }

  /** 执行预览；请求失败原样抛出，由面板提示 */
  async function execute(): Promise<PreviewOutcome> {
    const current = rule.value
    if (!current || loading.value)
      return 'stale'
    const start = startValue.value.trim()
    if (!SEQUENCE_VALUE.test(start))
      return 'invalid_start'
    const size = mode.value === 'single' ? 1 : count.value
    if (size === null || !Number.isInteger(size) || size < 1 || size > PREVIEW_MAX_COUNT)
      return 'invalid_count'

    const ticket = ++version
    loading.value = true
    clear()
    try {
      const result = await rulesApi.preview({
        prefix: current.prefix?.trim() || null,
        separator: current.separator,
        dateFormat: current.dateFormat,
        serialLength: current.serialLength,
        resetCycle: current.resetCycle,
        timeZoneId: current.timeZoneId,
        startValue: start,
        count: size,
      })
      if (ticket !== version)
        return 'stale'
      numbers.value = result.numbers
      metadata.value = { periodKey: result.periodKey, dateText: result.dateText ?? null, startValue: start, endValue: (BigInt(start) + BigInt(size - 1)).toString() }
      return 'done'
    }
    finally {
      if (ticket === version)
        loading.value = false
    }
  }

  /** 面板卸载：作废在途请求 */
  function dispose() {
    version++
  }

  return { rule, mode, startValue, count, loading, rows, metadata, load, execute, dispose }
}

/**
 * 发号记录抽屉：按规则远程分页、关键字匹配幂等键 / 业务类型 / 业务单据。租户对供租户使用的平台规则只看到自己发起的。
 * 版本号隔离快速切换规则时的旧响应；记录只读。
 */
export function useNumberingAllocations(pageSize = 10) {
  const rule = ref<RuleRow | null>(null)
  const keyword = ref('')
  const page = ref(1)
  const total = ref(0)
  const items = ref<AllocationRow[]>([])
  const loading = ref(false)
  let version = 0

  /** 取当前页；请求失败原样抛出（被更新的请求取代时不抛），由抽屉提示 */
  async function load(): Promise<void> {
    const current = rule.value
    if (!current)
      return
    const ticket = ++version
    loading.value = true
    try {
      const result = await rulesApi.allocations({ id: current.id, page: { index: page.value, size: pageSize }, keyword: keyword.value.trim() || null })
      if (ticket !== version)
        return
      items.value = result.items
      total.value = Number(result.total)
    }
    catch (error) {
      if (ticket === version)
        throw error
    }
    finally {
      if (ticket === version)
        loading.value = false
    }
  }

  function open(row: RuleRow): Promise<void> {
    rule.value = row
    keyword.value = ''
    page.value = 1
    items.value = []
    total.value = 0
    return load()
  }

  function search(): Promise<void> {
    page.value = 1
    return load()
  }

  function changePage(next: number): Promise<void> {
    page.value = next
    return load()
  }

  /** 抽屉卸载：作废在途请求 */
  function dispose() {
    version++
  }

  return { rule, keyword, page, pageSize, total, items, loading, open, search, changePage, load, dispose }
}
