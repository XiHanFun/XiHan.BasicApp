import type { ApiId, PageResult } from '../../types'
import type {
  FieldLevelSecurityCreateDto,
  FieldLevelSecurityDetailDto,
  FieldLevelSecurityListItemDto,
  FieldLevelSecurityPageQueryDto,
  FieldLevelSecurityStatusUpdateDto,
  FieldLevelSecurityUpdateDto,
} from './field-level-security.types'
import {
  appendDynamicApiParam,
  createCommandApi,
  createDynamicApiClient,
  createPageRequestParams,
  createReadApi,
  formatDynamicApiRouteValue,
} from '../../base'

const fieldLevelSecurityQueryApi = createDynamicApiClient('FieldLevelSecurityQuery')
const fieldLevelSecurityCommandApi = createDynamicApiClient('FieldLevelSecurity')
const fieldLevelSecurityReadApi = createReadApi<
  FieldLevelSecurityListItemDto,
  FieldLevelSecurityDetailDto,
  FieldLevelSecurityPageQueryDto
>('FieldLevelSecurityQuery', 'FieldLevelSecurity')
const fieldLevelSecurityBaseCommandApi = createCommandApi<
  FieldLevelSecurityCreateDto,
  FieldLevelSecurityUpdateDto,
  FieldLevelSecurityDetailDto
>('FieldLevelSecurity', 'FieldLevelSecurity')

export const fieldLevelSecurityApi = {
  create(input: FieldLevelSecurityCreateDto) {
    return fieldLevelSecurityBaseCommandApi.create(input)
  },
  delete(id: ApiId) {
    return fieldLevelSecurityCommandApi.delete(`FieldLevelSecurity/${formatDynamicApiRouteValue(id)}`)
  },
  detail(id: ApiId) {
    return fieldLevelSecurityReadApi.detail(id)
  },
  page(input: FieldLevelSecurityPageQueryDto) {
    return fieldLevelSecurityQueryApi.get<PageResult<FieldLevelSecurityListItemDto>>(
      'FieldLevelSecurityPage',
      toFieldLevelSecurityPageParams(input),
    )
  },
  update(input: FieldLevelSecurityUpdateDto) {
    return fieldLevelSecurityBaseCommandApi.update(input)
  },
  updateStatus(input: FieldLevelSecurityStatusUpdateDto) {
    return fieldLevelSecurityCommandApi.put<FieldLevelSecurityDetailDto, FieldLevelSecurityStatusUpdateDto>(
      'FieldLevelSecurityStatus',
      input,
    )
  },
}

function toFieldLevelSecurityPageParams(input: FieldLevelSecurityPageQueryDto) {
  const params = createPageRequestParams(input)

  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'MaskStrategy', input.maskStrategy)
  appendDynamicApiParam(params, 'ResourceId', input.resourceId)
  appendDynamicApiParam(params, 'Status', input.status)
  appendDynamicApiParam(params, 'TargetId', input.targetId)
  appendDynamicApiParam(params, 'TargetType', input.targetType)

  return params
}
