/**
 * 设计器全局配置跟随应用语言的那个 watch，不随第一次登记它的组件卸载而停（elements-config.ts）。
 * 单独一个文件：模块里「已在跟随」的状态是全局的，这里的第一次登记必须发生在组件的作用域里。
 */
import { describe, expect, it, vi } from 'vitest'
import { effectScope, nextTick, ref } from 'vue'

const state = vi.hoisted(() => ({ configs: [] as Record<string, unknown>[] }))

vi.mock('@xihan-ui/web-components', () => ({
  setXhConfig: (config: Record<string, unknown>) => state.configs.push(config),
  mergeXhConfig: (base: Record<string, unknown>, extra?: Record<string, unknown>) => ({
    ...base,
    ...extra,
    translations: { ...(base.translations as object), ...(extra?.translations as object | undefined) },
  }),
}))

vi.mock('~/locales', () => ({ i18n: { global: { locale: ref('zh-CN') } } }))

vi.mock('~/locales/xihan-ui', () => ({
  xhLocales: { 'zh-CN': { translations: { root: 'ui-zh' } }, 'en-US': { translations: { root: 'ui-en' } } },
  xhFallbackLocale: { translations: { root: 'ui-en' } },
}))

describe('设计器的全局配置跟随应用语言', () => {
  it('第一次登记它的组件卸载以后，切语言照样重写全局配置', async () => {
    const [{ registerXhElementsLocale }, { i18n }] = await Promise.all([import('./elements-config'), import('~/locales')])
    // 设计器在组件 setup 里登记；关掉编辑器时组件卸载，它的作用域随之停掉
    const component = effectScope()
    component.run(() => registerXhElementsLocale(locale => ({ translations: { flow: `flow-${locale}` } } as never)))
    component.stop()
    state.configs.length = 0

    i18n.global.locale.value = 'en-US'
    await nextTick()

    expect(state.configs.at(-1)?.locale).toBe('en-US')
    expect(state.configs.at(-1)?.translations).toEqual({ root: 'ui-en', flow: 'flow-en-US' })
  })
})
