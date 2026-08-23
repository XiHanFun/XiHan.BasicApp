import type { FormInvalidDetails } from '@xihan-ui/headless'
import { useI18n } from 'vue-i18n'
import { toast } from '~/composables'

/**
 * 认证类表单的校验失败反馈。
 *
 * 这几页不摆 XhFieldErrorText：错误文案是块级节点，出现时把整张卡片撑高，
 * 底部的第三方登录与演示账号会被挤出可视区。校验照常拦提交，反馈改走浮层提示。
 */
export function useAuthFormInvalid() {
  const { t } = useI18n()

  return function onInvalid(details: FormInvalidDetails): void {
    const first = Object.values(details.errors).find(message => message)
    toast.warning(first || t('common.messages.validate_failed'))
  }
}
