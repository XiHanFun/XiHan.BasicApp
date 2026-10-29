import type { TemplateForm, TemplateRow } from './template'
import { describe, expect, it } from 'vitest'
import { toPageRequest } from '~/contracts'
import { configureRequest } from '~/request'
import { QueryOperator } from '~/types/contracts'
import { emptyTemplateForm, findTemplateProblem, TEMPLATE_QUERY, templateProblemMessage, templateResource, toCreateBody, toTemplateForm, toUpdateBody } from './template'

const t = (key: string) => key

function json(status: number, body: object): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

function stub(...responses: object[]): { method: string, url: string, body: unknown }[] {
  const calls: { method: string, url: string, body: unknown }[] = []
  configureRequest({
    baseUrl: '',
    getToken: () => 't',
    fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
      calls.push({ method: init?.method ?? 'GET', url: String(url), body: init?.body ? JSON.parse(String(init.body)) : undefined })
      return json(200, responses[calls.length - 1] ?? {})
    }) as unknown as typeof fetch,
  })
  return calls
}

function form(patch: Partial<TemplateForm> = {}): TemplateForm {
  return { ...emptyTemplateForm(), code: 'auth-email-login-code', name: '登录验证码', subject: '您的验证码', content: '验证码 {{ code }}', ...patch }
}

const ROW: TemplateRow = {
  id: 'tp1',
  code: 'auth-sms-login-code',
  channel: 'Sms',
  name: '短信验证码',
  subject: null,
  content: '验证码 {{ code }}',
  isHtml: false,
  description: 'code=验证码',
  isEnabled: true,
  order: 10,
  remark: null,
  createdAt: '2026-10-29T00:00:00Z',
  modifiedAt: null,
}

describe('消息模板的列表筛选', () => {
  it('渠道多选合成 in，是否启用按 true / false 精确匹配，编码可排序', () => {
    const request = toPageRequest({
      page: 1,
      pageSize: 20,
      filters: { keyword: ' login ', channel: ['Email'], isEnabled: 'true' },
      conditionFilters: [{ field: 'channel', operator: QueryOperator.In, values: ['Email'] }],
      sorts: [{ field: 'code', order: 'asc' }],
    }, TEMPLATE_QUERY)
    expect(request).toEqual({
      page: { index: 1, size: 20 },
      keyword: 'login',
      filters: [
        { field: 'channel', operator: 'in', value: 'Email' },
        { field: 'isEnabled', operator: 'eq', value: 'true' },
      ],
      sorts: [{ field: 'code', descending: false }],
    })
  })
})

describe('消息模板的表单', () => {
  it('提交前找出第一处问题：新建时编码不合文法、名称或正文空白；编辑时不看编码', () => {
    expect(findTemplateProblem(form())).toBeNull()
    expect(findTemplateProblem(form({ code: '' }))).toBe('code')
    expect(findTemplateProblem(form({ code: 'Auth Code' }))).toBe('code')
    expect(findTemplateProblem(form({ name: ' ' }))).toBe('name')
    expect(findTemplateProblem(form({ content: '  ' }))).toBe('content')
    expect(findTemplateProblem(form({ id: 'tp1', code: '' }))).toBeNull()
    expect(templateProblemMessage(t, 'code')).toBe('notification.templates.msg_code_invalid')
  })

  it('邮件模板带主题与 HTML；正文原样，空白写 null，排序缺省为 0', () => {
    expect(toCreateBody(form({ code: ' auth-email-login-code ', isHtml: true, description: ' ', order: null, content: '  <p>{{ code }}</p>\n' }))).toEqual({
      code: 'auth-email-login-code',
      channel: 'Email',
      name: '登录验证码',
      content: '  <p>{{ code }}</p>\n',
      subject: '您的验证码',
      isHtml: true,
      description: null,
      isEnabled: true,
      order: 0,
      remark: null,
    })
  })

  it('短信模板不带主题、不按 HTML 渲染；修改不带编码与渠道', () => {
    const filled = toTemplateForm(ROW)
    expect(filled).toMatchObject({ id: 'tp1', channel: 'Sms', subject: '', description: 'code=验证码', remark: '' })
    const body = toUpdateBody({ ...filled, id: 'tp1', subject: '残留主题', isHtml: true })
    expect(body).toEqual({ id: 'tp1', name: '短信验证码', content: '验证码 {{ code }}', subject: null, isHtml: false, description: 'code=验证码', isEnabled: true, order: 10, remark: null })
    expect(body).not.toHaveProperty('code')
    expect(body).not.toHaveProperty('channel')
  })

  it('启停取最新一份再整体修改；删除逐项拒绝时翻成文案', async () => {
    const calls = stub(ROW, { ...ROW, isEnabled: false }, { succeeded: [], failed: [{ id: 'tp1', code: 'NOTIFICATION_NOTFOUND_003' }] })
    const resource = templateResource(t)
    await resource.updateStatus!('tp1', false)
    await expect(resource.remove!('tp1')).rejects.toThrow('notification.messaging.failures.template_not_found')
    expect(calls[0]).toMatchObject({ method: 'GET', url: '/api/message-templates/tp1' })
    expect(calls[1]).toMatchObject({ method: 'PUT', url: '/api/message-templates/tp1', body: { name: '短信验证码', content: '验证码 {{ code }}', isEnabled: false, order: 10 } })
    expect(calls[2]).toMatchObject({ method: 'DELETE', url: '/api/message-templates?ids=tp1' })
  })
})
