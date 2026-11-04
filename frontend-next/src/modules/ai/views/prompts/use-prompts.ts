import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { ResponseOf } from '~/contracts'
import { computed, reactive, ref } from 'vue'
import { aiPrompts, toPageRequest, toSchemaPageResult } from '~/contracts'
import { ApiError, throwOnFailed } from '~/request'

export type PromptRow = ResponseOf<'ai-prompts.list'>['items'][number]
export type PromptDetail = ResponseOf<'ai-prompts.get'>
export type PromptStatus = PromptRow['status']

/** 编辑弹窗的表单：新建与编辑共用 */
export interface PromptForm {
  code: string
  name: string
  category: string
  version: string
  content: string
  order: number
  status: PromptStatus
  remark: string
}

export function emptyPromptForm(): PromptForm {
  return { code: '', name: '', category: '', version: '', content: '', order: 100, status: 'Active', remark: '' }
}

function blankToNull(value: string): null | string {
  return value.trim() === '' ? null : value.trim()
}

/**
 * SchemaPage 的资源：分页与删除
 * 搜索字段按后端白名单下发：状态多选，关键字匹配编码与名称
 */
export function promptsResource(): SchemaResource<PromptRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(
      await aiPrompts.list(toPageRequest(params, { multiples: ['status'] })),
      params,
    ),
    remove: async id => throwOnFailed(await aiPrompts.delete({ ids: [String(id)] })),
  }
}

/**
 * 提示词的编辑弹窗：新建与编辑（照 4.x：编码只在新建时填，状态只在新建时选，之后走行内启停）
 * 列表不带正文，编辑先取详情；字段级错误挂回对应字段
 */
export function usePromptEditor() {
  const open = ref(false)
  const editing = ref<null | PromptDetail>(null)
  const form = reactive<PromptForm>(emptyPromptForm())
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<null | string>(null)
  const submitting = ref(false)
  const isEdit = computed(() => editing.value !== null)

  function reset(next: Partial<PromptForm> = {}) {
    Object.assign(form, emptyPromptForm(), next)
    fieldErrors.value = {}
    submitError.value = null
  }

  function startCreate() {
    editing.value = null
    reset()
    open.value = true
  }

  async function startEdit(id: string) {
    const detail = await aiPrompts.get({ id })
    editing.value = detail
    reset({
      code: detail.code,
      name: detail.name,
      category: detail.category ?? '',
      version: detail.version ?? '',
      content: detail.content,
      order: detail.order,
      status: detail.status,
      remark: detail.remark ?? '',
    })
    open.value = true
  }

  function fieldError(field: keyof PromptForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  async function submit(): Promise<boolean> {
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      const current = editing.value
      const common = {
        name: form.name.trim(),
        category: blankToNull(form.category),
        version: blankToNull(form.version),
        content: form.content,
        order: form.order,
        remark: blankToNull(form.remark),
      }
      if (current)
        await aiPrompts.update({ id: current.id, concurrencyStamp: current.concurrencyStamp, ...common })
      else
        await aiPrompts.create({ ...common, code: form.code.trim(), status: form.status })
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

  return { open, editing, isEdit, form, fieldError, submitError, submitting, startCreate, startEdit, submit }
}
