// 由 scripts/gen-contracts.mjs 从 contracts/openapi.json 生成；不要手改，改后端契约后执行 pnpm gen:contracts。

import type { OperationId } from './operations'

/** 权限定义表（码、显示名、说明）：授权界面按它生成。 */
export const permissions = {
  'identity.users.create': { code: 'identity.users.create', displayName: 'Create users', description: 'Create users in the current tenant.' },
  'identity.users.read': { code: 'identity.users.read', displayName: 'Read users', description: 'View users of the current tenant.' },
} as const

/** 权限码字面量联合；`can()` 只认精确匹配，不认通配。 */
export type Permission = keyof typeof permissions

/** 操作 → 保护它的权限码（按钮即操作：门控直接用操作的权限码）。 */
export const operationPermission: Partial<Record<OperationId, Permission>> = {
  'users.create': 'identity.users.create',
  'users.get': 'identity.users.read',
  'users.list': 'identity.users.read',
}

/** 操作 → 策略名（如 `authenticated`）；与权限码二选一。 */
export const operationPolicy: Partial<Record<OperationId, string>> = {
}
