/**
 * 标准 CRUD 资源工厂（src/api/factory.ts）单元测试。
 *
 * 职责边界：锁定 defineResource 生成的 5 个动作各自的 HTTP 谓词、路由与参数位置，
 * 以及「查询控制器 / 命令控制器 / 资源段」三者的分工与默认值。
 */
import type { AxiosRequestConfig } from '~/request'
import { describe, expect, it, vi } from 'vitest'
import { defineResource } from './factory'

interface RecordedCall {
  method: 'delete' | 'get' | 'post' | 'put'
  url: string
  body?: unknown
  params?: unknown
}

const hoisted = vi.hoisted(() => ({ calls: [] as RecordedCall[] }))

vi.mock('@/api/request', () => ({
  requestClient: {
    get(url: string, config?: AxiosRequestConfig) {
      hoisted.calls.push({ method: 'get', url, params: config?.params })
      return Promise.resolve(null)
    },
    post(url: string, body?: unknown, config?: AxiosRequestConfig) {
      hoisted.calls.push({ method: 'post', url, body, params: config?.params })
      return Promise.resolve(null)
    },
    put(url: string, body?: unknown, config?: AxiosRequestConfig) {
      hoisted.calls.push({ method: 'put', url, body, params: config?.params })
      return Promise.resolve(null)
    },
    delete(url: string, config?: AxiosRequestConfig) {
      hoisted.calls.push({ method: 'delete', url, params: config?.params })
      return Promise.resolve(undefined)
    },
  },
}))

const { calls } = hoisted

function lastCall(): RecordedCall {
  const call = calls.at(-1)
  if (!call) {
    throw new Error('没有记录到任何请求调用')
  }
  return call
}

function userResource() {
  return defineResource<unknown, unknown, unknown, unknown>({ query: 'UserQuery', command: 'User' })
}

describe('defineResource 的五个标准动作', () => {
  it('分页走查询控制器的 POST，动作名为「资源段 + Page」，查询对象整体作 body', async () => {
    const query = { conditions: { filters: [], keyword: null, sorts: [] }, page: { pageIndex: 1, pageSize: 20 } }
    await userResource().page(query)

    expect(lastCall()).toEqual({
      method: 'post',
      url: '/UserQuery/UserPage',
      body: query,
      params: undefined,
    })
  })

  it('详情走查询控制器的 GET，主键以 id 为查询参数名', async () => {
    await userResource().detail('1975')

    expect(lastCall()).toEqual({
      method: 'get',
      url: '/UserQuery/UserDetail',
      params: { id: '1975' },
    })
  })

  it('新增走命令控制器的 POST，路由不带任何动词后缀', async () => {
    await userResource().create({ userName: 'zhangsan' })

    expect(lastCall()).toEqual({
      method: 'post',
      url: '/User/User',
      body: { userName: 'zhangsan' },
      params: undefined,
    })
  })

  it('更新走命令控制器的 PUT，与新增共用同一条路由', async () => {
    await userResource().update({ basicId: '1', userName: 'lisi' })

    expect(lastCall()).toEqual({
      method: 'put',
      url: '/User/User',
      body: { basicId: '1', userName: 'lisi' },
      params: undefined,
    })
  })

  it('删除走命令控制器的 DELETE，主键走 query 而非 body', async () => {
    await userResource().remove('1975')

    expect(lastCall()).toEqual({
      method: 'delete',
      url: '/User/User',
      params: { id: '1975' },
    })
  })
})

describe('defineResource 的资源段解析', () => {
  it('未指定 resource 时以命令控制器名为资源段', async () => {
    const api = defineResource<unknown, unknown, unknown, unknown>({ query: 'RoleQuery', command: 'Role' })
    await api.detail('1')

    expect(lastCall().url).toBe('/RoleQuery/RoleDetail')
  })

  it('显式 resource 只影响动作名，不影响两个控制器名——TenantMember 挂在 Tenant 控制器下就靠它', async () => {
    const api = defineResource<unknown, unknown, unknown, unknown>({
      query: 'TenantMemberQuery',
      command: 'Tenant',
      resource: 'TenantMember',
    })

    await api.page({ conditions: { filters: [], keyword: null, sorts: [] }, page: { pageIndex: 1, pageSize: 20 } })
    expect(lastCall().url).toBe('/TenantMemberQuery/TenantMemberPage')

    await api.create({})
    expect(lastCall().url).toBe('/Tenant/TenantMember')

    await api.remove('9')
    expect(lastCall()).toMatchObject({ method: 'delete', url: '/Tenant/TenantMember', params: { id: '9' } })
  })

  it('空串 resource 不会退回命令控制器名——?? 只在 undefined 时回落，空段会当场抛错', () => {
    expect(() => defineResource({ query: 'RoleQuery', command: 'Role', resource: '' }))
      .not
      .toThrow()

    const api = defineResource<unknown, unknown, unknown, unknown>({
      query: 'RoleQuery',
      command: 'Role',
      resource: '',
    })
    expect(() => api.create({})).toThrow(/不能为空/)
  })

  it('空控制器名在定义资源时立刻抛错', () => {
    expect(() => defineResource({ query: '', command: 'Role' })).toThrow(/不能为空/)
    expect(() => defineResource({ query: 'RoleQuery', command: '   ' })).toThrow(/不能为空/)
  })
})

describe('defineResource 暴露的底层客户端', () => {
  it('query / command 是两个独立客户端，各自绑定到自己的控制器', async () => {
    const api = defineResource<unknown, unknown, unknown, unknown>({ query: 'MenuQuery', command: 'Menu' })

    await api.query.get('MenuTree', { OnlyEnabled: true })
    expect(lastCall()).toEqual({ method: 'get', url: '/MenuQuery/MenuTree', params: { OnlyEnabled: true } })

    await api.command.put('MenuStatus', { basicId: '1', status: 'Disabled' })
    expect(lastCall()).toMatchObject({ method: 'put', url: '/Menu/MenuStatus' })
  })

  it('两个资源实例互不共享客户端，路由不会串台', async () => {
    const userApi = defineResource<unknown, unknown, unknown, unknown>({ query: 'UserQuery', command: 'User' })
    const roleApi = defineResource<unknown, unknown, unknown, unknown>({ query: 'RoleQuery', command: 'Role' })

    await userApi.detail('1')
    const userUrl = lastCall().url
    await roleApi.detail('1')

    expect(userUrl).toBe('/UserQuery/UserDetail')
    expect(lastCall().url).toBe('/RoleQuery/RoleDetail')
  })
})
