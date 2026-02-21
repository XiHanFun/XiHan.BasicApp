import type { PageResult, SysTenant } from '~/types'
import requestClient from '../request'
import { API_CONTRACT } from '../contract'
import { normalizePageResult } from '../helpers'

export async function getTenantPageApi(params: Record<string, any>): Promise<PageResult<SysTenant>> {
  const data = await requestClient.get<any>(API_CONTRACT.system.tenants, { params })
  return normalizePageResult<SysTenant>(data)
}
