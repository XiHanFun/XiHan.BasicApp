/**
 * usePermission 权限判定单元测试。
 * 职责：锁定与 userStore / accessStore 的判定一致性——
 * 用户权限位与访问码任一命中即通过、通配符 `*` 放行、空入参一律放行，
 * 以及角色判定只看 userStore.roles（不参与访问码回退）。
 */
import type { UserInfo } from '~/types'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { useAccessStore, useUserStore } from '~/stores'
import { usePermission } from './usePermission'

function makeUser(permissions: string[], roles: string[] = []): UserInfo {
  return {
    basicId: 'u-1',
    userName: 'tester',
    roles,
    permissions,
  }
}

beforeEach(() => {
  setActivePinia(createPinia())
})

describe('usePermission.hasPermission 与 store 判定一致', () => {
  it('用户权限位命中即通过', () => {
    useUserStore().setUserInfo(makeUser(['sys:user:list']))

    expect(usePermission().hasPermission('sys:user:list')).toBe(true)
  })

  it('用户权限位不含时回退查访问码，访问码命中同样通过', () => {
    useUserStore().setUserInfo(makeUser([]))
    useAccessStore().setAccessCodes(['sys:role:add'])

    expect(usePermission().hasPermission('sys:role:add')).toBe(true)
  })

  it('两个 store 都不含时判定为无权限', () => {
    useUserStore().setUserInfo(makeUser(['sys:user:list']))
    useAccessStore().setAccessCodes(['sys:role:add'])

    expect(usePermission().hasPermission('sys:user:delete')).toBe(false)
  })

  it('用户权限位里的通配符 * 放行任意权限码', () => {
    useUserStore().setUserInfo(makeUser(['*']))

    expect(usePermission().hasPermission('anything:at:all')).toBe(true)
  })

  it('访问码里的通配符 * 同样放行任意权限码', () => {
    useUserStore().setUserInfo(makeUser([]))
    useAccessStore().setAccessCodes(['*'])

    expect(usePermission().hasPermission('anything:at:all')).toBe(true)
  })

  it('未登录（userInfo 为 null）且无访问码时一律拒绝', () => {
    expect(usePermission().hasPermission('sys:user:list')).toBe(false)
  })

  it('权限码大小写敏感，不做归一化', () => {
    useUserStore().setUserInfo(makeUser(['Sys:User:List']))

    expect(usePermission().hasPermission('sys:user:list')).toBe(false)
    expect(usePermission().hasPermission('Sys:User:List')).toBe(true)
  })

  it('权限码含中文与 emoji 时按原样全等比较', () => {
    useUserStore().setUserInfo(makeUser(['系统:用户:查看🚀']))

    expect(usePermission().hasPermission('系统:用户:查看🚀')).toBe(true)
    expect(usePermission().hasPermission('系统:用户:查看')).toBe(false)
  })
})

describe('usePermission.hasPermission 空值与数组分支', () => {
  it('传空串视为不限制，直接放行', () => {
    expect(usePermission().hasPermission('')).toBe(true)
  })

  it('传空数组落进「或」判定，没有任何候选因此拒绝', () => {
    useUserStore().setUserInfo(makeUser(['*']))

    expect(usePermission().hasPermission([])).toBe(false)
  })

  it('数组形式只要有一项命中就放行', () => {
    useUserStore().setUserInfo(makeUser(['sys:user:list']))

    expect(usePermission().hasPermission(['sys:role:add', 'sys:user:list'])).toBe(true)
  })

  it('数组形式全部未命中才拒绝', () => {
    useUserStore().setUserInfo(makeUser(['sys:user:list']))

    expect(usePermission().hasPermission(['sys:role:add', 'sys:dept:add'])).toBe(false)
  })

  it('数组里的空串项不享受「不限制」豁免，仍按空权限码逐项比对而拒绝', () => {
    expect(usePermission().hasPermission(['sys:none', ''])).toBe(false)
  })
})

describe('usePermission.hasRole 只看用户角色', () => {
  it('角色命中即通过', () => {
    useUserStore().setUserInfo(makeUser([], ['admin']))

    expect(usePermission().hasRole('admin')).toBe(true)
  })

  it('角色未命中判定失败', () => {
    useUserStore().setUserInfo(makeUser([], ['admin']))

    expect(usePermission().hasRole('auditor')).toBe(false)
  })

  it('角色判定不吃访问码通配符，访问码为 * 也不放行角色', () => {
    useUserStore().setUserInfo(makeUser([], []))
    useAccessStore().setAccessCodes(['*'])

    expect(usePermission().hasRole('admin')).toBe(false)
  })

  it('角色判定不吃权限位通配符，权限为 * 也不放行角色', () => {
    useUserStore().setUserInfo(makeUser(['*'], []))

    expect(usePermission().hasRole('admin')).toBe(false)
  })

  it('传空串视为不限制角色，直接放行', () => {
    expect(usePermission().hasRole('')).toBe(true)
  })

  it('传空数组时没有候选角色，拒绝', () => {
    useUserStore().setUserInfo(makeUser([], ['admin']))

    expect(usePermission().hasRole([])).toBe(false)
  })

  it('数组形式任一角色命中即通过', () => {
    useUserStore().setUserInfo(makeUser([], ['auditor']))

    expect(usePermission().hasRole(['admin', 'auditor'])).toBe(true)
  })
})

describe('usePermission.hasAnyPermission', () => {
  it('空数组没有候选，判定为 false', () => {
    useUserStore().setUserInfo(makeUser(['*']))

    expect(usePermission().hasAnyPermission([])).toBe(false)
  })

  it('任一权限命中即通过', () => {
    useUserStore().setUserInfo(makeUser([]))
    useAccessStore().setAccessCodes(['sys:dept:list'])

    expect(usePermission().hasAnyPermission(['sys:user:list', 'sys:dept:list'])).toBe(true)
  })

  // 回归锚点（清单条目 16）：空串项不得触发「不限制」豁免而整组放行，
  // 且必须与 hasPermission(同一数组) 得出相同结论。
  it('数组含空串项时空串被剔除，未登录用户仍被拒绝', () => {
    expect(usePermission().hasAnyPermission(['sys:none', ''])).toBe(false)
    expect(usePermission().hasPermission(['sys:none', ''])).toBe(false)
  })

  // 回归锚点（清单条目 16）：全是空串等价于没有任何候选，按空数组口径拒绝。
  it('数组全部为空串时判定为 false，不整组放行', () => {
    useUserStore().setUserInfo(makeUser(['sys:user:list']))

    expect(usePermission().hasAnyPermission(['', ''])).toBe(false)
  })

  // 回归锚点（清单条目 16）：剔除空串不能误伤同数组里真实命中的权限码。
  it('空串与有效权限码混排时，有效项命中仍然放行', () => {
    useUserStore().setUserInfo(makeUser(['sys:user:list']))

    expect(usePermission().hasAnyPermission(['', 'sys:user:list'])).toBe(true)
  })

  it('全部未命中才判定为 false', () => {
    useUserStore().setUserInfo(makeUser(['sys:user:list']))

    expect(usePermission().hasAnyPermission(['a', 'b', 'c'])).toBe(false)
  })
})

describe('usePermission 与 store 变更同步', () => {
  it('登出清空用户信息后原先通过的权限立即失效', () => {
    const userStore = useUserStore()
    userStore.setUserInfo(makeUser(['sys:user:list']))
    const { hasPermission } = usePermission()

    expect(hasPermission('sys:user:list')).toBe(true)

    userStore.$reset()

    expect(hasPermission('sys:user:list')).toBe(false)
  })

  it('访问码后置下发后，先前取得的判定函数立刻认账', () => {
    const { hasPermission } = usePermission()
    expect(hasPermission('sys:role:add')).toBe(false)

    useAccessStore().setAccessCodes(['sys:role:add'])

    expect(hasPermission('sys:role:add')).toBe(true)
  })
})
