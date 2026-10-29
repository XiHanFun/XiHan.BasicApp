import type { OAuthAppForm, OAuthAppRow } from './oauth-app'
import { describe, expect, it } from 'vitest'
import { toPageRequest } from '~/contracts'
import { configureRequest } from '~/request'
import { QueryOperator } from '~/types/contracts'
import {
  appTypeLabel,
  assertBatch,
  CLIENT_ID_PATTERN,
  clientTypeLabel,
  emptyOAuthAppForm,
  enabledOptions,
  findOAuthAppProblem,
  formatSeconds,
  normalizeGrantTypes,
  normalizeScopes,
  OAUTH_APP_QUERY,
  oauthAppProblemMessage,
  oauthAppResource,
  ownerLabel,
  splitRedirectUris,
  toCreateBody,
  toOAuthAppForm,
  toUpdateBody,
} from './oauth-app'

/** 假翻译：键原样返回，带参数时拼上参数便于断言 */
const t = (key: string, values?: Record<string, unknown>) => (values ? `${key}:${JSON.stringify(values)}` : key)

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

function form(patch: Partial<OAuthAppForm> = {}): OAuthAppForm {
  return { ...emptyOAuthAppForm(), appName: '报表门户', redirectUris: 'https://portal.example/cb', ...patch }
}

const ROW: OAuthAppRow = {
  id: 'a1',
  appName: '报表门户',
  appDescription: null,
  clientId: 'portal',
  clientType: 'Confidential',
  appType: 'Web',
  grantTypes: ['authorization_code', 'refresh_token'],
  redirectUris: ['https://portal.example/cb', 'https://portal.example/cb2'],
  scopes: ['openid', 'profile'],
  accessTokenLifetime: 3600,
  refreshTokenLifetime: 2592000,
  authorizationCodeLifetime: 300,
  logo: null,
  homepage: 'https://portal.example',
  skipConsent: true,
  isEnabled: true,
  ownerUserId: null,
  ownerUserName: null,
  remark: null,
  createdAt: '2026-10-30T00:00:00Z',
  modifiedAt: null,
}

describe('应用管理：列表筛选', () => {
  it('应用类型多选合成 in；客户端类型、启用、授权确认与个人应用按值精确匹配', () => {
    const request = toPageRequest({
      page: 2,
      pageSize: 20,
      filters: { keyword: ' portal ', clientType: 'Public', isEnabled: 'true', skipConsent: 'false', personal: 'true' },
      conditionFilters: [{ field: 'appType', operator: QueryOperator.In, values: ['Web', 'Service'] }],
      sorts: [{ field: 'accessTokenLifetime', order: 'desc' }],
    }, OAUTH_APP_QUERY)
    expect(request.filters).toEqual([
      { field: 'appType', operator: 'in', value: 'Web,Service' },
      { field: 'clientType', operator: 'eq', value: 'Public' },
      { field: 'isEnabled', operator: 'eq', value: 'true' },
      { field: 'skipConsent', operator: 'eq', value: 'false' },
      { field: 'personal', operator: 'eq', value: 'true' },
    ])
    expect(request.keyword).toBe('portal')
    expect(request.page).toEqual({ index: 2, size: 20 })
    expect(request.sorts).toEqual([{ field: 'accessTokenLifetime', descending: true }])
  })
})

describe('应用管理：文案', () => {
  it('枚举、启用、归属与有效期（4.x 的秒 / 分 / 时 / 天）', () => {
    expect(appTypeLabel(t, 'Service')).toBe('openplatform.oauth_apps.app_types.service')
    expect(appTypeLabel(t, 'Web')).toBe('openplatform.oauth_apps.app_types.web')
    expect(clientTypeLabel(t, 'Public')).toBe('openplatform.oauth_apps.client_types.public')
    expect(enabledOptions(t).map(option => option.value)).toEqual(['true', 'false'])
    expect(ownerLabel(t, ROW)).toBe('openplatform.oauth_apps.owner_platform')
    expect(ownerLabel(t, { ownerUserId: 'u1', ownerUserName: 'alice' })).toBe('alice')
    expect(formatSeconds(t, 45)).toBe('openplatform.oauth_apps.time_seconds:{"value":45}')
    expect(formatSeconds(t, 600)).toBe('openplatform.oauth_apps.time_minutes:{"value":10}')
    expect(formatSeconds(t, 3600)).toBe('openplatform.oauth_apps.time_hours:{"value":1}')
    expect(formatSeconds(t, 2592000)).toBe('openplatform.oauth_apps.time_days:{"value":30}')
  })
})

describe('应用管理：表单', () => {
  it('客户端 ID 文法与后端一致；回调地址按行或逗号拆开；授权码与 openid 总在内，只留后端认的', () => {
    expect(CLIENT_ID_PATTERN.test('my.app_1-x')).toBe(true)
    expect(CLIENT_ID_PATTERN.test('ab')).toBe(false)
    expect(CLIENT_ID_PATTERN.test('a b c')).toBe(false)
    expect(CLIENT_ID_PATTERN.test('a'.repeat(101))).toBe(false)
    expect(splitRedirectUris(' https://a/cb \n\nhttps://b/cb, https://c/cb ,')).toEqual(['https://a/cb', 'https://b/cb', 'https://c/cb'])
    expect(normalizeGrantTypes(['refresh_token', 'password'])).toEqual(['authorization_code', 'refresh_token'])
    expect(normalizeGrantTypes([])).toEqual(['authorization_code'])
    expect(normalizeScopes(['email', 'offline_access'])).toEqual(['openid', 'email'])
  })

  it('提交前找出第一处问题：名称必填；新建的 Client ID 填了就要合规；回调地址 1 到 10 个；有效期在范围内', () => {
    expect(findOAuthAppProblem(form())).toBeNull()
    expect(findOAuthAppProblem(form({ appName: ' ' }))).toBe('app_name')
    expect(findOAuthAppProblem(form({ clientId: 'x' }))).toBe('client_id')
    expect(findOAuthAppProblem(form({ id: 'a1', clientId: 'legacy id' }))).toBeNull()
    expect(findOAuthAppProblem(form({ redirectUris: ' \n ' }))).toBe('redirect_uris')
    expect(findOAuthAppProblem(form({ redirectUris: Array.from({ length: 11 }, (_, i) => `https://a/${i}`).join('\n') }))).toBe('redirect_uris')
    expect(findOAuthAppProblem(form({ accessTokenLifetime: 59 }))).toBe('lifetimes')
    expect(findOAuthAppProblem(form({ refreshTokenLifetime: null }))).toBe('lifetimes')
    expect(findOAuthAppProblem(form({ authorizationCodeLifetime: 601 }))).toBe('lifetimes')
    expect(oauthAppProblemMessage(t, 'redirect_uris')).toBe('openplatform.oauth_apps.msg_input_redirect_uris:{"max":10}')
    expect(oauthAppProblemMessage(t, 'client_id')).toBe('openplatform.oauth_apps.msg_client_id_invalid')
  })

  it('新建：Client ID 留空写 null、带客户端类型，空白写 null；编辑回填按行拼回调地址，修改整体替换且不带 Client ID 与客户端类型', () => {
    expect(toCreateBody(form({ clientType: 'Public', grantTypes: ['refresh_token'], scopes: ['phone'], appDescription: ' ' }))).toEqual({
      appName: '报表门户',
      appDescription: null,
      appType: 'Web',
      grantTypes: ['authorization_code', 'refresh_token'],
      redirectUris: ['https://portal.example/cb'],
      scopes: ['openid', 'phone'],
      accessTokenLifetime: 3600,
      refreshTokenLifetime: 2592000,
      authorizationCodeLifetime: 600,
      logo: null,
      homepage: null,
      skipConsent: false,
      isEnabled: true,
      remark: null,
      clientId: null,
      clientType: 'Public',
    })
    const filled = toOAuthAppForm(ROW)
    expect(filled).toMatchObject({ id: 'a1', clientId: 'portal', redirectUris: 'https://portal.example/cb\nhttps://portal.example/cb2', homepage: 'https://portal.example', remark: '' })
    const body = toUpdateBody({ ...filled, id: 'a1' })
    expect(body).toMatchObject({ id: 'a1', redirectUris: ['https://portal.example/cb', 'https://portal.example/cb2'], scopes: ['openid', 'profile'], authorizationCodeLifetime: 300, skipConsent: true })
    expect(body).not.toHaveProperty('clientId')
    expect(body).not.toHaveProperty('clientType')
  })
})

describe('应用管理：资源', () => {
  it('启停取最新一份再整体修改；删除逐项拒绝时翻成文案', async () => {
    const calls = stub(ROW, ROW, { succeeded: [], failed: [{ id: 'a1', code: 'OPENPLATFORM_NOTFOUND_001' }] })
    const resource = oauthAppResource(t)
    await resource.updateStatus!('a1', false)
    await expect(resource.remove!('a1')).rejects.toThrow('openplatform.oauth_apps.msg_app_not_found')
    expect(calls[0]).toMatchObject({ method: 'GET', url: '/api/oauth-apps/a1' })
    expect(calls[1]).toMatchObject({ method: 'PUT', url: '/api/oauth-apps/a1', body: { isEnabled: false, redirectUris: ['https://portal.example/cb', 'https://portal.example/cb2'], grantTypes: ['authorization_code', 'refresh_token'], refreshTokenLifetime: 2592000 } })
    expect(calls[2]).toMatchObject({ method: 'DELETE', url: '/api/oauth-apps?ids=a1' })
  })

  it('分页：POST oauth-apps:list，结果换成 SchemaPage 的分页；未知失败码原样', async () => {
    const calls = stub({ items: [ROW], total: 1, page: { index: 1, size: 20 } })
    const result = await oauthAppResource(t).page!({ page: 1, pageSize: 20, filters: {}, conditionFilters: [], sorts: [] })
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/oauth-apps:list', body: { page: { index: 1, size: 20 }, filters: [], sorts: [], keyword: null } })
    expect(result.items).toHaveLength(1)
    expect(result.page.totalCount).toBe(1)
    expect(() => assertBatch(t, { failed: [{ code: 'X_1' }, { code: 'X_1' }] })).toThrow(/^X_1$/)
  })
})
