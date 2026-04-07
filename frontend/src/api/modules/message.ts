import type { SysEmail } from './email'
import type { SysSms } from './sms'
import { useBaseApi } from '../base'

const api = useBaseApi('Message')

export interface MessageDispatchResult extends Record<string, any> {}

export const messageApi = {
  send: (command: Record<string, any>) =>
    api.request.post<MessageDispatchResult>(`${api.baseUrl}Send`, command),

  getPendingEmails: (maxCount = 100, tenantId?: number) =>
    api.request.get<SysEmail[]>(`${api.baseUrl}PendingEmails/${tenantId ?? 0}`, { params: { maxCount } }),

  getPendingSms: (maxCount = 100, tenantId?: number) =>
    api.request.get<SysSms[]>(`${api.baseUrl}PendingSms/${tenantId ?? 0}`, { params: { maxCount } }),

  updateEmailDispatchStatus: (command: Record<string, any>) =>
    api.request.put(`${api.baseUrl}EmailDispatchStatus`, command),

  updateSmsDispatchStatus: (command: Record<string, any>) =>
    api.request.put(`${api.baseUrl}SmsDispatchStatus`, command),
}
