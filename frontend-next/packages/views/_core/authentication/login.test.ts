/**
 * 登录页（login.vue，4.x 原样搬入）与第三方登录的衔接：
 * 回调换票据时账号开了双因素，store 记下挑战、带着回跳地址回到登录页，登录页直接从验证阶段接着走（第二段凭挑战票据）；
 * 第三方登录按钮带上「租户」一栏与回跳地址整页跳去发起。
 */
import type { Router } from 'vue-router'
import type { AppContextApis } from '~/types'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createMemoryHistory, createRouter } from 'vue-router'
import { LOGIN_PATH } from '~/constants'
import { i18n } from '~/locales'
import { configureRequest } from '~/request'
import { registerAppContext, useAuthStore } from '~/stores'
import LoginPage from './login.vue'

vi.mock('~/utils/device-fingerprint', () => ({ generateDeviceFingerprint: async () => 'device-1' }))

const getLoginConfigApi = vi.fn()

async function open(path: string) {
  const router = createRouter({ history: createMemoryHistory(), routes: [{ path: '/:p(.*)*', component: { render: () => null } }] })
  await router.push(path)
  registerAppContext({ apis: { getLoginConfigApi, getCaptchaApi: vi.fn() } as unknown as AppContextApis })
  const wrapper = mount(LoginPage, { global: { plugins: [i18n, router] } })
  await flushPromises()
  return wrapper
}

const t = (key: string) => i18n.global.t(key)

beforeEach(() => {
  setActivePinia(createPinia())
  localStorage.clear()
  sessionStorage.clear()
  i18n.global.locale.value = 'zh-CN'
  getLoginConfigApi.mockReset()
  getLoginConfigApi.mockResolvedValue({ loginMethods: ['password'], oAuthProviders: [{ name: 'github', displayName: 'GitHub', type: 'github' }], captchaEnabled: false })
})

describe('登录页与第三方登录', () => {
  it('第三方登录带回的双因素挑战：直接进验证阶段，第二段凭挑战票据与回跳地址提交', async () => {
    configureRequest({
      baseUrl: '',
      getToken: () => null,
      fetch: (async () => new Response(JSON.stringify({ token: null, twoFactor: { ticket: 't-1', methods: ['Totp'], method: 'Totp', codeSent: false, expiresAt: '2026-10-28T00:10:00Z' } }), { status: 200, headers: { 'Content-Type': 'application/json' } })) as unknown as typeof fetch,
    })
    const replace = vi.fn(async () => {})
    registerAppContext({ getRouter: () => Promise.resolve({ replace } as unknown as Router) })
    const store = useAuthStore()
    await store.handleOAuthCallback('x-1', '/settings/configs')
    expect(replace).toHaveBeenCalledWith({ path: LOGIN_PATH, query: { redirect: '/settings/configs' } })

    const login = vi.spyOn(store, 'login').mockResolvedValue(null)
    const wrapper = await open(`${LOGIN_PATH}?redirect=/settings/configs`)
    expect(wrapper.text()).toContain(t('page.auth.two_factor_title'))
    const verify = wrapper.findAll('button').find(button => button.text() === t('page.auth.two_factor_verify'))
    await verify!.trigger('click')
    expect(login).toHaveBeenCalledWith(expect.objectContaining({ twoFactorTicket: 't-1', twoFactorMethod: 'totp' }), '/settings/configs')
  })

  it('没有挑战时停在账号密码阶段；第三方按钮带上租户一栏与回跳地址发起', async () => {
    const wrapper = await open(`${LOGIN_PATH}?redirect=/settings/configs`)
    expect(wrapper.text()).not.toContain(t('page.auth.two_factor_title'))
    const start = vi.spyOn(useAuthStore(), 'startOAuthLogin').mockImplementation(() => {})
    await wrapper.find('input[autocomplete="organization"]').setValue('acme')
    const github = wrapper.findAll('button').find(button => button.text() === 'GitHub')
    await github!.trigger('click')
    expect(start).toHaveBeenCalledWith({ name: 'github', displayName: 'GitHub', type: 'github' }, { tenant: 'acme', redirect: '/settings/configs' })
  })
})
