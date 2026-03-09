import type { MessageDispatchResult, SysEmail, SysSms } from '~/types'
import requestClient from '../request'

const MESSAGE_API = '/api/Message'

export function sendMessageApi(command: Record<string, any>) {
  return requestClient.post<MessageDispatchResult>(`${MESSAGE_API}/Send`, command)
}

export function getPendingEmailsByMessageApi(maxCount = 100, tenantId?: number) {
  return requestClient.get<SysEmail[]>(`${MESSAGE_API}/PendingEmails/${tenantId ?? 0}`, {
    params: { maxCount },
  })
}

export function getPendingSmsByMessageApi(maxCount = 100, tenantId?: number) {
  return requestClient.get<SysSms[]>(`${MESSAGE_API}/PendingSms/${tenantId ?? 0}`, {
    params: { maxCount },
  })
}

export function updateEmailDispatchStatusApi(command: Record<string, any>) {
  return requestClient.put<void>(`${MESSAGE_API}/EmailDispatchStatus`, command)
}

export function updateSmsDispatchStatusApi(command: Record<string, any>) {
  return requestClient.put<void>(`${MESSAGE_API}/SmsDispatchStatus`, command)
}
