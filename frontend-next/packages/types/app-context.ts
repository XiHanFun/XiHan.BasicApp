import type {
  CaptchaChallenge,
  ImpersonationCandidate,
  ImpersonationCandidateQuery,
  ImpersonationTenantOption,
  LoginConfig,
  LoginToken,
  PermissionInfo,
  StartImpersonationParams,
  SwitchTenantParams,
  UserInfo,
  VerificationCodeResult,
} from './auth'
import type { NotificationContentFormat, NotificationPriority, NotificationStatus, NotificationType, TenantMemberType } from './enums'
import type { ChangePasswordParams, TwoFactorSetupResult, UserProfile } from './profile'
/**
 * 租户切换器条目（`tenants.mine` 的投影）：控制中心按它画卡片。
 * 注册地租户 `isHome`（没有成员类型），成员租户带 `memberType`；`isCurrent` 是当前会话所在的租户。
 */
export interface AppTenantSwitcherItem {
  tenantId: string
  tenantName: string
  isHome: boolean
  memberType: null | TenantMemberType
  isCurrent: boolean
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
  enumApi: {
    getBatch: (query: AppEnumBatchQuery) => Promise<Record<string, AppEnumDefinition>>
    getByName: (query: AppEnumNameQuery) => Promise<AppEnumDefinition>
  }
  /** 系统字典选项（业务表单下拉；值为字典项编码，树形字典按深度优先展平） */
  dictApi: {
    options: (dictCode: string) => Promise<Array<{ value: string, label: string, parentValue?: null | string, isDefault: boolean, disabled: boolean }>>
  }
}
