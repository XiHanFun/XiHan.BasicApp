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
  createCommandApi,
  createDynamicApiClient,
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
    return fieldLevelSecurityQueryApi.post<PageResult<FieldLevelSecurityListItemDto>>(
      'FieldLevelSecurityPage',
      input,
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
