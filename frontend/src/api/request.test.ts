/**
 * 应用请求客户端装配（src/api/request.ts）单元测试。
 *
 * 职责边界：这层只做一件事——把两个构建期环境变量喂给底层 createRequestClient。
 * 用例锁定「缺省值」与「原样透传」两条约定；底层 RequestClient 的拦截器行为不在本文件范围内。
 * 每条用例都重置模块注册表，保证环境变量改动能被重新读取。
 */
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

const hoisted = vi.hoisted(() => ({
  args: [] as [string, string][],
}))

vi.mock('~/request', () => ({
  createRequestClient: (baseURL: string, apiPrefix: string) => {
    hoisted.args.push([baseURL, apiPrefix])
    return { baseURL, apiPrefix }
  },
}))

const { args } = hoisted

async function loadWithEnv(env: Record<string, string | undefined>) {
  vi.resetModules()
  args.length = 0
  for (const [key, value] of Object.entries(env)) {
    vi.stubEnv(key, value)
  }
  await import('./request')
  const call = args[0]
  if (!call) {
    throw new Error('createRequestClient 没有被调用')
  }
  return call
}

beforeEach(() => {
  vi.resetModules()
})

afterEach(() => {
  vi.unstubAllEnvs()
  vi.resetModules()
})

describe('请求客户端的环境变量装配', () => {
  it('两个变量都缺省时：baseURL 为空串走同源，前缀回落到 /api', async () => {
    expect(await loadWithEnv({ VITE_API_BASE_URL: undefined, VITE_API_PREFIX: undefined }))
      .toEqual(['', '/api'])
  })

  it('显式配置的域名与前缀原样透传，不做补斜杠或去斜杠', async () => {
    expect(await loadWithEnv({
      VITE_API_BASE_URL: 'https://basicappapi.xihanfun.com',
      VITE_API_PREFIX: '/gateway/v2',
    })).toEqual(['https://basicappapi.xihanfun.com', '/gateway/v2'])
  })

  it('开发环境把 baseURL 留空以走本地代理，此时前缀仍然生效', async () => {
    expect(await loadWithEnv({ VITE_API_BASE_URL: '', VITE_API_PREFIX: '/api' }))
      .toEqual(['', '/api'])
  })

  it('前缀显式配成空串时不回落到 /api——?? 只在 undefined 时兜底，空前缀是合法配置', async () => {
    expect(await loadWithEnv({ VITE_API_BASE_URL: '', VITE_API_PREFIX: '' }))
      .toEqual(['', ''])
  })

  it('只配了域名没配前缀时，前缀仍取默认 /api', async () => {
    expect(await loadWithEnv({ VITE_API_BASE_URL: 'https://a.example.com', VITE_API_PREFIX: undefined }))
      .toEqual(['https://a.example.com', '/api'])
  })

  it('模块只在首次求值时装配一次客户端，重复导入不会重复建实例', async () => {
    await loadWithEnv({ VITE_API_BASE_URL: 'https://a.example.com', VITE_API_PREFIX: '/api' })
    const again = await import('./request')

    expect(args).toHaveLength(1)
    expect(again.requestClient).toMatchObject({ baseURL: 'https://a.example.com', apiPrefix: '/api' })
  })
})
