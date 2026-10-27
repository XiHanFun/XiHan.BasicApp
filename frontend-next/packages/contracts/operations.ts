// 由 scripts/gen-contracts.mjs 从 contracts/openapi.json 生成；不要手改，改后端契约后执行 pnpm gen:contracts。

import type { CallOptions, RouteDef } from '~/request'
import { invoke } from '~/request'
import type { operations } from './types'

/** 每个操作的 HTTP 落点：方法、路由模板、路由值与查询串里的字段名；其余字段进请求体（GET / DELETE 没有请求体）。 */
export const routes = {
  'users.create': { method: 'POST', template: '/api/users', path: [], query: [], success: 201 },
  'users.get': { method: 'GET', template: '/api/users/{id}', path: ['id'], query: [], success: 200 },
  'users.list': { method: 'POST', template: '/api/users:list', path: [], query: [], success: 200 },
} as const satisfies Record<string, RouteDef>

export type OperationId = keyof typeof routes

type JsonBody<T> = T extends { content: { 'application/json': infer B } } ? B : never
type Params<T> = T extends { parameters: infer P } ? (P extends { path: infer A } ? (A extends object ? A : object) : object) & (P extends { query?: infer Q } ? (Q extends object ? Q : object) : object) : object
/** 操作的请求类型：有请求体取请求体，否则是路由值与查询串的并集。 */
export type RequestOf<Id extends OperationId> = operations[Id] extends { requestBody: infer R } ? JsonBody<R> : (operations[Id] extends { requestBody?: infer R } ? JsonBody<R> : Params<operations[Id]>)
type SuccessCode<Id extends OperationId> = (typeof routes)[Id]['success']
/** 操作的成功响应类型。 */
export type ResponseOf<Id extends OperationId> = operations[Id]['responses'] extends infer R ? (SuccessCode<Id> extends keyof R ? JsonBody<R[SuccessCode<Id>]> : never) : never

/** 资源 `users` 的操作。 */
export const users = {
  create: (request: RequestOf<'users.create'>, options?: CallOptions) => invoke<RequestOf<'users.create'>, ResponseOf<'users.create'>>('users.create', routes['users.create'], request, options),
  get: (request: RequestOf<'users.get'>, options?: CallOptions) => invoke<RequestOf<'users.get'>, ResponseOf<'users.get'>>('users.get', routes['users.get'], request, options),
  list: (request: RequestOf<'users.list'>, options?: CallOptions) => invoke<RequestOf<'users.list'>, ResponseOf<'users.list'>>('users.list', routes['users.list'], request, options),
}
