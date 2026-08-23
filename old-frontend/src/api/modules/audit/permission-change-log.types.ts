import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum PermissionChangeType {
  RoleGrantPermission = 'RoleGrantPermission',
  RoleRevokePermission = 'RoleRevokePermission',
  UserGrantPermission = 'UserGrantPermission',
  UserRevokePermission = 'UserRevokePermission',
  UserAssignRole = 'UserAssignRole',
  UserRemoveRole = 'UserRemoveRole',
  UserDenyPermission = 'UserDenyPermission',
  RoleDenyPermission = 'RoleDenyPermission',
  UserDelegateGrant = 'UserDelegateGrant',
  UserDelegateRevoke = 'UserDelegateRevoke',
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
  operatorUserName?: string | null
  permissionId?: ApiId | null
  permissionName?: string | null
  targetRoleId?: ApiId | null
  targetRoleName?: string | null
  targetUserId?: ApiId | null
  targetUserName?: string | null
  traceId?: string | null
}

export interface PermissionChangeLogDetailDto extends PermissionChangeLogListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
}
