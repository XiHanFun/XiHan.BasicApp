import type { ApiId, BasicDto } from '../../types'
import type { EnableStatus } from '../shared'

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
