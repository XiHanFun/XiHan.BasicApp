import type { ApiId, PageResult } from '../../types'
import type {
  DictDetailDto,
  DictItemDetailDto,
  DictItemListItemDto,
  DictItemPageQueryDto,
  DictItemTreeNodeDto,
  DictItemTreeQueryDto,
  DictListItemDto,
  DictPageQueryDto,
} from './dict.types'
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
  createReadApi,
} from '../../base'

const dictQueryApi = createDynamicApiClient('DictQuery')
const dictReadApi = createReadApi<DictListItemDto, DictDetailDto, DictPageQueryDto>('DictQuery', 'Dict')
const dictItemReadApi = createReadApi<DictItemListItemDto, DictItemDetailDto, DictItemPageQueryDto>('DictQuery', 'DictItem')

export const dictApi = {
  detail(id: ApiId) {
    return dictReadApi.detail(id)
  },
  itemDetail(id: ApiId) {
    return dictItemReadApi.detail(id)
  },
  itemPage(input: DictItemPageQueryDto) {
    return dictQueryApi.get<PageResult<DictItemListItemDto>>(
      'DictItemPage',
      toDictItemPageParams(input),
    )
  },
  itemTree(input: DictItemTreeQueryDto) {
    return dictQueryApi.get<DictItemTreeNodeDto[]>('DictItemTree', {
      DictId: input.dictId,
      Limit: input.limit,
      OnlyEnabled: input.onlyEnabled,
    })
  },
  page(input: DictPageQueryDto) {
    return dictQueryApi.get<PageResult<DictListItemDto>>('DictPage', toDictPageParams(input))
  },
}

function toDictPageParams(input: DictPageQueryDto) {
  const params = createPageRequestParams(input)
  appendDynamicApiParam(params, 'DictCode', input.dictCode)
  appendDynamicApiParam(params, 'DictType', input.dictType)
  appendDynamicApiParam(params, 'IsBuiltIn', input.isBuiltIn)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'Status', input.status)
  return params
}

function toDictItemPageParams(input: DictItemPageQueryDto) {
  const params = createPageRequestParams(input)
  appendDynamicApiParam(params, 'DictId', input.dictId)
  appendDynamicApiParam(params, 'IsDefault', input.isDefault)
  appendDynamicApiParam(params, 'ItemCode', input.itemCode)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'ParentId', input.parentId)
  appendDynamicApiParam(params, 'Status', input.status)
  return params
}
