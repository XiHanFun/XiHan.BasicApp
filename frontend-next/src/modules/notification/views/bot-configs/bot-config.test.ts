import type { BotConfigForm, BotConfigRow } from './bot-config'
import { describe, expect, it } from 'vitest'
import { toPageRequest } from '~/contracts'
import { configureRequest } from '~/request'
import { QueryOperator } from '~/types/contracts'
import { botProviderLabel } from '../../labels'
import { BOT_CONFIG_QUERY, botConfigProblemMessage, botConfigResource, emptyBotConfigForm, findBotConfigProblem, isOfficialWebhook, supportsSigning, toBotConfigForm, toCreateBody, toUpdateBody } from './bot-config'

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

const DINGTALK = 'https://oapi.dingtalk.com/robot/send?access_token=abc'

function form(patch: Partial<BotConfigForm> = {}): BotConfigForm {
  return { ...emptyBotConfigForm(), code: 'bot-alert', name: '告警群', webhookUrl: DINGTALK, secret: 'SEC', keyword: '告警', ...patch }
}

const ROW: BotConfigRow = {
  id: 'b1',
  code: 'bot-lark',
  name: '飞书群',
  provider: 'Lark',
  webhookUrl: 'https://open.feishu.cn/open-apis/bot/v2/hook/xyz',
  hasSecret: true,
  keyword: '通知',
  isDefault: false,
  isEnabled: true,
  order: 0,
  remark: null,
  createdAt: '2026-10-29T00:00:00Z',
  modifiedAt: null,
}

describe('机器人配置的列表筛选', () => {
  it('服务商多选合成 in，是否启用按 true / false 精确匹配', () => {
    const request = toPageRequest({
      page: 1,
      pageSize: 20,
      filters: { provider: ['DingTalk', 'WeCom'], isEnabled: 'false' },
      conditionFilters: [{ field: 'provider', operator: QueryOperator.In, values: ['DingTalk', 'WeCom'] }],
      sorts: [{ field: 'order', order: 'asc' }],
    }, BOT_CONFIG_QUERY)
    expect(request.filters).toEqual([
      { field: 'provider', operator: 'in', value: 'DingTalk,WeCom' },
      { field: 'isEnabled', operator: 'eq', value: 'false' },
    ])
  })
})

describe('机器人配置的 Webhook 地址', () => {
  it('只认所选服务商的官方 https 地址（与后端一致：缺省端口、主机不分大小写）', () => {
    expect(isOfficialWebhook('DingTalk', DINGTALK)).toBe(true)
    expect(isOfficialWebhook('DingTalk', `  ${DINGTALK}  `)).toBe(true)
    expect(isOfficialWebhook('DingTalk', 'https://OAPI.DingTalk.com:443/robot/send')).toBe(true)
    expect(isOfficialWebhook('Lark', 'https://open.feishu.cn/open-apis/bot/v2/hook/x')).toBe(true)
    expect(isOfficialWebhook('Lark', 'https://open.larksuite.com/open-apis/bot/v2/hook/x')).toBe(true)
    expect(isOfficialWebhook('WeCom', 'https://qyapi.weixin.qq.com/cgi-bin/webhook/send?key=k')).toBe(true)
    // 别的服务商的地址、http、非缺省端口、相似域名与不是地址的都不认
    expect(isOfficialWebhook('WeCom', DINGTALK)).toBe(false)
    expect(isOfficialWebhook('DingTalk', 'http://oapi.dingtalk.com/robot/send')).toBe(false)
    expect(isOfficialWebhook('DingTalk', 'https://oapi.dingtalk.com:8443/robot/send')).toBe(false)
    expect(isOfficialWebhook('DingTalk', 'https://oapi.dingtalk.com.evil.example/robot/send')).toBe(false)
    expect(isOfficialWebhook('Lark', 'https://feishu.cn/open-apis/bot/v2/hook/x')).toBe(false)
    expect(isOfficialWebhook('DingTalk', 'oapi.dingtalk.com/robot/send')).toBe(false)
    expect(isOfficialWebhook('DingTalk', '')).toBe(false)
  })
})

describe('机器人配置的表单', () => {
  it('提交前找出第一处问题：编码文法、名称、Webhook 必填且须是官方地址；编辑不查编码', () => {
    expect(findBotConfigProblem(form())).toBeNull()
    expect(findBotConfigProblem(form({ code: 'Bot Alert' }))).toBe('code')
    expect(findBotConfigProblem(form({ id: 'b1', code: 'Bot Alert' }))).toBeNull()
    expect(findBotConfigProblem(form({ name: ' ' }))).toBe('name')
    expect(findBotConfigProblem(form({ webhookUrl: ' ' }))).toBe('webhook_url')
    expect(findBotConfigProblem(form({ provider: 'Lark' }))).toBe('webhook_url_invalid')
    expect(botConfigProblemMessage(t, 'webhook_url_invalid')).toBe('notification.bot_configs.message.webhook_url_invalid')
    expect(botConfigProblemMessage(t, 'code')).toBe('notification.bot_configs.message.input_config_code')
  })

  it('签名密钥与关键词只有钉钉、飞书带；企业微信一律写 null，空白写 null', () => {
    expect(supportsSigning('DingTalk')).toBe(true)
    expect(supportsSigning('Lark')).toBe(true)
    expect(supportsSigning('WeCom')).toBe(false)
    expect(toCreateBody(form({ webhookUrl: ` ${DINGTALK} `, remark: ' ' }))).toEqual({
      code: 'bot-alert',
      name: '告警群',
      provider: 'DingTalk',
      webhookUrl: DINGTALK,
      secret: 'SEC',
      keyword: '告警',
      isEnabled: true,
      order: 100,
      remark: null,
    })
    expect(toCreateBody(form({ provider: 'WeCom', webhookUrl: 'https://qyapi.weixin.qq.com/cgi-bin/webhook/send?key=k' }))).toMatchObject({ provider: 'WeCom', secret: null, keyword: null })
  })

  it('编辑回填不带密钥；修改不带编码，密钥留空即保留', () => {
    const filled = toBotConfigForm(ROW)
    expect(filled).toMatchObject({ id: 'b1', code: 'bot-lark', provider: 'Lark', secret: '', keyword: '通知', remark: '' })
    const body = toUpdateBody({ ...filled, id: 'b1' })
    expect(body).toMatchObject({ id: 'b1', provider: 'Lark', secret: null, keyword: '通知', remark: null })
    expect(body).not.toHaveProperty('code')
  })

  it('启停取最新一份再整体修改（不带密钥）；删除逐项拒绝时翻成文案', async () => {
    const calls = stub(ROW, ROW, { succeeded: [], failed: [{ id: 'b1', code: 'NOTIFICATION_CONFLICT_005' }] }, { succeeded: [], failed: [{ id: 'b2', code: 'NOTIFICATION_NOTFOUND_008' }] })
    const resource = botConfigResource(t)
    await resource.updateStatus!('b1', false)
    await expect(resource.remove!('b1')).rejects.toThrow('notification.messaging.failures.default_locked')
    await expect(resource.remove!('b2')).rejects.toThrow('notification.messaging.failures.config_not_found')
    expect(calls[0]).toMatchObject({ method: 'GET', url: '/api/bot-configs/b1' })
    expect(calls[1]).toMatchObject({ method: 'PUT', url: '/api/bot-configs/b1', body: { isEnabled: false, secret: null, provider: 'Lark', keyword: '通知' } })
    expect(calls[2]).toMatchObject({ method: 'DELETE', url: '/api/bot-configs?ids=b1' })
  })

  it('服务商文案按契约字面量逐个写出', () => {
    expect(botProviderLabel(t, 'DingTalk')).toBe('notification.messaging.bot_providers.ding_talk')
    expect(botProviderLabel(t, 'Lark')).toBe('notification.messaging.bot_providers.lark')
    expect(botProviderLabel(t, 'WeCom')).toBe('notification.messaging.bot_providers.we_com')
  })
})
