import type { ResponseOf } from '~/contracts'
import { computed, reactive, ref } from 'vue'
import { configs as configsApi } from '~/contracts'
import { ApiError } from '~/request'

export type ConfigRow = ResponseOf<'configs.list'>['items'][number]
export type ConfigDataType = ConfigRow['dataType']
export type ConfigStatus = ConfigRow['status']

export const CONFIG_DATA_TYPES: readonly ConfigDataType[] = ['Text', 'Number', 'Boolean', 'Json', 'Array']

export interface CreateConfigForm {
  key: string
  name: string
  group: string
  dataType: ConfigDataType
  value: string
  defaultValue: string
  isEncrypted: boolean
  allowTenantOverride: boolean
  order: string
  description: string
}

export interface EditConfigForm {
  name: string
  group: string
  dataType: ConfigDataType
  value: string
  defaultValue: string
  isEncrypted: boolean
  allowTenantOverride: boolean
  order: string
  description: string
  status: ConfigStatus
}

function describe(error: unknown): string {
  return error instanceof ApiError ? error.detail : String(error)
}

function toOrder(value: string): number {
  return value.trim() === '' ? 0 : Number(value)
}

function blankToNull(value: string): null | string {
  return value.trim() === '' ? null : value
}

function emptyCreateForm(): CreateConfigForm {
  return { key: '', name: '', group: '', dataType: 'Text', value: '', defaultValue: '', isEncrypted: false, allowTenantOverride: true, order: '', description: '' }
}

/**
 * 参数页的状态与动作（两侧都有）：分页列表（读共享：平台参数 + 本侧参数）、新建（租户同键即覆盖平台参数）、选中参数的编辑与删除。
 * 另一侧的参数只读；加密参数不回显值，编辑时值留空即保留原值。
 */
export function useConfigs(isPlatformCaller: () => boolean, pageSize = 20) {
  const items = ref<ConfigRow[]>([])
  const total = ref(0)
  const pageIndex = ref(1)
  const keyword = ref('')
  const loading = ref(false)
  const listError = ref<string | null>(null)
  const pageCount = computed(() => Math.max(1, Math.ceil(total.value / pageSize)))

  const form = reactive<CreateConfigForm>(emptyCreateForm())
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<string | null>(null)
  const submitting = ref(false)

  const selected = ref<ConfigRow | null>(null)
  const edit = reactive<EditConfigForm>({ name: '', group: '', dataType: 'Text', value: '', defaultValue: '', isEncrypted: false, allowTenantOverride: true, order: '', description: '', status: 'Active' })
  const editErrors = ref<Record<string, string[]>>({})
  const detailError = ref<string | null>(null)
  const notice = ref<'saved' | null>(null)
  const working = ref(false)

  /** 这行参数是否属于调用方这一侧（只有本侧的能改、能删） */
  function isOwn(row: ConfigRow): boolean {
    return row.isPlatform === isPlatformCaller()
  }

  const editable = computed(() => selected.value !== null && isOwn(selected.value))

  async function load(index = pageIndex.value) {
    loading.value = true
    listError.value = null
    try {
      const result = await configsApi.list({ page: { index, size: pageSize }, filters: [], sorts: [], keyword: keyword.value.trim() || null })
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

  function fieldError(field: keyof CreateConfigForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  function editError(field: keyof EditConfigForm): string | undefined {
    return editErrors.value[field]?.[0]
  }

  async function submit(): Promise<boolean> {
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      const created = await configsApi.create({
        key: form.key.trim(),
        name: form.name.trim(),
        group: blankToNull(form.group),
        dataType: form.dataType,
        value: blankToNull(form.value),
        defaultValue: blankToNull(form.defaultValue),
        isEncrypted: form.isEncrypted,
        allowTenantOverride: form.allowTenantOverride,
        order: toOrder(form.order),
        description: blankToNull(form.description),
      })
      Object.assign(form, emptyCreateForm())
      await load(1)
      select(created.id)
      return true
    }
    catch (error) {
      if (error instanceof ApiError) {
        fieldErrors.value = error.errors ?? {}
        if (!error.hasFieldErrors)
          submitError.value = error.detail
      }
      else {
        submitError.value = String(error)
      }
      return false
    }
    finally {
      submitting.value = false
    }
  }

  /** 租户覆盖平台参数：用平台参数的键、名称、分组与数据类型预填新建表单 */
  function overrideFrom(row: ConfigRow) {
    Object.assign(form, emptyCreateForm(), { key: row.key, name: row.name, group: row.group ?? '', dataType: row.dataType, allowTenantOverride: false })
  }

  function select(id: string) {
    const row = items.value.find(c => c.id === id) ?? null
    selected.value = row
    detailError.value = null
    notice.value = null
    editErrors.value = {}
    if (row) {
      edit.name = row.name
      edit.group = row.group ?? ''
      edit.dataType = row.dataType
      edit.value = row.value ?? ''
      edit.defaultValue = row.defaultValue ?? ''
      edit.isEncrypted = row.isEncrypted
      edit.allowTenantOverride = row.allowTenantOverride
      edit.order = String(row.order)
      edit.description = row.description ?? ''
      edit.status = row.status
    }
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
      await configsApi.update({
        id: current.id,
        name: edit.name.trim(),
        group: blankToNull(edit.group),
        dataType: edit.dataType,
        value: blankToNull(edit.value),
        defaultValue: blankToNull(edit.defaultValue),
        isEncrypted: edit.isEncrypted,
        allowTenantOverride: edit.allowTenantOverride,
        order: toOrder(edit.order),
        description: blankToNull(edit.description),
        status: edit.status,
      })
      await load()
      select(current.id)
      notice.value = 'saved'
      return true
    }
    catch (error) {
      if (error instanceof ApiError) {
        editErrors.value = error.errors ?? {}
        if (!error.hasFieldErrors)
          detailError.value = error.detail
      }
      else {
        detailError.value = String(error)
      }
      return false
    }
    finally {
      working.value = false
    }
  }

  /** 删除：内建与另一侧的参数由后端逐项拒绝 */
  async function remove(): Promise<boolean> {
    const current = selected.value
    if (!current)
      return false
    working.value = true
    detailError.value = null
    notice.value = null
    try {
      const result = await configsApi.delete({ ids: [current.id] })
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

  return {
    items,
    total,
    pageIndex,
    pageCount,
    pageSize,
    keyword,
    loading,
    listError,
    form,
    fieldError,
    submitError,
    submitting,
    selected,
    edit,
    editable,
    editError,
    detailError,
    notice,
    working,
    isOwn,
    load,
    submit,
    overrideFrom,
    select,
    save,
    remove,
  }
}
