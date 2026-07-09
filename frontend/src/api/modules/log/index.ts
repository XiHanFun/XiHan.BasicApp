import {
  accessLogApi,
  apiLogApi,
  diffLogApi,
  exceptionLogApi,
  loginLogApi,
  operationLogApi,
  permissionChangeLogApi,
  traceLogApi,
} from '../audit'

export const logManagementApi = {
  access: accessLogApi,
  api: apiLogApi,
  diff: diffLogApi,
  exception: exceptionLogApi,
  login: loginLogApi,
  operation: operationLogApi,
  permissionChanges: permissionChangeLogApi,
  trace: traceLogApi,
}
