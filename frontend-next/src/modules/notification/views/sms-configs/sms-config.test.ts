import type { SmsConfigForm, SmsConfigRow } from './sms-config'
import { describe, expect, it } from 'vitest'
import { toPageRequest } from '~/contracts'
import { configureRequest } from '~/request'
import { QueryOperator } from '~/types/contracts'
import { emptySmsConfigForm, findSmsConfigProblem, isValidTemplateMap, SMS_CONFIG_QUERY, smsConfigProblemMessage, smsConfigResource, toCreateBody, toSmsConfigForm, toUpdateBody } from './sms-config'

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

function form(patch: Partial<SmsConfigForm> = {}): SmsConfigForm {
  return { ...emptySmsConfigForm(), code: 'sms-main', name: '阿里云短信', accessKeyId: 'AK', accessKeySecret: 'SK', signName: '曦寒', ...patch }
}

const ROW: SmsConfigRow = {
  id: 's1',
  code: 'sms-tencent',
  name: '腾讯云短信',
  provider: 'Tencent',
  accessKeyId: 'SID',
  hasAccessKeySecret: true,
  sdkAppId: '1400000000',
  signName: '曦寒',
  region: null,
  templateMap: '{"auth-sms-login-code":"123456"}',
  isDefault: false,
  isEnabled: true,
  order: 0,
  remark: null,
  createdAt: '2026-10-29T00:00:00Z',
  modifiedAt: null,
}

describe('短信配置的列表筛选', () => {
  it('服务商多选合成 in，是否启用按 true / false 精确匹配', () => {
    const request = toPageRequest({
      page: 1,
      pageSize: 20,
      filters: { provider: ['Aliyun', 'Tencent'], isEnabled: 'true' },
      conditionFilters: [{ field: 'provider', operator: QueryOperator.In, values: ['Aliyun', 'Tencent'] }],
      sorts: [{ field: 'order', order: 'asc' }],
    }, SMS_CONFIG_QUERY)
    expect(request.filters).toEqual([
      { field: 'provider', operator: 'in', value: 'Aliyun,Tencent' },
      { field: 'isEnabled', operator: 'eq', value: 'true' },
    ])
  })
})

describe('短信配置的模板映射', () => {
  it('空，或值都是字符串的 JSON 对象（与后端 Dictionary<string, string> 一致）', () => {
    expect(isValidTemplateMap('')).toBe(true)
    expect(isValidTemplateMap('  ')).toBe(true)
    expect(isValidTemplateMap('{}')).toBe(true)
    expect(isValidTemplateMap('{ "auth-sms-login-code": "SMS_123456789" }')).toBe(true)
    expect(isValidTemplateMap('[]')).toBe(false)
    expect(isValidTemplateMap('null')).toBe(false)
    expect(isValidTemplateMap('"SMS_1"')).toBe(false)
    // 4.x 的嵌套写法在 6.0 不再合法
    expect(isValidTemplateMap('{"auth-sms-login-code":{"templateCode":"SMS_1","paramOrder":["code"]}}')).toBe(false)
    expect(isValidTemplateMap('{"a": 1}')).toBe(false)
    expect(isValidTemplateMap('{a:1}')).toBe(false)
  })
})

describe('短信配置的表单', () => {
  it('提交前找出第一处问题：新建或原先没有密钥时要填密钥，腾讯云要应用 ID，模板映射要合规', () => {
    expect(findSmsConfigProblem(form(), false)).toBeNull()
    expect(findSmsConfigProblem(form({ code: 'sms main' }), false)).toBe('code')
    expect(findSmsConfigProblem(form({ accessKeyId: '' }), false)).toBe('access_key_id')
    expect(findSmsConfigProblem(form({ accessKeySecret: ' ' }), false)).toBe('access_key_secret')
    expect(findSmsConfigProblem(form({ id: 's1', accessKeySecret: '' }), true)).toBeNull()
    expect(findSmsConfigProblem(form({ id: 's1', accessKeySecret: '' }), false)).toBe('access_key_secret')
    expect(findSmsConfigProblem(form({ signName: '' }), false)).toBe('sign_name')
    expect(findSmsConfigProblem(form({ provider: 'Tencent' }), false)).toBe('sdk_app_id')
    expect(findSmsConfigProblem(form({ provider: 'Tencent', sdkAppId: '1400000000' }), false)).toBeNull()
    expect(findSmsConfigProblem(form({ templateMap: '[1]' }), false)).toBe('template_map')
    expect(smsConfigProblemMessage(t, 'template_map')).toBe('notification.sms_configs.message.template_map_invalid')
  })

  it('阿里云不带应用 ID，空白写 null；编辑不带编码，密钥留空即保留', () => {
    expect(toCreateBody(form({ sdkAppId: '1400', region: ' ', templateMap: ' {"a":"b"} ' }))).toEqual({
      code: 'sms-main',
      name: '阿里云短信',
      provider: 'Aliyun',
      accessKeyId: 'AK',
      accessKeySecret: 'SK',
      sdkAppId: null,
      signName: '曦寒',
      region: null,
      templateMap: '{"a":"b"}',
      isEnabled: true,
      order: 100,
      remark: null,
    })
    const filled = toSmsConfigForm(ROW)
    expect(filled).toMatchObject({ id: 's1', provider: 'Tencent', accessKeySecret: '', region: '', templateMap: '{"auth-sms-login-code":"123456"}' })
    const body = toUpdateBody({ ...filled, id: 's1' })
    expect(body).toMatchObject({ id: 's1', accessKeySecret: null, sdkAppId: '1400000000' })
    expect(body).not.toHaveProperty('code')
  })

  it('启停取最新一份再整体修改（不带密钥）；删除逐项拒绝时翻成文案', async () => {
    const calls = stub(ROW, ROW, { succeeded: [], failed: [{ id: 's1', code: 'NOTIFICATION_NOTFOUND_005' }] })
    const resource = smsConfigResource(t)
    await resource.updateStatus!('s1', false)
    await expect(resource.remove!('s1')).rejects.toThrow('notification.messaging.failures.config_not_found')
    expect(calls[0]).toMatchObject({ method: 'GET', url: '/api/sms-configs/s1' })
    expect(calls[1]).toMatchObject({ method: 'PUT', url: '/api/sms-configs/s1', body: { isEnabled: false, accessKeySecret: null, provider: 'Tencent' } })
    expect(calls[2]).toMatchObject({ method: 'DELETE', url: '/api/sms-configs?ids=s1' })
  })
})
