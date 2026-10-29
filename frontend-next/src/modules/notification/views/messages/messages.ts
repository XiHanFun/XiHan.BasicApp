import type { Translate } from '../../labels'
import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { MessageSendStatus, PageQueryMapping, ResponseOf } from '~/contracts'
import { emails as emailsApi, sms as smsApi, toPageRequest, toSchemaPageResult } from '~/contracts'
import { assertBatch } from '../../labels'

export type EmailRecordRow = ResponseOf<'emails.list'>['items'][number]
export type SmsRecordRow = ResponseOf<'sms.list'>['items'][number]
/** 详情抽屉显示的是哪一种记录 */
export type MessageTab = 'email' | 'sms'

/**
 * 两种记录共用的后端筛选：关键字（邮件匹配收件地址与主题，短信匹配手机号与正文）；状态多选（合成 in）；
 * 模板编码与业务类型精确匹配；写入时间按区间（拆成 ge / le）。可排序的只有写入时间与发出时间。
 */
export const RECORD_QUERY: PageQueryMapping = {
  multiples: ['status'],
  ranges: ['createdAt'],
  filters: { templateCode: 'eq', businessType: 'eq' },
}

/** 重发：排队中与发送中的不行（后端 NOTIFICATION_CONFLICT_006）；成功、失败与已取消的都可以重新排队 */
export function canResend(status: MessageSendStatus): boolean {
  return status !== 'Pending' && status !== 'Sending'
}

/** 取消：只有还在排队的（NOTIFICATION_CONFLICT_007） */
export function canCancel(status: MessageSendStatus): boolean {
  return status === 'Pending'
}

/** 删除：发送中的不行（NOTIFICATION_CONFLICT_006） */
export function canDelete(status: MessageSendStatus): boolean {
  return status !== 'Sending'
}

/** 尝试次数：已尝试 / 最多尝试（4.x 的「重试」列） */
export function formatAttempts(row: { attempts: number, maxAttempts: number }): string {
  return `${row.attempts}/${row.maxAttempts}`
}

/** SchemaPage 的资源：分页与删除（逐项拒绝的原因拼成一条报错） */
export function emailRecordResource(t: Translate): SchemaResource<EmailRecordRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(await emailsApi.list(toPageRequest(params, RECORD_QUERY)), params),
    remove: async id => assertBatch(t, await emailsApi.delete({ ids: [String(id)] })),
  }
}

export function smsRecordResource(t: Translate): SchemaResource<SmsRecordRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(await smsApi.list(toPageRequest(params, RECORD_QUERY)), params),
    remove: async id => assertBatch(t, await smsApi.delete({ ids: [String(id)] })),
  }
}

/** 重发（重新排队、清零尝试次数）与取消：批量接口逐项拒绝即抛错 */
export async function resendRecord(t: Translate, tab: MessageTab, id: string): Promise<void> {
  assertBatch(t, tab === 'email' ? await emailsApi.resend({ ids: [id] }) : await smsApi.resend({ ids: [id] }))
}

export async function cancelRecord(t: Translate, tab: MessageTab, id: string): Promise<void> {
  assertBatch(t, tab === 'email' ? await emailsApi.cancel({ ids: [id] }) : await smsApi.cancel({ ids: [id] }))
}

/** 短信模板参数（JSON）排版成多行；不是合法 JSON 的原样返回 */
export function prettyJson(value: null | string | undefined): string {
  if (!value)
    return ''
  try {
    return JSON.stringify(JSON.parse(value), null, 2)
  }
  catch {
    return value
  }
}
