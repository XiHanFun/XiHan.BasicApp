/**
 * 新版欢迎页登记点与开合状态（welcome）单元测试
 * 职责边界：版本线的解析与比较、亮点登记的覆盖规则、按页面 / 权限 / 条件裁剪、
 * 「看过没有」的读写与各种不该弹的情形（看过、模仿登录、没有会话、读不到、开关关着）
 * 登记表是模块级单例，每个用例经 vi.resetModules + 动态 import 取全新副本；后端经 AppContext 注入替身
 */
import type { AppContextApis } from '~/types'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { UserSettingScene, WELCOME_SETTING_KEY } from '~/constants'

type WelcomeModule = typeof import('~/stores/welcome')
type Highlight = import('~/stores/welcome').WelcomeHighlight

interface SettingDto { scene: number, settingKey: string, settingValue?: null | string }

const getApi = vi.fn<(input: { scene: number, settingKey: string }) => Promise<SettingDto>>()
const saveApi = vi.fn<(input: { scene: number, settingKey: string, settingValue?: null | string }) => Promise<SettingDto>>()

/** 当前应用版本所在的版本线：用例里的亮点都登记在它上面 */
let current = ''

function highlight(key: string, overrides: Partial<Highlight> = {}): Highlight {
  return {
    release: current,
    key,
    order: 10,
    icon: 'lucide:box',
    categoryKey: `${key}.category`,
    titleKey: `${key}.title`,
    descriptionKey: `${key}.description`,
    pointKeys: [`${key}.point`],
    demo: () => Promise.resolve({ default: {} }),
    ...overrides,
  }
}

function seen(release: null | string): SettingDto {
  return {
    scene: UserSettingScene.Preference,
    settingKey: WELCOME_SETTING_KEY,
    settingValue: release === null
      ? null
      : JSON.stringify({ release }),
  }
}

/** 取一份全新的登记表与仓库，并给当前用户配上可见页面与权限码 */
async function setup(options: { pages?: string[], permissions?: string[], impersonator?: string, signedOut?: boolean } = {}): Promise<WelcomeModule & { store: ReturnType<WelcomeModule['useWelcomeStore']> }> {
  setActivePinia(createPinia())
  const { registerAppContext } = await import('~/stores/app-context')
  registerAppContext({
    apis: {
      userSettingApi: { get: getApi, save: saveApi },
    } as unknown as AppContextApis,
  })
  const { useAuthStore } = await import('~/stores/auth')
  const auth = useAuthStore()
  auth.permissions = {
    permissions: options.permissions ?? [],
    pages: options.pages ?? [],
    roles: [],
    menus: [],
  } as unknown as typeof auth.permissions
  auth.session = options.signedOut
    ? null
    : {
        accessToken: 'a',
        refreshToken: 'r',
        user: { id: 'u' },
        impersonatorUserName: options.impersonator,
      } as unknown as typeof auth.session
  const module = await import('~/stores/welcome')
  current = module.releaseLineOf(__APP_VERSION__)!
  return {
    ...module,
    store: module.useWelcomeStore(),
  }
}

beforeEach(() => {
  vi.resetModules()
  localStorage.clear()
  getApi.mockReset()
  saveApi.mockReset()
  getApi.mockResolvedValue(seen(null))
  saveApi.mockResolvedValue(seen(null))
})

afterEach(() => {
  vi.unstubAllEnvs()
})

describe('版本线', () => {
  it('从版本号里取主版本.次版本，预发布后缀不算', async () => {
    const { releaseLineOf } = await setup()

    expect(releaseLineOf('6.0.0-preview.2')).toBe('6.0')
    expect(releaseLineOf('10.12.3')).toBe('10.12')
    expect(releaseLineOf('next')).toBeNull()
  })

  it('按数值比较，不按字符串', async () => {
    const { compareReleaseLines } = await setup()

    expect(compareReleaseLines('6.10', '6.9')).toBeGreaterThan(0)
    expect(compareReleaseLines('6.0', '6.0')).toBe(0)
    expect(compareReleaseLines('5.9', '6.0')).toBeLessThan(0)
  })

  it('取登记过、又不高于当前版本的最新一条', async () => {
    const { welcomeReleaseFor } = await setup()

    expect(welcomeReleaseFor('6.1.0', ['6.0', '6.1', '6.2'])).toBe('6.1')
    expect(welcomeReleaseFor('6.1.0', ['6.2', '7.0'])).toBeNull()
    expect(welcomeReleaseFor('6.1.0', [])).toBeNull()
  })
})

describe('登记', () => {
  it('同一条版本线里同名的后登记覆盖先登记，不同版本线各记各的', async () => {
    const { registerWelcomeHighlights, useWelcomeHighlights } = await setup()

    registerWelcomeHighlights(highlight('a', { order: 1 }), highlight('a', { order: 2 }), highlight('a', { release: '1.0', order: 3 }))

    expect(useWelcomeHighlights().map(item => [item.release, item.order])).toEqual([
      [current, 2],
      ['1.0', 3],
    ])
  })
})

describe('进入壳层时的检查', () => {
  it('这条版本线没看过：打开欢迎层，亮点按排序号排', async () => {
    const { registerWelcomeHighlights, store } = await setup()
    registerWelcomeHighlights(highlight('late', { order: 20 }), highlight('early', { order: 10 }))

    await store.check()

    expect(getApi).toHaveBeenCalledWith({ scene: UserSettingScene.Preference, settingKey: WELCOME_SETTING_KEY })
    expect(store.open).toBe(true)
    expect(store.replay).toBe(false)
    expect(store.release).toBe(current)
    expect(store.slides.map(item => item.key)).toEqual(['early', 'late'])
    expect(store.checked).toBe(true)
  })

  it('看过这条版本线或更新的：不打开', async () => {
    const { registerWelcomeHighlights, store } = await setup()
    registerWelcomeHighlights(highlight('a'))

    getApi.mockResolvedValue(seen(current))
    await store.check()
    expect(store.open).toBe(false)

    getApi.mockResolvedValue(seen('99.0'))
    await store.check()
    expect(store.open).toBe(false)
    expect(store.checked).toBe(true)
  })

  it('只看过更早的版本线：打开', async () => {
    const { registerWelcomeHighlights, store } = await setup()
    registerWelcomeHighlights(highlight('a'))
    getApi.mockResolvedValue(seen('0.1'))

    await store.check()

    expect(store.open).toBe(true)
  })

  it('没有登记过不高于当前版本的亮点：不发请求、不打开', async () => {
    const { registerWelcomeHighlights, store } = await setup()
    registerWelcomeHighlights(highlight('future', { release: '99.0' }))

    await store.check()

    expect(getApi).not.toHaveBeenCalled()
    expect(store.open).toBe(false)
    expect(store.available).toBe(false)
  })

  it('读不到「看过没有」：这一次不打开，照样算检查完了', async () => {
    const { registerWelcomeHighlights, store } = await setup()
    registerWelcomeHighlights(highlight('a'))
    getApi.mockRejectedValue(new Error('offline'))

    await store.check()

    expect(store.open).toBe(false)
    expect(store.checked).toBe(true)
  })

  it('模仿登录时不打开，也不去读', async () => {
    const { registerWelcomeHighlights, store } = await setup({ impersonator: 'admin' })
    registerWelcomeHighlights(highlight('a'))

    await store.check()

    expect(getApi).not.toHaveBeenCalled()
    expect(store.open).toBe(false)
  })

  it('会话已经没了（正在退出）：不打开，也不去读', async () => {
    const { registerWelcomeHighlights, store } = await setup({ signedOut: true })
    registerWelcomeHighlights(highlight('a'))

    await store.check()

    expect(getApi).not.toHaveBeenCalled()
    expect(store.open).toBe(false)
  })

  it('开关关着：不打开，重看入口也收起', async () => {
    vi.stubEnv('VITE_APP_WELCOME', 'false')
    const { registerWelcomeHighlights, store } = await setup()
    registerWelcomeHighlights(highlight('a'))

    await store.check()
    await store.show()

    expect(getApi).not.toHaveBeenCalled()
    expect(store.open).toBe(false)
    expect(store.available).toBe(false)
  })

  it('并发的检查只发一次请求', async () => {
    const { registerWelcomeHighlights, store } = await setup()
    registerWelcomeHighlights(highlight('a'))

    await Promise.all([
      store.check(),
      store.check(),
    ])

    expect(getApi).toHaveBeenCalledTimes(1)
  })
})

describe('亮点的裁剪', () => {
  it('挂在页面上的亮点：后端没有下发那个页面就不出现', async () => {
    const { registerWelcomeHighlights, store } = await setup({
      pages: ['pkg.shown'],
    })
    registerWelcomeHighlights(highlight('shown', { page: 'pkg.shown' }), highlight('hidden', { page: 'pkg.hidden' }), highlight('free'))

    await store.check()

    expect(store.slides.map(item => item.key).sort()).toEqual(['free', 'shown'])
  })

  it('声明了权限码的亮点：没有这个权限就不出现', async () => {
    const { registerWelcomeHighlights, store } = await setup({
      permissions: ['chat.chat-conversations.read'],
    })
    registerWelcomeHighlights(highlight('granted', { permission: 'chat.chat-conversations.read' }), highlight('denied', { permission: 'chat.chat-messages.send' }))

    await store.check()

    expect(store.slides.map(item => item.key)).toEqual(['granted'])
  })

  it('另外的条件：不成立或判不出来的那一屏不出现，其余照常', async () => {
    const { registerWelcomeHighlights, store } = await setup()
    registerWelcomeHighlights(
      highlight('yes', {
        when: () => true,
      }),
      highlight('later', {
        when: () => Promise.resolve(true),
      }),
      highlight('no', {
        when: () => false,
      }),
      highlight('broken', {
        when: () => Promise.reject(new Error('unknown')),
      }),
    )

    await store.check()

    expect(store.slides.map(item => item.key).sort()).toEqual(['later', 'yes'])
  })
})

describe('关闭与重看', () => {
  it('首次出现的这一次关掉：把版本线记到服务端', async () => {
    const { registerWelcomeHighlights, store } = await setup()
    registerWelcomeHighlights(highlight('a'))
    await store.check()

    store.finish()

    expect(store.open).toBe(false)
    expect(saveApi).toHaveBeenCalledWith({
      scene: UserSettingScene.Preference,
      settingKey: WELCOME_SETTING_KEY,
      settingValue: JSON.stringify({ release: current }),
    })
  })

  it('记不上不拦关闭', async () => {
    const { registerWelcomeHighlights, store } = await setup()
    registerWelcomeHighlights(highlight('a'))
    saveApi.mockRejectedValue(new Error('offline'))
    await store.check()

    expect(() => store.finish()).not.toThrow()
    expect(store.open).toBe(false)
  })

  it('重看：不读也不改「看过」的记录', async () => {
    const { registerWelcomeHighlights, store } = await setup()
    registerWelcomeHighlights(highlight('a'))

    await store.show()
    expect(store.open).toBe(true)
    expect(store.replay).toBe(true)
    expect(store.available).toBe(true)

    store.finish()
    expect(getApi).not.toHaveBeenCalled()
    expect(saveApi).not.toHaveBeenCalled()
  })

  it('没开着的时候关闭是空操作', async () => {
    const { store } = await setup()

    store.finish()

    expect(saveApi).not.toHaveBeenCalled()
  })
})
