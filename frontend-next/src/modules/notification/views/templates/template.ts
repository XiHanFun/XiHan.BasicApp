import type { Translate } from '../../labels'
import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { PageQueryMapping, RequestOf, ResponseOf, TemplateChannel } from '~/contracts'
import { messageTemplates as templatesApi, toPageRequest, toSchemaPageResult } from '~/contracts'
import { assertBatch, MESSAGING_CODE_PATTERN } from '../../labels'

export type TemplateRow = ResponseOf<'message-templates.list'>['items'][number]

/** 列表的后端筛选：关键字匹配编码与名称；渠道多选（合成 in），是否启用按 true / false 精确匹配 */
export const TEMPLATE_QUERY: PageQueryMapping = {
  multiples: ['channel'],
  filters: { isEnabled: 'eq' },
}

/** HTML 正文只对邮件有意义：其余渠道的正文按纯文本渲染 */
export function isEmailChannel(channel: TemplateChannel): boolean {
  return channel === 'Email'
}

/** 主题是邮件主题与站内通知的标题（4.x 同）：短信与机器人不带主题 */
export function hasSubject(channel: TemplateChannel): boolean {
  return channel === 'Email' || channel === 'InApp'
}

/** 这行模板是否属于调用方这一侧：只有本侧的能改、启停与删除（租户看得到平台的，4.x 同） */
export function isOwnTemplate(row: Pick<TemplateRow, 'isPlatform'>, platformCaller: boolean): boolean {
  return row.isPlatform === platformCaller
}

/** 租户可以覆盖平台的模板：建一份同编码、同渠道的，发送时先用租户的 */
export function canOverrideTemplate(row: Pick<TemplateRow, 'isPlatform'>, platformCaller: boolean): boolean {
  return !platformCaller && row.isPlatform
}

/** 编辑弹窗的表单（编码与渠道建好不改） */
export interface TemplateForm {
  id: null | string
  code: string
  channel: TemplateChannel
  name: string
  subject: string
  content: string
  isHtml: boolean
  description: string
  isEnabled: boolean
  order: null | number
  remark: string
}

export function emptyTemplateForm(): TemplateForm {
  return { id: null, code: '', channel: 'Email', name: '', subject: '', content: '', isHtml: false, description: '', isEnabled: true, order: 100, remark: '' }
}

export function toTemplateForm(row: TemplateRow): TemplateForm {
  return {
    id: row.id,
    code: row.code,
    channel: row.channel,
    name: row.name,
    subject: row.subject ?? '',
    content: row.content,
    isHtml: row.isHtml,
    description: row.description ?? '',
    isEnabled: row.isEnabled,
    order: row.order,
    remark: row.remark ?? '',
  }
}

/** 覆盖平台模板的表单：照平台那份预填，存成本侧的新模板（编码与渠道不改） */
export function toOverrideForm(row: TemplateRow): TemplateForm {
  return { ...toTemplateForm(row), id: null, isEnabled: true }
}

/** 表单的第一处问题（4.x 的提交前校验，编码另按后端文法核对）；都好为空 */
export type TemplateProblem = 'code' | 'content' | 'name'

export function findTemplateProblem(form: TemplateForm): TemplateProblem | null {
  if (!form.id && !MESSAGING_CODE_PATTERN.test(form.code.trim()))
    return 'code'
  if (!form.name.trim())
    return 'name'
  if (!form.content.trim())
    return 'content'
  return null
}

export function templateProblemMessage(t: Translate, problem: TemplateProblem): string {
  switch (problem) {
    case 'code': return t('notification.templates.msg_code_invalid')
    case 'name': return t('notification.templates.msg_name_required')
    case 'content': return t('notification.templates.msg_content_required')
  }
}

function blankToNull(value: string): null | string {
  return value.trim() || null
}

/** 新建与修改共用的内容；正文原样（Scriban 的空白有意义），短信与机器人不带主题，只有邮件按 HTML 渲染 */
function draft(form: TemplateForm) {
  return {
    name: form.name.trim(),
    content: form.content,
    subject: hasSubject(form.channel) ? blankToNull(form.subject) : null,
    isHtml: isEmailChannel(form.channel) && form.isHtml,
    description: blankToNull(form.description),
    isEnabled: form.isEnabled,
    order: form.order ?? 0,
    remark: blankToNull(form.remark),
  }
}

export function toCreateBody(form: TemplateForm): RequestOf<'message-templates.create'> {
  return { code: form.code.trim(), channel: form.channel, ...draft(form) }
}

export function toUpdateBody(form: TemplateForm & { id: string }): RequestOf<'message-templates.update'> {
  return { id: form.id, ...draft(form) }
}

/**
 * SchemaPage 的资源：分页、删除与启停。6.0 没有单独的启停操作，启停是取最新一份再整体修改。
 */
export function templateResource(t: Translate): SchemaResource<TemplateRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(await templatesApi.list(toPageRequest(params, TEMPLATE_QUERY)), params),
    remove: async id => assertBatch(t, await templatesApi.delete({ ids: [String(id)] })),
    updateStatus: async (id, enabled) => {
      const current = await templatesApi.get({ id: String(id) })
      return templatesApi.update(toUpdateBody({ ...toTemplateForm(current), id: current.id, isEnabled: enabled }))
    },
  }
}
