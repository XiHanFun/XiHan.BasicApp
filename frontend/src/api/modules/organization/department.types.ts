import type { ApiId, BasicDto, BasicUpdateDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus } from '../shared'

export { EnableStatus } from '../shared'

export enum DepartmentType {
  Corporation = 0,
  Headquarters = 1,
  Company = 2,
  Branch = 3,
  Division = 4,
  Center = 5,
  Department = 6,
  Section = 7,
  Team = 8,
  Group = 9,
  Project = 10,
  Workgroup = 11,
  Virtual = 12,
  Office = 13,
  Subsidiary = 14,
  Other = 99,
}

export interface DepartmentPageQueryDto extends PageRequest {
  departmentType?: DepartmentType | null
  keyword?: string | null
  leaderId?: ApiId | null
  parentId?: ApiId | null
  status?: EnableStatus | null
}

export interface DepartmentListItemDto extends BasicDto {
  createdTime: DateTimeString
  departmentCode: string
  departmentName: string
  departmentType: DepartmentType
  email?: string | null
  leaderId?: ApiId | null
  modifiedTime?: DateTimeString | null
  parentId?: ApiId | null
  phone?: string | null
  sort: number
  status: EnableStatus
}

export interface DepartmentDetailDto extends DepartmentListItemDto {
  address?: string | null
  createdBy?: string | null
  createdId?: ApiId | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  remark?: string | null
}

export interface DepartmentCreateDto {
  address?: string | null
  departmentCode: string
  departmentName: string
  departmentType: DepartmentType
  email?: string | null
  leaderId?: ApiId | null
  parentId?: ApiId | null
  phone?: string | null
  remark?: string | null
  sort: number
  status: EnableStatus
}

export interface DepartmentUpdateDto extends BasicUpdateDto {
  address?: string | null
  departmentName: string
  departmentType: DepartmentType
  email?: string | null
  leaderId?: ApiId | null
  parentId?: ApiId | null
  phone?: string | null
  remark?: string | null
  sort: number
}

export interface DepartmentStatusUpdateDto extends BasicUpdateDto {
  remark?: string | null
  status: EnableStatus
}

export interface DepartmentTreeQueryDto {
  keyword?: string | null
  limit: number
  onlyEnabled: boolean
}

export interface DepartmentTreeNodeDto extends BasicDto {
  children: DepartmentTreeNodeDto[]
  departmentCode: string
  departmentName: string
  departmentType: DepartmentType
  leaderId?: ApiId | null
  parentId?: ApiId | null
  sort: number
  status: EnableStatus
}
