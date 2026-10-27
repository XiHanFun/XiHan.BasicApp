import { reactive, ref } from 'vue'
import { profile } from '~/contracts'
import { ApiError } from '~/request'
import { useAuthStore } from '~/stores'

export interface ChangePasswordForm {
  currentPassword: string
  newPassword: string
  confirmPassword: string
}

/**
 * 本人改密：两次新密码一致由页面判（只是输入辅助），策略、历史与当前密码校验以后端为准；成功后当前会话保留，强制改密标记清除。
 */
export function useChangePassword(mismatchMessage: () => string) {
  const auth = useAuthStore()
  const form = reactive<ChangePasswordForm>({ currentPassword: '', newPassword: '', confirmPassword: '' })
  const fieldErrors = ref<Record<string, string[]>>({})
  const error = ref<string | null>(null)
  const succeeded = ref(false)
  const submitting = ref(false)

  function fieldError(field: keyof ChangePasswordForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  async function submit(): Promise<boolean> {
    error.value = null
    succeeded.value = false
    fieldErrors.value = {}
    if (form.newPassword !== form.confirmPassword) {
      fieldErrors.value = { confirmPassword: [mismatchMessage()] }
      return false
    }
    submitting.value = true
    try {
      await profile.changePassword({ currentPassword: form.currentPassword, newPassword: form.newPassword })
      auth.markPasswordChanged()
      form.currentPassword = ''
      form.newPassword = ''
      form.confirmPassword = ''
      succeeded.value = true
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

  return { form, fieldErrors, fieldError, error, succeeded, submitting, submit }
}
