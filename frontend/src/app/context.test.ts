/**
 * 应用上下文接口（context.ts）的请求地址测试。
 *
 * 锁定：主键一律走查询串、不拼进路由段；Framework 只为显式 [FromRoute] 的参数生成路由段，
 * BasicApp 后端没有这种参数，拼进路由段的请求一律 404。
 * 另扫一遍 src 与 packages 的源码，请求地址里不出现由插值拼出来的路由段。
 */
import type { AxiosRequestConfig } from '~/request'
import { readdirSync, readFileSync } from 'node:fs'
import { join, relative } from 'node:path'
import { beforeEach, describe, expect, it, vi } from 'vitest'

interface RecordedCall {
  method: 'DELETE' | 'GET' | 'POST' | 'PUT'
  url: string
  body?: unknown
  params?: unknown
}

const hoisted = vi.hoisted(() => ({ calls: [] as RecordedCall[] }))

vi.mock('@/api/request', () => ({
  requestClient: {
    get(url: string, config?: AxiosRequestConfig) {
      hoisted.calls.push({ method: 'GET', url, params: config?.params })
      return Promise.resolve(null)
    },
    post(url: string, body?: unknown, config?: AxiosRequestConfig) {
      hoisted.calls.push({ method: 'POST', url, body, params: config?.params })
      return Promise.resolve(null)
    },
    put(url: string, body?: unknown, config?: AxiosRequestConfig) {
      hoisted.calls.push({ method: 'PUT', url, body, params: config?.params })
      return Promise.resolve(null)
    },
    delete(url: string, config?: AxiosRequestConfig) {
      hoisted.calls.push({ method: 'DELETE', url, params: config?.params })
      return Promise.resolve(undefined)
    },
  },
}))

vi.mock('@/router', () => ({ router: {} }))
vi.mock('@/router/routes', () => ({ staticRoutes: [] }))

const { calls } = hoisted

beforeEach(() => {
  calls.length = 0
})

function only(): RecordedCall {
  expect(calls).toHaveLength(1)
  const call = calls[0]
  if (!call) {
    throw new Error('没有记录到任何请求调用')
  }
  return call
}

async function apis() {
  const { createApplicationApis } = await import('./context')
  return createApplicationApis()
}

describe('主键走查询串', () => {
  it('删除 API 凭证：DELETE /Profile/ApiCredential?id=', async () => {
    await (await apis()).deleteApiCredentialApi('213681228308811793')

    expect(only()).toEqual({ method: 'DELETE', url: '/Profile/ApiCredential', params: { id: '213681228308811793' } })
  })

  it('重置我的 OAuth 应用密钥：POST /MyOAuthApp/RegenerateMyOAuthAppSecret?id=，没有请求体', async () => {
    await (await apis()).regenerateMyOAuthAppSecretApi('1975')

    expect(only()).toEqual({ method: 'POST', url: '/MyOAuthApp/RegenerateMyOAuthAppSecret', body: undefined, params: { id: '1975' } })
  })

  it('删除我的 OAuth 应用：DELETE /MyOAuthApp/MyOAuthApp?id=', async () => {
    await (await apis()).deleteMyOAuthAppApi('1975')

    expect(only()).toEqual({ method: 'DELETE', url: '/MyOAuthApp/MyOAuthApp', params: { id: '1975' } })
  })
})

describe('源码里的请求地址', () => {
  it('src 与 packages 里没有用插值拼路由段的请求地址', () => {
    const root = join(__dirname, '..', '..')
    const offenders: string[] = []
    // 形如 `/Profile/ApiCredential/${id}` 或 `PrintTemplateDetail/${...}`：控制器或动作名后面紧跟插值路由段
    const routeSegment = /`\/?(?:[A-Z][\w-]*\/)+\$\{/

    for (const dir of ['src', 'packages']) {
      for (const file of listSources(join(root, dir))) {
        readFileSync(file, 'utf8').split('\n').forEach((line, index) => {
          if (routeSegment.test(line)) {
            offenders.push(`${relative(root, file)}:${index + 1}`)
          }
        })
      }
    }

    expect(offenders).toEqual([])
  })
})

function listSources(dir: string): string[] {
  return readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
    const path = join(dir, entry.name)
    if (entry.isDirectory()) {
      return entry.name === 'node_modules' || entry.name === 'dist' ? [] : listSources(path)
    }
    return /\.(?:ts|vue)$/.test(entry.name) && !entry.name.endsWith('.test.ts') ? [path] : []
  })
}
