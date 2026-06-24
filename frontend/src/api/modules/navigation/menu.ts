import type { DynamicApiParams } from '../../base'
import type { ApiId, PageResult } from '../../types'
import type {
  MenuCreateDto,
  MenuDetailDto,
  MenuListItemDto,
  MenuListQueryDto,
  MenuPageQueryDto,
  MenuStatusUpdateDto,
  MenuTreeNodeDto,
  MenuTreeQueryDto,
  MenuUpdateDto,
} from './menu.types'
import {
  appendDynamicApiParam,
  createCommandApi,
  createDynamicApiClient,
  createReadApi,
} from '../../base'

const menuQueryApi = createDynamicApiClient('MenuQuery')
const menuCommandApi = createDynamicApiClient('Menu')
const menuReadApi = createReadApi<MenuListItemDto, MenuDetailDto, MenuPageQueryDto>('MenuQuery', 'Menu')
const menuBaseCommandApi = createCommandApi<MenuCreateDto, MenuUpdateDto, MenuDetailDto>('Menu', 'Menu')

export const menuApi = {
  create(input: MenuCreateDto) {
    return menuBaseCommandApi.create(input)
  },
  delete(id: ApiId) {
    return menuCommandApi.delete(`Menu/${id}`)
  },
  detail(id: ApiId) {
    return menuReadApi.detail(id)
  },
  page(input: MenuPageQueryDto) {
    return menuQueryApi.post<PageResult<MenuListItemDto>>('MenuPage', input)
  },
  list(input: MenuListQueryDto = {}) {
    const params: DynamicApiParams = {}
    appendDynamicApiParam(params, 'Keyword', input.keyword)
    appendDynamicApiParam(params, 'ParentId', input.parentId)
    appendDynamicApiParam(params, 'PermissionId', input.permissionId)
    appendDynamicApiParam(params, 'MenuType', input.menuType)
    appendDynamicApiParam(params, 'IsExternal', input.isExternal)
    appendDynamicApiParam(params, 'IsVisible', input.isVisible)
    appendDynamicApiParam(params, 'IsGlobal', input.isGlobal)
    appendDynamicApiParam(params, 'Status', input.status)
    return menuQueryApi.get<MenuListItemDto[]>('MenuList', params)
  },
  tree(input: MenuTreeQueryDto) {
    const params: DynamicApiParams = {
      Limit: input.limit,
      OnlyEnabled: input.onlyEnabled,
    }
    appendDynamicApiParam(params, 'Keyword', input.keyword)
    appendDynamicApiParam(params, 'IncludeButtons', input.includeButtons)
    appendDynamicApiParam(params, 'OnlyVisible', input.onlyVisible)
    return menuQueryApi.get<MenuTreeNodeDto[]>('MenuTree', params)
  },
  update(input: MenuUpdateDto) {
    return menuBaseCommandApi.update(input)
  },
  updateStatus(input: MenuStatusUpdateDto) {
    return menuCommandApi.put<MenuDetailDto, MenuStatusUpdateDto>('MenuStatus', input)
  },
}
