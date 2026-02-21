import type { SysDepartment } from '~/types'
import requestClient from '../request'
import { API_CONTRACT } from '../contract'

export function getDepartmentTreeApi() {
  return requestClient.get<SysDepartment[]>(`${API_CONTRACT.system.departments}/tree`)
}
