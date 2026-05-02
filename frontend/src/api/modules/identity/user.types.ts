import type { ApiId, BasicDto, BasicUpdateDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus } from '../shared'
import type { TenantMemberType } from '../tenant'

export { EnableStatus } from '../shared'

export enum UserGender {
  Unknown = 0,
  Male = 1,
  Female = 2,
}

export interface UserPageQueryDto extends PageRequest {
  country?: string | null
  gender?: UserGender | null
  isSystemAccount?: boolean | null
  keyword?: string | null
  language?: string | null
  status?: EnableStatus | null
}

export interface UserListItemDto extends BasicDto {
  avatar?: string | null
  country?: string | null
  createdTime: DateTimeString
  gender: UserGender
  isSystemAccount: boolean
  language?: string | null
  lastLoginTime?: DateTimeString | null
  modifiedTime?: DateTimeString | null
  nickName?: string | null
  realName?: string | null
  status: EnableStatus
  timeZone?: string | null
  userName: string
}

export interface UserDetailDto extends UserListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  remark?: string | null
}

export interface UserCreateDto {
  avatar?: string | null
  birthday?: DateTimeString | null
  country?: string | null
  displayName?: string | null
  effectiveTime?: DateTimeString | null
  email?: string | null
  expirationTime?: DateTimeString | null
  gender: UserGender
  initialPassword: string
  inviteRemark?: string | null
  language?: string | null
  memberType: TenantMemberType
  nickName?: string | null
  phone?: string | null
  realName?: string | null
  remark?: string | null
  status: EnableStatus
  timeZone?: string | null
  userName: string
}

export interface UserUpdateDto extends BasicUpdateDto {
  avatar?: string | null
  birthday?: DateTimeString | null
  country?: string | null
  email?: string | null
  gender: UserGender
  language?: string | null
  nickName?: string | null
  phone?: string | null
  realName?: string | null
  remark?: string | null
  timeZone?: string | null
}

export interface UserStatusUpdateDto extends BasicDto {
  remark?: string | null
  status: EnableStatus
}

export interface UserSelectQueryDto {
  gender?: UserGender | null
  isSystemAccount?: boolean | null
  keyword?: string | null
  limit: number
}

export interface UserSelectItemDto extends BasicDto {
  avatar?: string | null
  gender: UserGender
  isSystemAccount: boolean
  nickName?: string | null
  realName?: string | null
  userName: string
}

export interface UserPasswordResetDto {
  newPassword: string
  passwordExpiryTime?: DateTimeString | null
  remark?: string | null
  userId: ApiId
}
