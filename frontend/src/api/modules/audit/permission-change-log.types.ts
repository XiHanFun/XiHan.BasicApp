import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'

export enum PermissionChangeType {
  RoleGrantPermission = 0,
  RoleRevokePermission = 1,
  UserGrantPermission = 2,
  UserRevokePermission = 3,
  UserAssignRole = 4,
  UserRemoveRole = 5,
  UserDenyPermission = 6,
  RoleDenyPermission = 7,
}

export interface PermissionChangeLogPageQueryDto extends PageRequest {
  changeTimeEnd?: DateTimeString | null
  changeTimeStart?: DateTimeString | null
  changeType?: PermissionChangeType | null
  keyword?: string | null
  operatorUserId?: ApiId | null
  permissionId?: ApiId | null
  targetRoleId?: ApiId | null
  targetUserId?: ApiId | null
  traceId?: string | null
}

export interface PermissionChangeLogListItemDto extends BasicDto {
  changeReason?: string | null
  changeTime: DateTimeString
  changeType: PermissionChangeType
  createdTime: DateTimeString
  description?: string | null
  operationIp?: string | null
  operatorUserId?: ApiId | null
  permissionId?: ApiId | null
  targetRoleId?: ApiId | null
  targetUserId?: ApiId | null
  traceId?: string | null
}

export interface PermissionChangeLogDetailDto extends PermissionChangeLogListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
}
