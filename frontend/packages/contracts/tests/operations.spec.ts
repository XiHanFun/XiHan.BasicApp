import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import process from 'node:process'
import { describe, expect, it } from 'vitest'
import { routes, users } from '~/contracts/operations'
import { operationPermission, permissionDefinitions } from '~/contracts/permissions'

interface DocumentOperation {
  'operationId'?: string
  'x-xihan-permission'?: string
  'x-xihan-stability'?: string
  'requestBody'?: { content?: Record<string, unknown> }
  'responses'?: Record<string, { content?: Record<string, unknown> }>
}

/** 请求体或成功响应不是 JSON 的是普通端点（文件上传、下载），操作只收发 JSON */
function isOperation(op: DocumentOperation): boolean {
  const types = [
    ...Object.keys(op.requestBody?.content ?? {}),
    ...Object.entries(op.responses ?? {}).filter(([code]) => /^2\d\d$/.test(code)).flatMap(([, response]) => Object.keys(response.content ?? {})),
  ]
  return Boolean(op.operationId) && types.every(type => /^application\/(?:[\w.+-]+\+)?json\b/i.test(type))
}

/** 生成物与 contracts/openapi.json 的对应关系：路由表覆盖全部操作（普通端点不进类型化客户端），权限码来自文档的扩展 */
describe('generated contracts', () => {
  const document = JSON.parse(readFileSync(resolve(process.cwd(), 'contracts/openapi.json'), 'utf8')) as {
    paths: Record<string, Record<string, DocumentOperation>>
    components: { 'x-xihan-permissions': Record<string, { code: string }> }
  }

  it('has a route for every operation in the document', () => {
    const ids = Object.values(document.paths).flatMap(methods => Object.values(methods).filter(isOperation).map(op => op.operationId!))
    expect(Object.keys(routes).sort()).toEqual(ids.sort())
    // 普通端点（文件上传、下载）有端点名但不是操作
    expect(Object.keys(routes)).not.toContain('files.upload')
    expect(Object.keys(routes)).not.toContain('files.content')
    // Stable 的操作不带 x-xihan-stability，可选模块的 Preview 操作带着，两种都是操作
    const stabilityOf = (id: string) => Object.values(document.paths).flatMap(methods => Object.values(methods)).find(op => op.operationId === id)?.['x-xihan-stability']
    expect(stabilityOf('users.create')).toBeUndefined()
    expect(stabilityOf('chat-audit.list')).toBe('Preview')
    expect(Object.keys(routes)).toEqual(expect.arrayContaining(['users.create', 'chat-audit.list']))
  })

  it('derives users.create as POST /api/users and users.list as POST /api/users:list', () => {
    expect(routes['users.create']).toMatchObject({ method: 'POST', template: '/api/users', success: 201 })
    expect(routes['users.get']).toMatchObject({
      method: 'GET',
      template: '/api/users/{id}',
      path: ['id'],
    })
    expect(routes['users.list']).toMatchObject({ method: 'POST', template: '/api/users:list' })
    expect(typeof users.create).toBe('function')
  })

  it('maps operations to their permission codes from x-xihan-permission', () => {
    for (const methods of Object.values(document.paths)) {
      for (const op of Object.values(methods)) {
        if (isOperation(op) && op['x-xihan-permission'])
          expect(operationPermission[op.operationId as keyof typeof operationPermission]).toBe(op['x-xihan-permission'])
      }
    }
    expect(Object.keys(permissionDefinitions).sort()).toEqual(Object.keys(document.components['x-xihan-permissions']).sort())
  })
})
