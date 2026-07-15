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

/** 聊天会话类型（与后端 JsonStringEnumConverter 序列化值一致） */
export enum ChatConversationType {
  Single = 'Single',
  Group = 'Group',
  Department = 'Department',
}

/** 聊天成员角色（与后端 JsonStringEnumConverter 序列化值一致） */
export enum ChatMemberRole {
  Owner = 'Owner',
  Admin = 'Admin',
  Member = 'Member',
}

/** 聊天消息类型（与后端 JsonStringEnumConverter 序列化值一致） */
export enum ChatMessageType {
  Text = 'Text',
  Image = 'Image',
  File = 'File',
  System = 'System',
}

/**
 * 租户成员类型（与后端 JsonStringEnumConverter 序列化值一致）。
 *
 * 下沉理由：控制中心与个人中心「我的租户」两个 shell 页把它当**运行时值**做标签配色判断
 * （Owner / Admin / PlatformAdmin），属「被 packages 复用的契约枚举」。
 * 同文件的 TenantStatus / TenantConfigStatus / TenantMemberInviteStatus 等**不下沉**——
 * packages 零引用，搬进来就是本文件开头警告的「污染契约层」。
 */
export enum TenantMemberType {
  Owner = 'Owner',
  Admin = 'Admin',
  Member = 'Member',
  External = 'External',
  Guest = 'Guest',
  Consultant = 'Consultant',
  PlatformAdmin = 'PlatformAdmin',
}
