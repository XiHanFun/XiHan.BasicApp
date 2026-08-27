/**
 * `@/api` 桶文件（src/api/index.ts）的导出完整性测试。
 *
 * 职责边界：`export *` 在遇到同名导出时会静默丢弃该名字——编译不报错、运行时直接 undefined。
 * 这里把每个被聚合的子桶逐一取命名空间，反查它的运行时导出是否都能从 `@/api` 拿到，
 * 从而在新增模块撞名的当天就失败，而不是等页面上报 "xxx is not a function"。
 */
import { describe, expect, it, vi } from 'vitest'
import * as base from './base'
import * as helpers from './helpers'
import * as api from './index'
import * as audit from './modules/audit'
import * as authorization from './modules/authorization'
import * as cache from './modules/cache'
import * as configuration from './modules/configuration'
import * as constraintRule from './modules/constraint-rule'
import * as exportModule from './modules/export'
import * as files from './modules/files'
import * as identity from './modules/identity'
import * as log from './modules/log'
import * as messaging from './modules/messaging'
import * as metadata from './modules/metadata'
import * as navigation from './modules/navigation'
import * as oauth from './modules/oauth'
import * as organization from './modules/organization'
import * as platform from './modules/platform'
import * as server from './modules/server'
import * as system from './modules/system'
import * as tenant from './modules/tenant'
import * as workbench from './modules/workbench'
import * as workflow from './modules/workflow'
import * as contracts from './types'

vi.mock('@/api/request', () => ({
  requestClient: {
    get: () => Promise.resolve(null),
    post: () => Promise.resolve(null),
    put: () => Promise.resolve(null),
    delete: () => Promise.resolve(undefined),
  },
}))

/** 被 `@/api` 聚合的全部子桶（顺序与 index.ts 的 export 顺序一致） */
const barrels: [string, Record<string, unknown>][] = [
  ['./base', base],
  ['./helpers', helpers],
  ['./modules/audit', audit],
  ['./modules/authorization', authorization],
  ['./modules/cache', cache],
  ['./modules/configuration', configuration],
  ['./modules/constraint-rule', constraintRule],
  ['./modules/export', exportModule],
  ['./modules/files', files],
  ['./modules/identity', identity],
  ['./modules/log', log],
  ['./modules/messaging', messaging],
  ['./modules/metadata', metadata],
  ['./modules/navigation', navigation],
  ['./modules/oauth', oauth],
  ['./modules/organization', organization],
  ['./modules/platform', platform],
  ['./modules/server', server],
  ['./modules/system', system],
  ['./modules/tenant', tenant],
  ['./modules/workbench', workbench],
  ['./modules/workflow', workflow],
  ['./types', contracts],
]

function runtimeExports(namespace: Record<string, unknown>) {
  return Object.keys(namespace).filter(key => key !== 'default')
}

describe('桶文件的导出完整性', () => {
  it('每个子桶的运行时导出都能从 @/api 取到，没有被同名导出静默吞掉', () => {
    const missing: string[] = []
    for (const [path, namespace] of barrels) {
      for (const name of runtimeExports(namespace)) {
        if (!(name in api)) {
          missing.push(`${path} → ${name}`)
        }
      }
    }

    expect(missing).toEqual([])
  })

  it('从 @/api 取到的是子桶里的同一个绑定，不是同名的另一份实现', () => {
    const mismatched: string[] = []
    for (const [path, namespace] of barrels) {
      for (const name of runtimeExports(namespace)) {
        if ((api as Record<string, unknown>)[name] !== namespace[name]) {
          mismatched.push(`${path} → ${name}`)
        }
      }
    }

    expect(mismatched).toEqual([])
  })

  it('聚合到的导出数量与量级相符——整块子桶被漏挂时这里先失败', () => {
    expect(runtimeExports(api).length).toBeGreaterThan(100)
  })
})

describe('桶文件对外承诺的关键入口', () => {
  it('动态接口底座与查询构造工具都能从 @/api 直接取用', () => {
    for (const name of [
      'createDynamicApiClient',
      'createReadApi',
      'createCommandApi',
      'appendDynamicApiParam',
      'formatDynamicApiRouteValue',
      'createPageRequest',
      'createDefaultQueryConditions',
      'queryKeyword',
      'queryFilter',
      'querySort',
      'querySortsFromSchema',
      'compactRecord',
    ]) {
      expect(typeof (api as Record<string, unknown>)[name]).toBe('function')
    }
  })

  it('查询算子与排序方向两个运行时枚举随契约类型一起导出，供页面拼查询条件', () => {
    expect(api.QueryOperator.Contains).toBe(2000)
    expect(api.SortDirection.Descending).toBe(1001)
  })

  it('各业务域的门面对象都挂在 @/api 上且与子桶同源', () => {
    expect(api.userApi).toBe(identity.userApi)
    expect(api.permissionApi).toBe(authorization.permissionApi)
    expect(api.menuApi).toBe(navigation.menuApi)
    expect(api.fileApi).toBe(files.fileApi)
    expect(api.tenantApi).toBe(tenant.tenantApi)
    expect(api.workbenchApi).toBe(workbench.workbenchApi)
    expect(api.logManagementApi).toBe(log.logManagementApi)
  })

  it('请求客户端本身也从 @/api 透出，页面无需再引 axios', () => {
    expect(typeof api.requestClient.get).toBe('function')
    expect(typeof api.requestClient.post).toBe('function')
    expect(typeof api.requestClient.put).toBe('function')
    expect(typeof api.requestClient.delete).toBe('function')
  })
})
