/**
 * 用户信息 Store（user）单元测试。
 * 职责边界：只覆盖 useUserStore——用户信息落地/还原、各 getter 派生的空值兜底、
 * 角色与权限判定（含 '*' 超管短路）、$reset 清场。
 */
import type { UserInfo } from '~/types'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { USER_INFO_KEY } from '~/constants'
import { useUserStore } from './user'

function makeUser(overrides?: Partial<UserInfo>): UserInfo {
  return {
    basicId: 'u-1',
    userName: 'admin',
    nickName: '管理员',
    avatar: 'https://example.com/a.png',
    roles: ['admin'],
    permissions: ['system:user:list'],
    ...overrides,
  }
}

beforeEach(() => {
  setActivePinia(createPinia())
})

describe('初始状态与本地还原', () => {
  it('无本地缓存时 userInfo 为 null 且未登录', () => {
    const store = useUserStore()

    expect(store.userInfo).toBeNull()
    expect(store.isLoggedIn).toBe(false)
  })

  it('store 初始化时从 localStorage 还原用户信息', () => {
    localStorage.setItem(USER_INFO_KEY, JSON.stringify(makeUser({ userName: 'restored' })))
    setActivePinia(createPinia())

    const store = useUserStore()

    expect(store.username).toBe('restored')
    expect(store.isLoggedIn).toBe(true)
  })

  it('本地缓存是损坏 JSON 时降级为未登录，不抛异常', () => {
    localStorage.setItem(USER_INFO_KEY, '<<broken>>')
    setActivePinia(createPinia())

    const store = useUserStore()

    expect(store.userInfo).toBeNull()
    expect(store.isLoggedIn).toBe(false)
  })
})

describe('登录态判定以 basicId 为准', () => {
  it('basicId 为空串时判为未登录，即使用户名存在', () => {
    const store = useUserStore()

    store.setUserInfo(makeUser({ basicId: '' }))

    expect(store.username).toBe('admin')
    expect(store.isLoggedIn).toBe(false)
  })

  it('basicId 非空即视为已登录', () => {
    const store = useUserStore()

    store.setUserInfo(makeUser({ basicId: '0' }))

    expect(store.isLoggedIn).toBe(true)
  })
})

describe('getter 的缺省兜底', () => {
  it('昵称/头像缺省时返回空串而非 undefined，模板可直接渲染', () => {
    const store = useUserStore()

    store.setUserInfo({ basicId: 'u', userName: 'u', roles: [], permissions: [] })

    expect(store.nickname).toBe('')
    expect(store.avatar).toBe('')
  })

  it('userInfo 为 null 时全部派生 getter 退化为空值', () => {
    const store = useUserStore()

    expect(store.username).toBe('')
    expect(store.nickname).toBe('')
    expect(store.avatar).toBe('')
    expect(store.roles).toEqual([])
    expect(store.permissions).toEqual([])
  })

  it('中文昵称与 emoji 原样保留，不做任何转义', () => {
    const store = useUserStore()

    store.setUserInfo(makeUser({ nickName: '曦寒🚀' }))

    expect(store.nickname).toBe('曦寒🚀')
  })
})

describe('用户信息落地与清除', () => {
  it('setUserInfo 写入 localStorage 的是完整对象', () => {
    const store = useUserStore()
    const info = makeUser()

    store.setUserInfo(info)

    expect(JSON.parse(localStorage.getItem(USER_INFO_KEY) ?? 'null')).toEqual(info)
  })

  it('setUserInfo(null) 移除本地键，而不是写入 null 字面量', () => {
    const store = useUserStore()
    store.setUserInfo(makeUser())

    store.setUserInfo(null)

    expect(localStorage.getItem(USER_INFO_KEY)).toBeNull()
  })

  it('再次 setUserInfo 整体覆盖旧信息，不做字段合并', () => {
    const store = useUserStore()
    store.setUserInfo(makeUser({ nickName: '旧昵称', roles: ['admin'] }))

    store.setUserInfo({ basicId: 'u-2', userName: 'guest', roles: [], permissions: [] })

    expect(store.nickname).toBe('')
    expect(store.roles).toEqual([])
  })
})

describe('角色与权限判定', () => {
  it('hasRole 全等匹配，不做大小写归一', () => {
    const store = useUserStore()
    store.setUserInfo(makeUser({ roles: ['admin'] }))

    expect(store.hasRole('admin')).toBe(true)
    expect(store.hasRole('Admin')).toBe(false)
  })

  it('hasRole 不支持 "*" 通配 —— 角色没有超管短路', () => {
    const store = useUserStore()
    store.setUserInfo(makeUser({ roles: ['*'] }))

    expect(store.hasRole('admin')).toBe(false)
    expect(store.hasRole('*')).toBe(true)
  })

  it('hasPermission 命中 "*" 时任意权限码短路通过', () => {
    const store = useUserStore()
    store.setUserInfo(makeUser({ permissions: ['*'] }))

    expect(store.hasPermission('any:code')).toBe(true)
    expect(store.hasPermission('')).toBe(true)
  })

  it('权限码列表为空时任意权限均判定为无', () => {
    const store = useUserStore()
    store.setUserInfo(makeUser({ permissions: [] }))

    expect(store.hasPermission('system:user:list')).toBe(false)
  })

  it('hasAnyRole 命中任意一个即为 true', () => {
    const store = useUserStore()
    store.setUserInfo(makeUser({ roles: ['editor'] }))

    expect(store.hasAnyRole(['admin', 'editor'])).toBe(true)
  })

  it('hasAnyRole 传空数组返回 false —— 空要求不等于放行', () => {
    const store = useUserStore()
    store.setUserInfo(makeUser({ roles: ['admin'] }))

    expect(store.hasAnyRole([])).toBe(false)
  })

  it('hasAnyRole 全部不命中返回 false', () => {
    const store = useUserStore()
    store.setUserInfo(makeUser({ roles: ['viewer'] }))

    expect(store.hasAnyRole(['admin', 'editor'])).toBe(false)
  })

  it('未登录时 hasRole / hasPermission / hasAnyRole 一律为 false', () => {
    const store = useUserStore()

    expect(store.hasRole('admin')).toBe(false)
    expect(store.hasPermission('*')).toBe(false)
    expect(store.hasAnyRole(['admin'])).toBe(false)
  })
})

describe('$reset', () => {
  it('$reset 清空内存并删除本地用户信息键', () => {
    const store = useUserStore()
    store.setUserInfo(makeUser())

    store.$reset()

    expect(store.userInfo).toBeNull()
    expect(store.isLoggedIn).toBe(false)
    expect(localStorage.getItem(USER_INFO_KEY)).toBeNull()
  })

  it('$reset 后权限通配随之失效', () => {
    const store = useUserStore()
    store.setUserInfo(makeUser({ permissions: ['*'] }))

    store.$reset()

    expect(store.hasPermission('any')).toBe(false)
  })
})
