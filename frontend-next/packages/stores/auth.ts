import type { ResponseOf, TwoFactorMethod } from '~/contracts'
import type { ImpersonationCandidate, ImpersonationCandidateQuery, ImpersonationTenantOption, LoginParams, LoginResponse, OAuthProviderItem, SessionRoleOption, StartImpersonationParams } from '~/types'
import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { LOGIN_PATH } from '~/constants'
import { auth as authApi } from '~/contracts'
import { endpointUrl } from '~/request'
import { useAppContext } from './app-context'
import { resetPreferenceBackendSync } from './helpers'
import { relaunchApp } from './relaunch'

const SESSION_KEY = 'xihan.next.session'
/** 模仿 / 退出模仿后整页重建前留的标记：重建后的偏好水合不拿本地那份（属于上一身份）给新身份播种 */
const IDENTITY_SWITCHED_KEY = 'xihan.next.identity-switched'

/** 取走（并清除）身份切换标记：本次页面生命周期是不是刚经模仿换了身份 */
export function takeIdentitySwitchedMarker(): boolean {
  try {
    const marked = sessionStorage.getItem(IDENTITY_SWITCHED_KEY) !== null
    sessionStorage.removeItem(IDENTITY_SWITCHED_KEY)
    return marked
  }
  catch {
    return false
  }
}

/** 登录 / 刷新返回的会话：访问令牌、刷新令牌、会话 ID、当前租户与用户资料（后端 `TokenResponse`）。 */
export type Session = ResponseOf<'auth.refresh'>

/** 双因素方式：契约是 `Totp` / `Email` / `Phone`，4.x 登录页按小写标识 `totp` / `email` / `phone` 认 */
const PAGE_METHODS: Record<TwoFactorMethod, string> = { Totp: 'totp', Email: 'email', Phone: 'phone' }

function contractMethodOf(method: string | undefined): TwoFactorMethod {
  const found = (Object.keys(PAGE_METHODS) as TwoFactorMethod[]).find(key => PAGE_METHODS[key] === method)
  if (!found)
    throw new Error(`未知的双因素方式 ${method}`)
  return found
}

type TwoFactorChallenge = NonNullable<ResponseOf<'auth.login'>['twoFactor']>

/** 双因素挑战（`auth.login` 与 `auth.external-login` 同一形态）→ 4.x 登录页认的响应 */
function toPageChallenge(twoFactor: TwoFactorChallenge): LoginResponse {
  return {
    requiresTwoFactor: true,
    availableTwoFactorMethods: twoFactor.methods.map(method => PAGE_METHODS[method]),
    twoFactorMethod: twoFactor.method ? PAGE_METHODS[twoFactor.method] : undefined,
    codeSent: twoFactor.codeSent,
    twoFactorTicket: twoFactor.ticket,
    token: null,
  }
}

/** 回跳只认站内路径（防开放重定向） */
function safeRedirect(redirect: string | undefined): string {
  return redirect && redirect.startsWith('/') && !redirect.startsWith('//') ? redirect : '/'
}

// ==================== 第三方登录 / 绑定的发起记录 ====================

const OAUTH_PENDING_KEY = 'xihan.next.oauth-pending'
/** 发起记录的时限：与后端 state 的有效期（10 分钟）一致，过了回调本就失败 */
const OAUTH_PENDING_TTL_MS = 10 * 60 * 1000

/** 第三方流程的用途：登录（登录页发起）或绑定（个人中心发起），与回调地址上的 `mode` 同值 */
export type OAuthMode = 'bind' | 'login'

/**
 * 本标签页发起的第三方流程：整页跳走前记进 sessionStorage，回调页取走并核对用途与提供方。
 * 回调地址上的一次性票据谁拿到谁能用，只认这个标签页自己发起的流程带回来的：别人发来的回调链接（带着他的票据）
 * 不会让当前登录的人确认绑上他的第三方账号，也不会把未登录的人登进他的账号。登录页的回跳地址随记录带回来（4.x 回调后一律落首页）。
 */
export interface OAuthPending {
  mode: OAuthMode
  /** 提供方名称（登录配置里的 name，小写，与回调地址上的 `provider` 同值） */
  provider: string
  redirect?: string
  startedAt: number
}

function writeOAuthPending(pending: OAuthPending): void {
  try {
    sessionStorage.setItem(OAUTH_PENDING_KEY, JSON.stringify(pending))
  }
  catch {}
}

/** 取走（并清除）发起记录；没有、过期或格式不对即 null */
export function takeOAuthPending(): OAuthPending | null {
  try {
    const raw = sessionStorage.getItem(OAUTH_PENDING_KEY)
    sessionStorage.removeItem(OAUTH_PENDING_KEY)
    if (!raw)
      return null
    const parsed = JSON.parse(raw) as Partial<OAuthPending>
    if ((parsed.mode !== 'login' && parsed.mode !== 'bind') || typeof parsed.provider !== 'string' || typeof parsed.startedAt !== 'number')
      return null
    if (Date.now() - parsed.startedAt > OAUTH_PENDING_TTL_MS)
      return null
    return { mode: parsed.mode, provider: parsed.provider, startedAt: parsed.startedAt, ...(typeof parsed.redirect === 'string' ? { redirect: parsed.redirect } : {}) }
  }
  catch {
    return null
  }
}

/**
 * 第三方登录的发起地址：普通端点 `/api/oauth/{名称}/challenge`（整页跳转，不经请求层、不带令牌），带注册地租户（登录页那一栏；平台账号留空即不带）。
 * 绑定的发起地址由后端给（`profile.begin-external-bind`，带着本人的绑定意图）。
 */
export function oauthChallengeUrl(provider: string, tenant?: string): string {
  const query = new URLSearchParams({ mode: 'login' })
  const home = tenant?.trim()
  if (home)
    query.set('tenant', home)
  return endpointUrl(`/api/oauth/${encodeURIComponent(provider)}/challenge?${query.toString()}`)
}
export type SessionUser = Session['user']
/** `auth.permissions`：有效权限码、角色、菜单树与页面码。 */
export type Permissions = ResponseOf<'auth.permissions'>
export type MenuNode = Permissions['menus'][number]

function readSession(): Session | null {
  try {
    const raw = localStorage.getItem(SESSION_KEY)
    if (!raw)
      return null
    const parsed = JSON.parse(raw) as Partial<Session>
    return typeof parsed.accessToken === 'string' && typeof parsed.refreshToken === 'string' && parsed.user ? (parsed as Session) : null
  }
  catch {
    return null
  }
}

function writeSession(session: Session | null): void {
  try {
    if (session)
      localStorage.setItem(SESSION_KEY, JSON.stringify(session))
    else localStorage.removeItem(SESSION_KEY)
  }
  catch {}
}

/**
 * 会话与权限：`auth.login` 写入会话，`auth.refresh` 轮换（单飞，并发的未认证重放共用一次刷新），`auth.logout` 吊销并清掉；
 * `auth.permissions` 在路由守卫首次进入时装载（刷新令牌后清空、下次导航重取，角色变化随之生效）。
 * 登录与刷新匿名调用（不带旧令牌，否则过期令牌先在 Authenticate 阶段被拒）。授权以后端为准，这里只保存后端返回的结果，`can` 精确匹配、不认通配。
 */
export const useAuthStore = defineStore('auth', () => {
  const session = ref<Session | null>(readSession())
  const permissions = ref<Permissions | null>(null)
  let refreshing: Promise<string | null> | null = null

  const isAuthenticated = computed(() => session.value !== null)
  const accessToken = computed(() => session.value?.accessToken ?? null)
  const user = computed(() => session.value?.user ?? null)
  const tenantId = computed(() => session.value?.tenantId ?? null)
  const mustChangePassword = computed(() => session.value?.mustChangePassword ?? false)
  const granted = computed(() => new Set(permissions.value?.permissions ?? []))
  const menus = computed(() => permissions.value?.menus ?? [])

  function store(next: Session | null) {
    session.value = next
    writeSession(next)
    if (!next)
      permissions.value = null
  }

  /** 是否持有某个权限码（精确匹配；后端已把 `*` 展开）。 */
  function can(permission: string): boolean {
    return granted.value.has(permission)
  }

  const loginLoading = ref(false)

  /**
   * 密码登录（4.x 登录页的入口，同一个函数走两步验证的每一段）：
   * - 不带票据是首段：账号、密码与图形验证码交给 `auth.login`；账号开了双因素即返回挑战（票据、可用方式），不存会话；
   * - 带票据与验证码是第二段：`auth.verify-two-factor` 换令牌；
   * 拿到令牌就存会话并跳到回跳地址，返回 null。页面上的点击、Enter、验证码输满自动提交都汇到这里，`loginLoading` 同步置位守住重入。
   */
  async function login(params: LoginParams, redirect?: string): Promise<LoginResponse | null> {
    if (loginLoading.value)
      return null
    loginLoading.value = true
    try {
      if (params.twoFactorTicket) {
        // 邮箱 / 短信方式要服务端先发码，随 Notification 接入；认证器方式直接输码，不走这里
        if (!params.twoFactorCode)
          throw new Error('邮箱与短信验证码尚未接入 6.0 后端')
        const token = await authApi.verifyTwoFactor({ ticket: params.twoFactorTicket, method: contractMethodOf(params.twoFactorMethod), code: params.twoFactorCode }, { anonymous: true })
        await afterLogin(token, redirect)
        return null
      }
      const result = await authApi.login({
        userName: params.username.trim(),
        password: params.password,
        tenant: params.tenant?.trim() || null,
        captchaId: params.captchaId ?? null,
        captchaCode: params.captchaCode ?? null,
      }, { anonymous: true })
      if (result.twoFactor)
        return toPageChallenge(result.twoFactor)
      if (!result.token)
        throw new Error('登录响应既没有令牌也没有双因素挑战')
      await afterLogin(result.token, redirect)
      return null
    }
    finally {
      loginLoading.value = false
    }
  }

  /** 存下会话后跳到回跳地址（缺省首页）；权限与菜单由路由守卫首次进入时装载 */
  async function afterLogin(token: Session, redirect?: string): Promise<void> {
    store(token)
    const router = await useAppContext().getRouter()
    await router.replace(safeRedirect(redirect))
  }

  /** 第三方登录（4.x 同为整页跳去发起）：带上登录页填的注册地租户（平台账号留空）；登录页的回跳地址随发起记录带回来 */
  function startOAuthLogin(provider: OAuthProviderItem, options: { tenant?: string, redirect?: string } = {}): void {
    writeOAuthPending({ mode: 'login', provider: provider.name.toLowerCase(), startedAt: Date.now(), ...(options.redirect ? { redirect: options.redirect } : {}) })
    window.location.assign(oauthChallengeUrl(provider.name, options.tenant))
  }

  /** 绑定第三方账号（个人中心发起，4.x 同为先换绑定票据再跳）：先领本人的绑定意图、拿到后端给的发起地址再整页跳去，回调页由本人调 `profile.link-external` 确认 */
  async function startOAuthBind(provider: string): Promise<void> {
    const url = await useAppContext().apis.beginOAuthBindApi(provider)
    writeOAuthPending({ mode: 'bind', provider: provider.toLowerCase(), startedAt: Date.now() })
    window.location.assign(endpointUrl(url))
  }

  /** 第三方登录换来的双因素挑战：登录页挂载时取走，直接进验证阶段 */
  const pendingTwoFactor = ref<LoginResponse | null>(null)

  /**
   * 第三方登录回调（4.x 回调把令牌放在地址片段里直接落地；6.0 是一次性票据，经 `auth.external-login` 换）：
   * 拿到令牌即与密码登录同样存会话、跳回跳地址；账号开了双因素即返回挑战——记下挑战、带着回跳地址回登录页，
   * 由登录页的两步验证阶段接着走（与密码登录首段返回挑战之后同一条路，第二段凭挑战票据走 `auth.verify-two-factor`）。
   */
  async function handleOAuthCallback(ticket: string, redirect?: string): Promise<void> {
    if (loginLoading.value)
      return
    loginLoading.value = true
    let challenge: LoginResponse
    try {
      const result = await authApi.externalLogin({ ticket }, { anonymous: true })
      if (!result.twoFactor) {
        if (!result.token)
          throw new Error('登录响应既没有令牌也没有双因素挑战')
        await afterLogin(result.token, redirect)
        return
      }
      challenge = toPageChallenge(result.twoFactor)
    }
    finally {
      loginLoading.value = false
    }
    pendingTwoFactor.value = challenge
    const target = safeRedirect(redirect)
    const router = await useAppContext().getRouter()
    await router.replace({ path: LOGIN_PATH, query: target === '/' ? {} : { redirect: target } })
  }

  /** 取走（并清除）第三方登录留下的双因素挑战 */
  function takePendingTwoFactor(): LoginResponse | null {
    const pending = pendingTwoFactor.value
    pendingTwoFactor.value = null
    return pending
  }

  /** 用刷新令牌换新会话；成功返回新的访问令牌（权限随后重取），失败（会话已吊销 / 过期）清掉会话并返回 null。 */
  function refresh(): Promise<string | null> {
    const current = session.value
    if (!current)
      return Promise.resolve(null)
    refreshing ??= authApi.refresh({ refreshToken: current.refreshToken }, { anonymous: true })
      .then((result) => {
        store(result)
        permissions.value = null
        return result.accessToken
      }, () => {
        store(null)
        return null
      })
      .finally(() => {
        refreshing = null
      })
    return refreshing
  }

  /** 装载当前主体的权限、菜单与页面；没有会话即 null。 */
  async function loadPermissions(): Promise<Permissions | null> {
    if (!session.value)
      return null
    const result = await authApi.permissions({})
    permissions.value = result
    return result
  }

  /** 注销：请求后端吊销会话（失败也照常清本地，会话本就不可用了），然后回登录页（壳层的各个退出入口都指望 store 自己跳）。 */
  async function logout(): Promise<void> {
    if (session.value) {
      try {
        await authApi.logout({})
      }
      catch {}
      store(null)
    }
    // 偏好回写通道属于这个人，退出即关；下一个登录的人重新水合
    resetPreferenceBackendSync()
    const router = await useAppContext().getRouter().catch(() => null)
    if (router && router.currentRoute.value.path !== LOGIN_PATH)
      await router.replace(LOGIN_PATH)
  }

  /** 按 `auth.me` 刷新资料（改名、改密后或打开页面时）。 */
  async function loadMe(): Promise<void> {
    if (!session.value)
      return
    const me = await authApi.me({})
    store({ ...session.value, user: me.user, tenantId: me.tenantId, mustChangePassword: me.mustChangePassword })
  }

  /** 本人改密成功后：当前会话保留，强制改密标记清除。 */
  function markPasswordChanged(): void {
    if (session.value)
      store({ ...session.value, mustChangePassword: false })
  }

  /** 本人改了资料（显示名、用户名、头像）后：会话里的用户换成后端回的那份（令牌不变），顶栏、用户菜单与个人中心侧栏随之刷新。 */
  function applyUser(nextUser: Session['user']): void {
    if (session.value)
      store({ ...session.value, user: nextUser })
  }

  function clear(): void {
    store(null)
  }

  /**
   * 切换上下文：进入某个租户，或（平台账号）回到平台（`tenantId` 为 null）。
   * 后端在目标上下文续接同一会话、签发新令牌（旧令牌随即失效）；本地存下新会话后整页重建，
   * 由守卫按新上下文重取权限、重装菜单路由（标签栏等壳层状态都属于上一上下文）。
   */
  async function switchTenant(tenantId: null | string): Promise<void> {
    store(await useAppContext().apis.tenantApi.switchTenant({ tenantId }))
    relaunchApp()
  }

  // ---- 切换角色（壳层入口；接口经应用上下文注入）----
  function sessionRoles(): Promise<SessionRoleOption[]> {
    return useAppContext().apis.sessionRoleApi.list()
  }

  /** 换本会话激活的角色：后端续接同一会话、签发新令牌；本地存下后整页重建（与切换租户同一条路） */
  async function activateRoles(roleIds: string[]): Promise<void> {
    store(await useAppContext().apis.sessionRoleApi.activate(roleIds))
    relaunchApp()
  }

  // ---- 模仿登录（壳层入口；接口经应用上下文注入）----
  const impersonationLoading = ref(false)

  function impersonationCandidates(input: ImpersonationCandidateQuery): Promise<ImpersonationCandidate[]> {
    return useAppContext().apis.impersonationApi.candidates(input)
  }

  function impersonationTenants(): Promise<ImpersonationTenantOption[]> {
    return useAppContext().apis.impersonationApi.tenants()
  }

  /** 模仿 / 退出模仿都拿回一份新会话：换掉当前会话后整页重建（与切换租户同一条路）；失败时放开加载态、把错误抛给调用方提示 */
  async function applyImpersonation(run: () => Promise<Session>): Promise<void> {
    if (impersonationLoading.value)
      return
    impersonationLoading.value = true
    try {
      store(await run())
      try {
        sessionStorage.setItem(IDENTITY_SWITCHED_KEY, '1')
      }
      catch {}
      relaunchApp()
    }
    catch (error) {
      impersonationLoading.value = false
      throw error
    }
  }

  function startImpersonation(input: StartImpersonationParams): Promise<void> {
    return applyImpersonation(() => useAppContext().apis.impersonationApi.start(input))
  }

  function stopImpersonation(): Promise<void> {
    return applyImpersonation(() => useAppContext().apis.impersonationApi.stop())
  }

  return {
    session,
    permissions,
    menus,
    isAuthenticated,
    accessToken,
    user,
    tenantId,
    mustChangePassword,
    can,
    login,
    loginLoading,
    startOAuthLogin,
    startOAuthBind,
    handleOAuthCallback,
    takePendingTwoFactor,
    refresh,
    loadPermissions,
    logout,
    loadMe,
    markPasswordChanged,
    applyUser,
    clear,
    switchTenant,
    sessionRoles,
    activateRoles,
    impersonationLoading,
    impersonationCandidates,
    impersonationTenants,
    startImpersonation,
    stopImpersonation,
  }
})
