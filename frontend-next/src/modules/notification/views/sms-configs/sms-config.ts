import type { Translate } from '../../labels'
import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { PageQueryMapping, RequestOf, ResponseOf, SmsProvider } from '~/contracts'
import { smsConfigs as smsConfigsApi, toPageRequest, toSchemaPageResult } from '~/contracts'
import { assertBatch, MESSAGING_CODE_PATTERN } from '../../labels'

export type SmsConfigRow = ResponseOf<'sms-configs.list'>['items'][number]

/** 列表的后端筛选：关键字匹配编码与名称；服务商多选（合成 in），是否启用按 true / false 精确匹配（缺省配置在前） */
export const SMS_CONFIG_QUERY: PageQueryMapping = {
  multiples: ['provider'],
  filters: { isEnabled: 'eq' },
}

/** 应用 ID（SmsSdkAppId）只有腾讯云用 */
export function needsSdkAppId(provider: SmsProvider): boolean {
  return provider === 'Tencent'
}

/** 编辑弹窗的表单 */
export interface SmsConfigForm {
  id: null | string
  code: string
  name: string
  provider: SmsProvider
  accessKeyId: string
  /** 新的访问密钥；编辑时留空即保留原值 */
  accessKeySecret: string
  sdkAppId: string
  signName: string
  region: string
  templateMap: string
  isEnabled: boolean
  order: null | number
  remark: string
}

export function emptySmsConfigForm(): SmsConfigForm {
  return { id: null, code: '', name: '', provider: 'Aliyun', accessKeyId: '', accessKeySecret: '', sdkAppId: '', signName: '', region: '', templateMap: '', isEnabled: true, order: 100, remark: '' }
}

/** 已保存的配置回填表单（访问密钥不回显） */
export function toSmsConfigForm(row: SmsConfigRow): SmsConfigForm {
  return {
    id: row.id,
    code: row.code,
    name: row.name,
    provider: row.provider,
    accessKeyId: row.accessKeyId,
    accessKeySecret: '',
    sdkAppId: row.sdkAppId ?? '',
    signName: row.signName,
    region: row.region ?? '',
    templateMap: row.templateMap ?? '',
    isEnabled: row.isEnabled,
    order: row.order,
    remark: row.remark ?? '',
  }
}

/**
 * 模板映射：空，或 JSON 对象且每个值都是字符串（我方模板编码 → 服务商模板 ID）。
 * 后端按 `Dictionary<string, string>` 解析，不合规即 NOTIFICATION_VALIDATION_009；这里先拦一道免得白提交。
 */
export function isValidTemplateMap(value: string): boolean {
  const text = value.trim()
  if (!text)
    return true
  try {
    const parsed: unknown = JSON.parse(text)
    return typeof parsed === 'object' && parsed !== null && !Array.isArray(parsed)
      && Object.values(parsed).every(item => typeof item === 'string')
  }
  catch {
    return false
  }
}

/** 表单的第一处问题（4.x 的提交前校验；地域在 6.0 可空，缺省阿里云 cn-hangzhou、腾讯云 ap-guangzhou）；都好为空 */
export type SmsConfigProblem = 'access_key_id' | 'access_key_secret' | 'code' | 'name' | 'sdk_app_id' | 'sign_name' | 'template_map'

/**
 * @param form 表单
 * @param hasSecret 编辑的配置是否已存了访问密钥（已存的留空即保留）
 */
export function findSmsConfigProblem(form: SmsConfigForm, hasSecret: boolean): SmsConfigProblem | null {
  if (!form.id && !MESSAGING_CODE_PATTERN.test(form.code.trim()))
    return 'code'
  if (!form.name.trim())
    return 'name'
  if (!form.accessKeyId.trim())
    return 'access_key_id'
  if (!form.accessKeySecret.trim() && !(form.id && hasSecret))
    return 'access_key_secret'
  if (!form.signName.trim())
    return 'sign_name'
  if (needsSdkAppId(form.provider) && !form.sdkAppId.trim())
    return 'sdk_app_id'
  if (!isValidTemplateMap(form.templateMap))
    return 'template_map'
  return null
}

export function smsConfigProblemMessage(t: Translate, problem: SmsConfigProblem): string {
  switch (problem) {
    case 'code': return t('notification.sms_configs.message.input_config_code')
    case 'name': return t('notification.sms_configs.message.input_config_name')
    case 'access_key_id': return t('notification.sms_configs.message.input_access_key_id')
    case 'access_key_secret': return t('notification.sms_configs.message.input_access_key_secret')
    case 'sign_name': return t('notification.sms_configs.message.input_sign_name')
    case 'sdk_app_id': return t('notification.sms_configs.message.input_sdk_app_id')
    case 'template_map': return t('notification.sms_configs.message.template_map_invalid')
  }
}

function blankToNull(value: string): null | string {
  return value.trim() || null
}

/** 新建与修改共用的内容；阿里云不带应用 ID，空白写 null */
function draft(form: SmsConfigForm) {
  return {
    name: form.name.trim(),
    provider: form.provider,
    accessKeyId: form.accessKeyId.trim(),
    accessKeySecret: blankToNull(form.accessKeySecret),
    sdkAppId: needsSdkAppId(form.provider) ? blankToNull(form.sdkAppId) : null,
    signName: form.signName.trim(),
    region: blankToNull(form.region),
    templateMap: blankToNull(form.templateMap),
    isEnabled: form.isEnabled,
    order: form.order ?? 0,
    remark: blankToNull(form.remark),
  }
}

/** 新建：本侧第一份配置由后端自动设为缺省（并强制启用） */
export function toCreateBody(form: SmsConfigForm): RequestOf<'sms-configs.create'> {
  return { code: form.code.trim(), ...draft(form) }
}

/** 编辑：编码不改；访问密钥留空即保留原值 */
export function toUpdateBody(form: SmsConfigForm & { id: string }): RequestOf<'sms-configs.update'> {
  return { id: form.id, ...draft(form) }
}

/**
 * SchemaPage 的资源：分页、删除与启停。6.0 没有单独的启停操作，启停是取最新一份再整体修改（访问密钥不带即保留）；
 * 缺省配置不能删除、不能停用（NOTIFICATION_CONFLICT_005）。
 */
export function smsConfigResource(t: Translate): SchemaResource<SmsConfigRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(await smsConfigsApi.list(toPageRequest(params, SMS_CONFIG_QUERY)), params),
    remove: async id => assertBatch(t, await smsConfigsApi.delete({ ids: [String(id)] })),
    updateStatus: async (id, enabled) => {
      const current = await smsConfigsApi.get({ id: String(id) })
      return smsConfigsApi.update(toUpdateBody({ ...toSmsConfigForm(current), id: current.id, isEnabled: enabled }))
    },
  }
}
