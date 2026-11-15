/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { OperationId, PageQueryMapping, ResponseOf } from '~/contracts'
import { exportTasks as exportTasksApi, toPageRequest, toSchemaPageResult } from '~/contracts'
import { throwOnFailed } from '~/request'

export type ExportTaskRow = ResponseOf<'export-tasks.list'>['items'][number]
export type ExportTaskStatus = ExportTaskRow['status']
export type ExportScope = ExportTaskRow['scope']
export type ExportFormat = ExportTaskRow['format']

type Translate = (key: string, values?: Record<string, unknown>) => string

/** 色调，与 XiHan.UI 标签一致 */
export type ExportTaskTone = 'danger' | 'info' | 'neutral' | 'success'

export const EXPORT_TASK_STATUSES: readonly ExportTaskStatus[] = [
  'Pending',
  'Processing',
  'Success',
  'Failed',
]
export const EXPORT_FORMATS: readonly ExportFormat[] = ['Csv', 'Xlsx']

/**
 * 认得的业务类型：接了导出中心的列表页的列表操作 ID（后端能导任何列表操作），
 * 不在这里的照原样显示
 */
export const KNOWN_BUSINESS_TYPES = [
  'users.list',
  'sessions.list',
  'roles.list',
  'permissions.list',
  'menus.list',
  'delegations.list',
  'permission-requests.list',
  'field-security.list',
  'constraint-rules.list',
  'tenants.list',
  'editions.list',
  'positions.list',
  'operation-logs.list',
  'access-logs.list',
  'login-logs.list',
  'exception-logs.list',
  'diff-logs.list',
  'open-api-logs.list',
  'permission-change-logs.list',
  'condition-hits.list',
  'files.list',
  'oauth-apps.list',
  'message-templates.list',
  'emails.list',
  'sms.list',
  'notifications.list',
  'configs.list',
  'dicts.list',
  'dict-items.list',
  'numbering-rules.list',
  'jobs.list',
  'versions.list',
] as const satisfies readonly OperationId[]

/** 重启打断的任务，后端在失败原因里记的字面量 */
export const INTERRUPTED_MESSAGE = 'interrupted'

/** 自动刷新间隔（只在标签页可见时刷新） */
export const REFRESH_INTERVAL_MS = 5_000

/**
 * 列表的后端筛选（与后端白名单一致）：关键字匹配任务名称与业务类型；状态、格式多选（合成 in）；业务类型精确匹配
 * 后端只列调用方自己的任务，缺省按创建时间倒序
 */
export const EXPORT_TASK_QUERY: PageQueryMapping = {
  multiples: ['status', 'format'],
  filters: { businessType: 'eq' },
}

/** SchemaPage 的资源：分页与删除（连同结果文件；执行中的后端逐项拒绝） */
export function exportTaskResource(): SchemaResource<ExportTaskRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(await exportTasksApi.list(toPageRequest(params, EXPORT_TASK_QUERY)), params),
    remove: async id => throwOnFailed(await exportTasksApi.delete({
      ids: [
        String(id),
      ],
    })),
  }
}

// ── 行操作 ──────────────────────────────────────────────────────

/** 成功且留有结果文件的才能下载 */
export function canDownload(task: ExportTaskRow): boolean {
  return task.status === 'Success' && !!task.fileId
}

/** 只有待执行的能取消（后端同一条件） */
export function canCancel(task: ExportTaskRow): boolean {
  return task.status === 'Pending'
}

/** 执行中的不能删除（后端逐项拒绝），其余连同结果文件删除 */
export function canDelete(task: ExportTaskRow): boolean {
  return task.status !== 'Processing'
}

/** 进度百分比，夹在 0 到 100 之间 */
export function progressPercent(task: Pick<ExportTaskRow, 'progress'>): number {
  return Math.min(100, Math.max(0, Math.round(task.progress)))
}

// ── 自动刷新 ────────────────────────────────────────────────────

export interface AutoRefresh {
  /** 开始定时刷新（已在刷新时不重复开） */
  start: () => void
  /** 停止定时刷新 */
  stop: () => void
}

/** 定时刷新：每个间隔调一次 `reload`，标签页不可见时跳过 */
export function createAutoRefresh(reload: () => void, isVisible: () => boolean, intervalMs = REFRESH_INTERVAL_MS): AutoRefresh {
  let timer: ReturnType<typeof setInterval> | null = null
  return {
    start() {
      if (timer !== null)
        return
      timer = setInterval(() => {
        if (isVisible())
          reload()
      }, intervalMs)
    },
    stop() {
      if (timer === null)
        return
      clearInterval(timer)
      timer = null
    },
  }
}

// ── 文案与色调 ──────────────────────────────────────────────────

export function statusTone(status: ExportTaskStatus): ExportTaskTone {
  switch (status) {
    case 'Success': return 'success'
    case 'Failed': return 'danger'
    case 'Processing': return 'info'
    default: return 'neutral'
  }
}

// 枚举值是契约字面量，文案键静态写出以便 i18n 门禁核对

export function statusLabel(t: Translate, status: ExportTaskStatus): string {
  switch (status) {
    case 'Processing': return t('files.export_center.statuses.processing')
    case 'Success': return t('files.export_center.statuses.success')
    case 'Failed': return t('files.export_center.statuses.failed')
    default: return t('files.export_center.statuses.pending')
  }
}

export function scopeLabel(t: Translate, scope: ExportScope): string {
  switch (scope) {
    case 'CurrentPage': return t('files.export_center.scopes.current_page')
    case 'All': return t('files.export_center.scopes.all')
    default: return t('files.export_center.scopes.search_result')
  }
}

/** 格式名不翻译 */
export function formatLabel(format: ExportFormat): string {
  return format === 'Xlsx'
    ? 'Excel'
    : 'CSV'
}

/** 业务类型是列表操作 ID：认得的翻成导出它的页面（或页签）的标题，其余原样 */
export function businessTypeLabel(t: Translate, businessType: string): string {
  switch (businessType) {
    case 'users.list': return t('identity.users.title')
    case 'sessions.list': return t('identity.online_users.title')
    case 'roles.list': return t('access.roles.title')
    case 'permissions.list': return t('access.permissions.title')
    case 'menus.list': return t('access.menus.title')
    case 'delegations.list': return t('access.delegations.tab')
    case 'permission-requests.list': return t('access.permission_requests.tab')
    case 'field-security.list': return t('access.field_security.title')
    case 'constraint-rules.list': return t('access.constraint_rules.title')
    case 'tenants.list': return t('tenancy.tenants.title')
    case 'editions.list': return t('tenancy.editions.title')
    case 'positions.list': return t('organization.positions.title')
    case 'operation-logs.list': return t('auditing.operation_logs.title')
    case 'access-logs.list': return t('auditing.access_logs.title')
    case 'login-logs.list': return t('auditing.login_logs.title')
    case 'exception-logs.list': return t('auditing.exception_logs.title')
    case 'diff-logs.list': return t('auditing.diff_logs.title')
    case 'open-api-logs.list': return t('auditing.open_api_logs.title')
    case 'permission-change-logs.list': return t('auditing.permission_change_logs.title')
    case 'condition-hits.list': return t('auditing.condition_hits.title')
    case 'files.list': return t('files.library.title')
    case 'oauth-apps.list': return t('openplatform.oauth_apps.title')
    case 'message-templates.list': return t('notification.templates.title')
    case 'emails.list': return t('notification.messages.tab_email')
    case 'sms.list': return t('notification.messages.tab_sms')
    case 'notifications.list': return t('notification.notifications.title')
    case 'configs.list': return t('settings.configs.title')
    case 'dicts.list': return t('settings.dicts.title')
    case 'dict-items.list': return t('settings.dicts.items')
    case 'numbering-rules.list': return t('settings.numbering_rules.title')
    case 'jobs.list': return t('maintenance.jobs.title')
    case 'versions.list': return t('maintenance.versions.title')
    default: return businessType
  }
}

/** 失败原因：重启打断的翻成文案；取消的后端已按区域写好，其余原样 */
export function errorMessageText(t: Translate, message: null | string | undefined): string {
  if (!message)
    return ''
  return message === INTERRUPTED_MESSAGE
    ? t('files.export_center.interrupted')
    : message
}
