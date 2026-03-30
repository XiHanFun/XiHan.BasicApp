import type { PageResult, SmsPageQuery, SysSms } from '~/types'
import { buildPageRequest, normalizePageResult, toId, toNumber } from '../helpers'
import requestClient from '../request'

const SMS_API = '/api/Sms'

function normalizeSms(raw: Record<string, any>): SysSms {
  return {
    basicId: toId(raw.basicId),
    senderId: raw.senderId === null || raw.senderId === undefined ? undefined : toId(raw.senderId),
    receiverId: raw.receiverId === null || raw.receiverId === undefined ? undefined : toId(raw.receiverId),
    smsType: toNumber(raw.smsType, 0),
    toPhone: raw.toPhone ?? '',
    content: raw.content ?? '',
    templateId: raw.templateId ?? undefined,
    templateParams: raw.templateParams ?? undefined,
    provider: raw.provider ?? undefined,
    smsStatus: toNumber(raw.smsStatus, 0),
    scheduledTime: raw.scheduledTime ?? undefined,
    sendTime: raw.sendTime ?? undefined,
    createTime: raw.createTime ?? raw.creationTime ?? raw.createdTime ?? undefined,
    updateTime: raw.updateTime ?? raw.lastModificationTime ?? undefined,
    remark: raw.remark ?? undefined,
  }
}

function toSmsCreatePayload(data: Partial<SysSms>) {
  return {
    senderId: data.senderId ?? null,
    receiverId: data.receiverId ?? null,
    smsType: toNumber(data.smsType, 0),
    toPhone: data.toPhone ?? '',
    content: data.content ?? '',
    templateId: data.templateId ? toId(data.templateId) : null,
    templateParams: data.templateParams ?? '',
    provider: data.provider ?? '',
    scheduledTime: data.scheduledTime ?? null,
    remark: data.remark ?? '',
  }
}

function toSmsUpdatePayload(id: string, data: Partial<SysSms>) {
  return {
    ...toSmsCreatePayload(data),
    smsStatus: toNumber(data.smsStatus, 0),
    sendTime: data.sendTime ?? null,
    basicId: toId(id),
  }
}

export async function getSmsPageApi(params: SmsPageQuery): Promise<PageResult<SysSms>> {
  const data = await requestClient.post<any>(
    `${SMS_API}/Page`,
    buildPageRequest(params, {
      keywordFields: ['ToPhone', 'Content'],
      filterFieldMap: {
        smsType: 'SmsType',
        smsStatus: 'SmsStatus',
      },
    }),
  )
  return normalizePageResult(data, normalizeSms)
}

export function getSmsDetailApi(id: string) {
  return requestClient
    .get<any>(`${SMS_API}/ById`, { params: { id } })
    .then(raw => normalizeSms(raw))
}

export function createSmsApi(data: Partial<SysSms>) {
  return requestClient.post<void>(`${SMS_API}/Create`, toSmsCreatePayload(data))
}

export function updateSmsApi(id: string, data: Partial<SysSms>) {
  return requestClient.put<void>(`${SMS_API}/Update`, toSmsUpdatePayload(id, data), {
    params: { id },
  })
}

export function deleteSmsApi(id: string) {
  return requestClient.delete<void>(`${SMS_API}/Delete`, {
    params: { id },
  })
}

export function getPendingSmsApi(maxCount = 100, tenantId?: number) {
  return requestClient
    .get<any[]>(`${SMS_API}/Pending/${tenantId ?? 0}`, {
      params: { maxCount },
    })
    .then(list => (Array.isArray(list) ? list.map(item => normalizeSms(item)) : []))
}
