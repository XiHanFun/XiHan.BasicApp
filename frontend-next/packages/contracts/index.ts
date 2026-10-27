// 契约层入口：四个文件由 scripts/gen-contracts.mjs 从 contracts/openapi.json 生成并入库，CI 以「重新生成后 diff 为空」做漂移门禁。
export * from './enums'
export * from './operations'
export * from './permissions'
export type { components, operations, paths } from './types'
