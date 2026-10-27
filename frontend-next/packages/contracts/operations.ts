// 由 scripts/gen-contracts.mjs 从 contracts/openapi.json 生成；不要手改，改后端契约后执行 pnpm gen:contracts。

import type { CallOptions, RouteDef } from '~/request'
import { invoke } from '~/request'
import type { operations } from './types'

/** 每个操作的 HTTP 落点：方法、路由模板、路由值与查询串里的字段名；其余字段进请求体（GET / DELETE 没有请求体）。 */
export const routes = {
  'auth.login': { method: 'POST', template: '/api/auth:login', path: [], query: [], success: 200 },
  'auth.logout': { method: 'POST', template: '/api/auth:logout', path: [], query: [], success: 200 },
  'auth.me': { method: 'POST', template: '/api/auth:me', path: [], query: [], success: 200 },
  'auth.permissions': { method: 'POST', template: '/api/auth:permissions', path: [], query: [], success: 200 },
  'auth.refresh': { method: 'POST', template: '/api/auth:refresh', path: [], query: [], success: 200 },
  'permissions.list': { method: 'GET', template: '/api/permissions', path: [], query: [], success: 200 },
  'profile.change-password': { method: 'POST', template: '/api/profile:change-password', path: [], query: [], success: 200 },
  'roles.assign': { method: 'POST', template: '/api/roles/{id}:assign', path: ['id'], query: [], success: 200 },
  'roles.create': { method: 'POST', template: '/api/roles', path: [], query: [], success: 201 },
  'roles.delete': { method: 'DELETE', template: '/api/roles', path: [], query: ['ids'], success: 200 },
  'roles.get': { method: 'GET', template: '/api/roles/{id}', path: ['id'], query: [], success: 200 },
  'roles.grant': { method: 'POST', template: '/api/roles/{id}:grant', path: ['id'], query: [], success: 200 },
  'roles.list': { method: 'POST', template: '/api/roles:list', path: [], query: [], success: 200 },
  'roles.members': { method: 'POST', template: '/api/roles/{id}:members', path: ['id'], query: [], success: 200 },
  'roles.revoke': { method: 'POST', template: '/api/roles/{id}:revoke', path: ['id'], query: [], success: 200 },
  'roles.unassign': { method: 'POST', template: '/api/roles/{id}:unassign', path: ['id'], query: [], success: 200 },
  'roles.update': { method: 'PUT', template: '/api/roles/{id}', path: ['id'], query: [], success: 200 },
  'users.create': { method: 'POST', template: '/api/users', path: [], query: [], success: 201 },
  'users.delete': { method: 'DELETE', template: '/api/users', path: [], query: ['ids'], success: 200 },
  'users.get': { method: 'GET', template: '/api/users/{id}', path: ['id'], query: [], success: 200 },
  'users.list': { method: 'POST', template: '/api/users:list', path: [], query: [], success: 200 },
  'users.lock': { method: 'POST', template: '/api/users:lock', path: [], query: [], success: 200 },
  'users.reset-password': { method: 'POST', template: '/api/users/{id}:reset-password', path: ['id'], query: [], success: 200 },
  'users.unlock': { method: 'POST', template: '/api/users:unlock', path: [], query: [], success: 200 },
  'users.update': { method: 'PUT', template: '/api/users/{id}', path: ['id'], query: [], success: 200 },
} as const satisfies Record<string, RouteDef>

export type OperationId = keyof typeof routes

type JsonBody<T> = T extends { content: { 'application/json': infer B } } ? B : never
type Params<T> = T extends { parameters: infer P } ? (P extends { path: infer A } ? (A extends object ? A : object) : object) & (P extends { query?: infer Q } ? (Q extends object ? Q : object) : object) : object
/** 操作的请求类型：有请求体取请求体，否则是路由值与查询串的并集。 */
type BodyOf<T> = T extends { requestBody?: infer R } ? JsonBody<NonNullable<R>> : never
export type RequestOf<Id extends OperationId> = [BodyOf<operations[Id]>] extends [never] ? Params<operations[Id]> : BodyOf<operations[Id]>
type SuccessCode<Id extends OperationId> = (typeof routes)[Id]['success']
/** 操作的成功响应类型。 */
export type ResponseOf<Id extends OperationId> = operations[Id]['responses'] extends infer R ? (SuccessCode<Id> extends keyof R ? JsonBody<R[SuccessCode<Id>]> : never) : never

/** 资源 `auth` 的操作。 */
export const auth = {
  login: (request: RequestOf<'auth.login'>, options?: CallOptions) => invoke<RequestOf<'auth.login'>, ResponseOf<'auth.login'>>('auth.login', routes['auth.login'], request, options),
  logout: (request: RequestOf<'auth.logout'>, options?: CallOptions) => invoke<RequestOf<'auth.logout'>, ResponseOf<'auth.logout'>>('auth.logout', routes['auth.logout'], request, options),
  me: (request: RequestOf<'auth.me'>, options?: CallOptions) => invoke<RequestOf<'auth.me'>, ResponseOf<'auth.me'>>('auth.me', routes['auth.me'], request, options),
  permissions: (request: RequestOf<'auth.permissions'>, options?: CallOptions) => invoke<RequestOf<'auth.permissions'>, ResponseOf<'auth.permissions'>>('auth.permissions', routes['auth.permissions'], request, options),
  refresh: (request: RequestOf<'auth.refresh'>, options?: CallOptions) => invoke<RequestOf<'auth.refresh'>, ResponseOf<'auth.refresh'>>('auth.refresh', routes['auth.refresh'], request, options),
}

/** 资源 `permissions` 的操作。 */
export const permissions = {
  list: (request: RequestOf<'permissions.list'>, options?: CallOptions) => invoke<RequestOf<'permissions.list'>, ResponseOf<'permissions.list'>>('permissions.list', routes['permissions.list'], request, options),
}

/** 资源 `profile` 的操作。 */
export const profile = {
  changePassword: (request: RequestOf<'profile.change-password'>, options?: CallOptions) => invoke<RequestOf<'profile.change-password'>, ResponseOf<'profile.change-password'>>('profile.change-password', routes['profile.change-password'], request, options),
}

/** 资源 `roles` 的操作。 */
export const roles = {
  assign: (request: RequestOf<'roles.assign'>, options?: CallOptions) => invoke<RequestOf<'roles.assign'>, ResponseOf<'roles.assign'>>('roles.assign', routes['roles.assign'], request, options),
  create: (request: RequestOf<'roles.create'>, options?: CallOptions) => invoke<RequestOf<'roles.create'>, ResponseOf<'roles.create'>>('roles.create', routes['roles.create'], request, options),
  delete: (request: RequestOf<'roles.delete'>, options?: CallOptions) => invoke<RequestOf<'roles.delete'>, ResponseOf<'roles.delete'>>('roles.delete', routes['roles.delete'], request, options),
  get: (request: RequestOf<'roles.get'>, options?: CallOptions) => invoke<RequestOf<'roles.get'>, ResponseOf<'roles.get'>>('roles.get', routes['roles.get'], request, options),
  grant: (request: RequestOf<'roles.grant'>, options?: CallOptions) => invoke<RequestOf<'roles.grant'>, ResponseOf<'roles.grant'>>('roles.grant', routes['roles.grant'], request, options),
  list: (request: RequestOf<'roles.list'>, options?: CallOptions) => invoke<RequestOf<'roles.list'>, ResponseOf<'roles.list'>>('roles.list', routes['roles.list'], request, options),
  members: (request: RequestOf<'roles.members'>, options?: CallOptions) => invoke<RequestOf<'roles.members'>, ResponseOf<'roles.members'>>('roles.members', routes['roles.members'], request, options),
  revoke: (request: RequestOf<'roles.revoke'>, options?: CallOptions) => invoke<RequestOf<'roles.revoke'>, ResponseOf<'roles.revoke'>>('roles.revoke', routes['roles.revoke'], request, options),
  unassign: (request: RequestOf<'roles.unassign'>, options?: CallOptions) => invoke<RequestOf<'roles.unassign'>, ResponseOf<'roles.unassign'>>('roles.unassign', routes['roles.unassign'], request, options),
  update: (request: RequestOf<'roles.update'>, options?: CallOptions) => invoke<RequestOf<'roles.update'>, ResponseOf<'roles.update'>>('roles.update', routes['roles.update'], request, options),
}

/** 资源 `users` 的操作。 */
export const users = {
  create: (request: RequestOf<'users.create'>, options?: CallOptions) => invoke<RequestOf<'users.create'>, ResponseOf<'users.create'>>('users.create', routes['users.create'], request, options),
  delete: (request: RequestOf<'users.delete'>, options?: CallOptions) => invoke<RequestOf<'users.delete'>, ResponseOf<'users.delete'>>('users.delete', routes['users.delete'], request, options),
  get: (request: RequestOf<'users.get'>, options?: CallOptions) => invoke<RequestOf<'users.get'>, ResponseOf<'users.get'>>('users.get', routes['users.get'], request, options),
  list: (request: RequestOf<'users.list'>, options?: CallOptions) => invoke<RequestOf<'users.list'>, ResponseOf<'users.list'>>('users.list', routes['users.list'], request, options),
  lock: (request: RequestOf<'users.lock'>, options?: CallOptions) => invoke<RequestOf<'users.lock'>, ResponseOf<'users.lock'>>('users.lock', routes['users.lock'], request, options),
  resetPassword: (request: RequestOf<'users.reset-password'>, options?: CallOptions) => invoke<RequestOf<'users.reset-password'>, ResponseOf<'users.reset-password'>>('users.reset-password', routes['users.reset-password'], request, options),
  unlock: (request: RequestOf<'users.unlock'>, options?: CallOptions) => invoke<RequestOf<'users.unlock'>, ResponseOf<'users.unlock'>>('users.unlock', routes['users.unlock'], request, options),
  update: (request: RequestOf<'users.update'>, options?: CallOptions) => invoke<RequestOf<'users.update'>, ResponseOf<'users.update'>>('users.update', routes['users.update'], request, options),
}
