import type { Tone } from '@xihan-ui/core'
import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { PageQueryMapping, RequestOf, ResponseOf } from '~/contracts'
import { oauthApps as oauthAppsApi, toPageRequest, toSchemaPageResult } from '~/contracts'
import { ApiError } from '~/request'

export type Translate = (key: string, values?: Record<string, unknown>) => string

export type OAuthAppRow = ResponseOf<'oauth-apps.list'>['items'][number]
export type OAuthAppType = OAuthAppRow['appType']
export type OAuthClientType = OAuthAppRow['clientType']
/** 新建与重置密钥的结果：密钥明文只此一次，公开客户端为 null */
export type OAuthAppSecret = ResponseOf<'oauth-apps.create'>

export const OAUTH_APP_TYPES: readonly OAuthAppType[] = ['Web', 'Mobile', 'Desktop', 'Service']
export const OAUTH_CLIENT_TYPES: readonly OAuthClientType[] = ['Confidential', 'Public']

/** 后端认的授权类型与范围（`OAuthAppService.SupportedGrantTypes / SupportedScopes`）；授权码与 openid 必选 */
export const GRANT_TYPES = ['authorization_code', 'refresh_token'] as const
export const SCOPES = ['openid', 'profile', 'email', 'phone'] as const
export const REQUIRED_GRANT_TYPE = 'authorization_code'
export const REQUIRED_SCOPE = 'openid'

/** 有效期（秒）的取值范围（与后端请求的 Range 一致） */
export const LIFETIME_RANGES = {
  accessTokenLifetime: { min: 60, max: 86400 },
  refreshTokenLifetime: { min: 3600, max: 31536000 },
  authorizationCodeLifetime: { min: 30, max: 600 },
} as const

/** Client ID 文法（与后端一致）：3 到 100 个字母、数字或 . _ -；新建时留空由后端生成 */
export const CLIENT_ID_PATTERN = /^[\w.-]{3,100}$/

/** 一个应用最多登记的回调地址数（与后端 `MaxRedirectUris` 一致） */
export const MAX_REDIRECT_URIS = 10

/**
 * 列表的后端筛选：关键字匹配名称、描述、Client ID、主页与备注；应用类型多选（合成 in）；
 * 客户端类型、是否启用、是否跳过授权确认与是否个人应用（用户在「我的 OAuth 应用」里建的）精确匹配。
 */
export const OAUTH_APP_QUERY: PageQueryMapping = {
  multiples: ['appType'],
  filters: { clientType: 'eq', isEnabled: 'eq', skipConsent: 'eq', personal: 'eq' },
}

// 枚举值是契约字面量，文案键静态写出以便 i18n 门禁核对

export function appTypeLabel(t: Translate, type: OAuthAppType): string {
  switch (type) {
    case 'Mobile': return t('openplatform.oauth_apps.app_types.mobile')
    case 'Desktop': return t('openplatform.oauth_apps.app_types.desktop')
    case 'Service': return t('openplatform.oauth_apps.app_types.service')
    default: return t('openplatform.oauth_apps.app_types.web')
  }
}

export function clientTypeLabel(t: Translate, type: OAuthClientType): string {
  return type === 'Public' ? t('openplatform.oauth_apps.client_types.public') : t('openplatform.oauth_apps.client_types.confidential')
}

/** 客户端类型标签的语气：公开 info、机密 neutral（个人中心同） */
export function clientTypeTone(type: OAuthClientType): Tone {
  return type === 'Public' ? 'info' : 'neutral'
}

/** 启用状态的搜索选项：后端按 `isEnabled eq true / false` 精确匹配 */
export function enabledOptions(t: Translate): { label: string, value: string }[] {
  return [
    { label: t('common.statuses.enabled'), value: 'true' },
    { label: t('common.statuses.disabled'), value: 'false' },
  ]
}

export function enabledLabel(t: Translate, enabled: boolean): string {
  return enabled ? t('common.statuses.enabled') : t('common.statuses.disabled')
}

/** 归属：用户在个人中心建的应用显示建它的用户，平台建的显示「平台」 */
export function ownerLabel(t: Translate, row: Pick<OAuthAppRow, 'ownerUserId' | 'ownerUserName'>): string {
  if (!row.ownerUserId)
    return t('openplatform.oauth_apps.owner_platform')
  return row.ownerUserName || '-'
}

/** 有效期（秒）的显示（4.x 同）：不足一分钟按秒，不足一小时按分，不足一天按时，其余按天 */
export function formatSeconds(t: Translate, seconds: number): string {
  if (seconds < 60)
    return t('openplatform.oauth_apps.time_seconds', { value: seconds })
  if (seconds < 3600)
    return t('openplatform.oauth_apps.time_minutes', { value: Math.floor(seconds / 60) })
  if (seconds < 86400)
    return t('openplatform.oauth_apps.time_hours', { value: Math.floor(seconds / 3600) })
  return t('openplatform.oauth_apps.time_days', { value: Math.floor(seconds / 86400) })
}

/** 报错文案：后端的问题详情只取正文（已按请求语言本地化），其余取消息；都没有时为空串，由调用点补上兜底文案 */
export function errorText(error: unknown): string {
  if (error instanceof ApiError)
    return error.detail
  return error instanceof Error ? error.message : String(error ?? '')
}

/** 批量删除只回失败项的错误码：认得的翻成文案，其余原样 */
export function batchFailureMessage(t: Translate, code: string): string {
  return code === 'OPENPLATFORM_NOTFOUND_001' ? t('openplatform.oauth_apps.msg_app_not_found') : code
}

/** 批量结果里有失败项即抛错：逐项原因去重后拼成一条 */
export function assertBatch(t: Translate, result: { failed: readonly { code: string }[] }): void {
  if (result.failed.length > 0)
    throw new Error([...new Set(result.failed.map(failure => batchFailureMessage(t, failure.code)))].join('; '))
}

/** 回调地址的文本（一行一个，逗号也认）→ 地址列表，去掉空项 */
export function splitRedirectUris(value: string): string[] {
  return value.split(/[\n,]/).map(uri => uri.trim()).filter(Boolean)
}

/** 勾选的授权类型：只留后端认的、按固定顺序，授权码总在内 */
export function normalizeGrantTypes(values: readonly string[]): string[] {
  return GRANT_TYPES.filter(value => value === REQUIRED_GRANT_TYPE || values.includes(value))
}

/** 勾选的范围：只留后端认的、按固定顺序，openid 总在内 */
export function normalizeScopes(values: readonly string[]): string[] {
  return SCOPES.filter(value => value === REQUIRED_SCOPE || values.includes(value))
}

/** 编辑弹窗的表单 */
export interface OAuthAppForm {
  id: null | string
  appName: string
  /** 新建时可填，留空由后端生成；编辑时只读 */
  clientId: string
  /** 新建时选，之后不能改 */
  clientType: OAuthClientType
  appType: OAuthAppType
  grantTypes: string[]
  scopes: string[]
  /** 回调地址：一行一个 */
  redirectUris: string
  homepage: string
  logo: string
  accessTokenLifetime: null | number
  refreshTokenLifetime: null | number
  authorizationCodeLifetime: null | number
  skipConsent: boolean
  isEnabled: boolean
  appDescription: string
  remark: string
}

/** 新建的缺省值：有效期同 4.x；授权类型与范围同个人应用的缺省（授权码 + 刷新令牌，openid / profile / email） */
export function emptyOAuthAppForm(): OAuthAppForm {
  return {
    id: null,
    appName: '',
    clientId: '',
    clientType: 'Confidential',
    appType: 'Web',
    grantTypes: [...GRANT_TYPES],
    scopes: ['openid', 'profile', 'email'],
    redirectUris: '',
    homepage: '',
    logo: '',
    accessTokenLifetime: 3600,
    refreshTokenLifetime: 2592000,
    authorizationCodeLifetime: 600,
    skipConsent: false,
    isEnabled: true,
    appDescription: '',
    remark: '',
  }
}

/** 已保存的应用回填表单（回调地址一行一个） */
export function toOAuthAppForm(row: OAuthAppRow): OAuthAppForm {
  return {
    id: row.id,
    appName: row.appName,
    clientId: row.clientId,
    clientType: row.clientType,
    appType: row.appType,
    grantTypes: [...row.grantTypes],
    scopes: [...row.scopes],
    redirectUris: row.redirectUris.join('\n'),
    homepage: row.homepage ?? '',
    logo: row.logo ?? '',
    accessTokenLifetime: row.accessTokenLifetime,
    refreshTokenLifetime: row.refreshTokenLifetime,
    authorizationCodeLifetime: row.authorizationCodeLifetime,
    skipConsent: row.skipConsent,
    isEnabled: row.isEnabled,
    appDescription: row.appDescription ?? '',
    remark: row.remark ?? '',
  }
}

/** 表单的第一处问题（4.x 的提交前校验，加上 6.0 的 Client ID 文法、回调地址必填与有效期范围）；都好为空 */
export type OAuthAppProblem = 'app_name' | 'client_id' | 'lifetimes' | 'redirect_uris'

function inRange(value: null | number, range: { min: number, max: number }): boolean {
  return value !== null && Number.isInteger(value) && value >= range.min && value <= range.max
}

export function findOAuthAppProblem(form: OAuthAppForm): OAuthAppProblem | null {
  if (!form.appName.trim())
    return 'app_name'
  const clientId = form.clientId.trim()
  if (!form.id && clientId && !CLIENT_ID_PATTERN.test(clientId))
    return 'client_id'
  const redirectUris = splitRedirectUris(form.redirectUris)
  if (redirectUris.length === 0 || redirectUris.length > MAX_REDIRECT_URIS)
    return 'redirect_uris'
  if (!inRange(form.accessTokenLifetime, LIFETIME_RANGES.accessTokenLifetime)
    || !inRange(form.refreshTokenLifetime, LIFETIME_RANGES.refreshTokenLifetime)
    || !inRange(form.authorizationCodeLifetime, LIFETIME_RANGES.authorizationCodeLifetime)) {
    return 'lifetimes'
  }
  return null
}

export function oauthAppProblemMessage(t: Translate, problem: OAuthAppProblem): string {
  switch (problem) {
    case 'app_name': return t('openplatform.oauth_apps.msg_input_app_name')
    case 'client_id': return t('openplatform.oauth_apps.msg_client_id_invalid')
    case 'redirect_uris': return t('openplatform.oauth_apps.msg_input_redirect_uris', { max: MAX_REDIRECT_URIS })
    case 'lifetimes': return t('openplatform.oauth_apps.msg_lifetime_invalid')
  }
}

function blankToNull(value: string): null | string {
  return value.trim() || null
}

/** 有效期已由 findOAuthAppProblem 校验过；没校验就提交是调用方的错，直接报错而不是补个缺省值 */
function lifetime(value: null | number): number {
  if (value === null)
    throw new Error('有效期未填写：提交前须先经 findOAuthAppProblem 校验')
  return value
}

/** 新建与修改共用的内容：授权类型与范围按后端认的规整，回调地址拆成列表，空白写 null */
function draft(form: OAuthAppForm) {
  return {
    appName: form.appName.trim(),
    appDescription: blankToNull(form.appDescription),
    appType: form.appType,
    grantTypes: normalizeGrantTypes(form.grantTypes),
    redirectUris: splitRedirectUris(form.redirectUris),
    scopes: normalizeScopes(form.scopes),
    accessTokenLifetime: lifetime(form.accessTokenLifetime),
    refreshTokenLifetime: lifetime(form.refreshTokenLifetime),
    authorizationCodeLifetime: lifetime(form.authorizationCodeLifetime),
    logo: blankToNull(form.logo),
    homepage: blankToNull(form.homepage),
    skipConsent: form.skipConsent,
    isEnabled: form.isEnabled,
    remark: blankToNull(form.remark),
  }
}

/** 新建：Client ID 留空写 null 由后端生成；客户端类型只在新建时定 */
export function toCreateBody(form: OAuthAppForm): RequestOf<'oauth-apps.create'> {
  return { ...draft(form), clientId: blankToNull(form.clientId), clientType: form.clientType }
}

/** 编辑：整体替换（含启停）；Client ID 与客户端类型不改 */
export function toUpdateBody(form: OAuthAppForm & { id: string }): RequestOf<'oauth-apps.update'> {
  return { id: form.id, ...draft(form) }
}

/**
 * SchemaPage 的资源：分页、删除与启停。6.0 没有单独的启停操作，启停是取最新一份再整体修改。
 */
export function oauthAppResource(t: Translate): SchemaResource<OAuthAppRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(await oauthAppsApi.list(toPageRequest(params, OAUTH_APP_QUERY)), params),
    remove: async id => assertBatch(t, await oauthAppsApi.delete({ ids: [String(id)] })),
    updateStatus: async (id, enabled) => {
      const current = await oauthAppsApi.get({ id: String(id) })
      return oauthAppsApi.update(toUpdateBody({ ...toOAuthAppForm(current), id: current.id, isEnabled: enabled }))
    },
  }
}
