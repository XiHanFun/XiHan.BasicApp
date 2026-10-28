import type { ResponseOf } from '~/contracts'
import { computed, reactive, ref } from 'vue'
import { numberingRules as rulesApi } from '~/contracts'
import { ApiError } from '~/request'

export type RuleRow = ResponseOf<'numbering-rules.list'>['items'][number]
export type AllocationRow = ResponseOf<'numbering-rules.allocations'>['items'][number]
export type DateFormat = RuleRow['dateFormat']
export type ResetCycle = RuleRow['resetCycle']
export type RuleStatus = RuleRow['status']

export const DATE_FORMATS: readonly DateFormat[] = ['None', 'Yyyy', 'YyyyMM', 'YyyyMMdd', 'YyMM', 'YyMMdd', 'MMdd']
export const RESET_CYCLES: readonly ResetCycle[] = ['Never', 'Daily', 'Monthly', 'Yearly']

export interface RuleForm {
  code: string
  name: string
  prefix: string
  separator: string
  dateFormat: DateFormat
  serialLength: string
  resetCycle: ResetCycle
  timeZoneId: string
  allowTenantUse: boolean
  order: string
  description: string
  status: RuleStatus
}

export interface ResetForm {
  nextValue: string
  reason: string
  confirmCode: string
}

function describe(error: unknown): string {
  return error instanceof ApiError ? error.detail : String(error)
}

function toNumber(value: string, fallback: number): number {
  return value.trim() === '' ? fallback : Number(value)
}

function blankToNull(value: string): null | string {
  return value.trim() === '' ? null : value.trim()
}

function emptyRuleForm(): RuleForm {
  return { code: '', name: '', prefix: '', separator: '-', dateFormat: 'YyyyMMdd', serialLength: '4', resetCycle: 'Daily', timeZoneId: 'UTC', allowTenantUse: false, order: '', description: '', status: 'Active' }
}

/**
 * 业务编号页的状态与动作（两侧都有）：规则列表（平台：平台规则；租户：自己的 + 供租户使用的平台规则）、新建、编辑（发过号后格式冻结）、
 * 按当前格式预览、安全重置（平台规则要回填编码）、发号记录分页。另一侧的规则只读。
 */
export function useNumberingRules(isPlatformCaller: () => boolean, pageSize = 20) {
  const items = ref<RuleRow[]>([])
  const total = ref(0)
  const pageIndex = ref(1)
  const loading = ref(false)
  const listError = ref<string | null>(null)
  const pageCount = computed(() => Math.max(1, Math.ceil(total.value / pageSize)))

  const form = reactive<RuleForm>(emptyRuleForm())
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<string | null>(null)
  const submitting = ref(false)

  const selected = ref<RuleRow | null>(null)
  const edit = reactive<RuleForm>(emptyRuleForm())
  const editErrors = ref<Record<string, string[]>>({})
  const detailError = ref<string | null>(null)
  const notice = ref<'reset' | 'saved' | null>(null)
  const working = ref(false)

  const preview = reactive({ startValue: '1', count: '3' })
  const previewNumbers = ref<string[]>([])
  const previewError = ref<string | null>(null)

  const reset = reactive<ResetForm>({ nextValue: '', reason: '', confirmCode: '' })
  const resetErrors = ref<Record<string, string[]>>({})

  const allocations = ref<AllocationRow[]>([])
  const allocationTotal = ref(0)
  const allocationPage = ref(1)
  const allocationsLoading = ref(false)

  const editable = computed(() => selected.value !== null && selected.value.isPlatform === isPlatformCaller())
  /** 发过号即格式冻结：前缀、分隔符、日期格式、流水位数、重置周期与时区不能再改 */
  const formatFrozen = computed(() => selected.value?.hasAllocated ?? false)

  async function load(index = pageIndex.value) {
    loading.value = true
    listError.value = null
    try {
      const result = await rulesApi.list({ page: { index, size: pageSize }, filters: [], sorts: [] })
      items.value = result.items
      total.value = Number(result.total)
      pageIndex.value = result.page.index ?? index
    }
    catch (error) {
      items.value = []
      total.value = 0
      listError.value = describe(error)
    }
    finally {
      loading.value = false
    }
  }

  function fieldError(field: keyof RuleForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  function editError(field: keyof RuleForm): string | undefined {
    return editErrors.value[field]?.[0]
  }

  function resetError(field: keyof ResetForm): string | undefined {
    return resetErrors.value[field]?.[0]
  }

  function capture(error: unknown, errors: { value: Record<string, string[]> }, message: { value: null | string }) {
    if (error instanceof ApiError) {
      errors.value = error.errors ?? {}
      if (!error.hasFieldErrors)
        message.value = error.detail
    }
    else {
      message.value = String(error)
    }
  }

  async function submit(): Promise<boolean> {
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      const created = await rulesApi.create({
        code: form.code.trim(),
        name: form.name.trim(),
        prefix: blankToNull(form.prefix),
        separator: form.separator,
        dateFormat: form.dateFormat,
        serialLength: toNumber(form.serialLength, 4),
        resetCycle: form.resetCycle,
        timeZoneId: form.timeZoneId.trim() || 'UTC',
        allowTenantUse: form.allowTenantUse,
        order: toNumber(form.order, 0),
        description: blankToNull(form.description),
      })
      Object.assign(form, emptyRuleForm())
      await load(1)
      await select(created.id)
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

  async function loadAllocations(index = allocationPage.value) {
    const current = selected.value
    if (!current)
      return
    allocationsLoading.value = true
    try {
      const result = await rulesApi.allocations({ id: current.id, page: { index, size: 10 } })
      allocations.value = result.items
      allocationTotal.value = Number(result.total)
      allocationPage.value = result.page.index ?? index
    }
    catch (error) {
      allocations.value = []
      detailError.value = describe(error)
    }
    finally {
      allocationsLoading.value = false
    }
  }

  async function select(id: string) {
    const row = items.value.find(r => r.id === id) ?? null
    selected.value = row
    detailError.value = null
    notice.value = null
    editErrors.value = {}
    resetErrors.value = {}
    previewNumbers.value = []
    previewError.value = null
    Object.assign(reset, { nextValue: '', reason: '', confirmCode: '' })
    if (row) {
      Object.assign(edit, {
        code: row.code,
        name: row.name,
        prefix: row.prefix ?? '',
        separator: row.separator,
        dateFormat: row.dateFormat,
        serialLength: String(row.serialLength),
        resetCycle: row.resetCycle,
        timeZoneId: row.timeZoneId,
        allowTenantUse: row.allowTenantUse,
        order: String(row.order),
        description: row.description ?? '',
        status: row.status,
      })
      await loadAllocations(1)
    }
  }

  async function refreshSelected(id: string) {
    await load()
    await select(id)
  }

  async function save(): Promise<boolean> {
    const current = selected.value
    if (!current)
      return false
    working.value = true
    detailError.value = null
    notice.value = null
    editErrors.value = {}
    try {
      await rulesApi.update({
        id: current.id,
        name: edit.name.trim(),
        prefix: blankToNull(edit.prefix),
        separator: edit.separator,
        dateFormat: edit.dateFormat,
        serialLength: toNumber(edit.serialLength, 4),
        resetCycle: edit.resetCycle,
        timeZoneId: edit.timeZoneId.trim() || 'UTC',
        allowTenantUse: edit.allowTenantUse,
        order: toNumber(edit.order, 0),
        description: blankToNull(edit.description),
        status: edit.status,
      })
      await refreshSelected(current.id)
      notice.value = 'saved'
      return true
    }
    catch (error) {
      capture(error, editErrors, detailError)
      return false
    }
    finally {
      working.value = false
    }
  }

  /** 删除：发过号的与另一侧的由后端逐项拒绝 */
  async function remove(): Promise<boolean> {
    const current = selected.value
    if (!current)
      return false
    working.value = true
    detailError.value = null
    notice.value = null
    try {
      const result = await rulesApi.delete({ ids: [current.id] })
      if (result.failed.length > 0) {
        detailError.value = result.failed.map(f => f.code).join('; ')
        return false
      }
      selected.value = null
      await load(1)
      return true
    }
    catch (error) {
      detailError.value = describe(error)
      return false
    }
    finally {
      working.value = false
    }
  }

  /** 按编辑表单里的格式预览（不发号） */
  async function runPreview(): Promise<boolean> {
    previewError.value = null
    try {
      const result = await rulesApi.preview({
        prefix: blankToNull(edit.prefix),
        separator: edit.separator,
        dateFormat: edit.dateFormat,
        serialLength: toNumber(edit.serialLength, 4),
        resetCycle: edit.resetCycle,
        timeZoneId: edit.timeZoneId.trim() || 'UTC',
        startValue: preview.startValue.trim() || '1',
        count: toNumber(preview.count, 1),
      })
      previewNumbers.value = result.numbers
      return true
    }
    catch (error) {
      previewNumbers.value = []
      previewError.value = describe(error)
      return false
    }
  }

  async function runReset(): Promise<boolean> {
    const current = selected.value
    if (!current)
      return false
    working.value = true
    detailError.value = null
    notice.value = null
    resetErrors.value = {}
    try {
      await rulesApi.reset({ id: current.id, nextValue: reset.nextValue.trim(), reason: reset.reason.trim(), confirmCode: blankToNull(reset.confirmCode) })
      await refreshSelected(current.id)
      notice.value = 'reset'
      return true
    }
    catch (error) {
      capture(error, resetErrors, detailError)
      return false
    }
    finally {
      working.value = false
    }
  }

  return {
    items,
    total,
    pageIndex,
    pageCount,
    pageSize,
    loading,
    listError,
    form,
    fieldError,
    submitError,
    submitting,
    selected,
    edit,
    editable,
    formatFrozen,
    editError,
    detailError,
    notice,
    working,
    preview,
    previewNumbers,
    previewError,
    reset,
    resetError,
    allocations,
    allocationTotal,
    allocationPage,
    allocationsLoading,
    load,
    submit,
    select,
    save,
    remove,
    runPreview,
    runReset,
    loadAllocations,
  }
}
