import type { SysPermission } from '~/types'
import requestClient from '../request'
import { API_CONTRACT } from '../contract'

export function getPermissionListApi() {
  return requestClient.get<SysPermission[]>(API_CONTRACT.system.permissions)
}
