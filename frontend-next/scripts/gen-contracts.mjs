#!/usr/bin/env node
// 从后端导出的 contracts/openapi.json 生成前端契约层（方案 D5 / 6.3）：
//   packages/contracts/types.ts        openapi-typescript 的类型（paths / components / operations）
//   packages/contracts/operations.ts   路由表 + 按资源分组的调用函数（users.create → invoke(...)）
//   packages/contracts/permissions.ts  权限码字面量联合、操作 → 权限 / 策略映射、权限定义表
//   packages/contracts/enums.ts        字符串枚举的成员数组与类型
//   packages/contracts/realtime.ts     实时 Hub 路径与推送方法名（来自同目录的 contracts/realtime.json）
// 生成物入库；`--check` 重新生成到内存并与入库文件比对，有差异即失败（CI 漂移门禁）。
import { readFile, writeFile } from 'node:fs/promises'
import { dirname, join, resolve } from 'node:path'
import process from 'node:process'
import { fileURLToPath } from 'node:url'
import openapiTS, { astToString } from 'openapi-typescript'

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const SOURCE = join(ROOT, 'contracts', 'openapi.json')
const REALTIME_SOURCE = join(ROOT, 'contracts', 'realtime.json')
const OUT_DIR = join(ROOT, 'packages', 'contracts')
const HEADER = '// 由 scripts/gen-contracts.mjs 从 contracts/openapi.json 生成；不要手改，改后端契约后执行 pnpm gen:contracts。\n'

const check = process.argv.includes('--check')
const document = JSON.parse(await readFile(SOURCE, 'utf8'))
const realtime = JSON.parse(await readFile(REALTIME_SOURCE, 'utf8'))

/** 单引号字符串字面量（与 lint 风格一致） */
function quote(text) {
  return `'${String(text).replace(/\\/g, '\\\\').replace(/'/g, '\\\'')}'`
}

/** `users.create` → `usersCreate`、`order-lines.list` → `orderLinesList` */
function camel(text) {
  return text.replace(/[-.]([a-z0-9])/g, (_, c) => c.toUpperCase())
}

/** `order-lines` → `OrderLines` */
function pascal(text) {
  const c = camel(text)
  return c.charAt(0).toUpperCase() + c.slice(1)
}

/** 文档里的全部操作（不含普通端点）：按 operationId 排序，带方法、路由模板、参数落点与授权扩展 */
function collectOperations(doc) {
  const operations = []
  for (const [template, methods] of Object.entries(doc.paths ?? {})) {
    for (const [method, operation] of Object.entries(methods)) {
      if (!operation || typeof operation !== 'object' || !operation.operationId)
        continue
      // 普通端点（文件上传、下载）只有端点名、没有操作的稳定性标注：不进类型化客户端，前端经 request 的 uploadForm / endpointUrl 调用
      if (!operation['x-xihan-stability'])
        continue
      const parameters = operation.parameters ?? []
      operations.push({
        id: operation.operationId,
        method: method.toUpperCase(),
        template,
        path: parameters.filter(p => p.in === 'path').map(p => p.name),
        query: parameters.filter(p => p.in === 'query').map(p => p.name),
        hasBody: Boolean(operation.requestBody),
        success: Object.keys(operation.responses ?? {}).find(code => /^2\d\d$/.test(code)) ?? '200',
        permission: operation['x-xihan-permission'] ?? null,
        policy: operation['x-xihan-policy'] ?? null,
      })
    }
  }
  return operations.sort((a, b) => a.id.localeCompare(b.id))
}

function emitOperations(operations) {
  const lines = [HEADER]
  lines.push(`import type { CallOptions, RouteDef } from '~/request'`)
  lines.push(`import { invoke } from '~/request'`)
  lines.push(`import type { operations } from './types'`)
  lines.push('')
  lines.push('/** 每个操作的 HTTP 落点：方法、路由模板、路由值与查询串里的字段名；其余字段进请求体（GET / DELETE 没有请求体）。 */')
  lines.push('export const routes = {')
  for (const op of operations) {
    lines.push(`  '${op.id}': { method: '${op.method}', template: '${op.template}', path: [${op.path.map(p => `'${p}'`).join(', ')}], query: [${op.query.map(q => `'${q}'`).join(', ')}], success: ${op.success} },`)
  }
  lines.push('} as const satisfies Record<string, RouteDef>')
  lines.push('')
  lines.push('export type OperationId = keyof typeof routes')
  lines.push('')
  lines.push(`type JsonBody<T> = T extends { content: { 'application/json': infer B } } ? B : never`)
  lines.push(`type Params<T> = T extends { parameters: infer P } ? (P extends { path: infer A } ? (A extends object ? A : object) : object) & (P extends { query?: infer Q } ? (Q extends object ? Q : object) : object) : object`)
  lines.push(`/** 操作的请求类型：有请求体取请求体，否则是路由值与查询串的并集。 */`)
  // 没有请求体的操作（GET / DELETE，openapi-typescript 生成 requestBody?: never）取路由值与查询串；有请求体（必填或可选）取请求体
  lines.push(`type BodyOf<T> = T extends { requestBody?: infer R } ? JsonBody<NonNullable<R>> : never`)
  lines.push(`export type RequestOf<Id extends OperationId> = [BodyOf<operations[Id]>] extends [never] ? Params<operations[Id]> : BodyOf<operations[Id]>`)
  lines.push(`type SuccessCode<Id extends OperationId> = (typeof routes)[Id]['success']`)
  lines.push(`/** 操作的成功响应类型。 */`)
  lines.push(`export type ResponseOf<Id extends OperationId> = operations[Id]['responses'] extends infer R ? (SuccessCode<Id> extends keyof R ? JsonBody<R[SuccessCode<Id>]> : never) : never`)
  lines.push('')

  const byResource = new Map()
  for (const op of operations) {
    const [resource, verb] = op.id.split('.')
    if (!byResource.has(resource))
      byResource.set(resource, [])
    byResource.get(resource).push({ ...op, verb })
  }
  for (const [resource, ops] of byResource) {
    lines.push(`/** 资源 \`${resource}\` 的操作。 */`)
    lines.push(`export const ${camel(resource)} = {`)
    for (const op of ops) {
      lines.push(`  ${camel(op.verb)}: (request: RequestOf<'${op.id}'>, options?: CallOptions) => invoke<RequestOf<'${op.id}'>, ResponseOf<'${op.id}'>>('${op.id}', routes['${op.id}'], request, options),`)
    }
    lines.push('}')
    lines.push('')
  }
  return lines.join('\n')
}

function emitPermissions(doc, operations) {
  const definitions = doc.components?.['x-xihan-permissions'] ?? {}
  const codes = Object.keys(definitions).sort()
  const lines = [HEADER]
  lines.push(`import type { OperationId } from './operations'`)
  lines.push('')
  lines.push('/** 权限定义表（码、显示名、说明）：授权界面按它生成。 */')
  // 权限定义表叫 permissionDefinitions：资源 `permissions` 的调用门面占了 `permissions` 这个名字
  lines.push('export const permissionDefinitions = {')
  for (const code of codes) {
    const d = definitions[code]
    lines.push(`  '${code}': { code: '${code}', displayName: ${quote(d.displayName ?? '')}, description: ${quote(d.description ?? '')} },`)
  }
  lines.push('} as const')
  lines.push('')
  lines.push('/** 权限码字面量联合；`can()` 只认精确匹配，不认通配。 */')
  lines.push('export type Permission = keyof typeof permissionDefinitions')
  lines.push('')
  lines.push('/** 操作 → 保护它的权限码（按钮即操作：门控直接用操作的权限码）。 */')
  lines.push('export const operationPermission: Partial<Record<OperationId, Permission>> = {')
  for (const op of operations.filter(o => o.permission))
    lines.push(`  '${op.id}': '${op.permission}',`)
  lines.push('}')
  lines.push('')
  lines.push('/** 操作 → 策略名（如 `authenticated`）；与权限码二选一。 */')
  lines.push('export const operationPolicy: Partial<Record<OperationId, string>> = {')
  for (const op of operations.filter(o => o.policy))
    lines.push(`  '${op.id}': '${op.policy}',`)
  lines.push('}')
  lines.push('')
  return lines.join('\n')
}

/**
 * 收集字符串枚举：按字符串序列化的 C# 枚举进 `components.schemas/<枚举名>`（type string + enum），名字就是枚举名；
 * 仍内联在属性上的枚举（整数枚举不在此列）按 `<Schema 标题><属性 Pascal>` 起名。
 */
function collectEnums(doc) {
  const found = new Map()
  for (const [name, schema] of Object.entries(doc.components?.schemas ?? {})) {
    if (schema?.type === 'string' && Array.isArray(schema.enum) && schema.enum.every(v => typeof v === 'string'))
      found.set(pascal(name), schema.enum)
  }
  const visitSchema = (schema, title) => {
    if (!schema || typeof schema !== 'object')
      return
    const name = schema.title ?? title
    for (const [property, definition] of Object.entries(schema.properties ?? {})) {
      if (Array.isArray(definition?.enum) && definition.enum.every(v => typeof v === 'string') && name) {
        const key = `${pascal(name)}${pascal(property)}`
        if (!found.has(key))
          found.set(key, definition.enum)
      }
      if (definition?.properties)
        visitSchema(definition, `${name}${pascal(property)}`)
    }
  }
  for (const [name, schema] of Object.entries(doc.components?.schemas ?? {}))
    visitSchema(schema, name)
  for (const methods of Object.values(doc.paths ?? {})) {
    for (const operation of Object.values(methods)) {
      if (!operation || typeof operation !== 'object')
        continue
      visitSchema(operation.requestBody?.content?.['application/json']?.schema)
      for (const response of Object.values(operation.responses ?? {}))
        visitSchema(response?.content?.['application/json']?.schema)
    }
  }
  return [...found.entries()].sort(([a], [b]) => a.localeCompare(b))
}

function emitEnums(enums) {
  const lines = [HEADER]
  lines.push('// 枚举文案由后端消息目录按语言提供（随 N1 的 metadata.enums.list 接入）；这里只有成员与类型。')
  lines.push('')
  for (const [name, members] of enums) {
    lines.push(`export const ${name} = [${members.map(m => `'${m}'`).join(', ')}] as const`)
    lines.push(`export type ${name} = (typeof ${name})[number]`)
    lines.push('')
  }
  lines.push('/** 全部枚举：类型名 → 成员。 */')
  lines.push('export const enums = {')
  for (const [name] of enums)
    lines.push(`  ${name},`)
  lines.push('} as const')
  lines.push('')
  lines.push('export type EnumName = keyof typeof enums')
  lines.push('')
  lines.push('/** 枚举成员的显示文案；后端枚举文案（metadata.enums.list）接入前原样返回成员名，未知成员也原样返回。 */')
  lines.push('export function enumLabel(_type: EnumName, value: string): string {')
  lines.push('  return value')
  lines.push('}')
  lines.push('')
  return lines.join('\n')
}

/** `/hubs/notification` → `Notification` */
function hubName(path) {
  return pascal(path.split('/').filter(Boolean).at(-1))
}

function emitRealtime(manifest) {
  const hubs = manifest.hubs ?? []
  const methods = [...new Set(hubs.flatMap(hub => hub.methods ?? []))].sort()
  const lines = [HEADER.replace('contracts/openapi.json', 'contracts/realtime.json')]
  lines.push('/** 实时 Hub 的路径（后端 RealtimeHubs）。 */')
  lines.push('export const RealtimeHubs = {')
  for (const hub of hubs)
    lines.push(`  ${hubName(hub.path)}: ${quote(hub.path)},`)
  lines.push('} as const')
  lines.push('')
  lines.push('/** 服务端推送的方法名（后端 RealtimeMethods）：订阅一律用这里的常量，后端改名时生成物漂移、类型检查报错。 */')
  lines.push('export const RealtimeMethods = {')
  for (const method of methods)
    lines.push(`  ${method}: ${quote(method)},`)
  lines.push('} as const')
  lines.push('')
  lines.push('export type RealtimeMethod = typeof RealtimeMethods[keyof typeof RealtimeMethods]')
  lines.push('')
  return lines.join('\n')
}

async function emitTypes(doc) {
  const ast = await openapiTS(doc, { exportType: false, alphabetize: false })
  return `${HEADER}/* eslint-disable */\n${astToString(ast)}`
}

const operations = collectOperations(document)
const outputs = {
  'types.ts': await emitTypes(document),
  'operations.ts': emitOperations(operations),
  'permissions.ts': emitPermissions(document, operations),
  'enums.ts': emitEnums(collectEnums(document)),
  'realtime.ts': emitRealtime(realtime),
}

let drift = 0
for (const [file, content] of Object.entries(outputs)) {
  const target = join(OUT_DIR, file)
  if (check) {
    // 只比内容：Windows 上 autocrlf 会把入库的生成物检出成 CRLF，换行符不算漂移
    const current = (await readFile(target, 'utf8').catch(() => null))?.replaceAll('\r\n', '\n') ?? null
    if (current !== content.replaceAll('\r\n', '\n')) {
      drift++
      console.error(`[contracts:check] ${file} 与 contracts/openapi.json 不一致，执行 pnpm gen:contracts 后提交`)
    }
  }
  else {
    await writeFile(target, content, 'utf8')
    console.log(`[gen:contracts] ${file}`)
  }
}

if (drift > 0)
  process.exit(1)
if (check)
  console.log(`[contracts:check] 通过（${operations.length} 个操作与 contracts/openapi.json 一致）`)
