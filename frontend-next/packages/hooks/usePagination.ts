import type { MaybeRefOrGetter } from 'vue'
import { computed, ref, toValue } from 'vue'

/**
 * 数组分页工具函数。
 *
 * 接受响应式或普通数组，自动计算分页数据。
 * 适用于前端本地分页（数据量较小且一次性加载的场景）。
 *
 * @param list 源数组（支持 ref / computed / 普通数组）
 * @param initialPageSize 初始每页条数，默认 10
 */
export function usePagination<T>(list: MaybeRefOrGetter<T[]>, initialPageSize = 10) {
  // 原始页码：只存用户意图，对外读取一律经 clampPage 夹取到当前有效范围。
  const rawPage = ref(1)
  const pageSize = ref(initialPageSize)

  const sourceList = computed(() => toValue(list))
  const total = computed(() => sourceList.value.length)
  const totalPages = computed(() => Math.max(1, Math.ceil(total.value / pageSize.value)))

  /** 把页码夹到 [1, totalPages]；非有限数（NaN / Infinity）视为无效输入，保持原值 */
  function clampPage(page: number): number {
    if (!Number.isFinite(page)) {
      return rawPage.value
    }
    return Math.max(1, Math.min(page, totalPages.value))
  }

  /**
   * 当前页码。读写都走边界夹取：源数组缩短（典型场景是在末页把记录删完）后，
   * 页码随 totalPages 自动回收到末页，不会停在越界页导致「有数据却是空表」。
   */
  const currentPage = computed<number>({
    get: () => clampPage(rawPage.value),
    set: (page) => {
      rawPage.value = clampPage(page)
    },
  })

  /** 当前页的数据切片 */
  const paginationList = computed<T[]>(() => {
    const start = (currentPage.value - 1) * pageSize.value
    return sourceList.value.slice(start, start + pageSize.value)
  })

  /** 跳转到指定页（自动边界修正；非有限数忽略） */
  function setCurrentPage(page: number) {
    currentPage.value = page
  }

  /** 修改每页条数，同时重置到第一页（非有限数忽略，避免页数保护整体失效） */
  function setPageSize(size: number) {
    if (!Number.isFinite(size)) {
      return
    }
    pageSize.value = Math.max(1, size)
    rawPage.value = 1
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
