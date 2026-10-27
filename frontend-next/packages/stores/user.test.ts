import type { Permissions } from './auth'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { useAuthStore } from './auth'
import { useUserStore } from './user'

const user = { id: 'u-1', userName: 'alice', displayName: 'Alice', email: null, phoneNumber: null, homeTenantId: 'platform', status: 'Active', lastLoginAt: null, createdAt: '2026-10-28T00:00:00Z', createdBy: null }

function session(extra: object = {}) {
  return { accessToken: 'a-1', accessExpiresAt: '2026-10-28T02:00:00Z', refreshToken: 'r-1', refreshExpiresAt: '2026-11-04T00:00:00Z', sessionId: 's-1', tenantId: null, tenantName: null, impersonatorUserName: null, mustChangePassword: false, user, ...extra }
}

function grant(permissions: string[]): Permissions {
  return { isPlatform: false, permissions, roles: [], menus: [], pages: [] } as unknown as Permissions
}

describe('user store（auth 会话的壳层投影）', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
  })

  it('没有会话时没有用户', () => {
    expect(useUserStore().userInfo).toBeNull()
    expect(useUserStore().isLoggedIn).toBe(false)
  })

  it('平台账号在平台：isPlatform 与 canAccessPlatform 为真、没有租户名，没有权限码时不出模仿入口', () => {
    localStorage.setItem('xihan.next.session', JSON.stringify(session()))
    const info = useUserStore().userInfo!
    expect(info).toMatchObject({ basicId: 'u-1', userName: 'alice', nickName: 'Alice', tenantId: null, tenantName: null, isPlatform: true, canAccessPlatform: true, canImpersonate: false, isImpersonating: false, impersonatorUserName: null })
  })

  it('在租户里并持有 identity.impersonation.start：带租户名、出模仿入口；注册地不是平台的进不了平台', () => {
    localStorage.setItem('xihan.next.session', JSON.stringify(session({ tenantId: 'acme', tenantName: 'Acme', user: { ...user, homeTenantId: 'acme' } })))
    useAuthStore().permissions = grant(['identity.impersonation.start'])
    const info = useUserStore().userInfo!
    expect(info).toMatchObject({ tenantId: 'acme', tenantName: 'Acme', isPlatform: false, canAccessPlatform: false, canImpersonate: true, permissions: ['identity.impersonation.start'] })
    expect(useUserStore().hasPermission('identity.impersonation.start')).toBe(true)
  })

  it('模仿态：横幅字段齐、即使有权限也收起模仿入口', () => {
    localStorage.setItem('xihan.next.session', JSON.stringify(session({ tenantId: 'acme', tenantName: 'Acme', impersonatorUserName: 'superadmin', user: { ...user, id: 'u-2', userName: 'bob', homeTenantId: 'acme' } })))
    useAuthStore().permissions = grant(['identity.impersonation.start'])
    const info = useUserStore().userInfo!
    expect(info).toMatchObject({ userName: 'bob', isImpersonating: true, impersonatorUserName: 'superadmin', canImpersonate: false })
  })
})
