/**
 * 列表取数（useSchemaTable.load）的先后
 * 锁定：连着翻页、搜索时先发的请求后回来，不覆盖最后一次的结果；先发的失败不清空、不提示；
 * loading 等最后一次收尾才放开；重置清空筛选与页码，排序换成调用方给的（页面给默认排序）
 */
import type { PageSchema } from '~/components/schema/types'
import type { PageResult } from '~/types/contracts'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { useSchemaTable } from '~/components/schema/useSchemaTable'
import { toast } from '~/composables'

interface Row { id: string }

interface Pending {
  resolve: (result: PageResult<Row>) => void
  reject: (error: Error) => void
}

function pageOf(ids: string[], totalCount: number): PageResult<Row> {
  return {
    items: ids.map(id => ({ id })),
    page: { totalCount } as PageResult<Row>['page'],
  }
}

function schemaWith(calls: Pending[]): PageSchema<Row> {
  return {
    pageCode: 'page-load-order',
    resource: {
      page: () => new Promise<PageResult<Row>>((resolve, reject) => {
        calls.push({ resolve, reject })
      }),
    },
    fields: [],
  } as unknown as PageSchema<Row>
}

/** 等手头的微任务都跑完（取数回来后的赋值与收尾） */
function settle(): Promise<void> {
  return new Promise(resolve => setTimeout(resolve, 0))
}

afterEach(() => {
  vi.restoreAllMocks()
})

describe('连着发的取数', () => {
  it('先发的后回来：不覆盖最后一次的结果，loading 等最后一次收尾', async () => {
    const calls: Pending[] = []
    const table = useSchemaTable(schemaWith(calls))

    table.changePage(1)
    table.changePage(2)
    calls[0]!.resolve(pageOf(['first'], 10))
    await settle()
    expect(table.rows.value).toStrictEqual([])
    expect(table.loading.value).toBe(true)

    calls[1]!.resolve(pageOf(['second'], 30))
    await settle()
    expect(table.rows.value).toStrictEqual([
      { id: 'second' },
    ])
    expect(table.total.value).toBe(30)
    expect(table.loading.value).toBe(false)
  })

  it('最后一次先回来、先发的再回来：结果仍是最后一次的', async () => {
    const calls: Pending[] = []
    const table = useSchemaTable(schemaWith(calls))

    table.search()
    table.changePage(3)
    calls[1]!.resolve(pageOf(['latest'], 41))
    await settle()
    calls[0]!.resolve(pageOf(['stale'], 5))
    await settle()

    expect(table.rows.value).toStrictEqual([
      { id: 'latest' },
    ])
    expect(table.total.value).toBe(41)
    expect(table.loading.value).toBe(false)
  })

  it('先发的失败：不清空最后一次的结果，也不提示', async () => {
    const danger = vi.spyOn(toast, 'danger').mockImplementation(() => '')
    const calls: Pending[] = []
    const table = useSchemaTable(schemaWith(calls))

    table.changePage(1)
    table.changePage(2)
    calls[1]!.resolve(pageOf(['kept'], 12))
    await settle()
    calls[0]!.reject(new Error('timeout'))
    await settle()

    expect(table.rows.value).toStrictEqual([
      { id: 'kept' },
    ])
    expect(table.total.value).toBe(12)
    expect(danger).not.toHaveBeenCalled()
  })

  it('最后一次失败：照常提示并清空', async () => {
    const danger = vi.spyOn(toast, 'danger').mockImplementation(() => '')
    const calls: Pending[] = []
    const table = useSchemaTable(schemaWith(calls))

    table.changePage(1)
    calls[0]!.reject(new Error('后端不可用'))
    await settle()

    expect(danger).toHaveBeenCalledWith('后端不可用')
    expect(table.rows.value).toStrictEqual([])
    expect(table.total.value).toBe(0)
    expect(table.loading.value).toBe(false)
  })
})

describe('重置', () => {
  it('清空筛选、回到第一页；排序换成给定的，缺省不排序', () => {
    const calls: Pending[] = []
    const table = useSchemaTable(schemaWith(calls))
    table.filters.name = 'alice'
    table.page.value = 3
    table.sorts.value = [
      { field: 'name', order: 'asc' },
    ]

    table.reset([
      { field: 'createdAt', order: 'desc' },
    ])
    expect(table.filters.name).toBeNull()
    expect(table.page.value).toBe(1)
    expect(table.sorts.value).toEqual([
      { field: 'createdAt', order: 'desc' },
    ])

    table.reset()
    expect(table.sorts.value).toEqual([])
    expect(calls).toHaveLength(2)
  })
})
