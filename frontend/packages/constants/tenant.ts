/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * 平台在租户目录里的 ID（后端 `TenantIds.Platform`）：平台账号的注册地租户
 * 会话与切换请求里的 `tenantId` 为空即平台；这个值只用来判断「注册地是否平台」
 */
export const PLATFORM_TENANT_ID = 'platform'

/**
 * `users.list` 的筛选：去掉运维支持成员
 * 他们只能配角色，部门、委托、字段安全与约束规则这些选人入口后端都不认，列出来选了也是报错
 */
export const NOT_SUPPORT_MEMBER_FILTER = { field: 'isSupportMember', operator: 'eq', value: 'false' } as const
