/**
 * 契约枚举（后端枚举的前端镜像）底层。
 *
 * 这些枚举被 packages 的 shell 功能（如布局通知中心）当运行时值使用，属可复用基建，故归属 packages；
 * `src/api` 业务模块反向 re-export 保持 `@/api` 入口不变。
 * 见架构审查报告 D1/H8。
 *
 * 注：仅「被 packages 复用的契约枚举」下沉于此；纯业务枚举（仅 src 业务页面使用）应保留在
 * `src/api/modules/*`，不要为了消除依赖把它们也搬进来污染契约层。
 */

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum NotificationType {
  System = 'System',
  Security = 'Security',
  Business = 'Business',
  Todo = 'Todo',
  Emergency = 'Emergency',
}

/** 通知优先级（与后端 JsonStringEnumConverter 序列化值一致） */
export enum NotificationPriority {
  Low = 'Low',
  Normal = 'Normal',
  High = 'High',
  Urgent = 'Urgent',
}

/** 通知正文格式（与后端 JsonStringEnumConverter 序列化值一致） */
export enum NotificationContentFormat {
  Text = 'Text',
  Markdown = 'Markdown',
  Html = 'Html',
}

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum NotificationStatus {
  Unread = 'Unread',
  Read = 'Read',
  Deleted = 'Deleted',
}

/**
 * 租户成员类型：直接用契约层生成的值集（Member / Owner，字符串联合），控制中心按它做标签配色；
 * 不另抄一份枚举，免得与 tenants.mine 的响应类型不兼容。
 */
export type { TenantMemberType } from '~/contracts'
