import type { SysPermission } from '~/types'
import { API_CONTRACT } from '../contract'
import requestClient from '../request'

export function getPermissionListApi() {
  return requestClient.get<SysPermission[]>(API_CONTRACT.system.permissions)
}
