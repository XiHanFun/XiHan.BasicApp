import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import process from 'node:process'
import { describe, expect, it } from 'vitest'
import { routes, users } from './operations'
import { operationPermission, permissionDefinitions } from './permissions'

/** 生成物与 contracts/openapi.json 的对应关系：路由表覆盖全部操作（带 x-xihan-stability；普通端点不进类型化客户端），权限码来自文档的扩展。 */
describe('generated contracts', () => {
  const document = JSON.parse(readFileSync(resolve(process.cwd(), 'contracts/openapi.json'), 'utf8')) as {
    paths: Record<string, Record<string, { 'operationId'?: string, 'x-xihan-permission'?: string, 'x-xihan-stability'?: string }>>
    components: { 'x-xihan-permissions': Record<string, { code: string }> }
  }

  it('has a route for every operation in the document', () => {
    const ids = Object.values(document.paths).flatMap(methods => Object.values(methods).filter(op => op['x-xihan-stability']).map(op => op.operationId).filter(Boolean))
    expect(Object.keys(routes).sort()).toEqual(ids.sort())
    // 普通端点（文件上传、下载）有端点名但不是操作
    expect(Object.keys(routes)).not.toContain('files.upload')
    expect(Object.keys(routes)).not.toContain('files.signed-content')
  })

  it('derives users.create as POST /api/users and users.list as POST /api/users:list', () => {
    expect(routes['users.create']).toMatchObject({ method: 'POST', template: '/api/users', success: 201 })
    expect(routes['users.get']).toMatchObject({ method: 'GET', template: '/api/users/{id}', path: ['id'] })
    expect(routes['users.list']).toMatchObject({ method: 'POST', template: '/api/users:list' })
    expect(typeof users.create).toBe('function')
  })

  it('maps operations to their permission codes from x-xihan-permission', () => {
    for (const methods of Object.values(document.paths)) {
      for (const op of Object.values(methods)) {
        if (op.operationId && op['x-xihan-stability'] && op['x-xihan-permission'])
          expect(operationPermission[op.operationId as keyof typeof operationPermission]).toBe(op['x-xihan-permission'])
      }
    }
    expect(Object.keys(permissionDefinitions).sort()).toEqual(Object.keys(document.components['x-xihan-permissions']).sort())
  })
})
