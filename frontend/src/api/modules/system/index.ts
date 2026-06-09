import type { ApiId } from '../../types'
import type {
  DepartmentManagementDetailDto,
  PermissionCenterDetailDto,
  RoleManagementDetailDto,
  UserManagementDetailDto,
} from './system.types'
import { createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'
import { permissionChangeLogApi } from '../audit'
import {
  fieldLevelSecurityApi,
  operationApi,
  permissionApi,
  permissionConditionApi,
  permissionDelegationApi,
  permissionRequestApi,
  resourceApi,
  roleApi,
  roleDataScopeApi,
  roleHierarchyApi,
  rolePermissionApi,
  userDataScopeApi,
  userPermissionApi,
  userRoleApi,
} from '../authorization'
import { userApi, userSecurityApi, userSessionApi, userStatisticsApi } from '../identity'
import { messageApi, notificationApi } from '../messaging'
import { departmentApi, userDepartmentApi } from '../organization'

const userManagementQueryApi = createDynamicApiClient('UserManagementQuery')
const roleManagementQueryApi = createDynamicApiClient('RoleManagementQuery')
const departmentManagementQueryApi = createDynamicApiClient('DepartmentManagementQuery')
const permissionCenterQueryApi = createDynamicApiClient('PermissionCenterQuery')

export const userManagementApi = {
  ...userApi,
  dataScopes: userDataScopeApi,
  departments: departmentApi,
  security: userSecurityApi,
  userDepartments: userDepartmentApi,
  detailView(id: ApiId) {
    return userManagementQueryApi.get<UserManagementDetailDto | null>(
      `UserManagementDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  permissions: userPermissionApi,
  roles: userRoleApi,
  sessions: userSessionApi,
  statistics: userStatisticsApi,
}

export const roleManagementApi = {
  ...roleApi,
  dataScopes: roleDataScopeApi,
  detailView(id: ApiId) {
    return roleManagementQueryApi.get<RoleManagementDetailDto | null>(
      `RoleManagementDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  hierarchy: roleHierarchyApi,
  permissions: rolePermissionApi,
}

export const orgManagementApi = {
  ...departmentApi,
  detailView(id: ApiId) {
    return departmentManagementQueryApi.get<DepartmentManagementDetailDto | null>(
      `DepartmentManagementDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
}

export const permissionCenterApi = {
  ...permissionApi,
  changeLogs: permissionChangeLogApi,
  conditions: permissionConditionApi,
  delegations: permissionDelegationApi,
  detailView(id: ApiId) {
    return permissionCenterQueryApi.get<PermissionCenterDetailDto | null>(
      `PermissionCenterDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  fieldSecurity: fieldLevelSecurityApi,
  operations: operationApi,
  requests: permissionRequestApi,
  resources: resourceApi,
}

export const messageCenterApi = {
  ...messageApi,
  notifications: notificationApi,
}

export * from './system.types'
