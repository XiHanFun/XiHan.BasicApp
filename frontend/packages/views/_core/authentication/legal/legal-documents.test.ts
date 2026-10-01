/**
 * 注册页法律文书的齐全性与结构一致性。
 *
 * 每种界面语言都要有隐私政策与服务条款两份，缺一份注册页就打不开对应弹窗；
 * 各语言是同一份文书的译本，章节数必须与简体中文一致，漏译、并段在这里报出来。
 * 标题由弹窗给出，正文不再写一级标题，免得同一个标题出现两遍。
 */
import { describe, expect, it } from 'vitest'
import { SUPPORTED_LOCALES } from '~/constants'
import { hasLegalDocument, LEGAL_DOCUMENT_KINDS, loadLegalDocument } from '.'

function sectionCount(markdown: string) {
  return markdown.split(/\r?\n/).filter(line => line.startsWith('## ')).length
}

describe('注册页法律文书', () => {
  it.each(LEGAL_DOCUMENT_KINDS)('%s：每种界面语言都有一份', (kind) => {
    const missing = SUPPORTED_LOCALES.filter(locale => !hasLegalDocument(kind, locale))

    expect(missing).toEqual([])
  })

  it.each(LEGAL_DOCUMENT_KINDS)('%s：各语言的章节数与简体中文一致，且不写一级标题', async (kind) => {
    const source = await loadLegalDocument(kind, 'zh-CN')
    const expected = sectionCount(source)
    expect(expected).toBeGreaterThan(0)

    for (const locale of SUPPORTED_LOCALES) {
      const markdown = await loadLegalDocument(kind, locale)

      expect({ locale, sections: sectionCount(markdown) }).toEqual({ locale, sections: expected })
      expect({ locale, h1: /^# /m.test(markdown) }).toEqual({ locale, h1: false })
    }
  })

  it('没有的语言如实报错，不拿别的语言顶替', async () => {
    await expect(loadLegalDocument('privacy-policy', 'xx-XX')).rejects.toThrow('xx-XX')
  })
})
