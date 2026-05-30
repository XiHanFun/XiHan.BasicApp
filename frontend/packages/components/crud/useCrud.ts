import type { ApiId, PageRequest } from '@/api'
import type { Ref } from 'vue'
import type { CrudResource } from './types'
import { useMessage } from 'naive-ui'
import { reactive, ref } from 'vue'

/**
 * useCrud 选项
 */
export interface UseCrudOptions<TQuery> {
  /** 查询对象默认值（分页外的过滤条件初值） */
  defaultQuery?: Partial<TQuery>
  /** 初始每页数量，默认 20 */
  pageSize?: number
  /** 删除成功提示文案 */
  removeSuccessText?: string
  /** 查询失败提示文案 */
  loadErrorText?: string
}

/**
 * 列表页数据状态机：分页 / 查询 / 删除 / loading 统一封装。
 * 仅依赖 CrudResource 的 page / remove，不绑定具体资源实现。
 */
export function useCrud<TList extends object, TQuery extends Partial<PageRequest>>(
  resource: CrudResource<TList, TQuery>,
  options: UseCrudOptions<TQuery> = {},
) {
  const message = useMessage()

  const loading = ref(false)
  const rows = ref<TList[]>([]) as Ref<TList[]>
  const total = ref(0)
  const page = ref(1)
  const pageSize = ref(options.pageSize ?? 20)

  const filters = reactive<Record<string, unknown>>({ ...(options.defaultQuery ?? {}) })

  function buildQuery(): TQuery {
    return {
      ...filters,
      page: { pageIndex: page.value, pageSize: pageSize.value },
    } as unknown as TQuery
  }

  async function load() {
    loading.value = true
    try {
      const result = await resource.page(buildQuery())
      rows.value = result.items
      total.value = result.page.totalCount
    }
    catch {
      message.error(options.loadErrorText ?? '查询失败')
      rows.value = []
      total.value = 0
    }
    finally {
      loading.value = false
    }
  }

  function search() {
    page.value = 1
    void load()
  }

  function reset() {
    for (const key of Object.keys(filters)) {
      delete filters[key]
    }
    Object.assign(filters, options.defaultQuery ?? {})
    page.value = 1
    void load()
  }

  function changePage(value: number) {
    page.value = value
    void load()
  }

  function changePageSize(value: number) {
    pageSize.value = value
    page.value = 1
    void load()
  }

  async function remove(id: ApiId) {
    if (!resource.remove) {
      return
    }
    await resource.remove(id)
    message.success(options.removeSuccessText ?? '删除成功')
    void load()
  }

  return {
    loading,
    rows,
    total,
    page,
    pageSize,
    filters,
    load,
    search,
    reset,
    changePage,
    changePageSize,
    remove,
  }
}
