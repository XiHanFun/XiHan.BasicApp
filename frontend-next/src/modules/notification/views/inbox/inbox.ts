import type { NotificationType, RequestOf, ResponseOf } from '~/contracts'
import type { NotificationItem } from '~/stores'
import type { NotificationContentFormat as ShellContentFormat, NotificationStatus as ShellStatus, NotificationType as ShellType } from '~/types/enums'
import { inbox as inboxApi } from '~/contracts'

export type InboxItem = ResponseOf<'inbox.list'>['items'][number]
export type InboxPage = ResponseOf<'inbox.list'>
/** 范围：全部，或只看待处理（未读或需要确认而未确认） */
export type InboxScope = 'all' | 'pending'

/** 一页多少条 */
export const INBOX_PAGE_SIZE = 20
/** 计数与顶栏同步取最近多少条待处理的（与顶栏通知中心、4.x 收件箱同为 100 条） */
export const PENDING_SAMPLE_SIZE = 100

export interface InboxQuery {
  type: NotificationType | null
  scope: InboxScope
  page: number
}

/** 列表请求：类型精确匹配、待处理走 `pending eq true`，按发布时间倒序（后端缺省） */
export function inboxRequest(query: InboxQuery, size: number = INBOX_PAGE_SIZE): RequestOf<'inbox.list'> {
  const filters: NonNullable<RequestOf<'inbox.list'>['filters']> = []
  if (query.type)
    filters.push({ field: 'notificationType', operator: 'eq', value: query.type })
  if (query.scope === 'pending')
    filters.push({ field: 'pending', operator: 'eq', value: 'true' })
  return { page: { index: query.page, size }, filters, sorts: [], keyword: null }
}

export function loadInbox(query: InboxQuery): Promise<InboxPage> {
  return inboxApi.list(inboxRequest(query))
}

/** 最近的待处理条目：页头的未读 / 待确认计数由它算，也拿它刷新顶栏通知中心 */
export async function loadPending(): Promise<InboxItem[]> {
  return (await inboxApi.list(inboxRequest({ type: null, scope: 'pending', page: 1 }, PENDING_SAMPLE_SIZE))).items
}

export function isUnread(item: Pick<InboxItem, 'status'>): boolean {
  return item.status === 'Unread'
}

/** 需要确认而还没确认 */
export function isUnconfirmed(item: Pick<InboxItem, 'confirmedAt' | 'needConfirm'>): boolean {
  return item.needConfirm && !item.confirmedAt
}

/** 需要关注：未读，或需要确认而还没确认（4.x 同） */
export function needsAttention(item: Pick<InboxItem, 'confirmedAt' | 'needConfirm' | 'status'>): boolean {
  return isUnread(item) || isUnconfirmed(item)
}

export function inboxCounts(items: readonly InboxItem[]): { unread: number, confirm: number } {
  return { unread: items.filter(isUnread).length, confirm: items.filter(isUnconfirmed).length }
}

/** 收件箱条目 → 顶栏通知中心的条目（字段名是 4.x 的；枚举名称与壳层的字符串枚举同值） */
export function toHeaderItem(item: InboxItem): NotificationItem {
  return {
    basicId: item.id,
    title: item.title,
    content: item.content ?? undefined,
    contentFormat: item.contentFormat as ShellContentFormat,
    notificationType: item.notificationType as ShellType,
    notificationStatus: item.status as ShellStatus,
    sendTime: item.publishedAt,
    readTime: item.readAt ?? undefined,
    confirmTime: item.confirmedAt ?? undefined,
    isGlobal: item.isGlobal,
    needConfirm: item.needConfirm,
    icon: item.icon ?? undefined,
    link: item.link ?? undefined,
  }
}

/** 批量操作只回失败项的错误码：认得的翻成文案，其余原样 */
function throwOnFailures(result: { failed: { code: string }[] }, notFound: string): void {
  if (result.failed.length > 0)
    throw new Error(result.failed.map(failure => (failure.code === 'NOTIFICATION_NOTFOUND_002' ? notFound : failure.code)).join('; '))
}

/** 标为已读；`notFound` 是条目已不在收件箱时的文案 */
export async function markRead(id: string, notFound: string): Promise<void> {
  throwOnFailures(await inboxApi.markRead({ ids: [id] }), notFound)
}

/** 从本人收件箱删除（通知本身不受影响） */
export async function deleteItem(id: string, notFound: string): Promise<void> {
  throwOnFailures(await inboxApi.delete({ ids: [id] }), notFound)
}

export async function confirmItem(id: string): Promise<void> {
  await inboxApi.confirm({ id })
}

/** 把本人当前一侧可见的未读全部标为已读，返回标了几条 */
export async function markAllRead(): Promise<number> {
  return (await inboxApi.markAllRead({})).count
}

/** 页码越界（删掉末页最后一条）时退回到最后一页 */
export function clampPage(page: number, total: number, size: number = INBOX_PAGE_SIZE): number {
  return Math.min(page, Math.max(1, Math.ceil(total / size)))
}

/**
 * 通知链接的去向：`/` 开头的站内路由走路由跳转，http(s) 地址在新窗口打开（4.x 同）；
 * 其它协议（javascript: 之类）不当链接，按钮不出。
 */
export function resolveLink(link: null | string | undefined): { kind: 'external', url: string } | { kind: 'route', path: string } | null {
  const value = link?.trim()
  if (!value)
    return null
  if (value.startsWith('/') && !value.startsWith('//'))
    return { kind: 'route', path: value }
  try {
    const url = new URL(value)
    return url.protocol === 'http:' || url.protocol === 'https:' ? { kind: 'external', url: url.href } : null
  }
  catch {
    return null
  }
}

/** 按通知类型给图标着色：语气与类型标签一致；待办的标签是 neutral，图标保留紫色好与系统类区分（4.x 同） */
export function typeColor(type: NotificationType): string {
  switch (type) {
    case 'Security': return 'var(--xh-color-warning-600)'
    case 'Business': return 'var(--xh-color-success-600)'
    case 'Todo': return 'var(--xh-color-purple-500)'
    case 'Emergency': return 'var(--xh-color-danger-600)'
    default: return 'var(--xh-color-info-600)'
  }
}
