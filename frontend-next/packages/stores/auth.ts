import { defineStore } from 'pinia'
import { computed, ref } from 'vue'

const TOKEN_KEY = 'xihan.next.token'

function readToken(): string | null {
  try {
    return localStorage.getItem(TOKEN_KEY)
  }
  catch {
    return null
  }
}

/** JWT 载荷里关心的声明：主体、租户、角色。只用于展示，授权以后端为准。 */
export interface TokenClaims {
  sub?: string
  tenant?: string
  role?: string | string[]
  exp?: number
}

export function decodeClaims(token: string | null): TokenClaims | null {
  if (!token)
    return null
  const part = token.split('.')[1]
  if (!part)
    return null
  try {
    const json = atob(part.replace(/-/g, '+').replace(/_/g, '/'))
    return JSON.parse(json) as TokenClaims
  }
  catch {
    return null
  }
}

/**
 * 会话：访问令牌与它的声明。N0 没有登录操作，令牌由开发者粘贴（`pnpm dev:token` 自签）；N1 的 auth.login 接入后换成登录流程写入。
 */
export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(readToken())
  const claims = computed(() => decodeClaims(token.value))
  const isAuthenticated = computed(() => !!claims.value?.sub)

  function setToken(value: string | null) {
    token.value = value?.trim() || null
    try {
      if (token.value)
        localStorage.setItem(TOKEN_KEY, token.value)
      else localStorage.removeItem(TOKEN_KEY)
    }
    catch {}
  }

  return { token, claims, isAuthenticated, setToken }
})
