import type { VersionRow } from '@/modules/maintenance/views/versions/version'
import { describe, expect, it } from 'vitest'
import { canReleaseLock, canUpgrade, databaseLabel, getVersion, planVersion, releaseVersionLock, statusLabel, statusOptions, statusTone, upgradeVersion, VERSION_QUERY, versionResource } from '@/modules/maintenance/views/versions/version'
import { toPageRequest } from '~/contracts'
import { configureRequest } from '~/request'

/** 假翻译：键原样返回 */
const t = (key: string) => key

function stub(...responses: object[]): { method: string, url: string, body: unknown }[] {
  const calls: { method: string, url: string, body: unknown }[] = []
  configureRequest({
    baseUrl: '',
    getToken: () => 't',
    fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
      calls.push({
        method: init?.method ?? 'GET',
        url: String(url),
        body: init?.body
          ? JSON.parse(String(init.body))
          : undefined,
      })
      return new Response(JSON.stringify(responses[calls.length - 1] ?? {}), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      })
    }) as unknown as typeof fetch,
  })
  return calls
}

const ROW: VersionRow = {
  id: '0199a1b2-0000-7000-8000-000000000001',
  database: 'platform',
  provider: 'postgresql',
  status: 'Current',
  appVersion: '6.0.0',
  dbVersion: '6.0.0',
  targetVersion: '6.0.0',
  isUpgrading: false,
  upgradeNode: null,
  upgradeStartedAt: '2026-10-30T02:00:00Z',
  upgradedAt: '2026-10-30T02:00:03Z',
  lastError: null,
  lastFailedAt: null,
}

describe('版本管理：列表筛选', () => {
  it('应用版本、数据库版本、库类型与结构状态精确匹配，关键字去空白，排序原样下发', () => {
    const request = toPageRequest({
      page: 1,
      pageSize: 20,
      filters: {
        keyword: ' node ',
        appVersion: '6.0.0',
        dbVersion: ' 5.9.0 ',
        provider: 'mysql',
        status: 'Failed',
      },
      sorts: [
        { field: 'upgradedAt', order: 'desc' },
      ],
    }, VERSION_QUERY)
    expect(request).toEqual({
      page: { index: 1, size: 20 },
      keyword: 'node',
      filters: [
        { field: 'appVersion', operator: 'eq', value: '6.0.0' },
        { field: 'dbVersion', operator: 'eq', value: '5.9.0' },
        { field: 'provider', operator: 'eq', value: 'mysql' },
        { field: 'status', operator: 'eq', value: 'Failed' },
      ],
      sorts: [
        { field: 'upgradedAt', descending: true },
      ],
    })
  })

  it('没有登记的搜索字段直接报错，而不是静默丢掉条件', () => {
    expect(() => toPageRequest({
      page: 1,
      pageSize: 20,
      filters: { upgradeNode: 'a' },
    }, VERSION_QUERY)).toThrow()
  })
})

describe('版本管理：文案', () => {
  it('平台库显示「平台库」，租户库原样；结构状态最新绿、待升级黄、升级中蓝、失败与不可达红；搜索选项按状态名下发', () => {
    expect(databaseLabel(t, 'platform')).toBe('maintenance.versions.platform_database')
    expect(databaseLabel(t, 'tenant_acme')).toBe('tenant_acme')
    expect(statusLabel(t, 'Failed')).toBe('maintenance.versions.status_failed')
    expect(([
      'Current',
      'Behind',
      'Upgrading',
      'Failed',
      'Unreachable',
    ] as const).map(statusTone)).toEqual([
      'success',
      'warning',
      'info',
      'danger',
      'danger',
    ])
    expect(statusOptions(t).map(option => option.value)).toEqual([
      'Current',
      'Behind',
      'Upgrading',
      'Failed',
      'Unreachable',
    ])
  })

  it('只有失败或待升级的租户独立库能重试；台账标着升级中且有节点才能释放锁', () => {
    expect(canUpgrade({ ...ROW, database: 'acme', status: 'Failed' })).toBe(true)
    expect(canUpgrade({ ...ROW, database: 'acme', status: 'Behind' })).toBe(true)
    expect(canUpgrade({ ...ROW, database: 'acme', status: 'Current' })).toBe(false)
    expect(canUpgrade({ ...ROW, status: 'Failed' })).toBe(false)
    expect(canReleaseLock({ ...ROW, isUpgrading: true, upgradeNode: 'ghost:1' })).toBe(true)
    expect(canReleaseLock({ ...ROW, isUpgrading: true, upgradeNode: null })).toBe(false)
    expect(canReleaseLock(ROW)).toBe(false)
  })
})

describe('版本管理：资源', () => {
  it('分页走 POST versions:list，结果换成 SchemaPage 的分页；详情按库名取', async () => {
    const calls = stub({
      items: [ROW],
      total: 1,
      page: { index: 1, size: 20 },
    }, ROW)
    const result = await versionResource(t).page!({
      page: 1,
      pageSize: 20,
      filters: {},
      conditionFilters: [],
      sorts: [],
    })
    expect(calls[0]).toMatchObject({
      method: 'POST',
      url: '/api/versions:list',
      body: {
        page: { index: 1, size: 20 },
        filters: [],
        sorts: [],
        keyword: null,
      },
    })
    expect(result.items).toEqual([
      {
        ...ROW,
        databaseName: databaseLabel(t, ROW.database),
      },
    ])
    expect(result.page.totalCount).toBe(1)
    expect(await getVersion('tenant_acme')).toEqual(ROW)
    expect(calls[1]).toMatchObject({ method: 'GET', url: '/api/versions/tenant_acme' })
  })

  it('预览走 GET，重试与释放锁走 POST，库名在路径里', async () => {
    const calls = stub({}, {}, ROW)
    await planVersion('acme')
    await upgradeVersion('acme')
    await releaseVersionLock('platform', 'ghost:1')
    expect(calls).toMatchObject([
      { method: 'GET', url: '/api/versions/acme:plan' },
      { method: 'POST', url: '/api/versions/acme:upgrade' },
      {
        method: 'POST',
        url: '/api/versions/platform:release-lock',
        body: { node: 'ghost:1' },
      },
    ])
  })
})
