/**
 * usePermission 权限判定单元测试。
 * 职责：锁定与 userStore / accessStore 的判定一致性——两者都是 auth.permissions 的投影，
 * 只认服务端展开下发的码（`*` 不当通配）、空入参一律放行，以及角色判定只看角色码（不参与权限码回退）。
 */
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { useAuthStore } from '~/stores'
import { usePermission } from './usePermission'

/** 以登录态 + 服务端下发的权限码与角色码落到 auth store */
function grant(permissions: string[], roles: string[] = []) {
  const auth = useAuthStore()
  auth.session = {
    accessToken: 'token',
    accessExpiresAt: '2099-01-01T00:00:00Z',
    refreshToken: 'refresh',
    refreshExpiresAt: '2099-01-01T00:00:00Z',
    sessionId: 'session-1',
    tenantId: null,
    mustChangePassword: false,
    user: { id: 'u-1', userName: 'tester', displayName: 'Tester' },
  } as unknown as typeof auth.session
  auth.permissions = {
    permissions,
    roles: roles.map(code => ({ id: code, code, name: code })),
    menus: [],
    pages: [],
    isPlatform: true,
  }
}

beforeEach(() => {
  setActivePinia(createPinia())
})

describe('usePermission.hasPermission 与 store 判定一致', () => {
  it('服务端下发的权限码命中即通过', () => {
    grant(['identity.users.read'])

    expect(usePermission().hasPermission('identity.users.read')).toBe(true)
  })

  it('不含时判定为无权限', () => {
    grant(['identity.users.read'])

    expect(usePermission().hasPermission('identity.users.delete')).toBe(false)
  })

  it('`*` 不当通配：服务端没展开下发的码判为无', () => {
    grant(['*', 'identity.users.create'])

    expect(usePermission().hasPermission('identity.users.create')).toBe(true)
    expect(usePermission().hasPermission('identity.users.lock')).toBe(false)
    expect(usePermission().hasPermission(['identity.users.lock', 'identity.users.reset-password'])).toBe(false)
  })

  it('未登录时一律拒绝', () => {
    expect(usePermission().hasPermission('identity.users.read')).toBe(false)
  })

  it('权限码大小写敏感，不做归一化', () => {
    grant(['Identity.Users.List'])

    expect(usePermission().hasPermission('identity.users.read')).toBe(false)
    expect(usePermission().hasPermission('Identity.Users.List')).toBe(true)
  })

  it('权限码含中文与 emoji 时按原样全等比较', () => {
    grant(['系统:用户:查看🚀'])

    expect(usePermission().hasPermission('系统:用户:查看🚀')).toBe(true)
    expect(usePermission().hasPermission('系统:用户:查看')).toBe(false)
  })
})

describe('usePermission.hasPermission 空值与数组分支', () => {
  it('传空串视为不限制，直接放行', () => {
    expect(usePermission().hasPermission('')).toBe(true)
  })

  it('传空数组落进「或」判定，没有任何候选因此拒绝', () => {
    grant(['*'])

    expect(usePermission().hasPermission([])).toBe(false)
  })

  it('数组形式只要有一项命中就放行', () => {
    grant(['identity.users.read'])

    expect(usePermission().hasPermission(['access.roles.create', 'identity.users.read'])).toBe(true)
  })

  it('数组形式全部未命中才拒绝', () => {
    grant(['identity.users.read'])

    expect(usePermission().hasPermission(['access.roles.create', 'organization.departments.create'])).toBe(false)
  })

  it('数组里的空串项不享受「不限制」豁免，仍按空权限码逐项比对而拒绝', () => {
    expect(usePermission().hasPermission(['sys:none', ''])).toBe(false)
  })
})

describe('usePermission.hasRole 只看角色码', () => {
  it('角色命中即通过', () => {
    grant([], ['admin'])

    expect(usePermission().hasRole('admin')).toBe(true)
  })

  it('角色未命中判定失败', () => {
    grant([], ['admin'])

    expect(usePermission().hasRole('auditor')).toBe(false)
  })

  it('角色判定不吃权限码通配符，权限为 * 也不放行角色', () => {
    grant(['*'], [])

    expect(usePermission().hasRole('admin')).toBe(false)
  })

  it('传空串视为不限制角色，直接放行', () => {
    expect(usePermission().hasRole('')).toBe(true)
  })

  it('传空数组时没有候选角色，拒绝', () => {
    grant([], ['admin'])

    expect(usePermission().hasRole([])).toBe(false)
  })

  it('数组形式任一角色命中即通过', () => {
    grant([], ['auditor'])

    expect(usePermission().hasRole(['admin', 'auditor'])).toBe(true)
  })
})

describe('usePermission.hasAnyPermission', () => {
  it('空数组没有候选，判定为 false', () => {
    grant(['*'])

    expect(usePermission().hasAnyPermission([])).toBe(false)
  })

  it('任一权限命中即通过', () => {
    grant(['organization.departments.read'])

    expect(usePermission().hasAnyPermission(['identity.users.read', 'organization.departments.read'])).toBe(true)
  })

  it('数组含空串项时空串被剔除，未登录用户仍被拒绝', () => {
    expect(usePermission().hasAnyPermission(['sys:none', ''])).toBe(false)
    expect(usePermission().hasPermission(['sys:none', ''])).toBe(false)
  })

  it('数组全部为空串时判定为 false，不整组放行', () => {
    grant(['identity.users.read'])

    expect(usePermission().hasAnyPermission(['', ''])).toBe(false)
  })

  it('空串与有效权限码混排时，有效项命中仍然放行', () => {
    grant(['identity.users.read'])

    expect(usePermission().hasAnyPermission(['', 'identity.users.read'])).toBe(true)
  })

  it('全部未命中才判定为 false', () => {
    grant(['identity.users.read'])

    expect(usePermission().hasAnyPermission(['a', 'b', 'c'])).toBe(false)
  })
})

describe('usePermission 与 store 变更同步', () => {
  it('登出清空会话后原先通过的权限立即失效', () => {
    grant(['identity.users.read'])
    const { hasPermission } = usePermission()
    expect(hasPermission('identity.users.read')).toBe(true)

    useAuthStore().clear()

    expect(hasPermission('identity.users.read')).toBe(false)
  })

  it('权限后置下发后，先前取得的判定函数立刻认账', () => {
    const { hasPermission } = usePermission()
    expect(hasPermission('access.roles.create')).toBe(false)

    grant(['access.roles.create'])

    expect(hasPermission('access.roles.create')).toBe(true)
  })
})
