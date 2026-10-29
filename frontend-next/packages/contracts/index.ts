// 契约层入口：五个文件由 scripts/gen-contracts.mjs 从 contracts/openapi.json 与 contracts/realtime.json 生成并入库，CI 以「重新生成后 diff 为空」做漂移门禁；
// paging.ts 是手写的 SchemaPage 查询 / 分页适配（4.x 由 @/api 的 createPageRequest 承担）。
export * from './enums'
export * from './operations'
export * from './paging'
export * from './permissions'
export * from './realtime'
export type { components, operations, paths } from './types'
