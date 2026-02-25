import type { PageResult, SysTenant } from '~/types'
import { API_CONTRACT } from '../contract'
import { normalizePageResult } from '../helpers'
import requestClient from '../request'

export async function getTenantPageApi(
  params: Record<string, any>,
): Promise<PageResult<SysTenant>> {
  const data = await requestClient.get<any>(API_CONTRACT.system.tenants, { params })
  return normalizePageResult<SysTenant>(data)
}
