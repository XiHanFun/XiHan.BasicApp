import type { ApiId, PageResult } from '../../types'
import type {
  PermissionChangeLogDetailDto,
  PermissionChangeLogListItemDto,
  PermissionChangeLogPageQueryDto,
} from './permission-change-log.types'
import { createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const permissionChangeLogQueryApi = createDynamicApiClient('PermissionChangeLogQuery')

export const permissionChangeLogApi = {
  detail(id: ApiId) {
    return permissionChangeLogQueryApi.get<PermissionChangeLogDetailDto | null>(
      `PermissionChangeLogDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  page(input: PermissionChangeLogPageQueryDto) {
    return permissionChangeLogQueryApi.post<PageResult<PermissionChangeLogListItemDto>>(
      'PermissionChangeLogPage',
      input,
    )
  },
}
