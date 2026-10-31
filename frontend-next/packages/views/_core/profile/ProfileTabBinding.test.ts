/**
 * 个人中心「账号绑定」（ProfileTabBinding.vue，4.x 原样搬入）：
 * 登录配置的渠道与本人的绑定合并成一张表——绑定记的是规范类型、登录配置给的是名称（可能是别名），按类型对齐；
 * 对不上任何已配置渠道的绑定也列出来（只能解绑）；绑定交给 store 先领意图再整页跳去发起（领不到即提示后端文案），解绑按绑定记录的类型。
 */
import type { AppContextApis } from '~/types'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { toast } from '~/composables'
import { i18n } from '~/locales'
import { ApiError } from '~/request'
import { registerAppContext, useAuthStore } from '~/stores'
import ProfileTabBinding from './ProfileTabBinding.vue'

const apis = {
  getLinkedAccountsApi: vi.fn(),
  getLoginConfigApi: vi.fn(),
  unlinkAccountApi: vi.fn(),
}

beforeEach(() => {
  setActivePinia(createPinia())
  i18n.global.locale.value = 'zh-CN'
  for (const fn of Object.values(apis))
    fn.mockReset()
  registerAppContext({ apis: apis as unknown as AppContextApis })
  apis.getLoginConfigApi.mockResolvedValue({
    loginMethods: ['password'],
    oAuthProviders: [{ name: 'github', displayName: 'GitHub', type: 'github' }, { name: 'feishu', displayName: '飞书', type: 'lark' }, { name: 'gitee', displayName: 'Gitee', type: 'gitee' }],
    captchaEnabled: false,
  })
  apis.getLinkedAccountsApi.mockResolvedValue([
    { provider: 'github', email: 'octo@example.com', lastLoginTime: '2026-10-30T01:00:00Z' },
    { provider: 'lark' },
    { provider: 'wecom', email: 'w@example.com' },
  ])
})

async function render() {
  const wrapper = mount(ProfileTabBinding, { global: { plugins: [i18n] } })
  await flushPromises()
  return wrapper
}

function rows(wrapper: Awaited<ReturnType<typeof render>>) {
  return wrapper.findAll('.pf-list-item').map(item => ({
    title: item.find('.pf-list-title').text(),
    desc: item.find('.pf-list-desc').text(),
    action: item.find('button').text(),
  }))
}

describe('账号绑定页签', () => {
  it('按登录配置带的规范类型对齐绑定（名称 feishu 的类型是 lark），对不上已配置渠道的绑定排在后面', async () => {
    const wrapper = await render()
    expect(rows(wrapper)).toEqual([
      { title: 'GitHub', desc: expect.stringContaining('octo@example.com'), action: '解除绑定' },
      { title: '飞书', desc: '未关联邮箱', action: '解除绑定' },
      { title: 'Gitee', desc: '未绑定', action: '绑定' },
      { title: 'wecom', desc: 'w@example.com', action: '解除绑定' },
    ])
    expect(rows(wrapper)[0]!.desc).toContain('最后登录')
  })

  it('绑定把登录配置的名称交给 store 发起；领不到绑定意图即提示后端文案', async () => {
    const startOAuthBind = vi.spyOn(useAuthStore(), 'startOAuthBind').mockResolvedValue()
    const danger = vi.spyOn(toast, 'danger').mockImplementation(() => '')
    const wrapper = await render()
    await wrapper.findAll('.pf-list-item')[2]!.find('button').trigger('click')
    expect(startOAuthBind).toHaveBeenCalledWith('gitee')
    expect(danger).not.toHaveBeenCalled()

    startOAuthBind.mockRejectedValue(new ApiError(404, { code: 'IDENTITY_NOTFOUND_005', detail: '该第三方登录方式不存在或未启用。' }))
    await wrapper.findAll('.pf-list-item')[2]!.find('button').trigger('click')
    await flushPromises()
    expect(danger).toHaveBeenCalledWith('该第三方登录方式不存在或未启用。')
  })

  it('没有配置任何渠道、也没有绑定时显示空态', async () => {
    apis.getLoginConfigApi.mockResolvedValue({ loginMethods: ['password'], oAuthProviders: [], captchaEnabled: false, registrationEnabled: false })
    apis.getLinkedAccountsApi.mockResolvedValue([])
    const wrapper = await render()
    expect(wrapper.text()).toContain(i18n.global.t('component.profile.binding.empty'))
  })
})
