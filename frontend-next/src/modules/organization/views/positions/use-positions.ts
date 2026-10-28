import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { ResponseOf } from '~/contracts'
import { computed, reactive, ref } from 'vue'
import { positions as positionsApi, toPageRequest, toSchemaPageResult } from '~/contracts'
import { ApiError } from '~/request'

export type PositionRow = ResponseOf<'positions.list'>['items'][number]
export type PositionStatus = PositionRow['status']

/** 弹窗表单：新建与编辑共用（编码只在新建时可改；6.0 新建不收状态，状态只在编辑时改） */
export interface PositionForm {
  code: string
  name: string
  order: null | number
  description: string
  status: PositionStatus
}

export type PositionEditorMode = 'create' | 'edit'

function blankToNull(value: string): null | string {
  return value.trim() === '' ? null : value.trim()
}

function emptyForm(): PositionForm {
  return { code: '', name: '', order: 0, description: '', status: 'Active' }
}

/** 批量删除的结果里只有失败项的错误码：逐项拒绝的原因拼成一条报错 */
function failedCodes(result: { failed: { code: string }[] }): void {
  if (result.failed.length > 0)
    throw new Error(result.failed.map(f => f.code).join('; '))
}

/**
 * SchemaPage 的资源：分页、删除、启停与导入新建。
 * 搜索字段按后端白名单下发：关键字匹配编码与名称，状态精确匹配；可排序列是编码、名称、排序与创建时间。
 */
export function positionsResource(): SchemaResource<PositionRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(
      await positionsApi.list(toPageRequest(params, { filters: { status: 'eq' } })),
      params,
    ),
    remove: async id => failedCodes(await positionsApi.delete({ ids: [String(id)] })),
    // 更新是整体替换：先取当前值再只改状态
    updateStatus: async (id, enabled) => {
      const row = await positionsApi.get({ id: String(id) })
      return positionsApi.update({
        id: row.id,
        name: row.name,
        order: row.order,
        description: row.description ?? null,
        status: enabled ? 'Active' : 'Disabled',
      })
    },
    // 导入：importable 字段组装的记录 → 新建请求（缺省值在此兜底；新建的岗位一律启用）
    create: record => positionsApi.create({
      code: String(record.code ?? '').trim(),
      name: String(record.name ?? '').trim(),
      order: typeof record.order === 'number' ? record.order : 0,
      description: blankToNull(String(record.description ?? '')),
    }),
  }
}

/**
 * 岗位页的编辑弹窗：新建、编辑。字段级错误挂回对应字段，其余错误给整条提示。
 */
export function usePositionEditor() {
  const open = ref(false)
  const mode = ref<PositionEditorMode>('create')
  const editing = ref<PositionRow | null>(null)
  const form = reactive<PositionForm>(emptyForm())
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<string | null>(null)
  const submitting = ref(false)

  const isEdit = computed(() => mode.value === 'edit')

  function reset(next: Partial<PositionForm> = {}) {
    Object.assign(form, emptyForm(), next)
    fieldErrors.value = {}
    submitError.value = null
  }

  function startCreate() {
    mode.value = 'create'
    editing.value = null
    reset()
    open.value = true
  }

  /** 列表行就是完整的岗位（含说明），不必再取详情 */
  function startEdit(row: PositionRow) {
    mode.value = 'edit'
    editing.value = row
    reset({ code: row.code, name: row.name, order: row.order, description: row.description ?? '', status: row.status })
    open.value = true
  }

  function fieldError(field: keyof PositionForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  async function submit(): Promise<boolean> {
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      const current = editing.value
      if (mode.value === 'edit' && current) {
        await positionsApi.update({
          id: current.id,
          name: form.name.trim(),
          order: form.order ?? 0,
          description: blankToNull(form.description),
          status: form.status,
        })
      }
      else {
        await positionsApi.create({
          code: form.code.trim(),
          name: form.name.trim(),
          order: form.order ?? 0,
          description: blankToNull(form.description),
        })
      }
      open.value = false
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

  return { open, mode, isEdit, editing, form, fieldError, submitError, submitting, startCreate, startEdit, submit }
}
