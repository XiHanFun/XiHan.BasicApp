import type { DynamicApiParams } from '../../base'
import type { ApiId, PageResult } from '../../types'
import type {
  OperationCreateDto,
  OperationDetailDto,
  OperationListItemDto,
  OperationPageQueryDto,
  OperationSelectItemDto,
  OperationSelectQueryDto,
  OperationStatusUpdateDto,
  OperationUpdateDto,
} from './operation.types'
import {
  appendDynamicApiParam,
  createCommandApi,
  createDynamicApiClient,
  createReadApi,
  formatDynamicApiRouteValue,
} from '../../base'

const operationQueryApi = createDynamicApiClient('OperationQuery')
const operationCommandApi = createDynamicApiClient('Operation')
const operationReadApi = createReadApi<OperationListItemDto, OperationDetailDto, OperationPageQueryDto>(
  'OperationQuery',
  'Operation',
)
const operationBaseCommandApi = createCommandApi<OperationCreateDto, OperationUpdateDto, OperationDetailDto>(
  'Operation',
  'Operation',
)

export const operationApi = {
  availableGlobal(input: OperationSelectQueryDto) {
    return operationQueryApi.get<OperationSelectItemDto[]>(
      'AvailableGlobalOperations',
      toOperationSelectParams(input),
    )
  },
  create(input: OperationCreateDto) {
    return operationBaseCommandApi.create(input)
  },
  delete(id: ApiId) {
    return operationCommandApi.delete(`Operation/${formatDynamicApiRouteValue(id)}`)
  },
  detail(id: ApiId) {
    return operationReadApi.detail(id)
  },
  page(input: OperationPageQueryDto) {
    return operationQueryApi.post<PageResult<OperationListItemDto>>('OperationPage', input)
  },
  update(input: OperationUpdateDto) {
    return operationBaseCommandApi.update(input)
  },
  updateStatus(input: OperationStatusUpdateDto) {
    return operationCommandApi.put<OperationDetailDto, OperationStatusUpdateDto>('OperationStatus', input)
  },
}

function toOperationSelectParams(input: OperationSelectQueryDto) {
  const params: DynamicApiParams = {
    Limit: input.limit,
  }

  appendDynamicApiParam(params, 'Category', input.category)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'OperationTypeCode', input.operationTypeCode)

  return params
}
