import type { ResponseOf } from '~/contracts'
import { computed, reactive, ref } from 'vue'
import { positions as positionsApi } from '~/contracts'
import { ApiError } from '~/request'

export type PositionRow = ResponseOf<'positions.list'>['items'][number]
export type PositionStatus = PositionRow['status']

export interface CreatePositionForm {
  code: string
  name: string
  description: string
  order: string
}

export interface EditPositionForm {
  name: string
  description: string
  order: string
  status: PositionStatus
}

function describe(error: unknown): string {
  return error instanceof ApiError ? error.detail : String(error)
}

function toOrder(value: string): number {
  return value.trim() === '' ? 0 : Number(value)
}

/**
 * 岗位页的状态与动作（租户侧）：分页列表、创建表单、选中岗位的编辑与删除。
 * 契约类型来自生成物；错误只看 `code`、`detail` 与字段级 `errors`。
 */
export function usePositions(pageSize = 20) {
  const items = ref<PositionRow[]>([])
  const total = ref(0)
  const pageIndex = ref(1)
  const loading = ref(false)
  const listError = ref<string | null>(null)
  const pageCount = computed(() => Math.max(1, Math.ceil(total.value / pageSize)))

  const form = reactive<CreatePositionForm>({ code: '', name: '', description: '', order: '' })
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<string | null>(null)
  const submitting = ref(false)

  const selected = ref<PositionRow | null>(null)
  const edit = reactive<EditPositionForm>({ name: '', description: '', order: '', status: 'Active' })
  const editErrors = ref<Record<string, string[]>>({})
  const detailError = ref<string | null>(null)
  const notice = ref<'saved' | null>(null)
  const working = ref(false)

  async function load(index = pageIndex.value) {
    loading.value = true
    listError.value = null
    try {
      const result = await positionsApi.list({ page: { index, size: pageSize }, filters: [], sorts: [{ field: 'order' }, { field: 'code' }] })
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

  function fieldError(field: keyof CreatePositionForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  function editError(field: keyof EditPositionForm): string | undefined {
    return editErrors.value[field]?.[0]
  }

  async function submit(): Promise<boolean> {
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      const created = await positionsApi.create({ code: form.code.trim(), name: form.name.trim(), description: form.description.trim() || null, order: toOrder(form.order) })
      form.code = ''
      form.name = ''
      form.description = ''
      form.order = ''
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

  function select(id: string) {
    const row = items.value.find(p => p.id === id) ?? null
    selected.value = row
    detailError.value = null
    notice.value = null
    editErrors.value = {}
    if (row) {
      edit.name = row.name
      edit.description = row.description ?? ''
      edit.order = String(row.order)
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
      await positionsApi.update({ id: current.id, name: edit.name.trim(), description: edit.description.trim() || null, order: toOrder(edit.order), status: edit.status })
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

  /** 删除：还被归属引用的由后端逐项拒绝（ORGANIZATION_CONFLICT_004） */
  async function remove(): Promise<boolean> {
    const current = selected.value
    if (!current)
      return false
    working.value = true
    detailError.value = null
    notice.value = null
    try {
      const result = await positionsApi.delete({ ids: [current.id] })
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
    loading,
    listError,
    form,
    fieldError,
    submitError,
    submitting,
    selected,
    edit,
    editError,
    detailError,
    notice,
    working,
    load,
    submit,
    select,
    save,
    remove,
  }
}
