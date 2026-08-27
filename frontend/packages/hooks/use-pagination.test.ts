import { describe, expect, it } from 'vitest'
/**
 * usePagination 前端本地分页单元测试。
 * 职责：锁定页码/页长/总数派生、切片边界、setCurrentPage 的自动边界修正、
 * setPageSize 的下限与重置到首页，以及源数组变化后的派生行为。
 */
import { computed, ref } from 'vue'
import { usePagination } from './usePagination'

function makeList(size: number): number[] {
  return Array.from({ length: size }, (_, index) => index + 1)
}

describe('usePagination 基础派生', () => {
  it('默认每页 10 条且停在第一页', () => {
    const { currentPage, pageSize, total, totalPages, paginationList } = usePagination(makeList(25))

    expect(currentPage.value).toBe(1)
    expect(pageSize.value).toBe(10)
    expect(total.value).toBe(25)
    expect(totalPages.value).toBe(3)
    expect(paginationList.value).toEqual(makeList(10))
  })

  it('末页只返回剩余条目而非补满一页', () => {
    const { setCurrentPage, paginationList } = usePagination(makeList(25))

    setCurrentPage(3)

    expect(paginationList.value).toEqual([21, 22, 23, 24, 25])
  })

  it('空数组的总页数按 1 计，切片为空', () => {
    const { total, totalPages, paginationList } = usePagination<number>([])

    expect(total.value).toBe(0)
    expect(totalPages.value).toBe(1)
    expect(paginationList.value).toEqual([])
  })

  it('总数恰好整除页长时不多出一个空白页', () => {
    const { totalPages } = usePagination(makeList(20), 10)

    expect(totalPages.value).toBe(2)
  })

  it('单条数据也占满一页', () => {
    const { totalPages, paginationList } = usePagination([42], 10)

    expect(totalPages.value).toBe(1)
    expect(paginationList.value).toEqual([42])
  })

  it('列表元素为对象时切片返回同一批引用而非深拷贝', () => {
    const first = { id: 1 }
    const second = { id: 2 }
    const { paginationList } = usePagination([first, second], 1)

    expect(paginationList.value).toHaveLength(1)
    expect(paginationList.value[0]).toBe(first)
    expect(paginationList.value).not.toContain(second)
  })
})

describe('usePagination 响应式源', () => {
  it('传入 ref 时源数组替换后总数与切片同步更新', () => {
    const source = ref(makeList(5))
    const { total, paginationList } = usePagination(source, 3)

    expect(total.value).toBe(5)
    expect(paginationList.value).toEqual([1, 2, 3])

    source.value = makeList(2)

    expect(total.value).toBe(2)
    expect(paginationList.value).toEqual([1, 2])
  })

  it('传入 getter 时按最新返回值分页', () => {
    const flag = ref(true)
    const { total, totalPages } = usePagination(() => (flag.value ? makeList(7) : []))

    expect(total.value).toBe(7)

    flag.value = false

    expect(total.value).toBe(0)
    expect(totalPages.value).toBe(1)
  })

  it('传入 computed 时页长变化立即反映到切片', () => {
    const source = computed(() => makeList(9))
    const { setPageSize, paginationList } = usePagination(source, 4)

    expect(paginationList.value).toEqual([1, 2, 3, 4])

    setPageSize(9)

    expect(paginationList.value).toEqual(makeList(9))
  })
})

describe('usePagination 边界翻页', () => {
  it('跳转到 0 页或负数页一律修正为第一页', () => {
    const { currentPage, setCurrentPage } = usePagination(makeList(30))

    setCurrentPage(0)
    expect(currentPage.value).toBe(1)

    setCurrentPage(-99)
    expect(currentPage.value).toBe(1)
  })

  it('跳转超过末页时截到末页而不是越界返回空切片', () => {
    const { currentPage, setCurrentPage, paginationList } = usePagination(makeList(30))

    setCurrentPage(999)

    expect(currentPage.value).toBe(3)
    expect(paginationList.value).toEqual([21, 22, 23, 24, 25, 26, 27, 28, 29, 30])
  })

  it('空列表下任何跳转都停在第一页', () => {
    const { currentPage, setCurrentPage } = usePagination<number>([])

    setCurrentPage(5)

    expect(currentPage.value).toBe(1)
  })

  it('页长小于 1 时抬到 1，避免除零导致的无限页', () => {
    const { pageSize, totalPages, setPageSize } = usePagination(makeList(3))

    setPageSize(0)
    expect(pageSize.value).toBe(1)
    expect(totalPages.value).toBe(3)

    setPageSize(-5)
    expect(pageSize.value).toBe(1)
    expect(totalPages.value).toBe(3)
  })

  it('修改页长后强制回到第一页，不保留原页码', () => {
    const { currentPage, setCurrentPage, setPageSize, paginationList } = usePagination(makeList(30))

    setCurrentPage(3)
    expect(currentPage.value).toBe(3)

    setPageSize(5)

    expect(currentPage.value).toBe(1)
    expect(paginationList.value).toEqual([1, 2, 3, 4, 5])
  })

  it('页长大于总数时只有一页且返回全部数据', () => {
    const { totalPages, paginationList } = usePagination(makeList(3), 1000)

    expect(totalPages.value).toBe(1)
    expect(paginationList.value).toEqual([1, 2, 3])
  })

  // 回归锚点（清单条目 15）：源数组缩短后页码必须自动回收，否则「在末页删完记录」会看到空表。
  it('源数组缩短后页码自动回收到末页，切片不会变空', () => {
    const source = ref(makeList(30))
    const pager = usePagination(source, 10)

    pager.setCurrentPage(3)
    expect(pager.currentPage.value).toBe(3)

    source.value = makeList(5)

    expect(pager.totalPages.value).toBe(1)
    expect(pager.currentPage.value).toBe(1)
    expect(pager.paginationList.value).toEqual([1, 2, 3, 4, 5])
  })

  // 回归锚点（清单条目 15）：回收只发生在读取侧，源数组补回来后仍回到原页码。
  it('源数组恢复长度后页码回到用户此前选定的页', () => {
    const source = ref(makeList(30))
    const pager = usePagination(source, 10)

    pager.setCurrentPage(3)
    source.value = makeList(5)
    expect(pager.currentPage.value).toBe(1)

    source.value = makeList(30)

    expect(pager.currentPage.value).toBe(3)
    expect(pager.paginationList.value).toEqual([21, 22, 23, 24, 25, 26, 27, 28, 29, 30])
  })
})

describe('usePagination 非法输入', () => {
  it('初始页长传 0 不受下限保护，总页数退化为无穷（下限只在 setPageSize 内）', () => {
    const { pageSize, totalPages, paginationList } = usePagination(makeList(3), 0)

    expect(pageSize.value).toBe(0)
    expect(totalPages.value).toBe(Number.POSITIVE_INFINITY)
    expect(paginationList.value).toEqual([])
  })

  // 回归锚点（清单条目 42）：NaN 页长会让 totalPages 变 NaN，后续所有边界保护随之失效。
  it('页长传 NaN 被忽略，页长与总页数保持原值', () => {
    const { pageSize, totalPages, setPageSize } = usePagination(makeList(10))

    setPageSize(Number.NaN)

    expect(pageSize.value).toBe(10)
    expect(totalPages.value).toBe(1)
  })

  // 回归锚点（清单条目 42）：分页组件清空输入框常给出 NaN，不能把它写进页码。
  it('跳转页码传 NaN 被忽略，停在原页且切片不变', () => {
    const { currentPage, setCurrentPage, paginationList } = usePagination(makeList(30))

    setCurrentPage(2)
    setCurrentPage(Number.NaN)

    expect(currentPage.value).toBe(2)
    expect(paginationList.value).toEqual([11, 12, 13, 14, 15, 16, 17, 18, 19, 20])
  })

  // 回归锚点（清单条目 42）：Infinity 同样不是有效页长/页码。
  it('页长与页码传 Infinity 一并被忽略', () => {
    const { currentPage, pageSize, setCurrentPage, setPageSize } = usePagination(makeList(30))

    setPageSize(Number.POSITIVE_INFINITY)
    setCurrentPage(Number.POSITIVE_INFINITY)

    expect(pageSize.value).toBe(10)
    expect(currentPage.value).toBe(1)
  })

  it('小数页长向上取整算页数，切片按小数下标截断', () => {
    const { setPageSize, pageSize, totalPages, paginationList } = usePagination(makeList(10))

    setPageSize(2.5)

    expect(pageSize.value).toBe(2.5)
    expect(totalPages.value).toBe(4)
    expect(paginationList.value).toEqual([1, 2])
  })

  it('超长列表下末页页码与切片长度仍精确', () => {
    const { setCurrentPage, currentPage, totalPages, paginationList } = usePagination(makeList(10001), 100)

    expect(totalPages.value).toBe(101)

    setCurrentPage(101)

    expect(currentPage.value).toBe(101)
    expect(paginationList.value).toEqual([10001])
  })
})
