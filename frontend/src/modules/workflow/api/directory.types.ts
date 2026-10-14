import type { NumericString } from '@/api/types'

/** 选人：按关键字搜启用用户，或按主键回显已选用户 */
export interface WorkflowUserOptionQueryDto {
  keyword?: string | null
  /** 按主键取（回显已选用户；传入时忽略关键字） */
  ids?: Array<number | string> | null
  limit: number
}

export interface WorkflowUserOptionDto {
  userId: NumericString
  userName: string
  displayName: string
  avatar?: string | null
}

/** 角色 / 岗位选项（规则按编码引用） */
export interface WorkflowOrganizationOptionDto {
  code: string
  name: string
}

/** 部门树节点（规则按编码引用） */
export interface WorkflowDepartmentOptionDto {
  code: string
  name: string
  children: WorkflowDepartmentOptionDto[]
}
