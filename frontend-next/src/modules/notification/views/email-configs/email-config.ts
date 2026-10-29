import type { Translate } from '../../labels'
import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { PageQueryMapping, RequestOf, ResponseOf } from '~/contracts'
import { emailConfigs as emailConfigsApi, toPageRequest, toSchemaPageResult } from '~/contracts'
import { assertBatch, MESSAGING_CODE_PATTERN } from '../../labels'

export type EmailConfigRow = ResponseOf<'email-configs.list'>['items'][number]

/** 列表的后端筛选：关键字匹配编码、名称与 SMTP 主机；是否启用按 true / false 精确匹配（缺省配置在前） */
export const EMAIL_CONFIG_QUERY: PageQueryMapping = {
  filters: { isEnabled: 'eq' },
}

/** 发件地址：恰好一个 @，前有本地部分、后是带点的域名，不含空白与尖括号（后端另以 MimeKit 核对，不认显示名） */
export function isEmailAddress(value: string): boolean {
  const at = value.indexOf('@')
  if (at <= 0 || at !== value.lastIndexOf('@') || /[\s<>]/.test(value))
    return false
  const domain = value.slice(at + 1)
  return domain.includes('.') && !domain.startsWith('.') && !domain.endsWith('.')
}

/** 编辑弹窗的表单 */
export interface EmailConfigForm {
  id: null | string
  code: string
  name: string
  smtpHost: string
  smtpPort: null | number
  useSsl: boolean
  acceptInvalidCertificate: boolean
  fromEmail: string
  fromName: string
  userName: string
  /** 新的认证密码；编辑时留空即保留原值 */
  password: string
  isEnabled: boolean
  order: null | number
  remark: string
}

export function emptyEmailConfigForm(): EmailConfigForm {
  return { id: null, code: '', name: '', smtpHost: '', smtpPort: 587, useSsl: true, acceptInvalidCertificate: false, fromEmail: '', fromName: '', userName: '', password: '', isEnabled: true, order: 100, remark: '' }
}

/** 已保存的配置回填表单（密码不回显） */
export function toEmailConfigForm(row: EmailConfigRow): EmailConfigForm {
  return {
    id: row.id,
    code: row.code,
    name: row.name,
    smtpHost: row.smtpHost,
    smtpPort: row.smtpPort,
    useSsl: row.useSsl,
    acceptInvalidCertificate: row.acceptInvalidCertificate,
    fromEmail: row.fromEmail,
    fromName: row.fromName ?? '',
    userName: row.userName ?? '',
    password: '',
    isEnabled: row.isEnabled,
    order: row.order,
    remark: row.remark ?? '',
  }
}

/** 表单的第一处问题（4.x 的提交前校验；6.0 的发件人名称可空，空时用发件地址）；都好为空 */
export type EmailConfigProblem = 'code' | 'from_email' | 'name' | 'smtp_host' | 'smtp_port'

export function findEmailConfigProblem(form: EmailConfigForm): EmailConfigProblem | null {
  if (!form.id && !MESSAGING_CODE_PATTERN.test(form.code.trim()))
    return 'code'
  if (!form.name.trim())
    return 'name'
  if (!form.smtpHost.trim())
    return 'smtp_host'
  if (form.smtpPort === null || !Number.isInteger(form.smtpPort) || form.smtpPort < 1 || form.smtpPort > 65535)
    return 'smtp_port'
  if (!isEmailAddress(form.fromEmail.trim()))
    return 'from_email'
  return null
}

export function emailConfigProblemMessage(t: Translate, problem: EmailConfigProblem): string {
  switch (problem) {
    case 'code': return t('notification.email_configs.message.input_config_code')
    case 'name': return t('notification.email_configs.message.input_config_name')
    case 'smtp_host': return t('notification.email_configs.message.input_smtp_host')
    case 'smtp_port': return t('notification.email_configs.message.input_smtp_port')
    case 'from_email': return t('notification.email_configs.message.input_from_email')
  }
}

function blankToNull(value: string): null | string {
  return value.trim() || null
}

/** 新建与修改共用的内容；空白写 null（密码为空即不认证 / 保留原值，4.x 同） */
function draft(form: EmailConfigForm) {
  return {
    name: form.name.trim(),
    smtpHost: form.smtpHost.trim(),
    smtpPort: form.smtpPort ?? 587,
    useSsl: form.useSsl,
    acceptInvalidCertificate: form.acceptInvalidCertificate,
    fromEmail: form.fromEmail.trim(),
    fromName: blankToNull(form.fromName),
    userName: blankToNull(form.userName),
    password: blankToNull(form.password),
    isEnabled: form.isEnabled,
    order: form.order ?? 0,
    remark: blankToNull(form.remark),
  }
}

/** 新建：本侧第一份配置由后端自动设为缺省（并强制启用） */
export function toCreateBody(form: EmailConfigForm): RequestOf<'email-configs.create'> {
  return { code: form.code.trim(), ...draft(form) }
}

/** 编辑：编码不改；密码留空即保留原值 */
export function toUpdateBody(form: EmailConfigForm & { id: string }): RequestOf<'email-configs.update'> {
  return { id: form.id, ...draft(form) }
}

/**
 * SchemaPage 的资源：分页、删除与启停。6.0 没有单独的启停操作，启停是取最新一份再整体修改（密码不带即保留）；
 * 缺省配置不能删除、不能停用（NOTIFICATION_CONFLICT_005）。
 */
export function emailConfigResource(t: Translate): SchemaResource<EmailConfigRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(await emailConfigsApi.list(toPageRequest(params, EMAIL_CONFIG_QUERY)), params),
    remove: async id => assertBatch(t, await emailConfigsApi.delete({ ids: [String(id)] })),
    updateStatus: async (id, enabled) => {
      const current = await emailConfigsApi.get({ id: String(id) })
      return emailConfigsApi.update(toUpdateBody({ ...toEmailConfigForm(current), id: current.id, isEnabled: enabled }))
    },
  }
}
