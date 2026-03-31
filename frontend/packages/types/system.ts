import type { PageQuery } from './common'

// ==================== 系统管理类型 ====================

export interface SysUser {
  basicId: string
  userName: string
  nickName: string
  realName?: string
  avatar?: string
  email?: string
  phone?: string
  gender: number
  status: number
  lastLoginTime?: string
  lastLoginIp?: string
  roles: string[]
  deptId?: string
  createTime: string
  updateTime?: string
  remark?: string
}

export interface SysRole {
  basicId: string
  roleName: string
  roleCode: string
  roleDescription?: string
  roleType?: number
  dataScope?: number
  status: number
  sort: number
  permissions: string[]
  createTime: string
  updateTime?: string
}

export interface SysMenu {
  basicId: string
  parentId?: string
  menuName: string
  menuCode: string
  menuType: number
  path?: string
  component?: string
  routeName?: string
  redirect?: string
  icon?: string
  title?: string
  isExternal?: boolean
  externalUrl?: string
  isCache?: boolean
  isVisible: boolean
  isAffix?: boolean
  sort: number
  status: number
  remark?: string
  children?: SysMenu[]
  createTime?: string
}

export interface SysPermission {
  basicId: string
  resourceId: string
  operationId: string
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
  leaderId?: string
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

export interface SysAccessLog {
  basicId?: string
  userId?: string
  userName?: string
  sessionId?: string
  resourcePath?: string
  resourceName?: string
  resourceType?: string
  method?: string
  accessResult?: string
  statusCode?: number
  accessIp?: string
  accessLocation?: string
  userAgent?: string
  browser?: string
  os?: string
  device?: string
  referer?: string
  responseTime?: number
  responseSize?: number
  accessTime?: string
  leaveTime?: string
  stayTime?: number
  errorMessage?: string
  extendData?: string
  remark?: string
  createdTime?: string
}

export interface SysOperationLog {
  basicId?: string
  userId?: string
  userName?: string
  operationType?: string
  module?: string
  function?: string
  title?: string
  description?: string
  method?: string
  requestUrl?: string
  requestParams?: string
  responseResult?: string
  executionTime?: number
  operationIp?: string
  operationLocation?: string
  browser?: string
  os?: string
  status?: string
  errorMessage?: string
  operationTime?: string
  createdTime?: string
}

export interface SysExceptionLog {
  basicId?: string
  userId?: string
  userName?: string
  requestId?: string
  sessionId?: string
  exceptionType?: string
  exceptionMessage?: string
  exceptionStackTrace?: string
  innerExceptionType?: string
  innerExceptionMessage?: string
  innerExceptionStackTrace?: string
  exceptionSource?: string
  exceptionLocation?: string
  severityLevel?: number
  requestPath?: string
  requestMethod?: string
  controllerName?: string
  actionName?: string
  requestParams?: string
  requestBody?: string
  requestHeaders?: string
  statusCode?: number
  operationIp?: string
  operationLocation?: string
  userAgent?: string
  browser?: string
  os?: string
  deviceType?: string
  deviceInfo?: string
  applicationName?: string
  applicationVersion?: string
  environmentName?: string
  serverHostName?: string
  threadId?: number
  processId?: number
  exceptionTime?: string
  isHandled?: boolean
  handledTime?: string
  handledBy?: string
  handledByName?: string
  handledRemark?: string
  businessModule?: string
  businessId?: string
  businessType?: string
  errorCode?: string
  extendData?: string
  remark?: string
  createdTime?: string
}

export interface SysAuditLog {
  basicId?: string
  userId?: string
  userName?: string
  realName?: string
  departmentId?: string
  departmentName?: string
  auditType?: string
  operationType?: string
  entityType?: string
  entityId?: string
  entityName?: string
  tableName?: string
  primaryKey?: string
  primaryKeyValue?: string
  module?: string
  function?: string
  description?: string
  beforeData?: string
  afterData?: string
  changedFields?: string
  changeDescription?: string
  requestPath?: string
  requestMethod?: string
  requestParams?: string
  responseResult?: string
  executionTime?: number
  operationIp?: string
  operationLocation?: string
  browser?: string
  os?: string
  deviceType?: string
  deviceInfo?: string
  userAgent?: string
  sessionId?: string
  requestId?: string
  businessId?: string
  businessType?: string
  isSuccess?: boolean
  exceptionMessage?: string
  exceptionStackTrace?: string
  riskLevel?: number
  auditTime?: string
  extendData?: string
  remark?: string
  createdTime?: string
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
  sendUserId?: string
  receiveUserId?: string
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
  senderId?: string
  receiverId?: string
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
  tenantId?: string
  status: number
  openApiSecurityEnabled?: boolean
  openApiSignatureAlgorithm?: string
  openApiContentSignAlgorithm?: string
  openApiEncryptionAlgorithm?: string
  openApiEncryptKey?: string
  openApiPublicKey?: string
  openApiSm2PublicKey?: string
  openApiAllowResponseEncryption?: boolean
  openApiIpWhitelist?: string
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
  submitUserId?: string
  submitTime: string
  currentReviewUserId?: string
  reviewLevel: number
  currentLevel: number
  status: number
  createTime?: string
  updateTime?: string
  remark?: string
}

export interface SysUserSession {
  basicId: string
  userId: string
  sessionId: string
  userSessionId?: string
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
  dictId: string
  dictCode: string
  parentId?: string
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
  recipientUserId?: string
  sendUserId?: string
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

export interface SysRuntimeInfo {
  osName: string
  osDescription: string
  osVersion: string
  osArchitecture: string
  processArchitecture: string
  frameworkDescription: string
  runtimeVersion: string
  is64BitOperatingSystem: boolean
  is64BitProcess: boolean
  isInteractive: boolean
  interactiveMode: string
  processorCount: number
  systemDirectory: string
  currentDirectory: string
  machineName: string
  userName: string
  userDomainName: string
  workingSet: number
  systemStartTime: string
  systemUptime: string
  processStartTime: string
  processUptime: string
  processId: number
  processName: string
  clrVersion: string
  environmentVariableCount: number
  commandLineArgs: string[]
}

export interface SysCpuInfo {
  processorName: string
  processorArchitecture: string
  physicalCoreCount: number
  logicalCoreCount: number
  baseClockSpeed: number
  cacheBytes: number
  usagePercentage: number
}

export interface SysMemoryInfo {
  totalBytes: number
  usedBytes: number
  freeBytes: number
  availableBytes: number
  buffersCachedBytes: number
  usagePercentage: number
  availablePercentage: number
}

export interface SysDiskInfo {
  diskName: string
  typeName: string
  totalSpace: number
  freeSpace: number
  usedSpace: number
  availableRate: number
}

export interface SysNetworkIpAddress {
  address: string
  subnetMask: string
  prefixLength: number
}

export interface SysNetworkStatistics {
  bytesReceived: number
  bytesSent: number
  packetsReceived: number
  packetsSent: number
  incomingPacketsDiscarded: number
  outgoingPacketsDiscarded: number
  incomingPacketsWithErrors: number
  outgoingPacketsWithErrors: number
}

export interface SysNetworkInfo {
  name: string
  description: string
  type: string
  operationalStatus: string
  speed: string
  physicalAddress: string
  supportsMulticast: boolean
  isReceiveOnly: boolean
  dnsAddresses: string[]
  gatewayAddresses: string[]
  dhcpServerAddresses: string[]
  iPv4Addresses: SysNetworkIpAddress[]
  iPv6Addresses: SysNetworkIpAddress[]
  statistics?: SysNetworkStatistics
}

export interface SysBoardInfo {
  product: string
  manufacturer: string
  serialNumber: string
  version: string
}

export interface SysGpuInfo {
  name: string
  description: string
  vendor: string
  deviceId: string
  busInfo: string
  driverVersion: string
  memoryBytes: number
  temperature?: number
  videoModeDescription: string
  status: string
  utilizationPercentage?: number
  memoryUtilizationPercentage?: number
}

export interface SysServerInfo {
  runtimeInfo: SysRuntimeInfo
  cpuInfo: SysCpuInfo
  memoryInfo: SysMemoryInfo
  diskInfos: SysDiskInfo[]
  networkInfos: SysNetworkInfo[]
  boardInfo: SysBoardInfo
  gpuInfos: SysGpuInfo[]
  collectedAt: string
}

export interface SysNuGetPackage {
  packageName: string
  packageVersion: string
}

export interface MessageDispatchResult extends Record<string, any> {}

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
  resourceId?: string | undefined
  operationId?: string | undefined
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
