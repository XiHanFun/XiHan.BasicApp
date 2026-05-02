import type { DynamicApiParams } from '../../base'
import type { ApiId, PageResult } from '../../types'
import type {
  DepartmentCreateDto,
  DepartmentDetailDto,
  DepartmentListItemDto,
  DepartmentPageQueryDto,
  DepartmentStatusUpdateDto,
  DepartmentTreeNodeDto,
  DepartmentTreeQueryDto,
  DepartmentUpdateDto,
} from './department.types'
import {
  appendDynamicApiParam,
  createCommandApi,
  createDynamicApiClient,
  createPageRequestParams,
  createReadApi,
} from '../../base'

const departmentQueryApi = createDynamicApiClient('DepartmentQuery')
const departmentCommandApi = createDynamicApiClient('Department')
const departmentReadApi = createReadApi<DepartmentListItemDto, DepartmentDetailDto, DepartmentPageQueryDto>(
  'DepartmentQuery',
  'Department',
)
const departmentBaseCommandApi = createCommandApi<DepartmentCreateDto, DepartmentUpdateDto, DepartmentDetailDto>(
  'Department',
  'Department',
)

export const departmentApi = {
  create(input: DepartmentCreateDto) {
    return departmentBaseCommandApi.create(input)
  },
  delete(id: ApiId) {
    return departmentCommandApi.delete(`Department/${id}`)
  },
  detail(id: ApiId) {
    return departmentReadApi.detail(id)
  },
  page(input: DepartmentPageQueryDto) {
    return departmentQueryApi.get<PageResult<DepartmentListItemDto>>(
      'DepartmentPage',
      toDepartmentPageParams(input),
    )
  },
  tree(input: DepartmentTreeQueryDto) {
    const params: DynamicApiParams = {
      Limit: input.limit,
      OnlyEnabled: input.onlyEnabled,
    }
    appendDynamicApiParam(params, 'Keyword', input.keyword)
    return departmentQueryApi.get<DepartmentTreeNodeDto[]>('DepartmentTree', params)
  },
  update(input: DepartmentUpdateDto) {
    return departmentBaseCommandApi.update(input)
  },
  updateStatus(input: DepartmentStatusUpdateDto) {
    return departmentCommandApi.put<DepartmentDetailDto, DepartmentStatusUpdateDto>('DepartmentStatus', input)
  },
}

function toDepartmentPageParams(input: DepartmentPageQueryDto) {
  const params = createPageRequestParams(input)
  appendDynamicApiParam(params, 'DepartmentType', input.departmentType)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'LeaderId', input.leaderId)
  appendDynamicApiParam(params, 'ParentId', input.parentId)
  appendDynamicApiParam(params, 'Status', input.status)
  return params
}
