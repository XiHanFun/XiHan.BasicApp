import type { ApiId, BasicDto, DateTimeString } from '../../types'
import type { EnableStatus, PermissionType, ValidityStatus } from '../shared'

export interface TenantEditionPermissionListItemDto extends BasicDto {
  createdTime: DateTimeString
  editionId: ApiId
  isGlobalPermission?: boolean | null
  isRequireAudit?: boolean | null
  moduleCode?: string | null
  permissionCode?: string | null
  permissionId: ApiId
  permissionName?: string | null
  permissionStatus?: EnableStatus | null
  permissionType?: PermissionType | null
  remark?: string | null
  status: ValidityStatus
}

export interface TenantEditionPermissionDetailDto extends TenantEditionPermissionListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  permissionDescription?: string | null
  permissionPriority?: number | null
  tags?: string | null
}

export interface TenantEditionPermissionGrantDto {
  editionId: ApiId
  permissionId: ApiId
  remark?: string | null
}

export interface TenantEditionPermissionStatusUpdateDto extends BasicDto {
  remark?: string | null
  status: ValidityStatus
}
