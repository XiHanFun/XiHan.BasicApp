import type { EmailPageQuery, PageResult, SysEmail } from '~/types'
import { buildPageRequest, normalizePageResult, toId, toNumber } from '../helpers'
import requestClient from '../request'

const EMAIL_API = '/api/Email'

function normalizeEmail(raw: Record<string, any>): SysEmail {
  return {
    basicId: toId(raw.basicId),
    sendUserId: raw.sendUserId === null || raw.sendUserId === undefined
      ? undefined
      : toId(raw.sendUserId),
    receiveUserId: raw.receiveUserId === null || raw.receiveUserId === undefined
      ? undefined
      : toId(raw.receiveUserId),
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

function toEmailCreatePayload(data: Partial<SysEmail>) {
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

function toEmailUpdatePayload(id: string, data: Partial<SysEmail>) {
  return {
    ...toEmailCreatePayload(data),
    emailStatus: toNumber(data.emailStatus, 0),
    sendTime: data.sendTime ?? null,
    basicId: toId(id),
  }
}

export async function getEmailPageApi(params: EmailPageQuery): Promise<PageResult<SysEmail>> {
  const data = await requestClient.post<any>(
    `${EMAIL_API}/Page`,
    buildPageRequest(params, {
      keywordFields: ['FromEmail', 'ToEmail', 'Subject'],
      filterFieldMap: {
        emailType: 'EmailType',
        emailStatus: 'EmailStatus',
      },
    }),
  )
  return normalizePageResult(data, normalizeEmail)
}

export function getEmailDetailApi(id: string) {
  return requestClient
    .get<any>(`${EMAIL_API}/ById`, { params: { id } })
    .then(raw => normalizeEmail(raw))
}

export function createEmailApi(data: Partial<SysEmail>) {
  return requestClient.post<void>(`${EMAIL_API}/Create`, toEmailCreatePayload(data))
}

export function updateEmailApi(id: string, data: Partial<SysEmail>) {
  return requestClient.put<void>(`${EMAIL_API}/Update`, toEmailUpdatePayload(id, data), {
    params: { id },
  })
}

export function deleteEmailApi(id: string) {
  return requestClient.delete<void>(`${EMAIL_API}/Delete`, {
    params: { id },
  })
}

export function getPendingEmailsApi(maxCount = 100, tenantId?: number) {
  return requestClient
    .get<any[]>(`${EMAIL_API}/Pending/${tenantId ?? 0}`, {
      params: { maxCount },
    })
    .then(list => (Array.isArray(list) ? list.map(item => normalizeEmail(item)) : []))
}
