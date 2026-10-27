import type { RequestOf, ResponseOf } from '~/contracts'
import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { auth as authApi } from '~/contracts'

const SESSION_KEY = 'xihan.next.session'

/** 登录 / 刷新返回的会话：访问令牌、刷新令牌、会话 ID、当前租户与用户资料（后端 `TokenResponse`）。 */
export type Session = ResponseOf<'auth.login'>
export type SessionUser = Session['user']

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
 * 会话：`auth.login` 写入，`auth.refresh` 轮换（单飞，并发的未认证重放共用一次刷新），`auth.logout` 吊销并清掉。
 * 登录与刷新匿名调用（不带旧令牌，否则过期令牌先在 Authenticate 阶段被拒）。授权以后端为准，这里只保存后端返回的资料。
 */
export const useAuthStore = defineStore('auth', () => {
  const session = ref<Session | null>(readSession())
  let refreshing: Promise<string | null> | null = null

  const isAuthenticated = computed(() => session.value !== null)
  const accessToken = computed(() => session.value?.accessToken ?? null)
  const user = computed(() => session.value?.user ?? null)
  const tenantId = computed(() => session.value?.tenantId ?? null)
  const mustChangePassword = computed(() => session.value?.mustChangePassword ?? false)

  function store(next: Session | null) {
    session.value = next
    writeSession(next)
  }

  async function login(request: RequestOf<'auth.login'>): Promise<Session> {
    const result = await authApi.login(request, { anonymous: true })
    store(result)
    return result
  }

  /** 用刷新令牌换新会话；成功返回新的访问令牌，失败（会话已吊销 / 过期）清掉会话并返回 null。 */
  function refresh(): Promise<string | null> {
    const current = session.value
    if (!current)
      return Promise.resolve(null)
    refreshing ??= authApi.refresh({ refreshToken: current.refreshToken }, { anonymous: true })
      .then((result) => {
        store(result)
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

  /** 注销：请求后端吊销会话（失败也照常清本地，会话本就不可用了）。 */
  async function logout(): Promise<void> {
    if (!session.value)
      return
    try {
      await authApi.logout({})
    }
    catch {}
    store(null)
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

  return { session, isAuthenticated, accessToken, user, tenantId, mustChangePassword, login, refresh, logout, loadMe, markPasswordChanged, clear }
})
