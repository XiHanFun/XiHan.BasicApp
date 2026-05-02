import type { DynamicApiParams } from '../../base'
import type { ApiId, PageResult } from '../../types'
import type {
  MenuCreateDto,
  MenuDetailDto,
  MenuListItemDto,
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
  createPageRequestParams,
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
    return menuQueryApi.get<PageResult<MenuListItemDto>>('MenuPage', toMenuPageParams(input))
  },
  tree(input: MenuTreeQueryDto) {
    const params: DynamicApiParams = {
      Limit: input.limit,
      OnlyEnabled: input.onlyEnabled,
    }
    appendDynamicApiParam(params, 'Keyword', input.keyword)
    return menuQueryApi.get<MenuTreeNodeDto[]>('MenuTree', params)
  },
  update(input: MenuUpdateDto) {
    return menuBaseCommandApi.update(input)
  },
  updateStatus(input: MenuStatusUpdateDto) {
    return menuCommandApi.put<MenuDetailDto, MenuStatusUpdateDto>('MenuStatus', input)
  },
}

function toMenuPageParams(input: MenuPageQueryDto) {
  const params = createPageRequestParams(input)
  appendDynamicApiParam(params, 'IsGlobal', input.isGlobal)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'MenuType', input.menuType)
  appendDynamicApiParam(params, 'ParentId', input.parentId)
  appendDynamicApiParam(params, 'PermissionId', input.permissionId)
  appendDynamicApiParam(params, 'Status', input.status)
  return params
}
