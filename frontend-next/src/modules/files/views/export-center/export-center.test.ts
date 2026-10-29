import type { ExportTaskRow } from './export-center'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { toPageRequest } from '~/contracts'
import { configureRequest } from '~/request'
import { QueryOperator } from '~/types/contracts'
import {
  batchFailureMessage,
  businessTypeLabel,
  canCancel,
  canDelete,
  canDownload,
  createAutoRefresh,
  errorMessageText,
  EXPORT_TASK_QUERY,
  exportTaskResource,
  formatLabel,
  KNOWN_BUSINESS_TYPES,
  progressPercent,
  statusTone,
} from './export-center'

const t = (key: string, values?: Record<string, unknown>) => `${key}${values ? JSON.stringify(values) : ''}`

function task(patch: Partial<ExportTaskRow> = {}): ExportTaskRow {
  return {
    id: 'e1',
    businessType: 'users.list',
    taskName: 'users.list_20261029080000',
    scope: 'SearchResult',
    format: 'Csv',
    status: 'Pending',
    progress: 0,
    totalCount: 0,
    processedCount: 0,
    fileId: null,
    fileName: null,
    fileSize: null,
    errorMessage: null,
    startedAt: null,
    finishedAt: null,
    createdAt: '2026-10-29T00:00:00Z',
    ...patch,
  }
}

describe('导出任务列表的后端筛选', () => {
  it('状态、格式多选合成 in，业务类型精确匹配，关键字单独下发', () => {
    const request = toPageRequest({
      page: 1,
      pageSize: 20,
      filters: { keyword: ' 用户 ', status: ['Pending', 'Processing'], format: ['Xlsx'], businessType: 'users.list' },
      conditionFilters: [
        { field: 'status', operator: QueryOperator.In, values: ['Pending', 'Processing'] },
        { field: 'format', operator: QueryOperator.In, values: ['Xlsx'] },
      ],
      sorts: [{ field: 'createdAt', order: 'desc' }],
    }, EXPORT_TASK_QUERY)
    expect(request.keyword).toBe('用户')
    expect(request.filters).toEqual([
      { field: 'status', operator: 'in', value: 'Pending,Processing' },
      { field: 'format', operator: 'in', value: 'Xlsx' },
      { field: 'businessType', operator: 'eq', value: 'users.list' },
    ])
    expect(request.sorts).toEqual([{ field: 'createdAt', descending: true }])
  })

  it('后端不支持的筛选（如范围）不静默丢弃，直接报错', () => {
    expect(() => toPageRequest({ page: 1, pageSize: 20, filters: { scope: 'All' } }, EXPORT_TASK_QUERY)).toThrow(/scope/)
  })
})

describe('导出任务的删除', () => {
  it('走 export-tasks.delete；执行中的被逐项拒绝时翻成文案抛出', async () => {
    const calls: string[] = []
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
        calls.push(`${init?.method ?? 'GET'} ${String(url)}`)
        return new Response(JSON.stringify({ succeeded: [], failed: [{ id: 'e1', code: 'FILES_CONFLICT_007' }] }), { status: 200, headers: { 'Content-Type': 'application/json' } })
      }) as unknown as typeof fetch,
    })
    await expect(exportTaskResource(t).remove!('e1')).rejects.toThrow('files.export_center.batch.processing')
    expect(calls).toEqual(['DELETE /api/export-tasks?ids=e1'])
  })

  it('认得的错误码翻成文案，其余原样', () => {
    expect(batchFailureMessage(t, 'FILES_NOTFOUND_005')).toBe('files.export_center.batch.not_found')
    expect(batchFailureMessage(t, 'FILES_CONFLICT_007')).toBe('files.export_center.batch.processing')
    expect(batchFailureMessage(t, 'OTHER_001')).toBe('OTHER_001')
  })
})

describe('导出任务的行操作', () => {
  it('成功且有结果文件才能下载，只有待执行的能取消，执行中的不能删除', () => {
    expect(canDownload(task({ status: 'Success', fileId: 'f1' }))).toBe(true)
    expect(canDownload(task({ status: 'Success', fileId: null }))).toBe(false)
    expect(canDownload(task({ status: 'Failed', fileId: 'f1' }))).toBe(false)

    expect(canCancel(task({ status: 'Pending' }))).toBe(true)
    expect(canCancel(task({ status: 'Processing' }))).toBe(false)

    expect(canDelete(task({ status: 'Processing' }))).toBe(false)
    expect(canDelete(task({ status: 'Pending' }))).toBe(true)
    expect(canDelete(task({ status: 'Failed' }))).toBe(true)
  })

  it('进度夹在 0 到 100 之间并取整', () => {
    expect(progressPercent({ progress: 42.6 })).toBe(43)
    expect(progressPercent({ progress: -1 })).toBe(0)
    expect(progressPercent({ progress: 120 })).toBe(100)
  })
})

describe('导出任务的文案', () => {
  it('认得的业务类型翻成页面名，未登记的照原样显示列表操作 ID', () => {
    expect(KNOWN_BUSINESS_TYPES.map(type => businessTypeLabel(t, type))).toEqual([
      'files.export_center.business_types.users',
      'files.export_center.business_types.operation_logs',
      'files.export_center.business_types.access_logs',
      'files.export_center.business_types.login_logs',
      'files.export_center.business_types.exception_logs',
      'files.export_center.business_types.diff_logs',
      'files.export_center.business_types.open_api_logs',
    ])
    expect(businessTypeLabel(t, 'roles.list')).toBe('roles.list')
  })

  it('重启打断的失败原因翻成文案，取消与其余原因照原样，没有原因为空', () => {
    expect(errorMessageText(t, 'interrupted')).toBe('files.export_center.interrupted')
    expect(errorMessageText(t, '已取消')).toBe('已取消')
    expect(errorMessageText(t, null)).toBe('')
  })

  it('状态色调与格式名', () => {
    expect(statusTone('Success')).toBe('success')
    expect(statusTone('Failed')).toBe('danger')
    expect(statusTone('Processing')).toBe('info')
    expect(statusTone('Pending')).toBe('neutral')
    expect(formatLabel('Csv')).toBe('CSV')
    expect(formatLabel('Xlsx')).toBe('Excel')
  })
})

describe('自动刷新', () => {
  afterEach(() => vi.useRealTimers())

  it('每个间隔刷新一次、不可见时跳过；重复开始不叠加，停止后不再刷新', () => {
    vi.useFakeTimers()
    const reload = vi.fn()
    let visible = true
    const refresh = createAutoRefresh(reload, () => visible, 5_000)

    refresh.start()
    refresh.start()
    vi.advanceTimersByTime(5_000)
    expect(reload).toHaveBeenCalledTimes(1)

    visible = false
    vi.advanceTimersByTime(5_000)
    expect(reload).toHaveBeenCalledTimes(1)

    visible = true
    vi.advanceTimersByTime(5_000)
    expect(reload).toHaveBeenCalledTimes(2)

    refresh.stop()
    vi.advanceTimersByTime(20_000)
    expect(reload).toHaveBeenCalledTimes(2)
  })
})
