// 由 scripts/gen-contracts.mjs 从 contracts/openapi.json 生成；不要手改，改后端契约后执行 pnpm gen:contracts。

import type { OperationId } from './operations'

/** 权限定义表（码、显示名、说明）：授权界面按它生成。 */
export const permissionDefinitions = {
  'access.permissions.read': { code: 'access.permissions.read', displayName: 'Read permissions', description: 'View the permission catalog of the current side.' },
  'access.roles.assign': { code: 'access.roles.assign', displayName: 'Assign members', description: 'Assign users to roles and remove them.' },
  'access.roles.create': { code: 'access.roles.create', displayName: 'Create roles', description: 'Create roles on the current side (platform or tenant).' },
  'access.roles.delete': { code: 'access.roles.delete', displayName: 'Delete roles', description: 'Delete roles together with their members and permissions.' },
  'access.roles.grant': { code: 'access.roles.grant', displayName: 'Grant permissions', description: 'Grant permissions to roles and revoke them.' },
  'access.roles.read': { code: 'access.roles.read', displayName: 'Read roles', description: 'View roles, their members and their permissions.' },
  'access.roles.update': { code: 'access.roles.update', displayName: 'Update roles', description: 'Edit role names, descriptions, order and status.' },
  'identity.users.create': { code: 'identity.users.create', displayName: 'Create users', description: 'Create users in the current tenant.' },
  'identity.users.delete': { code: 'identity.users.delete', displayName: 'Delete users', description: 'Delete users of the current tenant.' },
  'identity.users.lock': { code: 'identity.users.lock', displayName: 'Lock users', description: 'Lock and unlock users of the current tenant.' },
  'identity.users.read': { code: 'identity.users.read', displayName: 'Read users', description: 'View users of the current tenant.' },
  'identity.users.reset-password': { code: 'identity.users.reset-password', displayName: 'Reset passwords', description: 'Reset user passwords; the user must change the password at next sign-in.' },
  'identity.users.update': { code: 'identity.users.update', displayName: 'Update users', description: 'Edit user profiles of the current tenant.' },
} as const

/** 权限码字面量联合；`can()` 只认精确匹配，不认通配。 */
export type Permission = keyof typeof permissionDefinitions

/** 操作 → 保护它的权限码（按钮即操作：门控直接用操作的权限码）。 */
export const operationPermission: Partial<Record<OperationId, Permission>> = {
  'permissions.list': 'access.permissions.read',
  'roles.assign': 'access.roles.assign',
  'roles.create': 'access.roles.create',
  'roles.delete': 'access.roles.delete',
  'roles.get': 'access.roles.read',
  'roles.grant': 'access.roles.grant',
  'roles.list': 'access.roles.read',
  'roles.members': 'access.roles.read',
  'roles.revoke': 'access.roles.grant',
  'roles.unassign': 'access.roles.assign',
  'roles.update': 'access.roles.update',
  'users.create': 'identity.users.create',
  'users.delete': 'identity.users.delete',
  'users.get': 'identity.users.read',
  'users.list': 'identity.users.read',
  'users.lock': 'identity.users.lock',
  'users.reset-password': 'identity.users.reset-password',
  'users.unlock': 'identity.users.lock',
  'users.update': 'identity.users.update',
}

/** 操作 → 策略名（如 `authenticated`）；与权限码二选一。 */
export const operationPolicy: Partial<Record<OperationId, string>> = {
  'auth.logout': 'authenticated',
  'auth.me': 'authenticated',
  'auth.permissions': 'authenticated',
  'profile.change-password': 'authenticated',
}
