/**
 * 界面导览（shellGuideChapter / registerShellGuide）单元测试
 * 职责边界：登记成 shell 一章、步骤 id 不重复、文案键在十种区域都有译文、
 * 每个步骤指向的锚点都真的打在了壳层的某个组件上（锚点改名或删掉时这一步会悄悄消失，在这里拦住）
 */
import { createPinia, setActivePinia } from 'pinia'
import { describe, expect, it } from 'vitest'
import { registerShellGuide, shellGuideChapter } from '~/layouts/basic/components/guide/shell-guide'
import { i18n, LOCALES } from '~/locales'
import { useGuideStore } from '~/stores'

/** 壳层各组件的源码：锚点（data-guide="…"）都打在这里面 */
const sources = import.meta.glob<string>(['~/layouts/basic/**/*.vue'], { eager: true, query: '?raw', import: 'default' })

describe('界面导览', () => {
  it('登记后界面导览可重看；章节键是 shell，步骤 id 不重复，开场居中', () => {
    setActivePinia(createPinia())
    registerShellGuide()
    const steps = shellGuideChapter.steps()

    expect(useGuideStore().shellAvailable).toBe(true)
    expect(shellGuideChapter.key).toBe('shell')
    expect(new Set(steps.map(step => step.id)).size).toBe(steps.length)
    expect(steps[0]).toMatchObject({ id: 'intro', target: undefined })
  })

  it('章节名与每一步的文案键在十种区域里都有译文', () => {
    const keys = [
      'guide.shell.title',
      ...shellGuideChapter.steps().flatMap(step => [step.titleKey, step.descriptionKey]),
    ]

    for (const locale of LOCALES) {
      const missing = keys.filter(key => !i18n.global.te(key, locale))
      expect(missing, `${locale} 缺译文`).toEqual([])
    }
  })

  it('每一步指向的锚点都打在壳层的组件上', () => {
    const anchors = shellGuideChapter.steps()
      .map(step => step.target)
      .filter((target): target is string => typeof target === 'string')
    const placed = Object.values(sources).join('\n')

    expect(anchors.length).toBeGreaterThan(0)
    expect(anchors.filter(name => !placed.includes(`data-guide="${name}"`))).toEqual([])
  })
})
