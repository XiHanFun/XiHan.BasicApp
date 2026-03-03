import type { EmailPageQuery, PageResult, SysEmail } from '~/types'
import { API_CONTRACT } from '../contract'
import { normalizePageResult } from '../helpers'
import requestClient from '../request'

export async function getEmailPageApi(params: EmailPageQuery): Promise<PageResult<SysEmail>> {
  const data = await requestClient.get<any>(API_CONTRACT.system.emails, { params })
  return normalizePageResult<SysEmail>(data)
}

export function getEmailDetailApi(id: string) {
  return requestClient.get<SysEmail>(`${API_CONTRACT.system.emails}/${id}`)
}

export function createEmailApi(data: Partial<SysEmail>) {
  return requestClient.post<void>(API_CONTRACT.system.emails, data)
}

export function updateEmailApi(id: string, data: Partial<SysEmail>) {
  return requestClient.put<void>(`${API_CONTRACT.system.emails}/${id}`, data)
}

export function deleteEmailApi(id: string) {
  return requestClient.delete<void>(`${API_CONTRACT.system.emails}/${id}`)
}

export function getPendingEmailsApi(maxCount = 100, tenantId?: number) {
  return requestClient.get<SysEmail[]>(`${API_CONTRACT.system.emails}/pending`, {
    params: { maxCount, tenantId },
  })
}
