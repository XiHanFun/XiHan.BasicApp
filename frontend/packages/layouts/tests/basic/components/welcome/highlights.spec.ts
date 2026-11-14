/**
 * 壳层登记的欢迎页亮点（registerShellWelcomeHighlights）单元测试
 * 职责边界：登记的文案键在十种区域里都有译文、重复登记不重复计、
 * 各屏的出现条件（搜索入口关掉、灵动岛关掉、通行密钥用不了）与个人中心入口的有无
 * 登记表与「登记过没有」是模块级状态，每个用例经 vi.resetModules + 动态 import 取全新副本
 */
import type { AppContextApis } from '~/types'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { i18n, LOCALES } from '~/locales'

type Highlight = import('~/stores').WelcomeHighlight

const getLoginConfigApi = vi.fn<() => Promise<{ loginMethods: string[] }>>()

/** 取一份全新的登记表，登记壳层的几屏；profile 是应用登记的个人中心路由 */
async function setup(options: { profile?: string } = {}): Promise<{ highlights: readonly Highlight[], stores: typeof import('~/stores') }> {
  setActivePinia(createPinia())
  const stores = await import('~/stores')
  stores.registerAppContext({
    apis: { getLoginConfigApi } as unknown as AppContextApis,
    shellRoutes: options.profile
      ? { profile: options.profile }
      : {},
  })
  const { registerShellWelcomeHighlights } = await import('~/layouts/basic/components/welcome/highlights')
  registerShellWelcomeHighlights()
  return {
    highlights: stores.useWelcomeHighlights(),
    stores,
  }
}

function find(highlights: readonly Highlight[], key: string): Highlight {
  const found = highlights.find(item => item.key === key)
  if (!found)
    throw new Error(`没有登记 ${key}`)
  return found
}

/** 把浏览器装成支持通行密钥的样子 */
function stubPasskeys(): void {
  vi.stubGlobal('isSecureContext', true)
  vi.stubGlobal('PublicKeyCredential', class {})
  Object.defineProperty(navigator, 'credentials', {
    configurable: true,
    value: {
      create: vi.fn(),
      get: vi.fn(),
    },
  })
}

beforeEach(() => {
  vi.resetModules()
  localStorage.clear()
  getLoginConfigApi.mockReset()
  getLoginConfigApi.mockResolvedValue({
    loginMethods: ['password', 'passkey'],
  })
})

afterEach(() => {
  vi.unstubAllGlobals()
  Reflect.deleteProperty(navigator, 'credentials')
})

describe('壳层的欢迎页亮点', () => {
  it('登记界面、命令面板、灵动岛、通行密钥四屏，排序号互不相同；重复登记不重复计', async () => {
    const { highlights } = await setup()
    const { registerShellWelcomeHighlights } = await import('~/layouts/basic/components/welcome/highlights')
    registerShellWelcomeHighlights()

    expect(highlights.map(item => item.key)).toEqual([
      'shell',
      'command',
      'island',
      'passkey',
    ])
    expect(new Set(highlights.map(item => item.order)).size).toBe(highlights.length)
  })

  it('每一屏用到的文案键在十种区域里都有译文', async () => {
    const { highlights } = await setup({ profile: '/profile' })
    const keys = highlights.flatMap(item => [
      item.categoryKey,
      item.titleKey,
      item.descriptionKey,
      ...item.pointKeys,
      ...(item.action
        ? [item.action.labelKey]
        : []),
    ])

    expect(keys.length).toBeGreaterThan(0)
    for (const locale of LOCALES) {
      const missing = keys.filter(key => !i18n.global.te(key, locale))
      expect(missing, `${locale} 缺译文`).toEqual([])
    }
  })

  it('偏好里把搜索入口关掉：命令面板那一屏不出现', async () => {
    const { highlights, stores } = await setup()
    const when = find(highlights, 'command').when!

    expect(await when()).toBe(true)
    stores.useAppStore().widgetSearchPlacement = 'hidden'
    expect(await when()).toBe(false)
  })

  it('偏好里把灵动岛关掉：灵动岛那一屏不出现', async () => {
    const { highlights, stores } = await setup()
    const when = find(highlights, 'island').when!

    stores.useAppStore().widgetDynamicIsland = true
    expect(await when()).toBe(true)
    stores.useAppStore().widgetDynamicIsland = false
    expect(await when()).toBe(false)
  })

  it('通行密钥那一屏：浏览器支持且登录方式里开放了才出现', async () => {
    const { highlights } = await setup()
    const when = find(highlights, 'passkey').when!

    // 浏览器不支持：不必去问登录方式
    expect(await when()).toBe(false)
    expect(getLoginConfigApi).not.toHaveBeenCalled()

    stubPasskeys()
    expect(await when()).toBe(true)

    getLoginConfigApi.mockResolvedValue({
      loginMethods: ['password'],
    })
    expect(await when()).toBe(false)
  })

  it('应用登记了个人中心的路由才给「去添加通行密钥」的入口', async () => {
    expect(find((await setup()).highlights, 'passkey').action).toBeUndefined()

    vi.resetModules()
    expect(find((await setup({ profile: '/profile' })).highlights, 'passkey').action).toMatchObject({ to: '/profile' })
  })
})
