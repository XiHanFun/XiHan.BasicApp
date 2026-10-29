import type { SchemaResource } from '~/components'
import type { PageQueryMapping, ResponseOf } from '~/contracts'
import { sessions, toPageRequest, toSchemaPageResult } from '~/contracts'

export type OnlineSessionRow = ResponseOf<'sessions.list'>['items'][number]
export type SessionSummary = ResponseOf<'sessions.summary'>

/** 设备类型：后端由 User-Agent 粗分（认不出为空） */
export const DEVICE_TYPES = ['Desktop', 'Mobile', 'Tablet', 'Bot'] as const

/** 列表的筛选登记：设备多选（in），浏览器、系统、用户精确匹配；关键字匹配登录名与显示名 */
export const ONLINE_SESSION_QUERY: PageQueryMapping = {
  multiples: ['device'],
  filters: { browser: 'eq', os: 'eq', userId: 'eq' },
}

export function onlineSessionsResource(): SchemaResource<OnlineSessionRow> {
  return { page: async params => toSchemaPageResult(await sessions.list(toPageRequest(params, ONLINE_SESSION_QUERY)), params) }
}

/** 本次会话在线时长（秒）：登录时间至今；时钟偏差导致的负数按 0 计 */
export function onlineSeconds(row: Pick<OnlineSessionRow, 'loginAt'>, now: number = Date.now()): number {
  return Math.max(0, Math.floor((now - new Date(row.loginAt).getTime()) / 1000))
}

/** 时长文案：不足一分钟按秒、不足一小时按分、不足一天按时分、其余按天时（与 4.x 相同） */
export function formatDuration(seconds: number, t: (key: string, values?: Record<string, unknown>) => string): string {
  if (seconds < 60)
    return t('identity.online_users.duration_seconds', { n: seconds })
  if (seconds < 3600)
    return t('identity.online_users.duration_minutes', { n: Math.floor(seconds / 60) })
  if (seconds < 86400)
    return t('identity.online_users.duration_hours', { h: Math.floor(seconds / 3600), m: Math.floor((seconds % 3600) / 60) })
  return t('identity.online_users.duration_days', { d: Math.floor(seconds / 86400), h: Math.floor((seconds % 86400) / 3600) })
}

/** 展示名：显示名为空时退回登录名 */
export function sessionUserName(row: Pick<OnlineSessionRow, 'displayName' | 'userName' | 'userId'>): string {
  return row.displayName || row.userName || `#${row.userId}`
}

export function loadSessionSummary(): Promise<SessionSummary> {
  return sessions.summary({})
}

/** 强制下线一条会话；逐项拒绝（已失效、是自己当前的会话）按错误码抛出 */
export async function revokeSession(id: string, reason: string): Promise<void> {
  const result = await sessions.revoke({ ids: [id], reason })
  if (result.failed.length > 0)
    throw new Error(result.failed.map(f => f.code).join('; '))
}
