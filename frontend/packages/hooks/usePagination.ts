import { type MaybeRefOrGetter, computed, ref, toValue } from 'vue'

/**
 * 数组分页工具函数。
 *
 * 接受响应式或普通数组，自动计算分页数据。
 * 适用于前端本地分页（数据量较小且一次性加载的场景）。
 *
 * 对标 vben-admin 的 `use-pagination`。
 *
 * @param list 源数组（支持 ref / computed / 普通数组）
 * @param initialPageSize 初始每页条数，默认 10
 */
export function usePagination<T>(list: MaybeRefOrGetter<T[]>, initialPageSize = 10) {
  const currentPage = ref(1)
  const pageSize = ref(initialPageSize)

  const sourceList = computed(() => toValue(list))
  const total = computed(() => sourceList.value.length)
  const totalPages = computed(() => Math.max(1, Math.ceil(total.value / pageSize.value)))

  /** 当前页的数据切片 */
  const paginationList = computed<T[]>(() => {
    const start = (currentPage.value - 1) * pageSize.value
    return sourceList.value.slice(start, start + pageSize.value)
  })

  /** 跳转到指定页（自动边界修正） */
  function setCurrentPage(page: number) {
    currentPage.value = Math.max(1, Math.min(page, totalPages.value))
  }

  /** 修改每页条数，同时重置到第一页 */
  function setPageSize(size: number) {
    pageSize.value = Math.max(1, size)
    currentPage.value = 1
  }

  return {
    currentPage,
    pageSize,
    total,
    totalPages,
    paginationList,
    setCurrentPage,
    setPageSize,
  }
}
