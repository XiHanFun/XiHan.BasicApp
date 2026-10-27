import type { RequestOf, ResponseOf } from '~/contracts'
import type { ImpersonationCandidate, ImpersonationCandidateQuery, ImpersonationTenantOption, StartImpersonationParams } from '~/types'
import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { LOGIN_PATH } from '~/constants'
import { auth as authApi } from '~/contracts'
import { useAppContext } from './app-context'

const SESSION_KEY = 'xihan.next.session'

/** 登录 / 刷新返回的会话：访问令牌、刷新令牌、会话 ID、当前租户与用户资料（后端 `TokenResponse`）。 */
export type Session = ResponseOf<'auth.login'>
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

  async function login(request: RequestOf<'auth.login'>): Promise<Session> {
    const result = await authApi.login(request, { anonymous: true })
    store(result)
    return result
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

  // ---- 模仿登录（壳层入口；接口经应用上下文注入，6.0 后端接上前是显式拒绝）----
  const impersonationLoading = ref(false)

  function impersonationCandidates(input: ImpersonationCandidateQuery): Promise<ImpersonationCandidate[]> {
    return useAppContext().apis.impersonationApi.candidates(input)
  }

  function impersonationTenants(): Promise<ImpersonationTenantOption[]> {
    return useAppContext().apis.impersonationApi.tenants()
  }

  /** 模仿 / 退出模仿都拿回一份新会话：换掉当前会话并重取权限 */
  async function applyImpersonation(run: () => Promise<unknown>): Promise<void> {
    if (impersonationLoading.value)
      return
    impersonationLoading.value = true
    try {
      const next = await run() as Session
      store(next)
      await loadPermissions()
    }
    finally {
      impersonationLoading.value = false
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
    refresh,
    loadPermissions,
    logout,
    loadMe,
    markPasswordChanged,
    clear,
    impersonationLoading,
    impersonationCandidates,
    impersonationTenants,
    startImpersonation,
    stopImpersonation,
  }
})
