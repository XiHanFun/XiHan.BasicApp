import type { Translate } from '../../labels'
import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { BotProvider, PageQueryMapping, RequestOf, ResponseOf } from '~/contracts'
import { botConfigs as botConfigsApi, toPageRequest, toSchemaPageResult } from '~/contracts'
import { assertBatch, MESSAGING_CODE_PATTERN } from '../../labels'

export type BotConfigRow = ResponseOf<'bot-configs.list'>['items'][number]

/** 列表的后端筛选：关键字匹配编码与名称；服务商多选（合成 in），是否启用按 true / false 精确匹配（缺省按服务商、默认的在前） */
export const BOT_CONFIG_QUERY: PageQueryMapping = {
  multiples: ['provider'],
  filters: { isEnabled: 'eq' },
}

/** 各服务商的官方 Webhook 主机（与后端 `BotConfigService` 一致：租户填的地址不许指向别处） */
export const BOT_WEBHOOK_HOSTS: Readonly<Record<BotProvider, readonly string[]>> = {
  DingTalk: ['oapi.dingtalk.com'],
  Lark: ['open.feishu.cn', 'open.larksuite.com'],
  WeCom: ['qyapi.weixin.qq.com'],
}

/**
 * Webhook 地址是否这个服务商的官方地址：https、缺省端口、主机在官方清单里（不分大小写）。
 * 后端不合规即 NOTIFICATION_VALIDATION_011；这里先拦一道免得白提交。
 */
export function isOfficialWebhook(provider: BotProvider, value: string): boolean {
  let url: URL
  try {
    url = new URL(value.trim())
  }
  catch {
    return false
  }
  return url.protocol === 'https:' && url.port === '' && BOT_WEBHOOK_HOSTS[provider].includes(url.hostname.toLowerCase())
}

/** 签名密钥与安全关键词只有钉钉、飞书有；企业微信的 key 在 Webhook 地址里，没有签名（4.x 同） */
export function supportsSigning(provider: BotProvider): boolean {
  return provider !== 'WeCom'
}

/** 编辑弹窗的表单 */
export interface BotConfigForm {
  id: null | string
  code: string
  name: string
  provider: BotProvider
  webhookUrl: string
  /** 新的签名密钥；编辑时留空即保留原值 */
  secret: string
  keyword: string
  isEnabled: boolean
  order: null | number
  remark: string
}

export function emptyBotConfigForm(): BotConfigForm {
  return { id: null, code: '', name: '', provider: 'DingTalk', webhookUrl: '', secret: '', keyword: '', isEnabled: true, order: 100, remark: '' }
}

/** 已保存的配置回填表单（签名密钥不回显） */
export function toBotConfigForm(row: BotConfigRow): BotConfigForm {
  return {
    id: row.id,
    code: row.code,
    name: row.name,
    provider: row.provider,
    webhookUrl: row.webhookUrl,
    secret: '',
    keyword: row.keyword ?? '',
    isEnabled: row.isEnabled,
    order: row.order,
    remark: row.remark ?? '',
  }
}

/** 表单的第一处问题（4.x 的提交前校验，加上官方地址核对）；都好为空 */
export type BotConfigProblem = 'code' | 'name' | 'webhook_url' | 'webhook_url_invalid'

export function findBotConfigProblem(form: BotConfigForm): BotConfigProblem | null {
  if (!form.id && !MESSAGING_CODE_PATTERN.test(form.code.trim()))
    return 'code'
  if (!form.name.trim())
    return 'name'
  if (!form.webhookUrl.trim())
    return 'webhook_url'
  if (!isOfficialWebhook(form.provider, form.webhookUrl))
    return 'webhook_url_invalid'
  return null
}

export function botConfigProblemMessage(t: Translate, problem: BotConfigProblem): string {
  switch (problem) {
    case 'code': return t('notification.bot_configs.message.input_config_code')
    case 'name': return t('notification.bot_configs.message.input_config_name')
    case 'webhook_url': return t('notification.bot_configs.message.input_webhook_url')
    case 'webhook_url_invalid': return t('notification.bot_configs.message.webhook_url_invalid')
  }
}

function blankToNull(value: string): null | string {
  return value.trim() || null
}

/** 新建与修改共用的内容；企业微信不带签名密钥与关键词（4.x 同），空白写 null */
function draft(form: BotConfigForm) {
  const signing = supportsSigning(form.provider)
  return {
    name: form.name.trim(),
    provider: form.provider,
    webhookUrl: form.webhookUrl.trim(),
    secret: signing ? blankToNull(form.secret) : null,
    keyword: signing ? blankToNull(form.keyword) : null,
    isEnabled: form.isEnabled,
    order: form.order ?? 0,
    remark: blankToNull(form.remark),
  }
}

/** 新建：本侧这个服务商的第一个机器人由后端自动设为默认（并强制启用） */
export function toCreateBody(form: BotConfigForm): RequestOf<'bot-configs.create'> {
  return { code: form.code.trim(), ...draft(form) }
}

/** 编辑：编码不改；签名密钥留空即保留原值 */
export function toUpdateBody(form: BotConfigForm & { id: string }): RequestOf<'bot-configs.update'> {
  return { id: form.id, ...draft(form) }
}

/**
 * SchemaPage 的资源：分页、删除与启停。6.0 没有单独的启停操作，启停是取最新一份再整体修改（签名密钥不带即保留）；
 * 默认机器人不能删除、不能停用、不能换服务商（NOTIFICATION_CONFLICT_005）。
 */
export function botConfigResource(t: Translate): SchemaResource<BotConfigRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(await botConfigsApi.list(toPageRequest(params, BOT_CONFIG_QUERY)), params),
    remove: async id => assertBatch(t, await botConfigsApi.delete({ ids: [String(id)] })),
    updateStatus: async (id, enabled) => {
      const current = await botConfigsApi.get({ id: String(id) })
      return botConfigsApi.update(toUpdateBody({ ...toBotConfigForm(current), id: current.id, isEnabled: enabled }))
    },
  }
}
