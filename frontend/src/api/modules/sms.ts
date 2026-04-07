import type { AnyRecord } from '../helpers'
import type { PageQuery } from '~/types'
import { useBaseApi } from '../base'
import { toId, toNumber } from '../helpers'

const api = useBaseApi('Sms')

export interface SysSms {
  basicId: string
  senderId?: string
  receiverId?: string
  smsType: number
  toPhone: string
  content: string
  templateId?: string
  templateParams?: string
  provider?: string
  smsStatus: number
  scheduledTime?: string
  sendTime?: string
  createTime?: string
  updateTime?: string
  remark?: string
}

export interface SmsPageQuery extends PageQuery {
  smsType?: number
  smsStatus?: number
}

function normalizeSms(raw: Record<string, any>): SysSms {
  return {
    basicId: toId(raw.basicId),
    senderId: raw.senderId == null ? undefined : toId(raw.senderId),
    receiverId: raw.receiverId == null ? undefined : toId(raw.receiverId),
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

function toCreatePayload(data: Partial<SysSms>) {
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

function toUpdatePayload(id: string, data: Partial<SysSms>) {
  return {
    ...toCreatePayload(data),
    smsStatus: toNumber(data.smsStatus, 0),
    sendTime: data.sendTime ?? null,
    basicId: toId(id),
  }
}

export const smsApi = {
  page: (params: SmsPageQuery) =>
    api.page(params, {
      keywordFields: ['ToPhone', 'Content'],
      filterFieldMap: { smsType: 'SmsType', smsStatus: 'SmsStatus' },
    }).then(res => ({
      total: res.total,
      items: (res.items as AnyRecord[]).map(item => normalizeSms(item)),
    })),

  detail: (id: string) =>
    api.request.get<any>(`${api.baseUrl}ById`, { params: { id } }).then(normalizeSms),

  create: (data: Partial<SysSms>) => api.create(toCreatePayload(data)),

  update: (id: string, data: Partial<SysSms>) =>
    api.request.put(`${api.baseUrl}Update`, toUpdatePayload(id, data), { params: { id } }),

  delete: (id: string) => api.delete(id),

  getPending: (maxCount = 100, tenantId?: number) =>
    api.request.get<any[]>(`${api.baseUrl}Pending/${tenantId ?? 0}`, { params: { maxCount } })
      .then((list: any[]) => Array.isArray(list) ? list.map(normalizeSms) : []),
}
