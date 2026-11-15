/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { PasswordPolicy } from '~/types'

type Translate = (key: string, params?: Record<string, unknown>) => string

/**
 * 新密码没满足的第一条规则的文案，都满足为 null
 * 规则来自登录页配置（系统参数 identity.password），与后端同一顺序、同一口径：字母、数字与大小写按 Unicode，其他字符是字母与数字以外的可见字符；最终以后端校验为准
 */
export function passwordPolicyError(password: string, policy: PasswordPolicy, t: Translate): null | string {
  if (password.length < policy.minLength)
    return t('page.auth.password_rule_length', { min: policy.minLength })
  if (policy.requireLetter && !/\p{L}/u.test(password))
    return t('page.auth.password_rule_letter')
  if (policy.requireDigit && !/\p{Nd}/u.test(password))
    return t('page.auth.password_rule_digit')
  if (policy.requireUppercase && !/\p{Lu}/u.test(password))
    return t('page.auth.password_rule_uppercase')
  if (policy.requireLowercase && !/\p{Ll}/u.test(password))
    return t('page.auth.password_rule_lowercase')
  if (policy.requireSymbol && !/[^\p{L}\p{Nd}\s\p{Cc}]/u.test(password))
    return t('page.auth.password_rule_symbol')
  return null
}
