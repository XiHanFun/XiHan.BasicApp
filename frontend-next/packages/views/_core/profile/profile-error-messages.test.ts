/**
 * 个人中心的错误提示：后端错误只显示本地化后的 detail，不带错误码；不是 ApiError 的异常保持 message
 * 开发者、OAuth、安全三个页签与个人中心页本身，别的页签各自的测试里有
 */
import type { AppContextApis } from '~/types'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createMemoryHistory, createRouter } from 'vue-router'
import { toast } from '~/composables'
import { i18n } from '~/locales'
import { ApiError } from '~/request'
import { registerAppContext } from '~/stores'
import ProfilePage from './index.vue'
import ProfileTabDeveloper from './ProfileTabDeveloper.vue'
import ProfileTabOAuth from './ProfileTabOAuth.vue'
import ProfileTabSecurity from './ProfileTabSecurity.vue'

const rejected = vi.fn()
const problem = new ApiError(403, { code: 'IDENTITY_DENIED_021', detail: '没有权限。' })
const mounted: Array<{ unmount: () => void }> = []

beforeEach(() => {
  setActivePinia(createPinia())
  i18n.global.locale.value = 'zh-CN'
  rejected.mockReset()
  const names = ['getApiCredentialsApi', 'getApiCallSettingsApi', 'getMyOAuthAppsApi', 'getOAuthConsentsApi', 'getProfileApi', 'setup2FAApi']
  registerAppContext({ apis: Object.fromEntries(names.map(name => [name, rejected])) as unknown as AppContextApis })
})

afterEach(() => {
  for (const wrapper of mounted.splice(0))
    wrapper.unmount()
  vi.restoreAllMocks()
})

async function failuresOf(error: unknown, run: () => Promise<void>) {
  rejected.mockRejectedValue(error)
  const danger = vi.spyOn(toast, 'danger').mockImplementation(() => '')
  await run()
  await flushPromises()
  return danger.mock.calls.map(call => call[0])
}

function renderTab(component: typeof ProfileTabDeveloper | typeof ProfileTabOAuth) {
  return async () => {
    mounted.push(mount(component, { global: { plugins: [i18n] }, attachTo: document.body }))
  }
}

async function renderSecurityAndStartTotp() {
  const wrapper = mount(ProfileTabSecurity, { props: { profile: null }, global: { plugins: [i18n] }, attachTo: document.body })
  mounted.push(wrapper)
  await (wrapper.vm as unknown as { startTotpSetup: () => Promise<void> }).startTotpSetup()
}

async function renderPage() {
  const router = createRouter({ history: createMemoryHistory(), routes: [{ path: '/', component: { template: '<div />' } }] })
  mounted.push(mount(ProfilePage, { global: { plugins: [i18n, router], stubs: { ProfileTabInfo: true } }, attachTo: document.body }))
}

describe('个人中心的错误提示', () => {
  it.each([
    ['开发者', renderTab(ProfileTabDeveloper)],
    ['OAuth', renderTab(ProfileTabOAuth)],
    ['安全', renderSecurityAndStartTotp],
    ['个人中心页', renderPage],
  ])('%s：后端错误不带错误码，别的异常保持 message', async (_name, run) => {
    const failures = await failuresOf(problem, run)
    expect(failures.length).toBeGreaterThan(0)
    expect(new Set(failures)).toEqual(new Set(['没有权限。']))

    vi.restoreAllMocks()
    const others = await failuresOf(new Error('网络断开'), run)
    expect(others.length).toBeGreaterThan(0)
    expect(new Set(others)).toEqual(new Set(['网络断开']))
  })
})
