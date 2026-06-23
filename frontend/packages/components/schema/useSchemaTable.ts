import type { Ref } from 'vue'
import type { PageSchema, SchemaQueryParams } from './types'
import type { ApiId } from '~/types/contracts'
import { useMessage } from 'naive-ui'
import { reactive, ref } from 'vue'
import { i18n } from '~/locales'

/**
 * useSchemaTable 选项
 */
export interface UseSchemaTableOptions {
  /** 初始每页数量 */
  pageSize?: number
  /** 删除成功提示 */
  removeSuccessText?: string
  /** 查询失败提示 */
  loadErrorText?: string
}

/**
 * Schema 列表数据状态机：分页 / 搜索 / 排序 / 删除 / loading 统一封装。
 * 服务端模式：所有分页、排序、筛选均下发后端，前端不做本地处理。
 */
export function useSchemaTable<TRow extends object>(
  schema: PageSchema<TRow>,
  options: UseSchemaTableOptions = {},
) {
  const message = useMessage()

  const loading = ref(false)
  const rows = ref<TRow[]>([]) as Ref<TRow[]>
  const total = ref(0)
  const page = ref(1)
  const pageSize = ref(options.pageSize ?? schema.pageSize ?? 20)

  /** 当前搜索/筛选条件（key → value） */
  const filters = reactive<Record<string, unknown>>({})
  /** 当前排序 */
  const sortField = ref<string | undefined>(undefined)
  const sortOrder = ref<'asc' | 'desc' | undefined>(undefined)

  /** 树形模式：存在 schema.tree 即启用，走 resource.tree、不分页 */
  const isTree = !!schema.tree
  const childrenKey = schema.tree?.childrenKey ?? 'children'

  /** 递归统计树节点总数（用于「共 N 条」展示） */
  function countTreeNodes(list: TRow[]): number {
    let count = 0
    for (const node of list) {
      count += 1
      const children = (node as Record<string, unknown>)[childrenKey] as TRow[] | undefined
      if (children?.length) {
        count += countTreeNodes(children)
      }
    }
    return count
  }

  function buildParams(): SchemaQueryParams {
    return {
      page: page.value,
      pageSize: pageSize.value,
      sortField: sortField.value,
      sortOrder: sortOrder.value,
      filters: { ...filters },
    }
  }

  async function load() {
    loading.value = true
    try {
      if (isTree) {
        const treeFn = schema.resource.tree
        if (!treeFn) {
          throw new Error('[SchemaPage] 树形模式需在 resource.tree 提供取数函数')
        }
        const items = await treeFn(buildParams())
        rows.value = items
        total.value = countTreeNodes(items)
        return
      }
      const pageFn = schema.resource.page
      if (!pageFn) {
        throw new Error('[SchemaPage] 列表模式需在 resource.page 提供取数函数')
      }
      const result = await pageFn(buildParams())
      rows.value = result.items
      total.value = result.page?.totalCount ?? 0
    }
    catch {
      message.error(options.loadErrorText ?? i18n.global.t('component.schema_page.load_failed'))
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
    sortField.value = undefined
    sortOrder.value = undefined
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

  function changeSort(field?: string, order?: 'asc' | 'desc') {
    sortField.value = field
    sortOrder.value = order
    page.value = 1
    void load()
  }

  async function remove(id: ApiId) {
    if (!schema.resource.remove) {
      return
    }
    await schema.resource.remove(id)
    message.success(options.removeSuccessText ?? i18n.global.t('common.messages.delete_success'))
    void load()
  }

  return {
    loading,
    rows,
    total,
    page,
    pageSize,
    filters,
    sortField,
    sortOrder,
    isTree,
    load,
    search,
    reset,
    changePage,
    changePageSize,
    changeSort,
    remove,
  }
}
