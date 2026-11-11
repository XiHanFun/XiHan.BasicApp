/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { ResponseOf, TwoFactorMethod } from '~/contracts'
import type { EmailLoginParams, ImpersonationCandidate, ImpersonationCandidateQuery, ImpersonationTenantOption, LoginParams, LoginResponse, OAuthProviderItem, PhoneLoginParams, SessionRoleOption, StartImpersonationParams } from '~/types'
import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { clearLockState } from '~/composables/session-lock'
import { destroyAllSignalRConnections } from '~/composables/useSignalR'
import { LOGIN_PATH } from '~/constants'
import { auth as authApi } from '~/contracts'
import { endpointUrl } from '~/request'
import { getPasskey } from '~/utils'
import { useAccessStore } from './access'
import { useAppContext } from './app-context'
import { resetPreferenceBackendSync } from './helpers'
import { relaunchApp } from './relaunch'
import { useSplitViewStore } from './split-view'
import { useTabbarStore } from './tabbar'

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

/**
 * 退出时清掉会话级存储，只留身份切换标记
 * 标记等重建后的守卫取走；模仿后权限没装载成就回了登录页时它还在，下一个登录的人照样不拿上一身份的偏好播种
 */
function clearSessionStorage(): void {
  try {
    const marker = sessionStorage.getItem(IDENTITY_SWITCHED_KEY)
    sessionStorage.clear()
    if (marker !== null)
      sessionStorage.setItem(IDENTITY_SWITCHED_KEY, marker)
  }
  catch {}
}

/** 登录 / 刷新返回的会话：访问令牌、刷新令牌、会话 ID、当前租户与用户资料（后端 `TokenResponse`） */
export type Session = ResponseOf<'auth.refresh'>

/** 双因素方式：契约是 `Totp` / `Email` / `Phone`，登录页按小写标识 `totp` / `email` / `phone` 认 */
const PAGE_METHODS: Record<TwoFactorMethod, string> = { Totp: 'totp', Email: 'email', Phone: 'phone' }

function contractMethodOf(method: string | undefined): TwoFactorMethod {
  const found = (Object.keys(PAGE_METHODS) as TwoFactorMethod[]).find(key => PAGE_METHODS[key] === method)
  if (!found)
    throw new Error(`未知的双因素方式 ${method}`)
  return found
}

type TwoFactorChallenge = NonNullable<ResponseOf<'auth.login'>['twoFactor']>

/** 双因素挑战（`auth.login` 与 `auth.external-login` 同一形态）→ 登录页认的响应 */
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
  return redirect && redirect.startsWith('/') && !redirect.startsWith('//')
    ? redirect
    : '/'
}

// ==================== 第三方登录 / 绑定的发起记录 ====================

const OAUTH_PENDING_KEY = 'xihan.next.oauth-pending'
/** 发起记录的时限：与后端 state 的有效期（10 分钟）一致，过了回调本就失败 */
const OAUTH_PENDING_TTL_MS = 10 * 60 * 1000

/** 第三方流程的用途：登录（登录页发起）或绑定（个人中心发起），与回调地址上的 `mode` 同值 */
export type OAuthMode = 'bind' | 'login'

/**
 * 本标签页发起的第三方流程：整页跳走前记进 sessionStorage，回调页取走并核对用途与提供方
 * 回调地址上的一次性票据谁拿到谁能用，只认这个标签页自己发起的流程带回来的：别人发来的回调链接（带着他的票据）
 * 不会让当前登录的人确认绑上他的第三方账号，也不会把未登录的人登进他的账号。登录页的回跳地址随记录带回来
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
    return {
      mode: parsed.mode,
      provider: parsed.provider,
      startedAt: parsed.startedAt,
      ...(typeof parsed.redirect === 'string'
        ? { redirect: parsed.redirect }
        : {}),
    }
  }
  catch {
    return null
  }
}

/**
 * 第三方登录的发起地址：普通端点，整页跳转、不经请求层、不带令牌
 * 带注册地租户（登录页那一栏，平台账号留空即不带）；绑定的发起地址由后端给（`profile.begin-external-bind`，带着本人的绑定意图）
 */
export function oauthChallengeUrl(provider: string, tenant?: string): string {
  const query = new URLSearchParams({ mode: 'login' })
  const home = tenant?.trim()
  if (home)
    query.set('tenant', home)
  return endpointUrl(`/api/oauth/${encodeURIComponent(provider)}/challenge?${query.toString()}`)
}
export type SessionUser = Session['user']
/** `auth.permissions`：有效权限码、角色、菜单树与页面码 */
export type Permissions = ResponseOf<'auth.permissions'>
export type MenuNode = Permissions['menus'][number]

function readSession(): Session | null {
  try {
    const raw = localStorage.getItem(SESSION_KEY)
    if (!raw)
      return null
    const parsed = JSON.parse(raw) as Partial<Session>
    return typeof parsed.accessToken === 'string' && typeof parsed.refreshToken === 'string' && parsed.user
      ? (parsed as Session)
      : null
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
 * `auth.permissions` 在路由守卫首次进入时装载（刷新令牌后清空、下次导航重取，角色变化随之生效）
 * 登录与刷新匿名调用（不带旧令牌，否则过期令牌先在 Authenticate 阶段被拒）。授权以后端为准，这里只保存后端返回的结果，`can` 精确匹配、不认通配
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

  /** 是否持有某个权限码（精确匹配；后端已把 `*` 展开） */
  function can(permission: string): boolean {
    return granted.value.has(permission)
  }

  const loginLoading = ref(false)

  /**
   * 密码登录（两步验证各段共用）：拿到令牌即存会话、跳回跳地址并返回 null
   * 不带票据是首段（`auth.login`），开了双因素即返回挑战、不存会话；带票据与验证码是第二段（`auth.verify-two-factor`）
   * 点击、Enter、验证码输满自动提交都汇到此，`loginLoading` 同步置位守住重入
   */
  async function login(params: LoginParams, redirect?: string): Promise<LoginResponse | null> {
    if (loginLoading.value)
      return null
    loginLoading.value = true
    try {
      if (params.twoFactorTicket) {
        const method = contractMethodOf(params.twoFactorMethod)
        // 没带码：登录页选好邮箱 / 短信方式或点重发，凭票据请服务端发码（认证器不发码，登录页不走这里）
        if (!params.twoFactorCode) {
          await authApi.sendTwoFactorCode({ ticket: params.twoFactorTicket, method }, { anonymous: true })
          return {
            requiresTwoFactor: true,
            twoFactorMethod: params.twoFactorMethod,
            codeSent: true,
            twoFactorTicket: params.twoFactorTicket,
            token: null,
          }
        }
        const token = await authApi.verifyTwoFactor({ ticket: params.twoFactorTicket, method, code: params.twoFactorCode }, { anonymous: true })
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

  /** 第三方登录：整页跳去发起，带注册地租户；回跳地址随发起记录带回 */
  function startOAuthLogin(provider: OAuthProviderItem, options: { tenant?: string, redirect?: string } = {}): void {
    writeOAuthPending({
      mode: 'login',
      provider: provider.name.toLowerCase(),
      startedAt: Date.now(),
      ...(options.redirect
        ? { redirect: options.redirect }
        : {}),
    })
    window.location.assign(oauthChallengeUrl(provider.name, options.tenant))
  }

  /**
   * 绑定第三方账号（个人中心发起）：领到本人绑定意图的发起地址后整页跳去
   * 回调页由本人调 `profile.link-external` 确认
   */
  async function startOAuthBind(provider: string): Promise<void> {
    const url = await useAppContext().apis.beginOAuthBindApi(provider)
    writeOAuthPending({
      mode: 'bind',
      provider: provider.toLowerCase(),
      startedAt: Date.now(),
    })
    window.location.assign(endpointUrl(url))
  }

  /** 第三方登录换来的双因素挑战：登录页挂载时取走，直接进验证阶段 */
  const pendingTwoFactor = ref<LoginResponse | null>(null)

  /**
   * 第三方登录回调：一次性票据经 `auth.external-login` 换令牌
   * 拿到令牌即同密码登录存会话、跳回跳地址；开了双因素即记下挑战、带回跳地址回登录页，由两步验证阶段凭挑战票据走 `auth.verify-two-factor`
   */
  async function handleOAuthCallback(ticket: string, redirect?: string): Promise<void> {
    await finishOrChallenge(() => authApi.externalLogin({ ticket }, { anonymous: true }), redirect)
  }

  /**
   * 验证码登录（`auth.login-by-code`，带注册地租户）
   * 同第三方登录：拿到令牌即落地，开了双因素即记下挑战、回登录页由两步验证阶段接着走
   */
  async function loginByPhoneCode(params: PhoneLoginParams, redirect?: string): Promise<void> {
    await finishOrChallenge(() => authApi.loginByCode({
      channel: 'Phone',
      target: params.phone.trim(),
      code: params.code,
      tenant: params.tenant?.trim() || null,
    }, { anonymous: true }), redirect)
  }

  /** 邮箱验证码登录（同上） */
  async function loginByEmailCode(params: EmailLoginParams, redirect?: string): Promise<void> {
    await finishOrChallenge(() => authApi.loginByCode({
      channel: 'Email',
      target: params.email.trim(),
      code: params.code,
      tenant: params.tenant?.trim() || null,
    }, { anonymous: true }), redirect)
  }

  /**
   * 通行密钥登录（`auth.begin-passkey-login` → 浏览器选通行密钥 → `auth.login-by-passkey`）
   * 不填账号与租户，由通行密钥找到账号与注册地；本人取消选择时抛 NotAllowedError，由登录页静默收下
   */
  async function loginByPasskey(redirect?: string): Promise<void> {
    await finishOrChallenge(async () => {
      const begun = await authApi.beginPasskeyLogin({}, { anonymous: true })
      const credential = await getPasskey(begun.options)
      return authApi.loginByPasskey({ ticket: begun.ticket, credential }, { anonymous: true })
    }, redirect)
  }

  /** 第三方登录、验证码登录与通行密钥登录的共同收尾：落地会话，或记下双因素挑战回登录页 */
  async function finishOrChallenge(signIn: () => Promise<ResponseOf<'auth.external-login'>>, redirect?: string): Promise<void> {
    if (loginLoading.value)
      return
    loginLoading.value = true
    let challenge: LoginResponse
    try {
      const result = await signIn()
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
    await router.replace({
      path: LOGIN_PATH,
      query: target === '/'
        ? {}
        : { redirect: target },
    })
  }

  /** 取走（并清除）第三方登录或验证码登录留下的双因素挑战 */
  function takePendingTwoFactor(): LoginResponse | null {
    const pending = pendingTwoFactor.value
    pendingTwoFactor.value = null
    return pending
  }

  /** 用刷新令牌换新会话，返回新访问令牌；失败（吊销 / 过期）清掉会话返回 null */
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

  /** 装载当前主体的权限、菜单与页面；没有会话即 null */
  async function loadPermissions(): Promise<Permissions | null> {
    if (!session.value)
      return null
    const result = await authApi.permissions({})
    permissions.value = result
    return result
  }

  /**
   * 退出时清掉属于这个人的本地状态，下一个登录的人不该看到
   * 菜单路由不在这里卸：守卫下次装载前先卸掉上一份
   */
  function clearLocalState(): void {
    useAccessStore().$reset()
    useTabbarStore().$reset()
    useSplitViewStore().close()
    clearSessionStorage()
    // 锁屏标记在 localStorage（跨标签页共享），不清会让重新登录后仍卡在锁屏；
    // 走 clearLockState 而非直接删键：它广播变更，本标签页的遮罩当场收起（storage 事件只通知其他标签页）
    clearLockState()
    // 偏好回写通道属于这个人，退出即关；下一个登录的人重新水合
    resetPreferenceBackendSync()
  }

  /**
   * 注销：断开实时连接、请求后端吊销会话，再清本地并回登录页
   * 吊销失败也照常清，会话本就不可用；没有会话也要清，刷新失败后从锁屏退出还是走这里
   * 回登录页由 store 自己跳，壳层各退出入口都指望这一点
   */
  async function logout(): Promise<void> {
    // 全部 Hub 一起销毁，不逐个枚举；各集成在令牌清空后的收尾照旧（销毁可重入）
    await destroyAllSignalRConnections().catch(() => {})
    if (session.value) {
      try {
        await authApi.logout({})
      }
      catch {}
    }
    clearLocalState()
    const router = await useAppContext().getRouter().catch(() => null)
    if (!router || router.currentRoute.value.path === LOGIN_PATH)
      return
    try {
      await router.replace(LOGIN_PATH)
    }
    catch {
      // 路由走不通也要回到登录页：整页跳过去
      const base = import.meta.env.VITE_BASE || '/'
      const normalizedBase = base.endsWith('/')
        ? base
        : `${base}/`
      window.location.assign(import.meta.env.VITE_ROUTER_HISTORY === 'history'
        ? LOGIN_PATH
        : `${normalizedBase}#${LOGIN_PATH}`)
    }
  }

  /** 按 `auth.me` 刷新资料（改名、改密后或打开页面时） */
  async function loadMe(): Promise<void> {
    if (!session.value)
      return
    const me = await authApi.me({})
    store({ ...session.value, user: me.user, tenantId: me.tenantId })
  }

  /** 本人改密成功后：当前会话保留，强制改密标记清除 */
  function markPasswordChanged(): void {
    if (session.value)
      store({ ...session.value, mustChangePassword: false })
  }

  /** 本人改资料后换上后端回的用户（令牌不变），顶栏、用户菜单与个人中心随之刷新 */
  function applyUser(nextUser: Session['user']): void {
    if (session.value)
      store({ ...session.value, user: nextUser })
  }

  function clear(): void {
    store(null)
  }

  /**
   * 切换上下文：进入某个租户，或平台账号回到平台（`tenantId` 为 null）
   * 后端在目标上下文续接同一会话、签发新令牌（旧令牌随即失效）；存下后整页重建（壳层状态属于上一上下文），由守卫重取权限、重装菜单路由
   */
  async function switchTenant(tenantId: null | string): Promise<void> {
    store(await useAppContext().apis.tenantApi.switchTenant({ tenantId }))
    relaunchApp()
  }

  // ---- 切换角色（壳层入口；接口经应用上下文注入）----
  function sessionRoles(): Promise<SessionRoleOption[]> {
    return useAppContext().apis.sessionRoleApi.list()
  }

  /** 换本会话激活的角色：后端续接会话并签发新令牌，存下后整页重建（同切换租户） */
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

  /**
   * 模仿 / 退出模仿：换上拿回的新会话后整页重建（同切换租户）
   * 失败时放开加载态，把错误抛给调用方提示
   */
  async function applyImpersonation(run: () => Promise<Session>): Promise<void> {
    if (impersonationLoading.value)
      return
    impersonationLoading.value = true
    try {
      store(await run())
      // 偏好属于人：上一身份还没发出去的回写不能带着新令牌写进新身份
      resetPreferenceBackendSync()
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
    loginByPhoneCode,
    loginByEmailCode,
    loginByPasskey,
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
