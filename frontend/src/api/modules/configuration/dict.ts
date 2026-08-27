import type { DynamicApiParams } from '../../base'
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
  appendDynamicApiParam,
  createDynamicApiClient,
  createReadApi,
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
    return dictCommandApi.delete('Dict', { id })
  },
  detail(id: ApiId) {
    return dictReadApi.detail(id)
  },
  itemCreate(input: DictItemCreateDto) {
    return dictCommandApi.post<DictItemDetailDto, DictItemCreateDto>('DictItem', input)
  },
  itemDelete(id: ApiId) {
    return dictCommandApi.delete('DictItem', { id })
  },
  itemDetail(id: ApiId) {
    return dictItemReadApi.detail(id)
  },
  itemPage(input: DictItemPageQueryDto) {
    return dictQueryApi.post<PageResult<DictItemListItemDto>>('DictItemPage', input)
  },
  itemTree(input: DictItemTreeQueryDto) {
    // 与 departmentApi.tree / menuApi.tree 同口径：逐字段 append，空值不入查询串（0 与 false 是有效取值照发）。
    // 直接拼对象字面量会把 undefined 写进 params，开启接口签名后 query 串形态不一致会影响签名。
    const params: DynamicApiParams = {}
    appendDynamicApiParam(params, 'DictId', input.dictId)
    appendDynamicApiParam(params, 'Limit', input.limit)
    appendDynamicApiParam(params, 'OnlyEnabled', input.onlyEnabled)
    return dictQueryApi.get<DictItemTreeNodeDto[]>('DictItemTree', params)
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
