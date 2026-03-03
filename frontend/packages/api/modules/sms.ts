import type { PageResult, SmsPageQuery, SysSms } from '~/types'
import { API_CONTRACT } from '../contract'
import { normalizePageResult } from '../helpers'
import requestClient from '../request'

export async function getSmsPageApi(params: SmsPageQuery): Promise<PageResult<SysSms>> {
  const data = await requestClient.get<any>(API_CONTRACT.system.sms, { params })
  return normalizePageResult<SysSms>(data)
}

export function getSmsDetailApi(id: string) {
  return requestClient.get<SysSms>(`${API_CONTRACT.system.sms}/${id}`)
}

export function createSmsApi(data: Partial<SysSms>) {
  return requestClient.post<void>(API_CONTRACT.system.sms, data)
}

export function updateSmsApi(id: string, data: Partial<SysSms>) {
  return requestClient.put<void>(`${API_CONTRACT.system.sms}/${id}`, data)
}

export function deleteSmsApi(id: string) {
  return requestClient.delete<void>(`${API_CONTRACT.system.sms}/${id}`)
}

export function getPendingSmsApi(maxCount = 100, tenantId?: number) {
  return requestClient.get<SysSms[]>(`${API_CONTRACT.system.sms}/pending`, {
    params: { maxCount, tenantId },
  })
}
