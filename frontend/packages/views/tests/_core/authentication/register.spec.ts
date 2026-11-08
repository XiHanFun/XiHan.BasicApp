/**
 * 注册页（register.vue）的条款勾选：
 * 没勾选同意隐私政策与服务条款就不提交、给出提示；勾选后照常提交；
 * 复选框的可访问名称取旁边那段文案，两份文书的名字都在里面
 */
import type { AppContextApis } from '~/types'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createMemoryHistory, createRouter } from 'vue-router'
import { i18n } from '~/locales'
import { registerAppContext } from '~/stores'
import RegisterPage from '~/views/_core/authentication/register.vue'

const warning = vi.fn()

vi.mock('~/composables', async importOriginal => ({
  ...await importOriginal<typeof import('~/composables')>(),
  toast: {
    warning: (...args: unknown[]) => warning(...args),
    success: vi.fn(),
    danger: vi.fn(),
  },
}))

const registerApi = vi.fn()
const getLoginConfigApi = vi.fn()

const t = (key: string) => i18n.global.t(key)

async function open() {
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
  await router.push('/auth/register')
  registerAppContext({
    apis: {
      registerApi,
      getLoginConfigApi,
      getCaptchaApi: vi.fn(),
    } as unknown as AppContextApis,
  })
  const wrapper = mount(RegisterPage, {
    global: {
      plugins: [i18n, router],
    },
  })
  await flushPromises()
  await wrapper.find('input[autocomplete="username"]').setValue('alice')
  await wrapper.find('input[autocomplete="email"]').setValue('alice@example.com')
  for (const input of wrapper.findAll('input[autocomplete="new-password"]')) {
    await input.setValue('Passw0rd!')
  }
  return wrapper
}

beforeEach(() => {
  setActivePinia(createPinia())
  i18n.global.locale.value = 'zh-CN'
  warning.mockReset()
  registerApi.mockReset()
  registerApi.mockResolvedValue({ userId: 'u-1', userName: 'alice' })
  getLoginConfigApi.mockReset()
  getLoginConfigApi.mockResolvedValue({
    loginMethods: ['password'],
    oAuthProviders: [],
    captchaEnabled: false,
    registrationEnabled: true,
  })
})

describe('注册页的条款勾选', () => {
  it('没勾选不提交并提示，勾选后照常提交', async () => {
    const wrapper = await open()

    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(registerApi).not.toHaveBeenCalled()
    expect(warning).toHaveBeenCalledWith(t('page.auth.agree_required'))

    await wrapper.find('[role="checkbox"]').trigger('click')
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(registerApi).toHaveBeenCalledWith(expect.objectContaining({ username: 'alice', email: 'alice@example.com', password: 'Passw0rd!' }))
  })

  it('复选框的名字取旁边的文案，两份文书都在里面', async () => {
    const wrapper = await open()
    const checkbox = wrapper.find('[role="checkbox"]')
    const label = wrapper.find(`[id="${checkbox.attributes('aria-labelledby')}"]`)

    expect(label.text()).toContain(t('page.auth.privacy_policy'))
    expect(label.text()).toContain(t('page.auth.terms_of_service'))
  })
})
