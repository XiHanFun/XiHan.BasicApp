import type { DynamicApiParams } from '../../base'
import type { DepartmentTreeNodeDto, DepartmentTreeQueryDto } from './types'
import { appendDynamicApiParam, createDynamicApiClient } from '../../base'

const departmentQueryApi = createDynamicApiClient('DepartmentQuery')

export const departmentApi = {
  tree(input: DepartmentTreeQueryDto) {
    const params: DynamicApiParams = {
      Limit: input.limit,
      OnlyEnabled: input.onlyEnabled,
    }

    appendDynamicApiParam(params, 'Keyword', input.keyword)

    return departmentQueryApi.get<DepartmentTreeNodeDto[]>('DepartmentTree', params)
  },
}
