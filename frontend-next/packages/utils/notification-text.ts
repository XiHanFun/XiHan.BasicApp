import { NotificationContentFormat } from '~/types/enums'

/**
 * 通知正文的一句话摘要：实时弹出的通知卡片、顶部横幅只出纯文本，
 * 把 Markdown / HTML 的标记去掉、空白压成一格，再按字数截断。完整正文仍在消息中心按原格式渲染。
 */
export function notificationSummary(
  content: null | string | undefined,
  format: NotificationContentFormat | null | undefined,
  maxLength = 120,
): string {
  const raw = content ?? ''
  const text = format === NotificationContentFormat.Html
    ? htmlToText(raw)
    : format === NotificationContentFormat.Markdown
      ? markdownToText(raw)
      : raw
  const flat = text.replace(/\s+/g, ' ').trim()
  if (flat.length <= maxLength) {
    return flat
  }
  return `${flat.slice(0, maxLength).trimEnd()}…`
}

/** 有 DOM 时交给解析器取文字（实体一并解码）；没有就按标签剥 */
function htmlToText(html: string): string {
  if (typeof DOMParser !== 'undefined') {
    // 块级元素之间补空格，免得相邻两段的字粘在一起
    const spaced = html.replace(/<(\/(?:p|div|li|h[1-6]|tr|blockquote)|br\s*\/?)>/gi, '$& ')
    return new DOMParser().parseFromString(spaced, 'text/html').body.textContent ?? ''
  }
  return html.replace(/<[^>]+>/g, ' ')
}

/** 去掉常见的 Markdown 标记，只留读者看得到的字 */
function markdownToText(markdown: string): string {
  return markdown
    // 代码块：围栏去掉，代码本身留着
    .replace(/^\s*(?:```|~~~).*$/gm, '')
    // 图片留替代文字、链接留链接文字
    .replace(/!\[([^\]]*)\]\([^)]*\)/g, '$1')
    .replace(/\[([^\]]+)\]\([^)]*\)/g, '$1')
    // 分隔线、标题井号、引用、列表记号、表格竖线
    .replace(/^\s*([-*_])(?:\s*\1){2,}\s*$/gm, '')
    .replace(/^\s{0,3}#{1,6}\s+/gm, '')
    .replace(/^\s*>\s?/gm, '')
    .replace(/^\s*(?:[-*+]|\d+[.)])\s+/gm, '')
    .replace(/\|/g, ' ')
    // 强调、删除线与行内代码的记号
    .replace(/(\*\*|__|~~)(.+?)\1/g, '$2')
    .replace(/(?<![\w*])[*_](\S(?:.*?\S)?)[*_](?![\w*])/g, '$1')
    .replace(/`([^`]*)`/g, '$1')
}
