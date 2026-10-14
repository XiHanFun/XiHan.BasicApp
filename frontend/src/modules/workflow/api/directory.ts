import type {
  WorkflowDepartmentOptionDto,
  WorkflowOrganizationOptionDto,
  WorkflowUserOptionDto,
  WorkflowUserOptionQueryDto,
} from './directory.types'
import { createDynamicApiClient } from '@/api/base'

const query = createDynamicApiClient('WorkflowDirectoryQuery')

/**
 * 审批人选择（设计器配置审批人、待办转办/加签选人）。
 * 选用户登录即可；角色/岗位/部门只在设计流程时用，需要流程查看权限。
 */
export const workflowDirectoryApi = {
  userOptions(input: WorkflowUserOptionQueryDto) {
    return query.post<WorkflowUserOptionDto[]>('UserOptions', input)
  },
  roleOptions() {
    return query.get<WorkflowOrganizationOptionDto[]>('RoleOptions')
  },
  positionOptions() {
    return query.get<WorkflowOrganizationOptionDto[]>('PositionOptions')
  },
  departmentTree() {
    return query.get<WorkflowDepartmentOptionDto[]>('DepartmentTree')
  },
}
