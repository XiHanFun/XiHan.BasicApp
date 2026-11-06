/**
 * 登录页（login.vue，4.x 原样搬入）与第三方登录的衔接：
 * 回调换票据时账号开了双因素，store 记下挑战、带着回跳地址回到登录页，登录页直接从验证阶段接着走（第二段凭挑战票据）；
 * 第三方登录按钮带上「租户」一栏与回跳地址整页跳去发起；登录配置取不到时表单上方常驻提示并可重试，取到之前不能提交
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

vi.mock('~/utils/device-fingerprint', () => ({
  generateDeviceFingerprint: async () => 'device-1',
}))

const getLoginConfigApi = vi.fn()
const getCaptchaApi = vi.fn()

/** attachTo：要看焦点时挂进文档 */
async function open(path: string, attachTo?: HTMLElement) {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      {
        path: '/:p(.*)*',
        component: {
          render: () => null,
        },
      },
    ],
  })
  await router.push(path)
  registerAppContext({
    apis: { getLoginConfigApi, getCaptchaApi } as unknown as AppContextApis,
  })
  const wrapper = mount(LoginPage, {
    attachTo,
    global: {
      plugins: [i18n, router],
    },
  })
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
  getCaptchaApi.mockReset()
  getCaptchaApi.mockResolvedValue({ captchaId: 'c-1', image: 'data:image/svg+xml,%3Csvg%2F%3E', expiresInSeconds: 120 })
  getLoginConfigApi.mockResolvedValue({
    loginMethods: ['password'],
    oAuthProviders: [
      { name: 'github', displayName: 'GitHub', type: 'github' },
    ],
    captchaEnabled: false,
    registrationEnabled: false,
  })
})

describe('登录页与第三方登录', () => {
  it('第三方登录带回的双因素挑战：直接进验证阶段，第二段凭挑战票据与回跳地址提交', async () => {
    configureRequest({
      baseUrl: '',
      getToken: () => null,
      fetch: (async () => new Response(JSON.stringify({
        token: null,
        twoFactor: {
          ticket: 't-1',
          methods: ['Totp'],
          method: 'Totp',
          codeSent: false,
          expiresAt: '2026-10-28T00:10:00Z',
        },
      }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      })) as unknown as typeof fetch,
    })
    const replace = vi.fn(async () => {})
    registerAppContext({
      getRouter: () => Promise.resolve({ replace } as unknown as Router),
    })
    const store = useAuthStore()
    await store.handleOAuthCallback('x-1', '/settings/configs')
    expect(replace).toHaveBeenCalledWith({
      path: LOGIN_PATH,
      query: { redirect: '/settings/configs' },
    })

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

describe('找回密码与注册入口', () => {
  async function mountWithAuthRoutes() {
    const router = createRouter({
      history: createMemoryHistory(),
      routes: [
        {
          path: LOGIN_PATH,
          component: {
            render: () => null,
          },
        },
        {
          path: '/auth/forget-password',
          name: 'ForgetPassword',
          component: {
            render: () => null,
          },
        },
        {
          path: '/auth/register',
          name: 'Register',
          component: {
            render: () => null,
          },
        },
      ],
    })
    await router.push(LOGIN_PATH)
    registerAppContext({
      apis: { getLoginConfigApi, getCaptchaApi } as unknown as AppContextApis,
    })
    const wrapper = mount(LoginPage, {
      global: {
        plugins: [i18n, router],
      },
    })
    await flushPromises()
    return wrapper
  }

  it('找回密码总在；注册入口只在登录配置开放注册时出现', async () => {
    const closed = await mountWithAuthRoutes()
    expect(closed.text()).toContain(t('page.login.forgot_password'))
    expect(closed.text()).not.toContain(t('page.auth.no_account'))

    getLoginConfigApi.mockResolvedValue({
      loginMethods: ['password'],
      oAuthProviders: [],
      captchaEnabled: false,
      registrationEnabled: true,
    })
    const open = await mountWithAuthRoutes()
    expect(open.text()).toContain(t('page.auth.no_account'))
    expect(open.text()).toContain(t('page.login.register'))
  })
})

describe('登录配置取不到', () => {
  it('表单上方常驻提示并可重试；取到之前提交钮禁用，重试取到后照常要验证码', async () => {
    getLoginConfigApi.mockRejectedValueOnce(new Error('网络不可用'))
    const wrapper = await open(LOGIN_PATH)
    const alert = wrapper.find('[data-testid="login-config-failed"]')
    expect(alert.exists()).toBe(true)
    expect(alert.text()).toContain(t('page.auth.load_config_failed'))
    expect(alert.text()).toContain('网络不可用')
    const submit = () => wrapper.findAll('button').find(button => button.text() === t('page.login.login_btn'))!
    expect(submit().attributes('disabled')).toBeDefined()
    expect(getCaptchaApi).not.toHaveBeenCalled()

    getLoginConfigApi.mockResolvedValue({
      loginMethods: ['password'],
      oAuthProviders: [],
      captchaEnabled: true,
      registrationEnabled: false,
    })
    await alert.findAll('button').find(button => button.text() === t('page.auth.load_config_retry'))!.trigger('click')
    await flushPromises()
    expect(wrapper.find('[data-testid="login-config-failed"]').exists()).toBe(false)
    expect(submit().attributes('disabled')).toBeUndefined()
    expect(getCaptchaApi).toHaveBeenCalledTimes(1)
    expect(wrapper.find('img[src^="data:image/svg+xml"]').exists()).toBe(true)
  })

  it('重试在途时提示不撤、焦点留在「重试」钮上，取到后焦点移到第一个输入框', async () => {
    getLoginConfigApi.mockRejectedValueOnce(new Error('网络不可用'))
    const wrapper = await open(LOGIN_PATH, document.body.appendChild(document.createElement('div')))
    const retry = wrapper.find('[data-testid="login-config-failed"]').findAll('button').find(button => button.text() === t('page.auth.load_config_retry'))!
    let resolveConfig!: (config: unknown) => void
    getLoginConfigApi.mockReturnValueOnce(new Promise((resolve) => {
      resolveConfig = resolve
    }))
    const retryButton = retry.element as HTMLElement
    retryButton.focus()
    await retry.trigger('click')
    expect(wrapper.find('[data-testid="login-config-failed"]').exists()).toBe(true)
    expect(retry.attributes('aria-busy')).toBe('true')
    expect(document.activeElement).toBe(retryButton)

    resolveConfig({
      loginMethods: ['password'],
      oAuthProviders: [],
      captchaEnabled: false,
      registrationEnabled: false,
    })
    await flushPromises()
    expect(wrapper.find('[data-testid="login-config-failed"]').exists()).toBe(false)
    expect(document.activeElement).toBe(wrapper.find('input').element)
    wrapper.unmount()
  })
})
