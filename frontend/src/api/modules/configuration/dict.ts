import type { ApiId, PageResult } from '../../types'
import type {
  DictCreateDto,
  DictDetailDto,
  DictItemCreateDto,
  DictItemDetailDto,
  DictItemListItemDto,
  DictItemPageQueryDto,
  DictItemStatusUpdateDto,
  DictItemTreeNodeDto,
  DictItemTreeQueryDto,
  DictItemUpdateDto,
  DictListItemDto,
  DictPageQueryDto,
  DictStatusUpdateDto,
  DictUpdateDto,
} from './dict.types'
import {
  createDynamicApiClient,
  createReadApi,
  formatDynamicApiRouteValue,
} from '../../base'

const dictQueryApi = createDynamicApiClient('DictQuery')
const dictCommandApi = createDynamicApiClient('Dict')
const dictReadApi = createReadApi<DictListItemDto, DictDetailDto, DictPageQueryDto>('DictQuery', 'Dict')
const dictItemReadApi = createReadApi<DictItemListItemDto, DictItemDetailDto, DictItemPageQueryDto>('DictQuery', 'DictItem')

export const dictApi = {
  create(input: DictCreateDto) {
    return dictCommandApi.post<DictDetailDto, DictCreateDto>('Dict', input)
  },
  delete(id: ApiId) {
    return dictCommandApi.delete(`Dict/${formatDynamicApiRouteValue(id)}`)
  },
  detail(id: ApiId) {
    return dictReadApi.detail(id)
  },
  itemCreate(input: DictItemCreateDto) {
    return dictCommandApi.post<DictItemDetailDto, DictItemCreateDto>('DictItem', input)
  },
  itemDelete(id: ApiId) {
    return dictCommandApi.delete(`DictItem/${formatDynamicApiRouteValue(id)}`)
  },
  itemDetail(id: ApiId) {
    return dictItemReadApi.detail(id)
  },
  itemPage(input: DictItemPageQueryDto) {
    return dictQueryApi.post<PageResult<DictItemListItemDto>>('DictItemPage', input)
  },
  itemTree(input: DictItemTreeQueryDto) {
    return dictQueryApi.get<DictItemTreeNodeDto[]>('DictItemTree', {
      DictId: input.dictId,
      Limit: input.limit,
      OnlyEnabled: input.onlyEnabled,
    })
  },
  itemUpdate(input: DictItemUpdateDto) {
    return dictCommandApi.put<DictItemDetailDto, DictItemUpdateDto>('DictItem', input)
  },
  itemUpdateStatus(input: DictItemStatusUpdateDto) {
    return dictCommandApi.put<DictItemDetailDto, DictItemStatusUpdateDto>('DictItemStatus', input)
  },
  page(input: DictPageQueryDto) {
    return dictQueryApi.post<PageResult<DictListItemDto>>('DictPage', input)
  },
  update(input: DictUpdateDto) {
    return dictCommandApi.put<DictDetailDto, DictUpdateDto>('Dict', input)
  },
  updateStatus(input: DictStatusUpdateDto) {
    return dictCommandApi.put<DictDetailDto, DictStatusUpdateDto>('DictStatus', input)
  },
}
