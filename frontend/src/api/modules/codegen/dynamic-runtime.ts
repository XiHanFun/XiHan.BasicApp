import type { DynamicApiParams } from '../../base'
import type { ApiId } from '../../types'
import type {
  DynamicRuntimePageQueryDto,
  DynamicRuntimePageResultDto,
  DynamicRuntimeSchemaDto,
} from './dynamic-runtime.types'
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  formatDynamicApiRouteValue,
} from '../../base'

const runtime = createDynamicApiClient('DynamicRuntime')

/**
 * 零代码只读运行时 API（DynamicRuntime 控制器）。
 * 动作名直调：Schema/{tableId}(GET) / Page(GET)，按元数据驱动渲染（不写死业务字段）。
 */
export const codeGenRuntimeApi = {
  /** 取表结构（动态列定义），运行时按此渲染表格列 */
  getSchema(tableId: ApiId) {
    return runtime.get<DynamicRuntimeSchemaDto>(`Schema/${formatDynamicApiRouteValue(tableId)}`)
  },
  /** 取运行时分页数据（动态行） */
  page(input: DynamicRuntimePageQueryDto) {
    const params: DynamicApiParams = {}
    appendDynamicApiParam(params, 'TableId', input.tableId)
    appendDynamicApiParam(params, 'PageIndex', input.pageIndex)
    appendDynamicApiParam(params, 'PageSize', input.pageSize)
    return runtime.get<DynamicRuntimePageResultDto>('Page', params)
  },
}
