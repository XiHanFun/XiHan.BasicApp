/**
 * 第三方登录回调页（oauth-callback.vue，4.x 原样搬入、换成 6.0 的一次性票据）：
 * 后端回跳的错误码换成友好文案、只认本标签页发起的流程带回来的票据、
 * 绑定由登录着的人确认后回个人中心「账号绑定」、换票据 / 确认失败显示后端本地化的 detail，稍后回登录页或个人中心。
 */
import type { AppContextApis } from '~/types'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createMemoryHistory, createRouter } from 'vue-router'
import { toast } from '~/composables'
import { LOGIN_PATH, OAUTH_CALLBACK_PATH } from '~/constants'
import { i18n } from '~/locales'
import { configureRequest } from '~/request'
import { registerAppContext, useAuthStore } from '~/stores'
import OAuthCallbackPage from './oauth-callback.vue'

const Blank = { render: () => null }

const user = { id: 'u-1', userName: 'alice', displayName: 'Alice', email: null, phoneNumber: null, homeTenantId: 'acme', status: 'Active', lastLoginAt: null, createdAt: '2026-10-28T00:00:00Z', createdBy: null }

function signIn() {
  useAuthStore().session = { accessToken: 'a-1', accessExpiresAt: '2026-10-28T02:00:00Z', refreshToken: 'r-1', refreshExpiresAt: '2026-11-04T00:00:00Z', sessionId: 's-1', tenantId: 'acme', tenantName: null, impersonatorUserName: null, mustChangePassword: false, user } as never
}

function pending(mode: 'bind' | 'login', provider = 'github', redirect?: string) {
  sessionStorage.setItem('xihan.next.oauth-pending', JSON.stringify({ mode, provider, startedAt: Date.now(), ...(redirect ? { redirect } : {}) }))
}

function problem(status: number, code: string, detail: string): Response {
  return new Response(JSON.stringify({ code, status, detail }), { status, headers: { 'Content-Type': 'application/problem+json' } })
}

const linkAccountApi = vi.fn()

async function open(query: string) {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: OAUTH_CALLBACK_PATH, component: Blank },
      { path: LOGIN_PATH, component: Blank },
      { path: '/profile', component: Blank },
      { path: '/:p(.*)*', component: Blank },
    ],
  })
  registerAppContext({ apis: { linkAccountApi } as unknown as AppContextApis, getRouter: () => Promise.resolve(router), shellRoutes: { profile: '/profile' } })
  await router.push(`${OAUTH_CALLBACK_PATH}?${query}`)
  const wrapper = mount(OAuthCallbackPage, { global: { plugins: [i18n, router] } })
  await flushPromises()
  return { router, wrapper }
}

const t = (key: string) => i18n.global.t(key)

beforeEach(() => {
  setActivePinia(createPinia())
  localStorage.clear()
  sessionStorage.clear()
  i18n.global.locale.value = 'zh-CN'
  linkAccountApi.mockReset()
  vi.spyOn(toast, 'success').mockImplementation(() => '')
  vi.spyOn(toast, 'danger').mockImplementation(() => '')
  vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] })
})

afterEach(() => {
  vi.useRealTimers()
})

describe('第三方登录回调页', () => {
  it('后端回跳的错误码换成友好文案（不显示裸错误码），3 秒后回登录页', async () => {
    pending('login')
    const { router, wrapper } = await open('error=access_denied&provider=github&mode=login')
    expect(wrapper.text()).toContain(t('page.auth.oauth_err_access_denied'))
    expect(wrapper.text()).not.toContain('access_denied')
    expect(toast.danger).toHaveBeenCalledWith(t('page.auth.oauth_err_access_denied'))
    vi.advanceTimersByTime(3000)
    await flushPromises()
    expect(router.currentRoute.value.path).toBe(LOGIN_PATH)
  })

  it('四个错误码都有文案；不认识的按登录失败说', async () => {
    for (const [code, key] of [['invalid_provider', 'oauth_err_invalid_provider'], ['invalid_state', 'oauth_err_invalid_state'], ['external_auth_failed', 'oauth_err_external_auth_failed'], ['server_error', 'oauth_callback_failed']]) {
      const { wrapper } = await open(`error=${code}&provider=github`)
      expect(wrapper.text()).toContain(t(`page.auth.${key}`))
      wrapper.unmount()
    }
  })

  it('不是本标签页发起的流程带回来的票据（别人发来的链接）一律作废，不调任何接口', async () => {
    signIn()
    const fetch = vi.fn()
    configureRequest({ baseUrl: '', fetch: fetch as unknown as typeof globalThis.fetch })
    const { wrapper } = await open('ticket=x-1&mode=bind&provider=github')
    expect(wrapper.text()).toContain(t('page.auth.oauth_callback_not_initiated'))
    expect(linkAccountApi).not.toHaveBeenCalled()

    // 用途或提供方对不上同样作废
    pending('login', 'github')
    const second = await open('ticket=x-2&mode=bind&provider=github')
    expect(second.wrapper.text()).toContain(t('page.auth.oauth_callback_not_initiated'))
    pending('bind', 'gitee')
    const third = await open('ticket=x-3&mode=bind&provider=github')
    expect(third.wrapper.text()).toContain(t('page.auth.oauth_callback_not_initiated'))
    expect(linkAccountApi).not.toHaveBeenCalled()
    expect(fetch).not.toHaveBeenCalled()
  })

  it('绑定：登录着的人凭票据确认，提示成功后回个人中心「账号绑定」', async () => {
    signIn()
    pending('bind', 'github')
    linkAccountApi.mockResolvedValue({ provider: 'github' })
    const { router } = await open('ticket=x-1&mode=bind&provider=github')
    expect(linkAccountApi).toHaveBeenCalledWith('x-1')
    expect(toast.success).toHaveBeenCalledWith(t('page.auth.oauth_bind_success'))
    expect(router.currentRoute.value.fullPath).toBe('/profile?tab=binding')
  })

  it('绑定失败显示后端的 detail（已被别人绑定等），3 秒后回个人中心', async () => {
    signIn()
    pending('bind', 'github')
    const { ApiError } = await import('~/request')
    linkAccountApi.mockRejectedValue(new ApiError(409, { code: 'IDENTITY_CONFLICT_008', detail: '该第三方账号已被其他用户绑定。' }))
    const { router, wrapper } = await open('ticket=x-1&mode=bind&provider=github')
    expect(wrapper.text()).toContain('该第三方账号已被其他用户绑定。')
    expect(wrapper.text()).not.toContain('IDENTITY_CONFLICT_008')
    expect(wrapper.text()).toContain(t('page.auth.oauth_callback_redirect_profile'))
    vi.advanceTimersByTime(3000)
    await flushPromises()
    expect(router.currentRoute.value.fullPath).toBe('/profile?tab=binding')
  })

  it('绑定回来时已不在登录态：不确认，提示先登录并回登录页', async () => {
    pending('bind', 'github')
    const { router, wrapper } = await open('ticket=x-1&mode=bind&provider=github')
    expect(linkAccountApi).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain(t('page.auth.oauth_bind_login_required'))
    vi.advanceTimersByTime(3000)
    await flushPromises()
    expect(router.currentRoute.value.path).toBe(LOGIN_PATH)
  })

  it('登录：票据换会话后落到发起时登录页的回跳地址', async () => {
    pending('login', 'github', '/oauth/authorize?client_id=c')
    configureRequest({
      baseUrl: '',
      getToken: () => null,
      fetch: (async () => new Response(JSON.stringify({ token: { accessToken: 'a-1', accessExpiresAt: '2026-10-28T02:00:00Z', refreshToken: 'r-1', refreshExpiresAt: '2026-11-04T00:00:00Z', sessionId: 's-1', tenantId: 'acme', mustChangePassword: false, user }, twoFactor: null }), { status: 200, headers: { 'Content-Type': 'application/json' } })) as unknown as typeof fetch,
    })
    const { router } = await open('ticket=x-1&mode=login&provider=github')
    expect(useAuthStore().accessToken).toBe('a-1')
    expect(router.currentRoute.value.fullPath).toBe('/oauth/authorize?client_id=c')
  })

  it('登录失败（没有绑定的第三方账号等）显示后端的 detail，3 秒后回登录页', async () => {
    pending('login', 'github')
    configureRequest({ baseUrl: '', getToken: () => null, fetch: (async () => problem(403, 'IDENTITY_DENIED_015', '这个第三方账号还没有绑定，请先用账号密码登录，再到个人中心绑定。')) as unknown as typeof fetch })
    const { router, wrapper } = await open('ticket=x-1&mode=login&provider=github')
    expect(wrapper.text()).toContain('这个第三方账号还没有绑定，请先用账号密码登录，再到个人中心绑定。')
    expect(wrapper.text()).toContain(t('page.auth.oauth_callback_redirect'))
    expect(useAuthStore().isAuthenticated).toBe(false)
    vi.advanceTimersByTime(3000)
    await flushPromises()
    expect(router.currentRoute.value.path).toBe(LOGIN_PATH)
  })

  it('缺票据即提示登录信息缺失', async () => {
    const { wrapper } = await open('mode=login&provider=github')
    expect(wrapper.text()).toContain(t('page.auth.oauth_callback_missing_ticket'))
  })
})
