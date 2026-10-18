import { describe, expect, it, vi } from 'vitest'
import { ref } from 'vue'

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
  xhLocales: { 'zh-CN': { translations: { root: 'ui-zh' } } },
  xhFallbackLocale: { translations: { root: 'ui-en' } },
}))

describe('设计器的全局配置', () => {
  it('写入时带上应用的语言、合并后的语言包与 Vue 一侧同一个滚动源', async () => {
    const [{ registerXhElementsLocale }, { getScrollRoot, setScrollRoot }] = await Promise.all([
      import('./elements-config'),
      import('~/composables/useScrollRoot'),
    ])
    const container = document.createElement('main')
    setScrollRoot(container)

    registerXhElementsLocale(locale => ({ translations: { flow: `flow-${locale}` } } as never))

    const config = state.configs.at(-1)!
    expect(config.locale).toBe('zh-CN')
    expect(config.translations).toEqual({ root: 'ui-zh', flow: 'flow-zh-CN' })
    expect(config.scrollRoot).toBe(getScrollRoot)
    expect((config.scrollRoot as () => HTMLElement | null)()).toBe(container)
    setScrollRoot(null)
  })
})
