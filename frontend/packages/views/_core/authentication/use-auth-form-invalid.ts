/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { FormInvalidDetails } from '@xihan-ui/headless'
import { useI18n } from 'vue-i18n'
import { toast } from '~/composables'

/**
 * 认证类表单的校验失败反馈，走浮层提示
 * 不摆 XhFieldErrorText：块级错误文案会撑高卡片，把底部的第三方登录与演示账号挤出可视区
 */
export function useAuthFormInvalid() {
  const { t } = useI18n()

  return function onInvalid(details: FormInvalidDetails): void {
    const first = Object.values(details.errors).find(message => message)
    toast.warning(first || t('common.messages.validate_failed'))
  }
}
