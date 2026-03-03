import type { ConfigPageQuery, PageResult, SysConfig } from '~/types'
import { API_CONTRACT } from '../contract'
import { normalizePageResult } from '../helpers'
import requestClient from '../request'

export async function getConfigPageApi(params: ConfigPageQuery): Promise<PageResult<SysConfig>> {
  const data = await requestClient.get<any>(API_CONTRACT.system.configs, { params })
  return normalizePageResult<SysConfig>(data)
}

export function getConfigDetailApi(id: string) {
  return requestClient.get<SysConfig>(`${API_CONTRACT.system.configs}/${id}`)
}

export function createConfigApi(data: Partial<SysConfig>) {
  return requestClient.post<void>(API_CONTRACT.system.configs, data)
}

export function updateConfigApi(id: string, data: Partial<SysConfig>) {
  return requestClient.put<void>(`${API_CONTRACT.system.configs}/${id}`, data)
}

export function deleteConfigApi(id: string) {
  return requestClient.delete<void>(`${API_CONTRACT.system.configs}/${id}`)
}

export function getConfigByKeyApi(configKey: string, tenantId?: number) {
  return requestClient.get<SysConfig | null>(`${API_CONTRACT.system.configs}/by-key`, {
    params: { configKey, tenantId },
  })
}
