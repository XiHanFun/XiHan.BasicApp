/**
 * 注册页的法律文书：隐私政策与服务条款，每种语言一份 Markdown，放在 `legal/<语言>/<文书>.md`。
 *
 * 正文按语言懒加载，点开哪份才取哪份，不把七种语言的全文打进登录页的包里。
 * 部署方要改成自己的主体、联系方式或条款，直接改对应语言的 .md；新增语言时两份文书都要补齐，
 * 缺了哪一份由单测报出来，运行时取不到就如实报错，不拿别的语言顶替。
 */

export type LegalDocumentKind = 'privacy-policy' | 'terms-of-service'

export const LEGAL_DOCUMENT_KINDS: readonly LegalDocumentKind[] = ['privacy-policy', 'terms-of-service']

const loaders = import.meta.glob<string>('./*/*.md', { query: '?raw', import: 'default' })

function documentPath(kind: LegalDocumentKind, locale: string) {
  return `./${locale}/${kind}.md`
}

/** 已经备好的文书，供单测核对每种语言是否齐全 */
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
