import type { AnyRecord } from '../helpers'
import type { PageQuery } from '~/types'
import { useBaseApi } from '../base'
import { toId, toNumber } from '../helpers'

const api = useBaseApi('Email')

export interface SysEmail {
  basicId: string
  sendUserId?: string
  receiveUserId?: string
  emailType: number
  fromEmail: string
  toEmail: string
  subject: string
  content?: string
  isHtml: boolean
  emailStatus: number
  scheduledTime?: string
  sendTime?: string
  createTime?: string
  updateTime?: string
  remark?: string
}

export interface EmailPageQuery extends PageQuery {
  emailType?: number
  emailStatus?: number
}

function normalizeEmail(raw: Record<string, any>): SysEmail {
  return {
    basicId: toId(raw.basicId),
    sendUserId: raw.sendUserId == null ? undefined : toId(raw.sendUserId),
    receiveUserId: raw.receiveUserId == null ? undefined : toId(raw.receiveUserId),
    emailType: toNumber(raw.emailType, 0),
    fromEmail: raw.fromEmail ?? '',
    toEmail: raw.toEmail ?? '',
    subject: raw.subject ?? '',
    content: raw.content ?? '',
    isHtml: Boolean(raw.isHtml),
    emailStatus: toNumber(raw.emailStatus, 0),
    scheduledTime: raw.scheduledTime ?? undefined,
    sendTime: raw.sendTime ?? undefined,
    createTime: raw.createTime ?? raw.creationTime ?? raw.createdTime ?? undefined,
    updateTime: raw.updateTime ?? raw.lastModificationTime ?? undefined,
    remark: raw.remark ?? undefined,
  }
}

function toCreatePayload(data: Partial<SysEmail>) {
  return {
    sendUserId: data.sendUserId ?? null,
    receiveUserId: data.receiveUserId ?? null,
    emailType: toNumber(data.emailType, 0),
    fromEmail: data.fromEmail ?? '',
    toEmail: data.toEmail ?? '',
    subject: data.subject ?? '',
    content: data.content ?? '',
    isHtml: Boolean(data.isHtml),
    scheduledTime: data.scheduledTime ?? null,
    remark: data.remark ?? '',
  }
}

function toUpdatePayload(id: string, data: Partial<SysEmail>) {
  return {
    ...toCreatePayload(data),
    emailStatus: toNumber(data.emailStatus, 0),
    sendTime: data.sendTime ?? null,
    basicId: toId(id),
  }
}

export const emailApi = {
  page: (params: EmailPageQuery) =>
    api.page(params, {
      keywordFields: ['FromEmail', 'ToEmail', 'Subject'],
      filterFieldMap: { emailType: 'EmailType', emailStatus: 'EmailStatus' },
    }).then(res => ({
      total: res.total,
      items: (res.items as AnyRecord[]).map(item => normalizeEmail(item)),
    })),

  detail: (id: string) =>
    api.request.get<any>(`${api.baseUrl}ById`, { params: { id } }).then(normalizeEmail),

  create: (data: Partial<SysEmail>) => api.create(toCreatePayload(data)),

  update: (id: string, data: Partial<SysEmail>) =>
    api.request.put(`${api.baseUrl}Update`, toUpdatePayload(id, data), { params: { id } }),

  delete: (id: string) => api.delete(id),

  getPending: (maxCount = 100, tenantId?: number) =>
    api.request.get<any[]>(`${api.baseUrl}Pending/${tenantId ?? 0}`, { params: { maxCount } })
      .then((list: any[]) => Array.isArray(list) ? list.map(normalizeEmail) : []),
}
