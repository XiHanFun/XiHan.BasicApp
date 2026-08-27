/**
 * Dynamic API 底座（src/api/base.ts）单元测试。
 *
 * 职责边界：只验证「控制器名 + 动作名 → URL / HTTP 谓词 / 参数位置」这层拼装契约，
 * 以及 appendDynamicApiParam / formatDynamicApiRouteValue 两个纯函数的全分支行为。
 * 不发真实请求：requestClient 被整体替换为记录器。
 */
import type { DynamicApiParams } from './base'
import type { AxiosRequestConfig } from '~/request'
import { describe, expect, it, vi } from 'vitest'
import {
  appendDynamicApiParam,
  createCommandApi,
  createDynamicApiClient,
  createReadApi,
  formatDynamicApiRouteValue,
} from './base'

interface RecordedCall {
  method: 'delete' | 'get' | 'post' | 'put'
  url: string
  body?: unknown
  config?: AxiosRequestConfig & { params?: unknown }
}

const hoisted = vi.hoisted(() => ({ calls: [] as RecordedCall[] }))

vi.mock('@/api/request', () => ({
  requestClient: {
    get(url: string, config?: AxiosRequestConfig) {
      hoisted.calls.push({ method: 'get', url, config })
      return Promise.resolve('get-result')
    },
    post(url: string, body?: unknown, config?: AxiosRequestConfig) {
      hoisted.calls.push({ method: 'post', url, body, config })
      return Promise.resolve('post-result')
    },
    put(url: string, body?: unknown, config?: AxiosRequestConfig) {
      hoisted.calls.push({ method: 'put', url, body, config })
      return Promise.resolve('put-result')
    },
    delete(url: string, config?: AxiosRequestConfig) {
      hoisted.calls.push({ method: 'delete', url, config })
      return Promise.resolve('delete-result')
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

describe('createDynamicApiClient 的路由拼装', () => {
  it('控制器名与动作名拼成「/控制器/动作」，且两段都不带 api 前缀（前缀由 request 层补）', async () => {
    const client = createDynamicApiClient('UserQuery')
    await client.get('UserPage')

    expect(lastCall().url).toBe('/UserQuery/UserPage')
  })

  it('控制器名两端的空白与斜杠被剥离，内部不做大小写改写', async () => {
    const client = createDynamicApiClient('  //TenantQuery//  ')
    await client.get('MyAvailableTenants')

    expect(lastCall().url).toBe('/TenantQuery/MyAvailableTenants')
  })

  it('动作名两端的斜杠被剥离，但动作内部的斜杠保留——路由段拼接依赖它', async () => {
    const client = createDynamicApiClient('PrintTemplateQuery')
    await client.get('/PrintTemplateDetail/1024/')

    expect(lastCall().url).toBe('/PrintTemplateQuery/PrintTemplateDetail/1024')
  })

  it('空控制器名在建客户端时立刻抛错，不拖到发请求才暴露', () => {
    expect(() => createDynamicApiClient('')).toThrow(/不能为空/)
    expect(() => createDynamicApiClient('   ')).toThrow(/不能为空/)
    expect(() => createDynamicApiClient('///')).toThrow(/不能为空/)
  })

  it('空动作名在调用时抛错', () => {
    const client = createDynamicApiClient('Cache')

    expect(() => client.get('')).toThrow(/不能为空/)
    expect(() => client.post('  ')).toThrow(/不能为空/)
    expect(() => client.put('/')).toThrow(/不能为空/)
    expect(() => client.delete('//')).toThrow(/不能为空/)
  })

  it('中文与特殊字符的控制器段原样透传，不做 URL 编码（编码是调用方的事）', async () => {
    const client = createDynamicApiClient('租户 Query')
    await client.get('列表')

    expect(lastCall().url).toBe('/租户 Query/列表')
  })
})

describe('createDynamicApiClient 的参数位置约定', () => {
  it('读取类谓词把参数放进 config.params，body 位不占用', async () => {
    const client = createDynamicApiClient('RoleQuery')
    await client.get('EnabledRoles', { Limit: 20, Keyword: 'admin' })

    const call = lastCall()
    expect(call.method).toBe('get')
    expect(call.config?.params).toEqual({ Limit: 20, Keyword: 'admin' })
    expect(call.body).toBeUndefined()
  })

  it('删除谓词同读取：参数走 query，不走 body——后端 DELETE 不收请求体', async () => {
    const client = createDynamicApiClient('File')
    await client.delete('File', { basicId: '7', deletePhysical: true })

    const call = lastCall()
    expect(call.method).toBe('delete')
    expect(call.config?.params).toEqual({ basicId: '7', deletePhysical: true })
  })

  it('写入类谓词把对象整体作为 body 上送，config 原样透传', async () => {
    const client = createDynamicApiClient('Role')
    await client.post('Role', { roleName: 'ops' }, { timeout: 5000 })
    expect(lastCall()).toMatchObject({
      method: 'post',
      url: '/Role/Role',
      body: { roleName: 'ops' },
      config: { timeout: 5000 },
    })

    await client.put('RoleStatus', { basicId: '1', status: 'Disabled' })
    expect(lastCall()).toMatchObject({
      method: 'put',
      url: '/Role/RoleStatus',
      body: { basicId: '1', status: 'Disabled' },
    })
  })

  it('写入谓词的 config.params 不被 params 位覆盖——Cancel/Remind 这类「id 走 query 的 POST」依赖它', async () => {
    const client = createDynamicApiClient('ExportTask')
    await client.post('Cancel', undefined, { params: { id: '99' } })

    const call = lastCall()
    expect(call.body).toBeUndefined()
    expect(call.config?.params).toEqual({ id: '99' })
  })

  // 回归锚点：修复前 get/delete 写作 `{ ...config, params }`，params 位为 undefined 也会把
  // config.params 整段覆盖成 undefined，查询条件静默丢失（与 post/put 的透传语义相反）。
  it('读取谓词不传 params 位时，config.params 照常下发——不再被 undefined 覆盖掉', async () => {
    const client = createDynamicApiClient('Server')
    await client.get('ServerInfo', undefined, { params: { IncludeDisk: true } })
    expect(lastCall().config?.params).toEqual({ IncludeDisk: true })

    await client.delete('ServerCache', undefined, { params: { Scope: 'all' } })
    expect(lastCall().config?.params).toEqual({ Scope: 'all' })
  })

  it('两条通道同时给值时按键合并，params 位覆盖同名键', async () => {
    const client = createDynamicApiClient('Server')
    await client.get('ServerInfo', { IncludeDisk: false, Limit: 10 }, { params: { IncludeDisk: true, IncludeNetwork: true } })

    expect(lastCall().config?.params).toEqual({ IncludeDisk: false, IncludeNetwork: true, Limit: 10 })
  })

  it('两条通道都不给时 params 保持 undefined，不凭空拼出一个空查询对象', async () => {
    const client = createDynamicApiClient('Server')
    await client.get('ServerInfo')

    expect(lastCall().config?.params).toBeUndefined()
  })

  it('四个谓词把 requestClient 的返回值原样透出，不做二次包装', async () => {
    const client = createDynamicApiClient('Cache')

    await expect(client.get<string>('Keys')).resolves.toBe('get-result')
    await expect(client.post<string>('Exists')).resolves.toBe('post-result')
    await expect(client.put<string>('String')).resolves.toBe('put-result')
    await expect(client.delete<string>('Remove')).resolves.toBe('delete-result')
  })
})

describe('createReadApi 的读侧约定', () => {
  it('分页统一走 POST，动作名为「资源段 + Page」，整个查询对象作 body', async () => {
    const api = createReadApi<unknown, unknown>('MenuQuery', 'Menu')
    const query = { conditions: { filters: [], sorts: [] }, page: { pageIndex: 2, pageSize: 50 } }
    await api.page(query)

    expect(lastCall()).toMatchObject({
      method: 'post',
      url: '/MenuQuery/MenuPage',
      body: query,
    })
  })

  it('详情走 GET，动作名为「资源段 + Detail」，主键以 id 为查询参数名', async () => {
    const api = createReadApi<unknown, unknown>('DictQuery', 'DictItem')
    await api.detail('1975')

    expect(lastCall()).toMatchObject({
      method: 'get',
      url: '/DictQuery/DictItemDetail',
      config: { params: { id: '1975' } },
    })
  })

  it('资源段两端的斜杠同样被剥离后才参与拼接', async () => {
    const api = createReadApi<unknown, unknown>('TenantQuery', '/Tenant/')
    await api.detail('1')

    expect(lastCall().url).toBe('/TenantQuery/TenantDetail')
  })

  it('空资源段建读 API 时立刻抛错', () => {
    expect(() => createReadApi('TenantQuery', '')).toThrow(/不能为空/)
  })
})

describe('createCommandApi 的写侧约定', () => {
  it('新增走 POST、更新走 PUT，两者共用不带动词的资源段路由', async () => {
    const api = createCommandApi<unknown, unknown, unknown>('Permission', 'Permission')

    await api.create({ permissionCode: 'saas:user:read' })
    expect(lastCall()).toMatchObject({
      method: 'post',
      url: '/Permission/Permission',
      body: { permissionCode: 'saas:user:read' },
    })

    await api.update({ basicId: '3', permissionName: '读用户' })
    expect(lastCall()).toMatchObject({
      method: 'put',
      url: '/Permission/Permission',
      body: { basicId: '3', permissionName: '读用户' },
    })
  })

  it('控制器名与资源段可以不同，路由取控制器名在前、资源段在后', async () => {
    const api = createCommandApi<unknown, unknown, unknown>('Tenant', 'TenantMember')
    await api.create({ userId: '1' })

    expect(lastCall().url).toBe('/Tenant/TenantMember')
  })
})

describe('appendDynamicApiParam 的空值过滤', () => {
  it('undefined / null / 空串一律不写入参数表，避免拼出 ?Keyword= 这种空条件', () => {
    const params: DynamicApiParams = {}
    appendDynamicApiParam(params, 'A', undefined)
    appendDynamicApiParam(params, 'B', null)
    appendDynamicApiParam(params, 'C', '')

    expect(params).toEqual({})
  })

  it('0 / false / 负数 / 纯空格字符串是有效取值，必须写入', () => {
    const params: DynamicApiParams = {}
    appendDynamicApiParam(params, 'Zero', 0)
    appendDynamicApiParam(params, 'False', false)
    appendDynamicApiParam(params, 'Negative', -1)
    appendDynamicApiParam(params, 'Blank', ' ')

    expect(params).toEqual({ Zero: 0, False: false, Negative: -1, Blank: ' ' })
  })

  it('同名键后写覆盖先写，并且不因为写入 undefined 而被删除（undefined 直接跳过）', () => {
    const params: DynamicApiParams = { Keyword: '旧值' }
    appendDynamicApiParam(params, 'Keyword', '新值')
    expect(params.Keyword).toBe('新值')

    appendDynamicApiParam(params, 'Keyword', undefined)
    expect(params.Keyword).toBe('新值')
  })

  it('中文、emoji 与超长字符串原样写入，不截断也不转码', () => {
    const long = '甲'.repeat(5000)
    const params: DynamicApiParams = {}
    appendDynamicApiParam(params, 'Keyword', '张三🙂')
    appendDynamicApiParam(params, 'Long', long)

    expect(params.Keyword).toBe('张三🙂')
    expect(params.Long).toHaveLength(5000)
  })
})

describe('formatDynamicApiRouteValue 的路由段编码', () => {
  it('普通雪花 ID 原样返回', () => {
    expect(formatDynamicApiRouteValue('1975062400000001')).toBe('1975062400000001')
  })

  it('斜杠与井号被编码，避免把一个值拆成多个路由段', () => {
    expect(formatDynamicApiRouteValue('a/b')).toBe('a%2Fb')
    expect(formatDynamicApiRouteValue('a#b')).toBe('a%23b')
    expect(formatDynamicApiRouteValue('a?b=c&d')).toBe('a%3Fb%3Dc%26d')
  })

  it('中文与 emoji 走 UTF-8 百分号编码', () => {
    expect(formatDynamicApiRouteValue('模板')).toBe('%E6%A8%A1%E6%9D%BF')
    expect(formatDynamicApiRouteValue('🙂')).toBe('%F0%9F%99%82')
  })

  it('空串返回空串——调用方需自行保证主键非空', () => {
    expect(formatDynamicApiRouteValue('')).toBe('')
  })
})
