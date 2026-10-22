/**
 * 顶栏右上角的上下文切换（HeaderContextMenu）与账号菜单（HeaderUserMenu）。
 *
 * 锁定：两处触发钮都不带下拉箭头、照常是菜单按钮（点击与键盘可开）；展开后上下文菜单列出可切换的租户并给当前一项打勾，
 * 选中别的租户即切换；账号菜单的触发钮只有头像，名字在浮层第一行。
 */
import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createI18n } from 'vue-i18n'
import zhHeader from '~/locales/langs/zh-CN/header'

const hoisted = vi.hoisted(() => ({
  myAvailableTenants: vi.fn(),
  switchContext: vi.fn(),
  userInfo: { canAccessPlatform: false, tenantId: '2', isPlatform: false } as Record<string, unknown>,
}))

vi.mock('~/stores', async importOriginal => ({
  ...await importOriginal<Record<string, unknown>>(),
  useAppContext: () => ({ apis: { tenantApi: { myAvailableTenants: hoisted.myAvailableTenants } } }),
  useAuthStore: () => ({ switchContext: hoisted.switchContext }),
  useUserStore: () => ({ userInfo: hoisted.userInfo }),
}))

const { default: HeaderContextMenu } = await import('./HeaderContextMenu.vue')
const { default: HeaderUserMenu } = await import('./HeaderUserMenu.vue')

function i18n() {
  return createI18n({ legacy: false, locale: 'zh-CN', messages: { 'zh-CN': { header: zhHeader } } })
}

function tenant(tenantId: string, tenantName: string, isCurrent: boolean) {
  return { membershipId: `m${tenantId}`, tenantId, tenantCode: `t${tenantId}`, tenantName, memberType: 'Member', isCurrent, joinedTime: '' }
}

async function open(trigger: HTMLElement) {
  trigger.click()
  await flushPromises()
}

beforeEach(() => {
  hoisted.myAvailableTenants.mockResolvedValue([tenant('1', '甲公司', false), tenant('2', '示例企业', true)])
  hoisted.switchContext.mockResolvedValue(undefined)
  hoisted.userInfo = { canAccessPlatform: false, tenantId: '2', isPlatform: false }
})

afterEach(() => {
  vi.clearAllMocks()
  document.body.innerHTML = ''
})

describe('上下文切换', () => {
  function mountContext() {
    return mount(HeaderContextMenu, {
      props: { label: '示例企业', isPlatform: false },
      global: { plugins: [i18n()] },
      attachTo: document.body,
    })
  }

  it('触发钮是菜单按钮，名字进可及名，不带下拉箭头', () => {
    const wrapper = mountContext()
    const trigger = wrapper.get('button')

    expect(trigger.attributes('aria-haspopup')).toBe('menu')
    expect(trigger.attributes('aria-label')).toBe('当前：示例企业，切换租户')
    expect(trigger.html()).not.toContain('chevron-down')
  })

  it('点击展开后列出可切换的租户，当前租户打勾，末项是控制中心', async () => {
    const wrapper = mountContext()
    await open(wrapper.get('button').element)

    expect(hoisted.myAvailableTenants).toHaveBeenCalledTimes(1)
    const radios = [...document.querySelectorAll<HTMLElement>('[role="menuitemradio"]')]
    expect(radios.map(item => item.textContent?.trim())).toEqual(['甲公司', '示例企业'])
    expect(radios.map(item => item.getAttribute('aria-checked'))).toEqual(['false', 'true'])
    const items = [...document.querySelectorAll<HTMLElement>('[role="menuitem"]')]
    expect(items.at(-1)?.textContent?.trim()).toBe('控制中心')
  })

  it('平台账号多一项「平台」', async () => {
    hoisted.userInfo = { canAccessPlatform: true, tenantId: null, isPlatform: true }
    const wrapper = mount(HeaderContextMenu, {
      props: { label: '平台', isPlatform: true },
      global: { plugins: [i18n()] },
      attachTo: document.body,
    })
    await open(wrapper.get('button').element)

    const radios = [...document.querySelectorAll<HTMLElement>('[role="menuitemradio"]')]
    expect(radios[0]?.textContent?.trim()).toBe('平台')
    expect(radios[0]?.getAttribute('aria-checked')).toBe('true')
  })

  it('选中别的租户即切换到它', async () => {
    const wrapper = mountContext()
    await open(wrapper.get('button').element)

    document.querySelector<HTMLElement>('[role="menuitemradio"]')!.click()
    await flushPromises()

    expect(hoisted.switchContext).toHaveBeenCalledWith('1')
  })

  it('点控制中心交给宿主跳转', async () => {
    const wrapper = mountContext()
    await open(wrapper.get('button').element)

    const items = [...document.querySelectorAll<HTMLElement>('[role="menuitem"]')]
    items.at(-1)!.click()
    await flushPromises()

    expect(wrapper.emitted('controlCenter')).toHaveLength(1)
    expect(hoisted.switchContext).not.toHaveBeenCalled()
  })
})

describe('账号菜单', () => {
  function mountUser() {
    return mount(HeaderUserMenu, {
      props: {
        avatar: null,
        nickname: '张总',
        username: 'owner',
        options: [
          { key: 'profile', label: '个人中心' },
          { key: 'divider', type: 'divider' },
          { key: 'logout', label: '退出登录' },
        ],
      },
      global: { plugins: [i18n()] },
      attachTo: document.body,
    })
  }

  it('触发钮只有头像：没有名字文字、没有下拉箭头，名字进可及名', () => {
    const wrapper = mountUser()
    const trigger = wrapper.get('button')

    expect(trigger.attributes('aria-haspopup')).toBe('menu')
    expect(trigger.attributes('aria-label')).toBe('张总')
    expect(trigger.text()).not.toContain('owner')
    expect(trigger.html()).not.toContain('chevron-down')
  })

  it('展开后第一行是头像与名字，下面才是账号操作', async () => {
    const wrapper = mountUser()
    await open(wrapper.get('button').element)

    const menu = document.querySelector<HTMLElement>('[role="menu"]')!
    const items = [...menu.querySelectorAll<HTMLElement>('[role="menuitem"]')]
    expect(items.map(item => item.textContent?.trim())).toEqual(['个人中心', '退出登录'])
    const firstRow = menu.textContent ?? ''
    expect(firstRow.indexOf('张总')).toBeLessThan(firstRow.indexOf('个人中心'))
    expect(firstRow).toContain('owner')
    // 分组以第一行为名，头像缩写不进可及名
    const group = menu.querySelector<HTMLElement>('[role="group"]')!
    const groupLabel = document.getElementById(group.getAttribute('aria-labelledby')!)!
    expect(groupLabel.querySelector('[aria-hidden="true"]')).not.toBeNull()
  })

  it('选中一项交给宿主', async () => {
    const wrapper = mountUser()
    await open(wrapper.get('button').element)

    document.querySelectorAll<HTMLElement>('[role="menuitem"]')[1]!.click()
    await flushPromises()

    expect(wrapper.emitted('select')).toEqual([['logout']])
  })
})
