import type { ResponseOf, TwoFactorMethod } from '~/contracts'
import type { ImpersonationCandidate, ImpersonationCandidateQuery, ImpersonationTenantOption, LoginParams, LoginResponse, OAuthProviderItem, SessionRoleOption, StartImpersonationParams } from '~/types'
import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { LOGIN_PATH } from '~/constants'
import { auth as authApi } from '~/contracts'
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

/** 回跳只认站内路径（防开放重定向） */
function safeRedirect(redirect: string | undefined): string {
  return redirect && redirect.startsWith('/') && !redirect.startsWith('//') ? redirect : '/'
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
      if (result.twoFactor) {
        return {
          requiresTwoFactor: true,
          availableTwoFactorMethods: result.twoFactor.methods.map(method => PAGE_METHODS[method]),
          twoFactorMethod: result.twoFactor.method ? PAGE_METHODS[result.twoFactor.method] : undefined,
          codeSent: result.twoFactor.codeSent,
          twoFactorTicket: result.twoFactor.ticket,
          token: null,
        }
      }
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

  /** 第三方登录：提供方列表要等 OpenPlatform（N2 第六批）才有，登录页拿到的列表为空，不会走到这里 */
  function startOAuthLogin(provider: OAuthProviderItem): never {
    throw new Error(`第三方登录（${provider.name}）尚未接入 6.0 后端`)
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
    refresh,
    loadPermissions,
    logout,
    loadMe,
    markPasswordChanged,
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
