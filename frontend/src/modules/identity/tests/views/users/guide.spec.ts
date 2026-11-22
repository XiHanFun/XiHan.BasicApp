/**
 * 用户管理的页面引导单元测试
 * 职责边界：页面在 PageSchema.guide 里写的每个文案键在十种区域都有译文（键写在页面源码里，i18n 门禁扫不到这种不经 t() 的引用）
 */
import { describe, expect, it } from 'vitest'
import { i18n, LOCALES } from '~/locales'

const source = Object.values(import.meta.glob<string>('@/modules/identity/views/users/index.vue', { eager: true, query: '?raw', import: 'default' }))[0]!

describe('用户管理的页面引导', () => {
  it('业务步骤的文案键在十种区域里都有译文', () => {
    const keys = [
      ...source.matchAll(/'(identity\.users\.guide\.[\w.]+)'/g),
    ].map(match => match[1]!)

    expect(keys.length).toBeGreaterThan(0)
    for (const locale of LOCALES) {
      const missing = keys.filter(key => !i18n.global.te(key, locale))
      expect(missing, `${locale} 缺译文`).toEqual([])
    }
  })
})
