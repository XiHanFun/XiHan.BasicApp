/**
 * 列表页的页面引导（schemaGuideChapter / schemaGuideTarget / schemaPageGuideChapter）单元测试
 * 职责边界：通用一章的文案键在十种区域都有译文、锚点都打在 SchemaPage 的组件上、
 * 速览一步随偏好出现，业务步骤取列头、首行单元格、页面操作与行操作的写法，业务一章的记录键
 */
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { schemaGuideChapter, schemaGuideTarget, schemaPageGuideChapter } from '~/components/schema/guide'
import { i18n, LOCALES } from '~/locales'
import { useAppStore } from '~/stores'

/** SchemaPage 及其面板的源码：锚点都打在这里面 */
const sources = import.meta.glob<string>(['~/components/schema/*.vue'], { eager: true, query: '?raw', import: 'default' })

beforeEach(() => {
  setActivePinia(createPinia())
  document.body.innerHTML = ''
})

describe('列表页通用的一章', () => {
  it('章节键是 schema，步骤 id 不重复', () => {
    const steps = schemaGuideChapter.steps()

    expect(schemaGuideChapter.key).toBe('schema')
    expect(new Set(steps.map(step => step.id)).size).toBe(steps.length)
  })

  it('章节名与每一步的文案键在十种区域里都有译文', () => {
    const keys = [
      'guide.schema.title',
      ...schemaGuideChapter.steps().flatMap(step => [step.titleKey, step.descriptionKey]),
    ]

    for (const locale of LOCALES) {
      const missing = keys.filter(key => !i18n.global.te(key, locale))
      expect(missing, `${locale} 缺译文`).toEqual([])
    }
  })

  it('每一步指向的锚点都打在 SchemaPage 的组件上', () => {
    const anchors = schemaGuideChapter.steps()
      .map(step => step.target)
      .filter((target): target is string => typeof target === 'string')
    const placed = Object.values(sources).join('\n')

    expect(anchors.filter(name => !placed.includes(`'${name}'`) && !placed.includes(`"${name}"`))).toEqual([])
  })

  it('悬停速览一步随偏好开关出现', () => {
    const peek = schemaGuideChapter.steps().find(step => step.id === 'peek')!

    useAppStore().tableRowPeek = true
    expect(peek.when?.()).toBe(true)
    useAppStore().tableRowPeek = false
    expect(peek.when?.()).toBe(false)
  })
})

describe('业务步骤取元素的写法', () => {
  it('列头、首行单元格、页面操作与首行的行操作钮', () => {
    const root = document.createElement('div')
    root.innerHTML = `
      <div data-guide-head="status"></div>
      <div data-guide-cell="userName"></div>
      <button data-guide-action="create"></button>
      <div data-guide-cell="__actions__"><button id="more"></button></div>
    `
    const pick = (target: ReturnType<typeof schemaGuideTarget.column>) => typeof target === 'function'
      ? target(root)
      : null

    expect(pick(schemaGuideTarget.column('status'))).toBe(root.querySelector('[data-guide-head="status"]'))
    expect(pick(schemaGuideTarget.cell('userName'))).toBe(root.querySelector('[data-guide-cell="userName"]'))
    expect(pick(schemaGuideTarget.action('create'))).toBe(root.querySelector('[data-guide-action="create"]'))
    expect(pick(schemaGuideTarget.rowActions())).toBe(root.querySelector('#more'))
    expect(pick(schemaGuideTarget.column('missing'))).toBeNull()
  })

  it('业务一章按页面码记看过，章节名与步骤取自页面', () => {
    const steps = [
      { id: 'a', titleKey: 'a.title', descriptionKey: 'a.description' },
    ]
    const chapter = schemaPageGuideChapter('identity.users', () => '用户管理', { version: 2, steps })

    expect(chapter.key).toBe('page:identity.users')
    expect(chapter.version).toBe(2)
    expect(chapter.title()).toBe('用户管理')
    expect(chapter.steps()).toBe(steps)
  })
})
