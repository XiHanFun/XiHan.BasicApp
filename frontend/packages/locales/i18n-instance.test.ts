/**
 * packages/locales/index.ts 的 i18n 实例装配行为。
 *
 * 职责边界：只测本文件自己负责的四件事——初始语言取值来源、fallbackLocale 回落、
 * registerLocaleMessages 的深合并语义、以及 `$t` 解构导出后仍可独立调用。
 * 语言包内容的结构校验在 locale-messages.test.ts，本文件不重复。
 *
 * 实例是模块级单例，因此每个用例都 vi.resetModules() 后重新动态导入，
 * 保证用例之间互不串状态、可任意顺序执行。
 */
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { DEFAULT_LOCALE, LOCALE_KEY } from '~/constants'

type LocalesModule = typeof import('./index')

async function loadLocales(): Promise<LocalesModule> {
  vi.resetModules()
  return import('./index')
}

beforeEach(() => {
  localStorage.clear()
})

afterEach(() => {
  vi.resetModules()
  localStorage.clear()
})

describe('初始语言取值', () => {
  it('本地存储没有语言偏好时落到 DEFAULT_LOCALE', async () => {
    const { i18n } = await loadLocales()

    expect(i18n.global.locale.value).toBe(DEFAULT_LOCALE)
    expect(DEFAULT_LOCALE).toBe('zh-CN')
  })

  it('本地存储写了语言偏好时按存储值启动，而不是固定用默认语言', async () => {
    localStorage.setItem(LOCALE_KEY, JSON.stringify('en-US'))

    const { i18n } = await loadLocales()

    expect(i18n.global.locale.value).toBe('en-US')
  })

  it('本地存储里是坏 JSON 时按默认语言启动，不让首屏崩在解析上', async () => {
    localStorage.setItem(LOCALE_KEY, '{ this is not json')

    const { i18n } = await loadLocales()

    expect(i18n.global.locale.value).toBe(DEFAULT_LOCALE)
  })
})

describe('翻译与回落', () => {
  it('en-US 下取到的是英文原文而非中文，证明两份消息都装进了同一实例', async () => {
    const { i18n } = await loadLocales()
    i18n.global.locale.value = 'en-US'

    expect(i18n.global.t('tabbar.reload')).toBe('Reload')

    i18n.global.locale.value = 'zh-CN'
    expect(i18n.global.t('tabbar.reload')).toBe('重新加载')
  })

  it('当前语言缺失的键回落到 zh-CN 的译文，而不是返回裸 key', async () => {
    const { i18n, registerLocaleMessages } = await loadLocales()
    registerLocaleMessages({ 'zh-CN': { probe: { onlyZh: '仅中文' } } })
    i18n.global.locale.value = 'en-US'

    expect(i18n.global.t('probe.onlyZh')).toBe('仅中文')
  })

  it('两份语言包都没有的键原样返回 key，便于人工发现漏定义', async () => {
    const { i18n } = await loadLocales()

    expect(i18n.global.t('tabbar.__not_defined__')).toBe('tabbar.__not_defined__')
  })

  it('具名占位符按传入命名参数插值', async () => {
    const { i18n, registerLocaleMessages } = await loadLocales()
    registerLocaleMessages({ 'zh-CN': { probe: { greet: '你好 {name}，共 {count} 项' } } })

    expect(i18n.global.t('probe.greet', { name: '张三', count: 3 })).toBe('你好 张三，共 3 项')
  })
})

describe('registerLocaleMessages 深合并', () => {
  it('注册业务命名空间不会冲掉 shell 自带的 tabbar 文案', async () => {
    const { i18n, registerLocaleMessages } = await loadLocales()

    registerLocaleMessages({ 'zh-CN': { identity: { title: '身份管理' } } })

    expect(i18n.global.t('identity.title')).toBe('身份管理')
    expect(i18n.global.t('tabbar.reload')).toBe('重新加载')
  })

  it('往已存在的命名空间补键时同层旧键保留，而非整块覆盖', async () => {
    const { i18n, registerLocaleMessages } = await loadLocales()

    registerLocaleMessages({ 'zh-CN': { tabbar: { extra_probe: '补充项' } } })

    expect(i18n.global.t('tabbar.extra_probe')).toBe('补充项')
    expect(i18n.global.t('tabbar.reload')).toBe('重新加载')
    expect(i18n.global.t('tabbar.overview')).toBe('标签总览')
  })

  it('同名键以后注册的应用文案为准，允许 src 覆写 shell 默认文案', async () => {
    const { i18n, registerLocaleMessages } = await loadLocales()

    registerLocaleMessages({ 'zh-CN': { tabbar: { reload: '刷新本页' } } })

    expect(i18n.global.t('tabbar.reload')).toBe('刷新本页')
  })

  it('一次调用可同时注册多语言，各自落到对应语言而不串味', async () => {
    const { i18n, registerLocaleMessages } = await loadLocales()

    registerLocaleMessages({
      'zh-CN': { probe: { word: '词' } },
      'en-US': { probe: { word: 'Word' } },
    })

    i18n.global.locale.value = 'zh-CN'
    expect(i18n.global.t('probe.word')).toBe('词')
    i18n.global.locale.value = 'en-US'
    expect(i18n.global.t('probe.word')).toBe('Word')
  })

  it('传入空对象是安全空转，既不抛错也不影响既有文案', async () => {
    const { i18n, registerLocaleMessages } = await loadLocales()

    expect(() => registerLocaleMessages({})).not.toThrow()
    expect(i18n.global.t('tabbar.reload')).toBe('重新加载')
  })
})

describe('$t 解构导出', () => {
  it('从 i18n.global 解构出来的 $t 脱离宿主对象仍能翻译（丢 this 即失效的回归锚点）', async () => {
    const { $t } = await loadLocales()
    const detached = $t

    expect(detached('tabbar.reload')).toBe('重新加载')
  })

  it('$t 与 i18n.global.t 指向同一份消息，注册后立即对 $t 生效', async () => {
    const { $t, registerLocaleMessages } = await loadLocales()

    registerLocaleMessages({ 'zh-CN': { probe: { late: '后注册' } } })

    expect($t('probe.late')).toBe('后注册')
  })
})
