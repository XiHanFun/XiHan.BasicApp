import type { PageQuery } from './common'

// ==================== 系统管理类型 ====================

export interface SysUser {
  id: string
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
  id: string
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
  id: string
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
  id: string
  permissionName: string
  permissionCode: string
  description?: string
  groupName?: string
  status?: number
}

export interface SysDepartment {
  id: string
  parentId?: string | null
  departmentName: string
  departmentCode?: string
  leader?: string
  phone?: string
  email?: string
  sort?: number
  status?: number
  children?: SysDepartment[]
}

export interface SysTenant {
  id: string
  tenantName: string
  tenantCode?: string
  contactName?: string
  contactPhone?: string
  status?: number
  expiredTime?: string
  createTime?: string
}

export interface SysLogItem {
  id?: string
  createdTime?: string
  userName?: string
  operationName?: string
  message?: string
  ip?: string
  [key: string]: any
}

export interface SysFile {
  id: string
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
  id: string
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
  id: string
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
  id: string
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
  id: string
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
  id: string
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
  id: string
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
  id: string
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
  id: string
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
  id: string
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
  id: string
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

export interface NotificationPageQuery extends PageQuery {
  notificationType?: number | undefined
  notificationStatus?: number | undefined
  status?: number | undefined
}
