/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * 注册页的法律文书：每种界面语言一份 Markdown，放在 `legal/<语言>/<文书>.md`
 * 正文按语言懒加载，点开哪份才取哪份；部署方改主体、联系方式或条款时直接改对应语言的 .md
 * 缺了哪份由单测报出来，运行时取不到就如实报错，不拿别的语言顶替
 */

export type LegalDocumentKind = 'privacy-policy' | 'terms-of-service'

export const LEGAL_DOCUMENT_KINDS: readonly LegalDocumentKind[] = ['privacy-policy', 'terms-of-service']

const loaders = import.meta.glob<string>('./*/*.md', { query: '?raw', import: 'default' })

function documentPath(kind: LegalDocumentKind, locale: string) {
  return `./${locale}/${kind}.md`
}

/** 这种语言是否备好了这份文书 */
export function hasLegalDocument(kind: LegalDocumentKind, locale: string) {
  return documentPath(kind, locale) in loaders
}

/** 取某种语言的文书正文（Markdown 原文） */
export async function loadLegalDocument(kind: LegalDocumentKind, locale: string): Promise<string> {
  const loader = loaders[documentPath(kind, locale)]
  if (!loader) {
    throw new Error(`Legal document "${kind}" is missing for locale "${locale}".`)
  }
  return loader()
}
