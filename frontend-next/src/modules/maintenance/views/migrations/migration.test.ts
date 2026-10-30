import type { MigrationRow } from './migration'
import { describe, expect, it } from 'vitest'
import { buildLogDetailView } from '@/modules/auditing/components/log-detail-view'
import { configureRequest } from '~/request'
import { QueryOperator } from '~/types/contracts'
import { executedAtFilters, getMigration, migrationDetailSchema, migrationResource, successLabel, successOptions, toMigrationsRequest } from './migration'

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

const FAILED: MigrationRow = {
  id: '0199a1b2-0000-7000-8000-000000000002',
  version: '6.0.1',
  scriptName: '6.0.1.sql',
  executedAt: '2026-10-30T02:00:00Z',
  durationMs: 42,
  success: false,
  nodeName: 'node-a',
  errorMessage: 'relation "sys_x" does not exist',
}

describe('升级记录：列表筛选', () => {
  it('版本、脚本名与节点按包含匹配，执行结果精确匹配；执行时间区间换成 ge 起点与 lt 终点后 1 毫秒', () => {
    const request = toMigrationsRequest({
      page: 2,
      pageSize: 20,
      filters: { keyword: ' 6.0 ', version: '6.0', scriptName: 'init', nodeName: 'node', success: 'false', executedAt: [1, 2] },
      conditionFilters: [{ field: 'executedAt', operator: QueryOperator.Between, values: ['2026-10-01T00:00:00.000Z', '2026-10-31T23:59:59.999Z'] }],
      sorts: [{ field: 'durationMs', order: 'desc' }],
    })
    expect(request).toEqual({
      page: { index: 2, size: 20 },
      keyword: '6.0',
      filters: [
        { field: 'version', operator: 'contains', value: '6.0' },
        { field: 'scriptName', operator: 'contains', value: 'init' },
        { field: 'nodeName', operator: 'contains', value: 'node' },
        { field: 'success', operator: 'eq', value: 'false' },
        { field: 'executedAt', operator: 'ge', value: '2026-10-01T00:00:00.000Z' },
        { field: 'executedAt', operator: 'lt', value: '2026-11-01T00:00:00.000Z' },
      ],
      sorts: [{ field: 'durationMs', descending: true }],
    })
  })

  it('不排序即交给后端按执行时间倒序；执行时间只认区间，别的写法直接报错', () => {
    expect(toMigrationsRequest({ page: 1, pageSize: 10, filters: {} })).toMatchObject({ filters: [], sorts: [] })
    expect(() => executedAtFilters({ field: 'executedAt', operator: QueryOperator.In, values: ['2026-10-01T00:00:00Z'] })).toThrow()
    expect(() => executedAtFilters({ field: 'executedAt', operator: QueryOperator.Between, values: ['2026-10-01T00:00:00Z', 'x'] })).toThrow()
  })
})

describe('升级记录：文案与详情', () => {
  it('执行结果成功 / 失败，搜索选项按 true / false 下发', () => {
    expect(successLabel(t, true)).toBe('maintenance.migrations.success_yes')
    expect(successLabel(t, false)).toBe('maintenance.migrations.success_no')
    expect(successOptions(t).map(option => option.value)).toEqual(['true', 'false'])
  })

  it('详情抬头是版本 · 脚本、红色的失败标签与耗时，副信息是执行时间与节点；记录 ID 在追踪组，失败原因排在最后', () => {
    const view = buildLogDetailView(migrationDetailSchema(t), FAILED, (field, value) => field.type === 'boolean' ? (value ? field.trueText! : field.falseText!) : String(value))
    expect(view.title).toBe('6.0.1 · 6.0.1.sql')
    expect(view.status).toMatchObject({ text: 'maintenance.migrations.success_no', tone: 'danger' })
    expect(view.badges.map(entry => entry.field.key)).toEqual(['durationMs'])
    expect(view.meta.map(entry => entry.field.key)).toEqual(['executedAt', 'nodeName'])
    expect(view.groups).toEqual([{ group: 'trace', entries: [expect.objectContaining({ raw: FAILED.id })] }])
    expect(view.blocks.map(entry => entry.text)).toEqual([FAILED.errorMessage])
  })
})

describe('升级记录：资源', () => {
  it('分页走 POST migrations:list，详情按 ID 取', async () => {
    const calls = stub({ items: [FAILED], total: 1, page: { index: 1, size: 20 } }, FAILED)
    const result = await migrationResource().page!({ page: 1, pageSize: 20, filters: {}, conditionFilters: [], sorts: [] })
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/migrations:list', body: { page: { index: 1, size: 20 }, filters: [], sorts: [], keyword: null } })
    expect(result.items).toEqual([FAILED])
    expect(await getMigration(FAILED.id)).toEqual(FAILED)
    expect(calls[1]).toMatchObject({ method: 'GET', url: `/api/migrations/${FAILED.id}` })
  })
})
