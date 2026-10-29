import type { EmailConfigForm, EmailConfigRow } from './email-config'
import { describe, expect, it } from 'vitest'
import { toPageRequest } from '~/contracts'
import { configureRequest } from '~/request'
import { EMAIL_CONFIG_QUERY, emailConfigProblemMessage, emailConfigResource, emptyEmailConfigForm, findEmailConfigProblem, isEmailAddress, toCreateBody, toEmailConfigForm, toUpdateBody } from './email-config'

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

function form(patch: Partial<EmailConfigForm> = {}): EmailConfigForm {
  return { ...emptyEmailConfigForm(), code: 'smtp-main', name: '主发件箱', smtpHost: 'smtp.example.com', fromEmail: 'noreply@example.com', ...patch }
}

const ROW: EmailConfigRow = {
  id: 'c1',
  code: 'smtp-main',
  name: '主发件箱',
  smtpHost: 'smtp.example.com',
  smtpPort: 465,
  useSsl: true,
  acceptInvalidCertificate: false,
  fromEmail: 'noreply@example.com',
  fromName: null,
  userName: 'noreply@example.com',
  hasPassword: true,
  isDefault: true,
  isEnabled: true,
  order: 0,
  remark: null,
  createdAt: '2026-10-29T00:00:00Z',
  modifiedAt: null,
}

describe('邮件配置的列表筛选', () => {
  it('是否启用按 true / false 精确匹配，关键字单独下发', () => {
    expect(toPageRequest({ page: 1, pageSize: 20, filters: { keyword: ' smtp ', isEnabled: 'false' } }, EMAIL_CONFIG_QUERY)).toEqual({
      page: { index: 1, size: 20 },
      keyword: 'smtp',
      filters: [{ field: 'isEnabled', operator: 'eq', value: 'false' }],
      sorts: [],
    })
  })
})

describe('邮件配置的表单', () => {
  it('提交前找出第一处问题：编码文法、名称、主机、端口与发件地址；发件人名称与密码可空', () => {
    expect(findEmailConfigProblem(form())).toBeNull()
    expect(findEmailConfigProblem(form({ code: 'SMTP main' }))).toBe('code')
    expect(findEmailConfigProblem(form({ name: '' }))).toBe('name')
    expect(findEmailConfigProblem(form({ smtpHost: ' ' }))).toBe('smtp_host')
    expect(findEmailConfigProblem(form({ smtpPort: null }))).toBe('smtp_port')
    expect(findEmailConfigProblem(form({ smtpPort: 70000 }))).toBe('smtp_port')
    expect(findEmailConfigProblem(form({ fromEmail: 'nope' }))).toBe('from_email')
    expect(findEmailConfigProblem(form({ fromEmail: 'Brand <noreply@example.com>' }))).toBe('from_email')
    expect(['noreply@example.com', 'a.b+c@mail.example.co'].every(isEmailAddress)).toBe(true)
    expect(['@example.com', 'a@b', 'a@@b.com', 'a@b.', 'a b@c.com'].some(isEmailAddress)).toBe(false)
    expect(findEmailConfigProblem(form({ id: 'c1', code: '' }))).toBeNull()
    expect(emailConfigProblemMessage(t, 'smtp_port')).toBe('notification.email_configs.message.input_smtp_port')
  })

  it('新建带编码，空白写 null；编辑不带编码，密码留空即保留（不回显）', () => {
    expect(toCreateBody(form({ code: ' smtp-main ', fromName: ' ', userName: ' u ', password: 'secret', remark: '' }))).toEqual({
      code: 'smtp-main',
      name: '主发件箱',
      smtpHost: 'smtp.example.com',
      smtpPort: 587,
      useSsl: true,
      acceptInvalidCertificate: false,
      fromEmail: 'noreply@example.com',
      fromName: null,
      userName: 'u',
      password: 'secret',
      isEnabled: true,
      order: 100,
      remark: null,
    })
    const filled = toEmailConfigForm(ROW)
    expect(filled).toMatchObject({ id: 'c1', password: '', fromName: '', smtpPort: 465 })
    const body = toUpdateBody({ ...filled, id: 'c1' })
    expect(body).toMatchObject({ id: 'c1', password: null, isEnabled: true, smtpPort: 465 })
    expect(body).not.toHaveProperty('code')
  })

  it('启停取最新一份再整体修改（不带密码）；缺省配置删除被拒时翻成文案', async () => {
    const calls = stub({ ...ROW, isDefault: false }, ROW, { succeeded: [], failed: [{ id: 'c1', code: 'NOTIFICATION_CONFLICT_005' }] })
    const resource = emailConfigResource(t)
    await resource.updateStatus!('c1', false)
    await expect(resource.remove!('c1')).rejects.toThrow('notification.messaging.failures.default_locked')
    expect(calls[0]).toMatchObject({ method: 'GET', url: '/api/email-configs/c1' })
    expect(calls[1]).toMatchObject({ method: 'PUT', url: '/api/email-configs/c1', body: { isEnabled: false, password: null, smtpHost: 'smtp.example.com' } })
    expect(calls[2]).toMatchObject({ method: 'DELETE', url: '/api/email-configs?ids=c1' })
  })
})
