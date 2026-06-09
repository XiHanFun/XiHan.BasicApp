import {
  accessLogApi,
  apiLogApi,
  diffLogApi,
  exceptionLogApi,
  loginLogApi,
  operationLogApi,
  permissionChangeLogApi,
} from '../audit'

export const logManagementApi = {
  access: accessLogApi,
  api: apiLogApi,
  diff: diffLogApi,
  exception: exceptionLogApi,
  login: loginLogApi,
  operation: operationLogApi,
  permissionChanges: permissionChangeLogApi,
}
