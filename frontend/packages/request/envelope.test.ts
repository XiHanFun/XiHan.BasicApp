/**
 * 响应信封拆包与 URL 拼接单元测试。
 *
 * 职责边界：锁定 `RequestClient.request` 如何判定「这是不是后端统一信封」、
 * 成功时返回哪一层数据、失败时抛什么消息，以及 get/post/put/delete/patch 与 flat
 * 快捷方法的 URL 前缀拼接与请求体传递规则。错误文案映射、令牌刷新、请求日志、
 * 安全签名分别在同目录其它测试文件里。
 */
import type { AxiosAdapter, InternalAxiosRequestConfig } from 'axios'
import type { Router } from 'vue-router'
import { AxiosHeaders } from 'axios'
import { beforeEach, expect, it } from 'vitest'
import { bindLockHook, bindLogoutHook, bindRouter, createRequestClient, RequestClient } from './index'

beforeEach(() => {
  bindRouter({ replace: () => Promise.resolve() } as unknown as Router)
  bindLogoutHook(() => {})
  bindLockHook(() => {})
})

it('isSuccess 为 true 时只返回信封里的 data 层', async () => {
  const client = clientReturning({ isSuccess: true, code: 200, message: 'ok', data: { id: 7, name: '曦寒' } })

  await expect(client.get('/Sys/Detail')).resolves.toEqual({ id: 7, name: '曦寒' })
})

it('后端省略 data 字段时按 null 返回，而不是把整个信封当业务数据吐出去', async () => {
  // 回归锚点：序列化 WhenWritingNull 会整段省略 data，判据必须是 isSuccess 而不是 data 是否存在
  const client = clientReturning({ isSuccess: true, code: 200, message: '秒传未命中' })

  await expect(client.get('/File/FastUpload')).resolves.toBeNull()
})

it('data 显式为 null 时返回 null', async () => {
  const client = clientReturning({ isSuccess: true, code: 200, data: null })

  await expect(client.get('/Sys/Empty')).resolves.toBeNull()
})

it('缺少 isSuccess 但同时有 data 与 code 时仍按信封解析', async () => {
  const client = clientReturning({ code: 200, data: ['a', 'b'] })

  await expect(client.get('/Sys/List')).resolves.toEqual(['a', 'b'])
})

it('code 为 0 同样视为成功', async () => {
  const client = clientReturning({ code: 0, data: 'zero-is-success' })

  await expect(client.get('/Sys/Zero')).resolves.toBe('zero-is-success')
})

it('isSuccess 为 false 时以后端 message 抛错', async () => {
  const client = clientReturning({ isSuccess: false, code: 400, message: '角色编码已存在', data: null })

  await expect(client.post('/Role/Create', {})).rejects.toThrow('角色编码已存在')
})

it('失败信封没有 message 时抛通用「请求失败」', async () => {
  const client = clientReturning({ isSuccess: false, code: 400, data: null })

  await expect(client.post('/Role/Create', {})).rejects.toThrow('请求失败')
})

it('失败信封的 message 不是字符串时同样抛通用「请求失败」', async () => {
  const client = clientReturning({ isSuccess: false, code: 400, message: { zh: '坏了' }, data: null })

  await expect(client.post('/Role/Create', {})).rejects.toThrow('请求失败')
})

it('code 为字符串 "200" 时不被判为成功而按业务失败抛出', async () => {
  // 锁定当前真实行为：成功判据用严格相等比数字 200，字符串码走失败分支
  const client = clientReturning({ code: '200', message: '字符串状态码', data: { id: 1 } })

  await expect(client.get('/Sys/StringCode')).rejects.toThrow('字符串状态码')
})

it('不含 isSuccess/code/data 的普通对象整体原样返回', async () => {
  const client = clientReturning({ token: 'abc', expiresIn: 7200 })

  await expect(client.get('/Third/Party')).resolves.toEqual({ token: 'abc', expiresIn: 7200 })
})

it('数组响应体没有信封字段，整体原样返回', async () => {
  const client = clientReturning([{ id: 1 }, { id: 2 }])

  await expect(client.get('/Raw/Array')).resolves.toEqual([{ id: 1 }, { id: 2 }])
})

it('字符串响应体原样返回', async () => {
  const client = clientReturning('plain-text-body')

  await expect(client.get('/Raw/Text')).resolves.toBe('plain-text-body')
})

it('null 响应体原样返回', async () => {
  const client = clientReturning(null)

  await expect(client.get('/Raw/Null')).resolves.toBeNull()
})

it('下载场景的 Blob 响应体不被信封逻辑改写，原样返回给调用方', async () => {
  const blob = new Blob(['file-bytes'], { type: 'application/octet-stream' })
  const client = clientReturning(blob)

  const result = await client.get<Blob>('/File/Download', { responseType: 'blob' })

  expect(result).toBe(blob)
})

it('默认前缀 /api 只在以斜杠开头的路径上拼接一次', async () => {
  const calls: InternalAxiosRequestConfig[] = []
  const client = clientCapturing(calls)

  await client.get('/Auth/Login')
  await client.get('/api/Auth/Login')

  expect(calls.map(item => item.url)).toEqual(['/api/Auth/Login', '/api/Auth/Login'])
})

it('绝对地址与相对路径都不加前缀', async () => {
  const calls: InternalAxiosRequestConfig[] = []
  const client = clientCapturing(calls)

  await client.get('https://cdn.example.com/config.json')
  await client.get('Auth/Login')

  expect(calls.map(item => item.url)).toEqual(['https://cdn.example.com/config.json', 'Auth/Login'])
})

it('自定义网关前缀下不把已有的 /api/ 当成自己的前缀而跳过拼接', async () => {
  // 回归锚点：去重判断必须用配置里的 apiPrefix，硬编码 '/api/' 会让这条路径漏掉网关前缀
  const calls: InternalAxiosRequestConfig[] = []
  const client = clientCapturing(calls, { apiPrefix: '/gateway' })

  await client.get('/Auth/Login')
  await client.get('/gateway/Auth/Login')
  await client.get('/api/Auth/Login')

  expect(calls.map(item => item.url)).toEqual([
    '/gateway/Auth/Login',
    '/gateway/Auth/Login',
    '/gateway/api/Auth/Login',
  ])
})

it('前缀配置为空串时路径原样透出', async () => {
  const calls: InternalAxiosRequestConfig[] = []
  const client = clientCapturing(calls, { apiPrefix: '' })

  await client.get('/Auth/Login')

  expect(calls[0]?.url).toBe('/Auth/Login')
})

it('createRequestClient 默认带 /api 前缀并把 baseURL 传给 axios', async () => {
  const calls: InternalAxiosRequestConfig[] = []
  const client = createRequestClient('https://api.example.com')
  installAdapter(client, calls)

  await client.get('/Auth/Login')

  expect(calls[0]?.baseURL).toBe('https://api.example.com')
  expect(calls[0]?.url).toBe('/api/Auth/Login')
})

it('createRequestClient 可指定自定义前缀', async () => {
  const calls: InternalAxiosRequestConfig[] = []
  const client = createRequestClient('https://api.example.com', '/open')
  installAdapter(client, calls)

  await client.get('/Auth/Login')

  expect(calls[0]?.url).toBe('/open/Auth/Login')
})

it('五个动词方法各自发出对应的 HTTP method', async () => {
  const calls: InternalAxiosRequestConfig[] = []
  const client = clientCapturing(calls)

  await client.get('/X')
  await client.post('/X', { a: 1 })
  await client.put('/X', { a: 1 })
  await client.patch('/X', { a: 1 })
  await client.delete('/X')

  expect(calls.map(item => item.method)).toEqual(['get', 'post', 'put', 'patch', 'delete'])
})

it('请求体字段保持 camelCase 原样，不做下划线转换', async () => {
  const calls: InternalAxiosRequestConfig[] = []
  const client = clientCapturing(calls)

  await client.post('/Sys/Save', { tenantId: 't1', createdAt: '2026-08-27', nestedValue: { innerKey: 1 } })

  expect(JSON.parse(String(calls[0]?.data))).toEqual({
    tenantId: 't1',
    createdAt: '2026-08-27',
    nestedValue: { innerKey: 1 },
  })
})

it('delete 携带 config.data 时把请求体一并发出', async () => {
  const calls: InternalAxiosRequestConfig[] = []
  const client = clientCapturing(calls)

  await client.delete('/Sys/BatchDelete', { data: { ids: ['1', '2'] } })

  expect(JSON.parse(String(calls[0]?.data))).toEqual({ ids: ['1', '2'] })
})

it('delete 的 config.data 为假值时被丢弃而不是原样传递', async () => {
  // 锁定当前真实行为：0 / 空串 / null 一律走 undefined 分支
  const calls: InternalAxiosRequestConfig[] = []
  const client = clientCapturing(calls)

  await client.delete('/Sys/Zero', { data: 0 })
  await client.delete('/Sys/EmptyText', { data: '' })

  expect(calls.map(item => item.data)).toEqual([undefined, undefined])
})

it('调用方传入的 config 不会覆盖 method 与 url', async () => {
  const calls: InternalAxiosRequestConfig[] = []
  const client = clientCapturing(calls)

  await client.post('/Sys/Save', { a: 1 }, { method: 'GET', url: '/Hijacked' } as never)

  expect(calls[0]?.method).toBe('post')
  expect(calls[0]?.url).toBe('/api/Sys/Save')
})

it('默认超时为 30 秒且可被构造参数覆盖', async () => {
  const defaults: InternalAxiosRequestConfig[] = []
  const overridden: InternalAxiosRequestConfig[] = []
  await clientCapturing(defaults).get('/X')
  await clientCapturing(overridden, { timeout: 1500 }).get('/X')

  expect(defaults[0]?.timeout).toBe(30000)
  expect(overridden[0]?.timeout).toBe(1500)
})

it('flat 模式成功时返回 data 且 error 为 null', async () => {
  const client = clientReturning({ isSuccess: true, code: 200, data: { total: 3 } })

  await expect(client.getFlat('/Sys/Page')).resolves.toEqual({ data: { total: 3 }, error: null })
})

it('flat 模式在业务失败时返回后端消息且 data 为 null', async () => {
  const client = clientReturning({ isSuccess: false, code: 400, message: '租户已停用' })

  const result = await client.getFlat('/Sys/Page')

  expect(result.data).toBeNull()
  expect(result.error?.message).toBe('租户已停用')
})

it('postFlat 与 getFlat 同样走前缀拼接并传递请求体', async () => {
  const calls: InternalAxiosRequestConfig[] = []
  const client = clientCapturing(calls)

  await client.getFlat('/Sys/Page', { params: { page: 1 } })
  await client.postFlat('/Sys/Save', { name: '曦寒' })

  expect(calls.map(item => ({ method: item.method, url: item.url }))).toEqual([
    { method: 'get', url: '/api/Sys/Page' },
    { method: 'post', url: '/api/Sys/Save' },
  ])
  expect(JSON.parse(String(calls[1]?.data))).toEqual({ name: '曦寒' })
})

/** 创建一个固定返回指定响应体的客户端。 */
function clientReturning(body: unknown): RequestClient {
  return new RequestClient({
    adapter: ((config: InternalAxiosRequestConfig) => Promise.resolve({
      config,
      data: body,
      headers: new AxiosHeaders(),
      status: 200,
      statusText: 'OK',
    })) as AxiosAdapter,
  })
}

/** 创建一个记录每次请求最终配置的客户端。 */
function clientCapturing(
  calls: InternalAxiosRequestConfig[],
  extra: { apiPrefix?: string, timeout?: number } = {},
): RequestClient {
  return new RequestClient({
    ...extra,
    adapter: ((config: InternalAxiosRequestConfig) => {
      calls.push(config)
      return Promise.resolve({
        config,
        data: { isSuccess: true, code: 200, data: null },
        headers: new AxiosHeaders(),
        status: 200,
        statusText: 'OK',
      })
    }) as AxiosAdapter,
  })
}

/** 给已创建的客户端换上记录型适配器（用于 createRequestClient 场景）。 */
function installAdapter(client: RequestClient, calls: InternalAxiosRequestConfig[]): void {
  const internal = client as unknown as { instance: { defaults: { adapter: AxiosAdapter } } }
  internal.instance.defaults.adapter = ((config: InternalAxiosRequestConfig) => {
    calls.push(config)
    return Promise.resolve({
      config,
      data: { isSuccess: true, code: 200, data: null },
      headers: new AxiosHeaders(),
      status: 200,
      statusText: 'OK',
    })
  }) as AxiosAdapter
}
