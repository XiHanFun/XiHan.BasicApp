import type { PageQuery } from '~/types'
import { useBaseApi } from '../base'
import { toId, toNumber } from '../helpers'

const api = useBaseApi('OAuth')

export interface SysOAuthApp {
  basicId: string
  appName: string
  appDescription?: string
  clientId: string
  clientSecret: string
  appType: number
  grantTypes: string
  redirectUris?: string
  scopes?: string
  accessTokenLifetime: number
  refreshTokenLifetime: number
  authorizationCodeLifetime: number
  skipConsent: boolean
  tenantId?: string
  status: number
  openApiSecurityEnabled?: boolean
  openApiSignatureAlgorithm?: string
  openApiContentSignAlgorithm?: string
  openApiEncryptionAlgorithm?: string
  openApiEncryptKey?: string
  openApiPublicKey?: string
  openApiSm2PublicKey?: string
  openApiAllowResponseEncryption?: boolean
  openApiIpWhitelist?: string
  createTime?: string
  updateTime?: string
  remark?: string
}

export interface OAuthAppPageQuery extends PageQuery {
  appType?: number
  status?: number
}

function normalizeOAuthApp(raw: Record<string, any>): SysOAuthApp {
  return {
    basicId: toId(raw.basicId),
    appName: raw.appName ?? '',
    appDescription: raw.appDescription ?? '',
    clientId: raw.clientId ?? '',
    clientSecret: raw.clientSecret ?? '',
    appType: toNumber(raw.appType, 0),
    grantTypes: raw.grantTypes ?? '',
    redirectUris: raw.redirectUris ?? '',
    scopes: raw.scopes ?? '',
    accessTokenLifetime: toNumber(raw.accessTokenLifetime, 3600),
    refreshTokenLifetime: toNumber(raw.refreshTokenLifetime, 2592000),
    authorizationCodeLifetime: toNumber(raw.authorizationCodeLifetime, 300),
    skipConsent: Boolean(raw.skipConsent),
    tenantId: raw.tenantId == null ? undefined : toId(raw.tenantId),
    status: toNumber(raw.status, 1),
    createTime: raw.createTime ?? raw.creationTime ?? raw.createdTime ?? undefined,
    updateTime: raw.updateTime ?? raw.lastModificationTime ?? undefined,
    remark: raw.remark ?? undefined,
  }
}

function normalizeOpenApiSecurity(raw: Record<string, any>): Partial<SysOAuthApp> {
  return {
    openApiSecurityEnabled: raw.isEnabled === undefined ? true : Boolean(raw.isEnabled),
    openApiSignatureAlgorithm: raw.signatureAlgorithm ?? 'HMACSHA256',
    openApiContentSignAlgorithm: raw.contentSignatureAlgorithm ?? 'SHA256',
    openApiEncryptionAlgorithm: raw.encryptionAlgorithm ?? 'AES-CBC',
    openApiEncryptKey: raw.encryptKey ?? '',
    openApiPublicKey: raw.publicKey ?? '',
    openApiSm2PublicKey: raw.sm2PublicKey ?? '',
    openApiAllowResponseEncryption:
      raw.allowResponseEncryption === undefined ? true : Boolean(raw.allowResponseEncryption),
    openApiIpWhitelist: raw.ipWhitelist ?? '',
  }
}

function toCreatePayload(data: Partial<SysOAuthApp>) {
  return {
    appName: data.appName ?? '',
    appDescription: data.appDescription ?? '',
    clientId: data.clientId ?? '',
    clientSecret: data.clientSecret ?? '',
    appType: toNumber(data.appType, 0),
    grantTypes: data.grantTypes ?? '',
    redirectUris: data.redirectUris ?? '',
    scopes: data.scopes ?? '',
    accessTokenLifetime: toNumber(data.accessTokenLifetime, 3600),
    refreshTokenLifetime: toNumber(data.refreshTokenLifetime, 2592000),
    authorizationCodeLifetime: toNumber(data.authorizationCodeLifetime, 300),
    skipConsent: Boolean(data.skipConsent),
    remark: data.remark ?? '',
  }
}

function toUpdatePayload(id: string, data: Partial<SysOAuthApp>) {
  return {
    ...toCreatePayload(data),
    status: toNumber(data.status, 1),
    basicId: toId(id),
  }
}

function toOpenApiSecurityPayload(id: string, data: Partial<SysOAuthApp>) {
  return {
    basicId: toId(id),
    isEnabled: data.openApiSecurityEnabled !== false,
    signatureAlgorithm: data.openApiSignatureAlgorithm ?? 'HMACSHA256',
    contentSignatureAlgorithm: data.openApiContentSignAlgorithm ?? 'SHA256',
    encryptionAlgorithm: data.openApiEncryptionAlgorithm ?? 'AES-CBC',
    encryptKey: data.openApiEncryptKey ?? '',
    publicKey: data.openApiPublicKey ?? '',
    sm2PublicKey: data.openApiSm2PublicKey ?? '',
    allowResponseEncryption: data.openApiAllowResponseEncryption !== false,
    ipWhitelist: data.openApiIpWhitelist ?? '',
  }
}

export const oauthAppApi = {
  page: (params: Record<string, any>) =>
    api.page(params, {
      keywordFields: ['AppName', 'ClientId'],
      filterFieldMap: { appType: 'AppType', status: 'Status' },
    }),

  detail: (id: string) =>
    api.request.get<any>(`${api.baseUrl}ById`, { params: { id } }).then(normalizeOAuthApp),

  create: (data: Partial<SysOAuthApp>) =>
    api.request.post<any>(`${api.baseUrl}Create`, toCreatePayload(data)).then(normalizeOAuthApp),

  update: (id: string, data: Partial<SysOAuthApp>) =>
    api.request
      .put<any>(`${api.baseUrl}Update`, toUpdatePayload(id, data), { params: { id } })
      .then(normalizeOAuthApp),

  delete: (id: string) => api.delete(id),

  getByClientId: (clientId: string, tenantId?: number) =>
    api.request
      .get<any>(`${api.baseUrl}ByClientId/${clientId}/${tenantId ?? 0}`)
      .then((raw: any) => (raw ? normalizeOAuthApp(raw) : null)),

  getOpenApiSecurity: (id: string) =>
    api.request
      .get<any>(`${api.baseUrl}OpenApiSecurity`, { params: { appId: id } })
      .then(normalizeOpenApiSecurity),

  updateOpenApiSecurity: (id: string, data: Partial<SysOAuthApp>) =>
    api.request
      .put<any>(`${api.baseUrl}UpdateOpenApiSecurity`, toOpenApiSecurityPayload(id, data))
      .then(normalizeOpenApiSecurity),
}
