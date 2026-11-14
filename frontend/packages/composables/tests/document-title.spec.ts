/**
 * composables/document-title.ts：页面标题
 * 锁定：正文与未读数各自设置、合成 document.title；未读数为 0 不带、超过 99 写成 99+、负数与小数收成非负整数；
 * 正文还没给过时把页面现有的标题当正文；换正文时未读数留着
 */
import { beforeEach, describe, expect, it, vi } from 'vitest'

type TitleModule = typeof import('~/composables/document-title')

let title: TitleModule

beforeEach(async () => {
  // 模块里记着正文与未读数：每个用例重新装一份
  vi.resetModules()
  document.title = 'Initial'
  title = await import('~/composables/document-title')
})

describe('页面标题', () => {
  it('只给正文：标题就是正文', () => {
    title.setDocumentTitle('Dashboard - App')

    expect(document.title).toBe('Dashboard - App')
  })

  it('有未读时前面带数，清零即去掉；换正文时未读数留着', () => {
    title.setDocumentTitle('Dashboard - App')
    title.setDocumentTitleBadge(3)
    expect(document.title).toBe('(3) Dashboard - App')

    title.setDocumentTitle('Chat - App')
    expect(document.title).toBe('(3) Chat - App')

    title.setDocumentTitleBadge(0)
    expect(document.title).toBe('Chat - App')
  })

  it('超过 99 写成 99+；负数与小数收成非负整数', () => {
    title.setDocumentTitle('App')

    title.setDocumentTitleBadge(120)
    expect(document.title).toBe('(99+) App')
    title.setDocumentTitleBadge(99)
    expect(document.title).toBe('(99) App')
    title.setDocumentTitleBadge(2.9)
    expect(document.title).toBe('(2) App')
    title.setDocumentTitleBadge(-4)
    expect(document.title).toBe('App')
  })

  it('正文还没给过：页面现有的标题当正文', () => {
    title.setDocumentTitleBadge(5)
    expect(document.title).toBe('(5) Initial')

    title.setDocumentTitleBadge(0)
    expect(document.title).toBe('Initial')
  })
})
