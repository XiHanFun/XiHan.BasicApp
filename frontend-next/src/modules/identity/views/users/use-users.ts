import type { RequestOf, ResponseOf } from '~/contracts'
import { computed, reactive, ref } from 'vue'
import { users } from '~/contracts'
import { ApiError } from '~/request'

export type UserRow = ResponseOf<'users.list'>['items'][number]
export type CreateUserForm = RequestOf<'users.create'>

/**
 * 用户页的状态与动作：分页列表、创建表单、字段级错误。契约类型来自生成物；错误只看 `code` 与 `errors`。
 */
export function useUsers(pageSize = 10) {
  const items = ref<UserRow[]>([])
  const total = ref(0)
  const pageIndex = ref(1)
  const loading = ref(false)
  const listError = ref<string | null>(null)
  const form = reactive<CreateUserForm>({ userName: '', displayName: '', email: null })
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<string | null>(null)
  const submitting = ref(false)
  const pageCount = computed(() => Math.max(1, Math.ceil(total.value / pageSize)))

  async function load(index = pageIndex.value) {
    loading.value = true
    listError.value = null
    try {
      const result = await users.list({ page: { index, size: pageSize }, filters: [], sorts: [{ field: 'createdAt', descending: true }] })
      items.value = result.items
      total.value = Number(result.total)
      pageIndex.value = result.page.index ?? index
    }
    catch (error) {
      items.value = []
      total.value = 0
      listError.value = error instanceof ApiError ? `${error.code}: ${error.detail}` : String(error)
    }
    finally {
      loading.value = false
    }
  }

  function fieldError(field: keyof CreateUserForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  async function submit(): Promise<boolean> {
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      await users.create({ userName: form.userName, displayName: form.displayName, email: form.email || null })
      form.userName = ''
      form.displayName = ''
      form.email = null
      await load(1)
      return true
    }
    catch (error) {
      if (error instanceof ApiError) {
        // 字段级错误交给表单项；其余按 code 展示本地化的 detail
        fieldErrors.value = error.errors ?? {}
        if (!error.hasFieldErrors)
          submitError.value = `${error.code}: ${error.detail}`
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

  return { items, total, pageIndex, pageCount, pageSize, loading, listError, form, fieldErrors, fieldError, submitError, submitting, load, submit }
}
