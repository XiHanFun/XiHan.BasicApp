import type {
  CaptchaChallenge,
  ImpersonationCandidate,
  ImpersonationCandidateQuery,
  ImpersonationTenantOption,
  LoginConfig,
  LoginToken,
  PermissionInfo,
  SessionRoleOption,
  StartImpersonationParams,
  SwitchTenantParams,
  UserInfo,
  VerificationCodeResult,
} from './auth'
import type { NotificationContentFormat, NotificationPriority, NotificationStatus, NotificationType, TenantMemberType } from './enums'
import type { ApiCallSettings, ApiCredentialItem, ApiCredentialSecret, ChangeEmailParams, ChangePasswordParams, ChangePhoneParams, ChangeUserNameParams, ExternalLoginItem, LoginLogPage, MyOAuthAppCreateInput, MyOAuthAppItem, MyOAuthAppSecret, MyOAuthAppUpdateInput, NotificationPreference, OAuthConsentItem, TwoFactorSetupResult, UpdateProfileParams, UserActivity, UserProfile, UserSessionItem } from './profile'
/**
 * 租户切换器条目（`tenants.mine` 的投影）：控制中心按它画卡片，个人中心「我的租户」按它列表。
 * 注册地租户 `isHome`（没有成员类型），成员租户带 `memberType`（Member 普通成员 / Support 运维支持）；
 * `isOwner` 是该租户的所有者（租户的 AdminUserId，与注册地、成员类型无关）；`isCurrent` 是当前会话所在的租户；
 * `joinedTime` 沿用 4.x 的字段名（后端 joinedAt）：成员是成为成员的时间，注册地是账号建立的时间。
 */
export interface AppTenantSwitcherItem {
  tenantId: string
  tenantName: string
  isHome: boolean
  memberType: null | TenantMemberType
  isOwner: boolean
  isCurrent: boolean
  joinedTime: string
}

/**
 * shell 需要跳转、但**由应用侧定义**的路由路径。
 *
 * 底层包不该认识 `/workbench/profile` 这类业务路径——它们由 src 的路由表
 * 或后端 PageRegistry 定义，换一个应用就不存在，硬编码后点击即 404。
 * 未配置的项，对应入口应隐藏（而不是留一个点了没反应的死按钮）。
 */
export interface AppShellRoutes {
  /** 个人中心 */
  profile?: string
  /** 控制中心（登录后未进入租户时的落点；视图在 packages，但路由由应用注册） */
  controlCenter?: string
  /** 收件箱 / 消息中心 */
  inbox?: string
}

export interface AppBackendDependency {
  packageName: string
  packageVersion: string
}

export interface AppEnumOption {
  name: string
  value: boolean | number | object | string
  valueText: string
  label: string
  description: string
  theme?: string
  icon?: string
  order: number
  disabled: boolean
  source: 'dict' | 'enum' | string
  extra?: Record<string, unknown>
}

export interface AppEnumDefinition {
  enumName: string
  fullName: string
  displayName: string
  cultureName: string
  isFlags: boolean
  underlyingTypeName: string
  items: AppEnumOption[]
}

export interface AppEnumBatchQuery {
  enumNames: string[]
  language?: string
  includeHidden?: boolean
  includeDict?: boolean
  dictCodes?: string[]
  tenantId?: string
}

export interface AppEnumNameQuery {
  enumName: string
  language?: string
  includeHidden?: boolean
  includeDict?: boolean
  dictCode?: string
  tenantId?: string
}

export interface AppUserInboxItem {
  basicId: string
  title: string
  content?: null | string
  contentFormat?: NotificationContentFormat
  notificationType: NotificationType
  notificationStatus: NotificationStatus
  sendTime: string
  readTime?: null | string
  confirmTime?: null | string
  isGlobal?: boolean
  needConfirm?: boolean
  icon?: null | string
  link?: null | string
}

/** 展示分级（横幅 / 弹窗 / 强制阅读）所需的收件箱条目，较 AppUserInboxItem 多带优先级与正文格式等展示字段 */
export interface AppUserInboxDisplayItem extends AppUserInboxItem {
  notificationId: string
  priority: NotificationPriority
  contentFormat: NotificationContentFormat
  isMandatory: boolean
  isBanner: boolean
  isPopup: boolean
}

// 刻意**不带**索引签名（`extends Record<string, unknown>`）：带上之后任何未声明的 key 都能塞进
// ctx.apis 且类型检查通过（历史上 createMenuPageApis 的整块死注册就是这么活下来的），
// 同时 `apis.typo` 也不报错（推成 unknown）。去掉后 packages 侧上百个 ctx.apis.* 调用点获得拼写保护。
export interface AppContextApis {
  /** 登录页配置：开放的登录方式、第三方登录与是否要图形验证码 */
  getLoginConfigApi: () => Promise<LoginConfig>
  /** 图形验证码（登录配置要求时，密码登录的首段带上） */
  getCaptchaApi: () => Promise<CaptchaChallenge>
  getPermissionsApi: () => Promise<PermissionInfo>
  getUserInfoApi: () => Promise<UserInfo>
  logoutApi: () => Promise<unknown>
  changePasswordApi: (input: ChangePasswordParams) => Promise<unknown>
  /** 个人中心：本人资料与账号安全概况 */
  getProfileApi: () => Promise<UserProfile>
  /** 保存本人资料（整体覆盖：显示名、真实姓名、性别、生日、国家或地区、简介），返回保存后的资料 */
  updateProfileApi: (input: UpdateProfileParams) => Promise<UserProfile>
  /** 上传头像（JPEG / PNG，不超过 2 MB）：上传即换成本人的头像、回收旧的，返回新头像的文件主键（显示时再换签名地址） */
  uploadAvatarApi: (file: File, onProgress?: (percent: number) => void) => Promise<{ fileId: string }>
  /** 去掉本人的头像（回收头像文件），返回保存后的资料（4.x 经 updateProfileApi 写空串） */
  removeAvatarApi: () => Promise<UserProfile>
  /** 邮箱验证码登录：发码（登录方式要在系统参数 identity.login-methods 里开放；账号不存在也回答已发送） */
  sendEmailLoginCodeApi: (email: string, tenant?: string) => Promise<VerificationCodeResult>
  /** 手机验证码登录：发码（手机号要已验证） */
  sendPhoneLoginCodeApi: (phone: string, tenant?: string) => Promise<VerificationCodeResult>
  /** 改用户名（凭当前密码，90 天一次） */
  changeUserNameApi: (input: ChangeUserNameParams) => Promise<unknown>
  /** 验证当前邮箱：发验证码到它（6 位、10 分钟；同一目标 60 秒内不能再发） */
  sendEmailVerifyCodeApi: () => Promise<VerificationCodeResult>
  /** 验证当前手机号：发短信验证码到它 */
  sendPhoneVerifyCodeApi: () => Promise<VerificationCodeResult>
  /** 用收到的验证码确认当前邮箱，记为已验证 */
  verifyEmailApi: (code: string) => Promise<unknown>
  /** 用收到的验证码确认当前手机号，记为已验证 */
  verifyPhoneApi: (code: string) => Promise<unknown>
  /** 换绑邮箱第一步：凭当前密码，把验证码发到新邮箱 */
  sendChangeEmailCodeApi: (input: ChangeEmailParams) => Promise<VerificationCodeResult>
  /** 换绑手机号第一步：凭当前密码，把验证码发到新手机号 */
  sendChangePhoneCodeApi: (input: ChangePhoneParams) => Promise<VerificationCodeResult>
  /** 换绑邮箱第二步：用新邮箱收到的码确认，换成新的并记为已验证 */
  confirmChangeEmailApi: (code: string) => Promise<unknown>
  /** 换绑手机号第二步：用新手机号收到的码确认 */
  confirmChangePhoneApi: (code: string) => Promise<unknown>
  /** 双因素：开始绑定认证器（返回待确认的密钥与扫码链接） */
  setup2FAApi: () => Promise<TwoFactorSetupResult>
  /** 双因素：开启一种方式（method 为位标志 1=TOTP 2=邮箱 4=手机，code 为该方式的验证码） */
  enable2FAApi: (method: number | string, code: string) => Promise<unknown>
  /** 双因素：关闭一种方式（同上） */
  disable2FAApi: (method: number | string, code: string) => Promise<unknown>
  /** 双因素：给邮箱 / 手机方式发开关用的验证码 */
  send2FASetupCodeApi: (method: number | string) => Promise<VerificationCodeResult>
  /** 停用本人账号（凭当前密码） */
  deactivateAccountApi: (password: string) => Promise<unknown>
  /** 注销本人账号（凭当前密码） */
  deleteAccountApi: (password: string) => Promise<unknown>
  /** 第三方账号绑定（个人中心「账号绑定」）：本人已绑定的第三方账号 */
  getLinkedAccountsApi: () => Promise<ExternalLoginItem[]>
  /** 发起绑定（4.x 为 createOAuthBindTicketApi 换票据再自拼地址）：给当前登录的人领一个一次性的绑定意图，返回带着它的发起地址（相对于后端根；整页跳转见 auth store 的 startOAuthBind） */
  beginOAuthBindApi: (provider: string) => Promise<string>
  /** 确认绑定：凭回调页拿到的一次性票据，把第三方账号绑到当前登录的人（票据只认发起绑定的本人） */
  linkAccountApi: (ticket: string) => Promise<ExternalLoginItem>
  /** 解绑本人的一个第三方账号（按提供方类型），返回剩下的绑定 */
  unlinkAccountApi: (provider: string) => Promise<ExternalLoginItem[]>
  /** 登录设备（个人中心「登录设备」）：本人全部在线的会话（平台与各租户），当前会话在最前 */
  getSessionsApi: () => Promise<UserSessionItem[]>
  /** 下线本人的一个会话（当前会话不能这样下线，要走注销） */
  revokeSessionApi: (sessionId: string) => Promise<unknown>
  /** 下线本人除当前会话外的全部会话 */
  revokeOtherSessionsApi: () => Promise<unknown>
  /** 登录日志（个人中心「登录日志」）：本人的认证事件按时间倒序分页，page 从 1 起 */
  getLoginLogsApi: (page: number, pageSize: number) => Promise<LoginLogPage & { page: number, pageSize: number }>
  /** 通知偏好（个人中心「通知」）：本人的接收渠道与通知类型，没保存过即全收 */
  getNotificationPreferenceApi: () => Promise<NotificationPreference>
  /** 保存通知偏好（整体覆盖），返回保存后的偏好 */
  updateNotificationPreferenceApi: (input: NotificationPreference) => Promise<NotificationPreference>
  /** 活跃度（个人中心「统计」）：今天 / 本周 / 本月的次数、最近活动时间与近一年按天的操作趋势，按浏览器时区分天 */
  getActivityApi: () => Promise<UserActivity>
  /** 锁屏（服务端强制）：设置会话级一次性口令并置位锁屏标记 */
  lockSessionApi: (input: { password: string }) => Promise<unknown>
  /** 解锁：口令由服务端校验；连续失败 5 次服务端会直接吊销会话 */
  unlockSessionApi: (input: { password: string }) => Promise<unknown>
  impersonationApi: {
    /** 平台可在其中发起模仿的租户 */
    tenants: () => Promise<ImpersonationTenantOption[]>
    candidates: (input: ImpersonationCandidateQuery) => Promise<ImpersonationCandidate[]>
    start: (input: StartImpersonationParams) => Promise<LoginToken>
    stop: () => Promise<LoginToken>
  }
  tenantApi: {
    myAvailableTenants: () => Promise<AppTenantSwitcherItem[]>
    switchTenant: (input: SwitchTenantParams) => Promise<LoginToken>
  }
  /** 切换角色：本会话此刻持有的角色与激活着哪些；换激活集即换签令牌（同一动态职责分离组的角色不能同时激活） */
  sessionRoleApi: {
    list: () => Promise<SessionRoleOption[]>
    activate: (roleIds: string[]) => Promise<LoginToken>
  }
  /** 用户设置（按 用户 × 场景 × 设置键 全场景跨端同步，载荷为 JSON 字符串；clientId 供后端推送回显过滤） */
  userSettingApi: {
    get: (input: { scene: number, settingKey: string }) => Promise<{ scene: number, settingKey: string, settingValue?: null | string }>
    /** origin 为不落库的中转字段：携带主题切换的点击位置（视口百分比），供其它设备复现同位置扩散动画 */
    save: (input: { scene: number, settingKey: string, settingValue?: null | string, clientId?: string, origin?: null | string }) => Promise<{ scene: number, settingKey: string, settingValue?: null | string }>
  }
  userInboxApi: {
    banner: () => Promise<AppUserInboxDisplayItem[]>
    confirm: (id: string, userId?: string, tenantId?: null | string) => Promise<unknown>
    list: (userId?: string, unreadOnly?: boolean, tenantId?: null | string) => Promise<AppUserInboxItem[]>
    mandatoryUnread: () => Promise<AppUserInboxDisplayItem[]>
    markAllRead: (userId?: string, tenantId?: null | string) => Promise<unknown>
    markPopupShown: (id: string) => Promise<unknown>
    markRead: (id: string, userId?: string, tenantId?: null | string) => Promise<unknown>
    popup: () => Promise<AppUserInboxDisplayItem[]>
  }
  /** 由文件主键换取对象存储预签名访问 URL（头像）；文件服务接入前显式拒绝，头像退回首字母 */
  getFilePresignedUrlApi: (fileId: string) => Promise<string>
  /** 时区目录（顶栏 / 个人中心共用） */
  timeZoneApi: {
    options: () => Promise<Array<{ id: string, displayName: string, baseUtcOffsetMinutes: number, supportsDaylightSavingTime: boolean }>>
  }
  /** 导入历史（Schema 页面导入留痕：执行完毕上报 + 当前用户最近导入记录） */
  importHistoryApi: {
    create: (input: { pageCode: string, resourceCode?: null | string, fileName: string, totalCount: number, successCount: number, failCount: number, errorSummary?: null | string }) => Promise<unknown>
    recent: (pageCode: string, count?: number) => Promise<Array<{ basicId: number | string, pageCode: string, resourceCode?: null | string, fileName: string, totalCount: number, successCount: number, failCount: number, errorSummary?: null | string, createdTime: string }>>
  }
  /** 导出中心（提交异步导出任务，后台 worker 执行；进度经灵动岛、产物在导出中心下载） */
  exportTaskApi: {
    submit: (input: { businessType: string, taskName?: string, scope: number, format: number, querySnapshot?: null | string, columns: Array<{ key: string, title: string, valueMap?: Record<string, string> }> }) => Promise<unknown>
  }
  /** 个人 API 凭证（开发者设置；Secret 明文仅创建/滚动时返回一次） */
  getApiCredentialsApi: () => Promise<ApiCredentialItem[]>
  createApiCredentialApi: (credentialName?: string) => Promise<ApiCredentialSecret>
  rotateApiCredentialSecretApi: (id: number | string) => Promise<ApiCredentialSecret>
  updateApiCredentialStatusApi: (id: number | string, status: 'Disabled' | 'Enabled') => Promise<ApiCredentialItem>
  deleteApiCredentialApi: (id: number | string) => Promise<unknown>
  /** 开放接口的调用安全设置（签名算法与 IP 白名单，作用于本人全部凭证，验签时服务端读取） */
  getApiCallSettingsApi: () => Promise<ApiCallSettings>
  saveApiCallSettingsApi: (input: { signAlgorithm: string, ipWhitelist: string[] }) => Promise<ApiCallSettings>
  /** 我的 OAuth 应用（开发者设置；ClientSecret 明文仅创建/重置机密客户端时返回一次） */
  getMyOAuthAppsApi: () => Promise<MyOAuthAppItem[]>
  createMyOAuthAppApi: (input: MyOAuthAppCreateInput) => Promise<MyOAuthAppSecret>
  updateMyOAuthAppApi: (input: MyOAuthAppUpdateInput) => Promise<MyOAuthAppItem>
  regenerateMyOAuthAppSecretApi: (id: number | string) => Promise<MyOAuthAppSecret>
  updateMyOAuthAppStatusApi: (id: number | string, status: 'Disabled' | 'Enabled') => Promise<MyOAuthAppItem>
  deleteMyOAuthAppApi: (id: number | string) => Promise<unknown>
  /** 已授权的第三方应用（本人同意过的 OAuth 客户端）；撤销即吊销它拿到的令牌，下次登录要重新授权 */
  getOAuthConsentsApi: () => Promise<OAuthConsentItem[]>
  revokeOAuthConsentApi: (clientId: string) => Promise<unknown>
  enumApi: {
    getBatch: (query: AppEnumBatchQuery) => Promise<Record<string, AppEnumDefinition>>
    getByName: (query: AppEnumNameQuery) => Promise<AppEnumDefinition>
  }
  /** 系统字典选项（业务表单下拉；值为字典项编码，树形字典按深度优先展平） */
  dictApi: {
    options: (dictCode: string) => Promise<Array<{ value: string, label: string, parentValue?: null | string, isDefault: boolean, disabled: boolean }>>
  }
}
