import { reactive, ref } from 'vue'
import { ApiError } from '~/request'
import { useAuthStore } from '~/stores'

export interface LoginForm {
  tenant: string
  userName: string
  password: string
}

/**
 * 登录页的状态与动作：表单、字段级错误（Schema 校验）与整体错误（用户名或密码不正确、账号不可用、锁定期等，detail 已本地化）。
 */
export function useLogin() {
  const auth = useAuthStore()
  const form = reactive<LoginForm>({ tenant: '', userName: '', password: '' })
  const fieldErrors = ref<Record<string, string[]>>({})
  const error = ref<string | null>(null)
  const submitting = ref(false)

  function fieldError(field: keyof LoginForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  async function submit(): Promise<boolean> {
    submitting.value = true
    error.value = null
    fieldErrors.value = {}
    try {
      await auth.login({ userName: form.userName.trim(), password: form.password, tenant: form.tenant.trim() || null })
      form.password = ''
      return true
    }
    catch (caught) {
      if (caught instanceof ApiError) {
        fieldErrors.value = caught.errors ?? {}
        if (!caught.hasFieldErrors)
          error.value = caught.detail
      }
      else {
        error.value = String(caught)
      }
      return false
    }
    finally {
      submitting.value = false
    }
  }

  return { form, fieldErrors, fieldError, error, submitting, submit }
}
