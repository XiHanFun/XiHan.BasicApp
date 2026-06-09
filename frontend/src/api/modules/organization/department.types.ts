import type { ApiId, BasicDto, BasicUpdateDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus } from '../shared'

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum DepartmentType {
  Corporation = 'Corporation',
  Headquarters = 'Headquarters',
  Company = 'Company',
  Branch = 'Branch',
  Division = 'Division',
  Center = 'Center',
  Department = 'Department',
  Section = 'Section',
  Team = 'Team',
  Group = 'Group',
  Project = 'Project',
  Workgroup = 'Workgroup',
  Virtual = 'Virtual',
  Office = 'Office',
  Subsidiary = 'Subsidiary',
  Other = 'Other',
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
  leaderId?: ApiId | null
  parentId?: ApiId | null
  sort: number
  status: EnableStatus
}

export interface DepartmentDetailDto extends DepartmentListItemDto {
  address?: string | null
  createdBy?: string | null
  createdId?: ApiId | null
  email?: string | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  phone?: string | null
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
  status: EnableStatus
}

export interface DepartmentStatusUpdateDto extends BasicDto {
  remark?: string | null
  status: EnableStatus
}

export interface DepartmentTreeQueryDto {
  keyword?: string | null
  limit: number
  onlyEnabled?: boolean | null
}

export interface DepartmentTreeNodeDto extends DepartmentListItemDto {
  children?: DepartmentTreeNodeDto[] | null
}
