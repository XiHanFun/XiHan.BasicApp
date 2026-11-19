/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/** 有效期草稿：时间戳（毫秒），为空即不限 */
export interface GrantWindowDraft {
  effectiveAt: null | number
  expiresAt: null | number
}

/** 授予与拒绝的有效期另带原因 */
export interface PermissionWindowDraft extends GrantWindowDraft {
  reason: string
}

export function emptyGrantWindow(): PermissionWindowDraft {
  return {
    effectiveAt: null,
    expiresAt: null,
    reason: '',
  }
}

function toTimestamp(value?: null | string): null | number {
  return value
    ? new Date(value).getTime()
    : null
}

/** 已有授予、拒绝或指派的有效期 → 草稿 */
export function grantWindowOf(source: { effectiveAt?: null | string, expiresAt?: null | string, reason?: null | string }): PermissionWindowDraft {
  return {
    effectiveAt: toTimestamp(source.effectiveAt),
    expiresAt: toTimestamp(source.expiresAt),
    reason: source.reason ?? '',
  }
}

/** 两头都有时到期必须晚于生效（与后端 ACCESS_VALIDATION 同一条规则） */
export function isGrantWindowValid(window: GrantWindowDraft): boolean {
  return window.effectiveAt === null || window.expiresAt === null || window.expiresAt > window.effectiveAt
}

/** 草稿 → 请求字段：为空的不写，后端按立即生效、长期有效处理 */
export function toGrantWindowPayload(window: GrantWindowDraft): { effectiveAt?: string, expiresAt?: string } {
  return {
    ...(window.effectiveAt === null
      ? {}
      : {
          effectiveAt: new Date(window.effectiveAt).toISOString(),
        }),
    ...(window.expiresAt === null
      ? {}
      : {
          expiresAt: new Date(window.expiresAt).toISOString(),
        }),
  }
}

/** 授予与拒绝的请求字段：有效期与原因都为空即普通授予（后端删掉原有的规则行） */
export function toPermissionWindowPayload(window: PermissionWindowDraft): { effectiveAt?: string, expiresAt?: string, reason?: string } {
  const reason = window.reason.trim()
  return {
    ...toGrantWindowPayload(window),
    ...(reason === ''
      ? {}
      : { reason }),
  }
}
