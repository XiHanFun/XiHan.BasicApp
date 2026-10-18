/**
 * 打印模板 JSON 的读入。
 * 职责：在交给打印设计器、预览与客户端之前确认它是当前格式的打印模板（schemaVersion 1、至少一张页面模板）；
 * 不是时抛出明确的格式错误，不静默退回、不临时转换。完整校验由打印设计器的 validate / validateDocument 做。
 */
import type { PrintDocument } from '@xihan-print/core'

/** 当前支持的打印模板格式版本。 */
export const PRINT_TEMPLATE_SCHEMA_VERSION = 1

/** 模板 JSON 不是当前格式的打印模板（如此前旧格式保存的内容）。 */
export class PrintTemplateFormatError extends Error {
  /** 出错模板的编码；读入未保存的草稿时为空。 */
  readonly templateCode?: string

  constructor(message: string, templateCode?: string, options?: ErrorOptions) {
    super(message, options)
    this.name = 'PrintTemplateFormatError'
    this.templateCode = templateCode
  }
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

/**
 * 解析模板 JSON 并确认它是当前格式的打印模板。
 * @param json 后端保存的模板 JSON。
 * @param templateCode 模板编码，写进错误信息。
 * @returns 打印模板。
 * @throws PrintTemplateFormatError JSON 无效或不是当前格式。
 */
export function parsePrintTemplateJson(json: string, templateCode?: string): PrintDocument {
  const subject = templateCode ? `打印模板 ${templateCode}` : '打印模板'
  let value: unknown
  try {
    value = JSON.parse(json)
  }
  catch (error) {
    throw new PrintTemplateFormatError(`${subject}不是有效的 JSON。`, templateCode, { cause: error })
  }
  if (!isRecord(value))
    throw new PrintTemplateFormatError(`${subject}的根节点必须是对象。`, templateCode)
  if (value.schemaVersion !== PRINT_TEMPLATE_SCHEMA_VERSION || !Array.isArray(value.pages) || value.pages.length === 0)
    throw new PrintTemplateFormatError(`${subject}不是当前的打印模板格式，请在打印模板页重新设计后再使用。`, templateCode)
  return value as unknown as PrintDocument
}
