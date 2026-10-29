import type { Translate } from '../../labels'
import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { NotificationContentFormat, NotificationPriority, NotificationRecipientFilter, NotificationTargetType, NotificationType, PageQueryMapping, RequestOf, ResponseOf } from '~/contracts'
import { departments as departmentsApi, notifications as notificationsApi, roles as rolesApi, toPageRequest, toSchemaPageResult } from '~/contracts'

export type NotificationRow = ResponseOf<'notifications.list'>['items'][number]
export type NotificationDetail = ResponseOf<'notifications.get'>
export type ReadStats = ResponseOf<'notifications.read-stats'>
export type RecipientRow = ResponseOf<'notifications.recipients'>['items'][number]
export type RecipientPage = ResponseOf<'notifications.recipients'>
type DepartmentNode = ResponseOf<'departments.tree'>['items'][number]

/** 列表的后端筛选：关键字匹配标题与正文；类型多选（合成 in），是否发布按 true / false 精确匹配 */
export const NOTIFICATION_QUERY: PageQueryMapping = {
  multiples: ['notificationType'],
  filters: { isPublished: 'eq' },
}

/** 运营数据抽屉里接收人列表的页大小（4.x 同） */
export const RECIPIENT_PAGE_SIZE = 10
/** 导出未读至多多少人（4.x 同） */
export const EXPORT_LIMIT = 1000
/** 后端 `PageInfo.MaxSize`：导出与目标候选一次取的上限 */
const PAGE_MAX_SIZE = 500

/** 角色、部门与用户目标的 ID 是 GUID（4.x 是正整数） */
const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

/** 投递目标要不要选具体对象：全员不要，角色、部门、用户至少一个 */
export function needsTargets(type: NotificationTargetType): boolean {
  return type !== 'All'
}

/** 编辑弹窗的表单（时间是毫秒时间戳，喂日期选择器） */
export interface NotificationForm {
  id: null | string
  title: string
  content: string
  contentFormat: NotificationContentFormat
  notificationType: NotificationType
  priority: NotificationPriority
  targetType: NotificationTargetType
  /** 角色、部门或用户的 ID；全员时不带 */
  targetIds: string[]
  icon: string
  link: string
  startsAt: null | number
  expiresAt: null | number
  needConfirm: boolean
  isMandatory: boolean
  isBanner: boolean
  isPopup: boolean
  // 表单不暴露、编辑时原样带回（4.x 同），免得整体覆盖时丢掉
  businessType: null | string
  businessId: null | string
  remark: null | string
}

export function emptyNotificationForm(): NotificationForm {
  return {
    id: null,
    title: '',
    content: '',
    contentFormat: 'Markdown',
    notificationType: 'System',
    priority: 'Normal',
    targetType: 'All',
    targetIds: [],
    icon: '',
    link: '',
    startsAt: null,
    expiresAt: null,
    needConfirm: false,
    isMandatory: false,
    isBanner: false,
    isPopup: false,
    businessType: null,
    businessId: null,
    remark: null,
  }
}

function toTimestamp(value: null | string | undefined): null | number {
  if (!value)
    return null
  const time = Date.parse(value)
  return Number.isNaN(time) ? null : time
}

/** 草稿回填表单：6.0 的详情带投递目标，编辑时原目标一并回填（4.x 要重新填写） */
export function toNotificationForm(detail: NotificationDetail): NotificationForm {
  return {
    id: detail.id,
    title: detail.title,
    content: detail.content ?? '',
    contentFormat: detail.contentFormat,
    notificationType: detail.notificationType,
    priority: detail.priority,
    targetType: detail.targetType,
    targetIds: needsTargets(detail.targetType) ? [...detail.targetIds] : [],
    icon: detail.icon ?? '',
    link: detail.link ?? '',
    startsAt: toTimestamp(detail.startsAt),
    expiresAt: toTimestamp(detail.expiresAt),
    needConfirm: detail.needConfirm,
    isMandatory: detail.isMandatory,
    isBanner: detail.isBanner,
    isPopup: detail.isPopup,
    businessType: detail.businessType ?? null,
    businessId: detail.businessId ?? null,
    remark: detail.remark ?? null,
  }
}

/** 表单的第一处问题（4.x 的提交前校验；用户 ID 由正整数改为 GUID）；都好为空 */
export type NotificationFormProblem = 'department_required' | 'role_required' | 'title' | 'user_id_invalid' | 'user_required'

export function findNotificationProblem(form: NotificationForm): NotificationFormProblem | null {
  if (!form.title.trim())
    return 'title'
  if (!needsTargets(form.targetType))
    return null
  const ids = form.targetIds.map(id => id.trim()).filter(Boolean)
  if (ids.length === 0) {
    switch (form.targetType) {
      case 'Role': return 'role_required'
      case 'Department': return 'department_required'
      default: return 'user_required'
    }
  }
  // 用户 ID 是手填的才逐个核对格式；角色与部门是选出来的
  if (form.targetType === 'User' && ids.some(id => !GUID.test(id)))
    return 'user_id_invalid'
  return null
}

export function problemMessage(t: Translate, problem: NotificationFormProblem): string {
  switch (problem) {
    case 'title': return t('notification.notifications.msg_title_required')
    case 'user_required': return t('notification.notifications.msg_user_required')
    case 'role_required': return t('notification.notifications.msg_role_required')
    case 'department_required': return t('notification.notifications.msg_department_required')
    case 'user_id_invalid': return t('notification.notifications.msg_user_id_invalid')
  }
}

function blankToNull(value: null | string): null | string {
  return value?.trim() || null
}

function toIso(value: null | number): null | string {
  return value === null ? null : new Date(value).toISOString()
}

/** 新建与修改共用的字段；目标 ID 去空白、去重，全员时为空 */
function commonBody(form: NotificationForm) {
  return {
    title: form.title.trim(),
    content: blankToNull(form.content),
    contentFormat: form.contentFormat,
    notificationType: form.notificationType,
    priority: form.priority,
    targetType: form.targetType,
    targetIds: needsTargets(form.targetType) ? [...new Set(form.targetIds.map(id => id.trim()).filter(Boolean))] : [],
    icon: blankToNull(form.icon),
    link: blankToNull(form.link),
    businessType: form.businessType,
    businessId: form.businessId,
    needConfirm: form.needConfirm,
    isMandatory: form.isMandatory,
    isBanner: form.isBanner,
    isPopup: form.isPopup,
    startsAt: toIso(form.startsAt),
    expiresAt: toIso(form.expiresAt),
    remark: form.remark,
  }
}

/** 新建只存草稿，在列表里再发布（4.x 同） */
export function toCreateBody(form: NotificationForm): RequestOf<'notifications.create'> {
  return { ...commonBody(form), publishImmediately: false }
}

export function toUpdateBody(form: NotificationForm & { id: string }): RequestOf<'notifications.update'> {
  return { id: form.id, ...commonBody(form) }
}

/** 批量操作只回失败项的错误码：认得的翻成文案，其余原样 */
export function batchFailureMessage(t: Translate, code: string): string {
  return code === 'NOTIFICATION_NOTFOUND_001' ? t('notification.notifications.msg_not_found') : code
}

/** SchemaPage 的资源：分页与删除（删除逐项拒绝的原因拼成一条报错） */
export function notificationResource(t: Translate): SchemaResource<NotificationRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(await notificationsApi.list(toPageRequest(params, NOTIFICATION_QUERY)), params),
    remove: async (id) => {
      const result = await notificationsApi.delete({ ids: [String(id)] })
      if (result.failed.length > 0)
        throw new Error(result.failed.map(failure => batchFailureMessage(t, failure.code)).join('; '))
    },
  }
}

/** 已读率（百分比取整）；没有接收人时为 0 */
export function readRate(stats: Pick<ReadStats, 'readCount' | 'recipientCount'>): number {
  return stats.recipientCount > 0 ? Math.round((stats.readCount / stats.recipientCount) * 100) : 0
}

/** 接收人筛选：要求确认的通知才有「未确认」一档 */
export function recipientFilters(needConfirm: boolean): readonly NotificationRecipientFilter[] {
  return needConfirm ? ['Unread', 'Read', 'Unconfirmed', 'All'] : ['Unread', 'Read', 'All']
}

export function recipientFilterLabel(t: Translate, filter: NotificationRecipientFilter): string {
  switch (filter) {
    case 'Unread': return t('notification.notifications.stats_filter_unread')
    case 'Read': return t('notification.notifications.stats_filter_read')
    case 'Unconfirmed': return t('notification.notifications.stats_filter_unconfirmed')
    default: return t('notification.notifications.stats_filter_all')
  }
}

/** 一页接收人（运营数据抽屉；4.x 只有未读人员一档） */
export function loadRecipients(id: string, filter: NotificationRecipientFilter, page: number): Promise<RecipientPage> {
  return notificationsApi.recipients({ id, filter, page: { index: page, size: RECIPIENT_PAGE_SIZE } })
}

/** 导出用的未读人员：按后端页上限逐页取，至多 {@link EXPORT_LIMIT} 人；`truncated` 表示还有没取到的 */
export async function loadUnreadForExport(id: string): Promise<{ rows: RecipientRow[], truncated: boolean }> {
  const rows: RecipientRow[] = []
  let total = 0
  for (let page = 1; rows.length < EXPORT_LIMIT; page++) {
    const result = await notificationsApi.recipients({ id, filter: 'Unread', page: { index: page, size: PAGE_MAX_SIZE } })
    total = Number(result.total)
    rows.push(...result.items)
    if (result.items.length < PAGE_MAX_SIZE || rows.length >= total)
      break
  }
  return { rows: rows.slice(0, EXPORT_LIMIT), truncated: total > EXPORT_LIMIT }
}

function csvCell(value: string): string {
  return `"${value.replaceAll('"', '""')}"`
}

/** 未读人员 CSV（4.x 同：带 BOM 防中文乱码、CRLF 换行、每格加引号）：用户名、显示名、收到时间 */
export function unreadCsv(header: readonly [string, string, string], rows: readonly RecipientRow[], formatTime: (value: string) => string): string {
  const lines = [header, ...rows.map(row => [row.userName, row.displayName || '-', formatTime(row.receivedAt)] as const)]
    .map(cells => cells.map(csvCell).join(','))
  return `${String.fromCharCode(0xFEFF)}${lines.join('\r\n')}`
}

/** 角色目标的候选：本侧启用的角色（4.x 同） */
export async function loadRoleOptions(): Promise<{ label: string, value: string }[]> {
  const result = await rolesApi.list({ page: { index: 1, size: PAGE_MAX_SIZE }, filters: [{ field: 'status', operator: 'eq', value: 'Active' }], sorts: [], keyword: null })
  return result.items.map(role => ({ label: role.name, value: role.id }))
}

/** 部门树展平成候选：停用的部门不列出（下级照常列） */
export function flattenDepartments(nodes: readonly DepartmentNode[]): { label: string, value: string }[] {
  return nodes.flatMap(node => [
    ...(node.department.status === 'Active' ? [{ label: node.department.name, value: node.department.id }] : []),
    ...flattenDepartments(node.children),
  ])
}

/** 部门目标的候选（只在租户一侧：平台没有部门） */
export async function loadDepartmentOptions(): Promise<{ label: string, value: string }[]> {
  return flattenDepartments((await departmentsApi.tree({})).items)
}
