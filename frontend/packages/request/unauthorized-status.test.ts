/**
 * 401 分支所用常量族的回归测试。
 *
 * 职责边界：只锁定一件事——响应错误拦截器判定「要不要刷新令牌」时比的是 **HTTP 状态码**，
 * 用的必须是 HTTP_STATUS 族而不是 BIZ_CODE 族。两族当前恰好都把 401 定义成 401，
 * 所以行为上看不出差别；本文件把 BIZ_CODE.UNAUTHORIZED 改写成业务码取值（4001），
 * 让「误用业务码」这一写法立刻失效，从而把语义错配变成可被测试捕获的事实。
 * 刷新链路本身的分支在 auth.test.ts 里。
 */
import type { AxiosAdapter, InternalAxiosRequestConfig } from 'axios'
import type { Router } from 'vue-router'
import { AxiosError, AxiosHeaders } from 'axios'
import { beforeEach, expect, it, vi } from 'vitest'
import { REFRESH_TOKEN_KEY, TOKEN_KEY } from '~/constants'
import { LocalStorage } from '~/utils'
import { bindLockHook, bindLogoutHook, bindRouter, RequestClient } from './index'

// 模拟「业务码体系调整了 UNAUTHORIZED 的取值」：BIZ_CODE 与 HTTP 状态码就此分家。
// 用 BIZ_CODE.UNAUTHORIZED 判 HTTP 401 的写法在这份常量下会静默失效——不刷新、不登出。
vi.mock('~/constants', async (importOriginal) => {
  const actual = await importOriginal<typeof import('~/constants')>()
  return {
    ...actual,
    BIZ_CODE: { ...actual.BIZ_CODE, UNAUTHORIZED: 4001 },
  }
})

beforeEach(() => {
  bindRouter({ replace: () => Promise.resolve() } as unknown as Router)
  bindLogoutHook(() => {})
  bindLockHook(() => {})
})

it('业务码 UNAUTHORIZED 被改成 4001 后，HTTP 401 依然触发刷新与重放', async () => {
  LocalStorage.set(TOKEN_KEY, 'old-access')
  LocalStorage.set(REFRESH_TOKEN_KEY, 'old-refresh')
  let refreshCount = 0
  const client = new RequestClient({
    adapter: ((config: InternalAxiosRequestConfig) => {
      if (String(config.url).endsWith('/Auth/RefreshToken')) {
        refreshCount += 1
        return Promise.resolve({
          config,
          data: { isSuccess: true, code: 200, data: { accessToken: 'new-access' } },
          headers: new AxiosHeaders(),
          status: 200,
          statusText: 'OK',
        })
      }
      return String(config.headers.get('Authorization')) === 'Bearer new-access'
        ? Promise.resolve({
            config,
            data: { isSuccess: true, code: 200, data: { id: 1 } },
            headers: new AxiosHeaders(),
            status: 200,
            statusText: 'OK',
          })
        : Promise.reject(new AxiosError(
            'Request failed with status code 401',
            AxiosError.ERR_BAD_RESPONSE,
            config,
            null,
            { config, data: null, headers: new AxiosHeaders(), status: 401, statusText: '' },
          ))
    }) as unknown as AxiosAdapter,
  })

  await expect(client.get('/Sys/Detail')).resolves.toEqual({ id: 1 })
  expect(refreshCount).toBe(1)
})
