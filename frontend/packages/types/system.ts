import type { PageQuery } from './common'

// ==================== 系统管理类型 ====================

export interface SysUser {
  basicId: string
  username: string
  nickname: string
  avatar?: string
  email?: string
  phone?: string
  gender: number
  status: number
  roles: string[]
  deptId?: string
  createTime: string
  updateTime?: string
  remark?: string
}

export interface SysRole {
  basicId: string
  name: string
  code: string
  description?: string
  status: number
  sort: number
  permissions: string[]
  createTime: string
  updateTime?: string
}

export interface SysMenu {
  basicId: string
  parentId?: string
  name: string
  path: string
  component?: string
  icon?: string
  type: number
  permission?: string
  sort: number
  status: number
  hidden: boolean
  children?: SysMenu[]
  createTime: string
}

export interface SysPermission {
  basicId: string
  resourceId: number
  operationId: number
  permissionName: string
  permissionCode: string
  permissionDescription?: string
  description?: string
  isRequireAudit?: boolean
  priority?: number
  sort?: number
  tenantId?: number
  remark?: string
  createTime?: string
  updateTime?: string
  groupName?: string
  status?: number
}

export interface SysDepartment {
  basicId: string
  parentId?: string | null
  departmentName: string
  departmentCode?: string
  departmentType?: number
  leaderId?: number
  leader?: string
  phone?: string
  email?: string
  address?: string
  tenantId?: number
  remark?: string
  createTime?: string
  updateTime?: string
  sort?: number
  status?: number
  children?: SysDepartment[]
}

export interface SysTenant {
  basicId: string
  tenantName: string
  tenantCode?: string
  tenantShortName?: string
  contactPerson?: string
  contactName?: string
  contactPhone?: string
  contactEmail?: string
  isolationMode?: number
  tenantStatus?: number
  status?: number
  expireTime?: string
  expiredTime?: string
  createTime?: string
  updateTime?: string
  remark?: string
}

export interface SysLogItem {
  basicId?: string
  createdTime?: string
  userName?: string
  operationName?: string
  message?: string
  ip?: string
  [key: string]: any
}

export interface SysFile {
  basicId: string
  fileName: string
  originalName: string
  fileExtension?: string
  fileType: number
  mimeType?: string
  fileSize: number
  fileHash?: string
  isPublic: boolean
  requireAuth: boolean
  isTemporary: boolean
  expiresAt?: string
  status: number
  tags?: string
  createTime?: string
  updateTime?: string
  remark?: string
}

export interface SysEmail {
  basicId: string
  sendUserId?: number
  receiveUserId?: number
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

export interface SysSms {
  basicId: string
  senderId?: number
  receiverId?: number
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

export interface SysTask {
  basicId: string
  taskCode: string
  taskName: string
  taskDescription?: string
  taskGroup?: string
  taskClass: string
  taskMethod?: string
  taskParams?: string
  triggerType: number
  cronExpression?: string
  runTaskStatus: number
  priority: number
  status: number
  createTime?: string
  updateTime?: string
  remark?: string
}

export interface SysOAuthApp {
  basicId: string
  appName: string
  appDescription?: string
  clientId: string
  clientSecret: string
  appType: number
  grantTypes: string
  redirectUris?: string
  scopes?: string
  accessTokenLifetime: number
  refreshTokenLifetime: number
  authorizationCodeLifetime: number
  skipConsent: boolean
  status: number
  createTime?: string
  updateTime?: string
  remark?: string
}

export interface SysReview {
  basicId: string
  reviewCode: string
  reviewTitle: string
  reviewType: string
  reviewContent?: string
  reviewStatus: number
  reviewResult?: number
  priority: number
  submitUserId?: number
  submitTime: string
  currentReviewUserId?: number
  reviewLevel: number
  currentLevel: number
  status: number
  createTime?: string
  updateTime?: string
  remark?: string
}

export interface SysUserSession {
  basicId: string
  userId: number
  sessionId: string
  deviceType: number
  deviceName?: string
  ipAddress?: string
  loginTime: string
  lastActivityTime: string
  isOnline: boolean
  isRevoked: boolean
  revokedAt?: string
  logoutTime?: string
  createTime?: string
  updateTime?: string
  remark?: string
}

export interface SysConfig {
  basicId: string
  configName: string
  configKey: string
  configValue?: string
  configType: number
  dataType: number
  status: number
  createTime?: string
  updateTime?: string
  remark?: string
}

export interface SysDict {
  basicId: string
  dictCode: string
  dictName: string
  dictType: string
  dictDescription?: string
  status: number
  sort: number
  createTime?: string
  updateTime?: string
  remark?: string
}

export interface SysDictItem {
  basicId: string
  dictId: number
  dictCode: string
  parentId?: number
  itemCode: string
  itemName: string
  itemValue?: string
  status: number
  sort: number
  createTime?: string
  updateTime?: string
  remark?: string
}

export interface SysNotification {
  basicId: string
  recipientUserId?: number
  sendUserId?: number
  notificationType: number
  title: string
  content?: string
  notificationStatus: number
  readTime?: string
  sendTime: string
  expireTime?: string
  isGlobal?: boolean
  needConfirm?: boolean
  status?: number
  createTime?: string
  updateTime?: string
  remark?: string
}

// ==================== 业务分页查询参数 ====================

export interface UserPageQuery extends PageQuery {
  status?: number | undefined
  roleId?: string
}

export interface RolePageQuery extends PageQuery {
  status?: number | undefined
}

export interface FilePageQuery extends PageQuery {
  status?: number | undefined
  fileType?: number | undefined
}

export interface EmailPageQuery extends PageQuery {
  emailType?: number | undefined
  emailStatus?: number | undefined
}

export interface SmsPageQuery extends PageQuery {
  smsType?: number | undefined
  smsStatus?: number | undefined
}

export interface TaskPageQuery extends PageQuery {
  triggerType?: number | undefined
  runTaskStatus?: number | undefined
  status?: number | undefined
}

export interface OAuthAppPageQuery extends PageQuery {
  appType?: number | undefined
  status?: number | undefined
}

export interface ReviewPageQuery extends PageQuery {
  reviewStatus?: number | undefined
  reviewResult?: number | undefined
  status?: number | undefined
}

export interface UserSessionPageQuery extends PageQuery {
  deviceType?: number | undefined
  isOnline?: boolean | undefined
  isRevoked?: boolean | undefined
}

export interface ConfigPageQuery extends PageQuery {
  configType?: number | undefined
  dataType?: number | undefined
  status?: number | undefined
}

export interface DictPageQuery extends PageQuery {
  dictType?: string | undefined
  status?: number | undefined
}

export interface DepartmentPageQuery extends PageQuery {
  status?: number | undefined
  departmentType?: number | undefined
  parentId?: string | undefined
}

export interface PermissionPageQuery extends PageQuery {
  status?: number | undefined
  resourceId?: number | undefined
  operationId?: number | undefined
}

export interface TenantPageQuery extends PageQuery {
  status?: number | undefined
  tenantStatus?: number | undefined
  isolationMode?: number | undefined
}

export interface NotificationPageQuery extends PageQuery {
  notificationType?: number | undefined
  notificationStatus?: number | undefined
  status?: number | undefined
}
