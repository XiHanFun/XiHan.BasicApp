import type { SysDepartment } from '~/types'
import { API_CONTRACT } from '../contract'
import requestClient from '../request'

export function getDepartmentTreeApi() {
  return requestClient.get<SysDepartment[]>(`${API_CONTRACT.system.departments}/tree`)
}
