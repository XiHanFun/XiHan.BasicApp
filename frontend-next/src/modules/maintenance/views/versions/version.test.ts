import type { VersionRow } from './version'
import { describe, expect, it } from 'vitest'
import { toPageRequest } from '~/contracts'
import { configureRequest } from '~/request'
import { databaseLabel, getVersion, upgradingLabel, upgradingOptions, upgradingTone, VERSION_QUERY, versionResource } from './version'

/** 假翻译：键原样返回 */
const t = (key: string) => key

function stub(...responses: object[]): { method: string, url: string, body: unknown }[] {
  const calls: { method: string, url: string, body: unknown }[] = []
  configureRequest({
    baseUrl: '',
    getToken: () => 't',
    fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
      calls.push({ method: init?.method ?? 'GET', url: String(url), body: init?.body ? JSON.parse(String(init.body)) : undefined })
      return new Response(JSON.stringify(responses[calls.length - 1] ?? {}), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }) as unknown as typeof fetch,
  })
  return calls
}

const ROW: VersionRow = {
  id: '0199a1b2-0000-7000-8000-000000000001',
  database: 'platform',
  appVersion: '6.0.0',
  dbVersion: '6.0.0',
  isUpgrading: false,
  upgradeNode: null,
  upgradeStartedAt: '2026-10-30T02:00:00Z',
  upgradedAt: '2026-10-30T02:00:03Z',
}

describe('版本管理：列表筛选', () => {
  it('应用版本、数据库版本与升级状态精确匹配，关键字去空白，排序原样下发', () => {
    const request = toPageRequest({
      page: 1,
      pageSize: 20,
      filters: { keyword: ' node ', appVersion: '6.0.0', dbVersion: ' 5.9.0 ', isUpgrading: 'true' },
      sorts: [{ field: 'upgradedAt', order: 'desc' }],
    }, VERSION_QUERY)
    expect(request).toEqual({
      page: { index: 1, size: 20 },
      keyword: 'node',
      filters: [
        { field: 'appVersion', operator: 'eq', value: '6.0.0' },
        { field: 'dbVersion', operator: 'eq', value: '5.9.0' },
        { field: 'isUpgrading', operator: 'eq', value: 'true' },
      ],
      sorts: [{ field: 'upgradedAt', descending: true }],
    })
  })

  it('没有登记的搜索字段直接报错，而不是静默丢掉条件', () => {
    expect(() => toPageRequest({ page: 1, pageSize: 20, filters: { upgradeNode: 'a' } }, VERSION_QUERY)).toThrow()
  })
})

describe('版本管理：文案', () => {
  it('平台库显示「平台库」，租户库原样；升级中黄、正常绿；搜索选项按 true / false 下发', () => {
    expect(databaseLabel(t, 'platform')).toBe('maintenance.versions.platform_database')
    expect(databaseLabel(t, 'tenant_acme')).toBe('tenant_acme')
    expect(upgradingLabel(t, true)).toBe('maintenance.versions.upgrading')
    expect(upgradingLabel(t, false)).toBe('maintenance.versions.normal')
    expect([upgradingTone(true), upgradingTone(false)]).toEqual(['warning', 'success'])
    expect(upgradingOptions(t).map(option => option.value)).toEqual(['true', 'false'])
  })
})

describe('版本管理：资源', () => {
  it('分页走 POST versions:list，结果换成 SchemaPage 的分页；详情按 ID 取', async () => {
    const calls = stub({ items: [ROW], total: 1, page: { index: 1, size: 20 } }, ROW)
    const result = await versionResource().page!({ page: 1, pageSize: 20, filters: {}, conditionFilters: [], sorts: [] })
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/versions:list', body: { page: { index: 1, size: 20 }, filters: [], sorts: [], keyword: null } })
    expect(result.items).toEqual([ROW])
    expect(result.page.totalCount).toBe(1)
    expect(await getVersion(ROW.id)).toEqual(ROW)
    expect(calls[1]).toMatchObject({ method: 'GET', url: `/api/versions/${ROW.id}` })
  })
})
