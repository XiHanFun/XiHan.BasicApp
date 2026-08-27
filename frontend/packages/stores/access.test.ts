/**
 * 访问控制 Store（access）单元测试。
 * 职责边界：只覆盖 useAccessStore 自身——令牌读写与 localStorage 落地、
 * 权限码判定（含通配 '*' 短路）、路由装载标记、homePath 派生与 $reset 清场。
 * 不涉及登录流程（见 auth.test.ts）与用户信息（见 user.test.ts）。
 */
import type { MenuRoute } from '~/types'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { HOME_PATH, REFRESH_TOKEN_KEY, TOKEN_KEY } from '~/constants'
import { useAccessStore } from './access'

function menu(path: string, name: string, extra?: Partial<MenuRoute>): MenuRoute {
  return { path, name, meta: { title: name }, ...extra }
}

beforeEach(() => {
  setActivePinia(createPinia())
})

describe('初始状态与本地存储还原', () => {
  it('localStorage 无令牌时 accessToken/refreshToken 均为 null 而非空串', () => {
    const store = useAccessStore()

    expect(store.accessToken).toBeNull()
    expect(store.refreshToken).toBeNull()
  })

  it('store 初始化时从 localStorage 还原两枚令牌', () => {
    localStorage.setItem(TOKEN_KEY, JSON.stringify('at-1'))
    localStorage.setItem(REFRESH_TOKEN_KEY, JSON.stringify('rt-1'))
    setActivePinia(createPinia())

    const store = useAccessStore()

    expect(store.accessToken).toBe('at-1')
    expect(store.refreshToken).toBe('rt-1')
  })

  it('localStorage 中的令牌是损坏 JSON 时降级为 null，不抛异常', () => {
    localStorage.setItem(TOKEN_KEY, '{不是 JSON')
    setActivePinia(createPinia())

    expect(() => useAccessStore()).not.toThrow()
    expect(useAccessStore().accessToken).toBeNull()
  })

  it('初始 accessRoutes 为空且 isRoutesLoaded 为 false，登录守卫据此决定拉取菜单', () => {
    const store = useAccessStore()

    expect(store.accessRoutes).toEqual([])
    expect(store.isRoutesLoaded).toBe(false)
    expect(store.loginExpired).toBe(false)
  })
})

describe('令牌写入与清除', () => {
  it('设置 accessToken 同时落地 localStorage（JSON 序列化）', () => {
    const store = useAccessStore()

    store.setAccessToken('token-abc')

    expect(localStorage.getItem(TOKEN_KEY)).toBe(JSON.stringify('token-abc'))
  })

  it('accessToken 传 null 时从 localStorage 移除该键，而不是写入 "null"', () => {
    const store = useAccessStore()
    store.setAccessToken('token-abc')

    store.setAccessToken(null)

    expect(store.accessToken).toBeNull()
    expect(localStorage.getItem(TOKEN_KEY)).toBeNull()
  })

  it('accessToken 传空串按「无令牌」处理并移除本地键', () => {
    const store = useAccessStore()
    store.setAccessToken('token-abc')

    store.setAccessToken('')

    expect(store.accessToken).toBe('')
    expect(localStorage.getItem(TOKEN_KEY)).toBeNull()
  })

  it('refreshToken 与 accessToken 用不同的存储键，互不覆盖', () => {
    const store = useAccessStore()

    store.setAccessToken('a')
    store.setRefreshToken('r')

    expect(localStorage.getItem(TOKEN_KEY)).toBe(JSON.stringify('a'))
    expect(localStorage.getItem(REFRESH_TOKEN_KEY)).toBe(JSON.stringify('r'))
    expect(TOKEN_KEY).not.toBe(REFRESH_TOKEN_KEY)
  })

  it('清除 refreshToken 不会连带清除 accessToken', () => {
    const store = useAccessStore()
    store.setAccessToken('a')
    store.setRefreshToken('r')

    store.setRefreshToken(null)

    expect(store.accessToken).toBe('a')
    expect(localStorage.getItem(TOKEN_KEY)).toBe(JSON.stringify('a'))
    expect(localStorage.getItem(REFRESH_TOKEN_KEY)).toBeNull()
  })
})

describe('权限码判定', () => {
  it('权限码命中返回 true，未命中返回 false（区分大小写、不做前缀匹配）', () => {
    const store = useAccessStore()
    store.setAccessCodes(['system:user:list'])

    expect(store.hasCode('system:user:list')).toBe(true)
    expect(store.hasCode('System:User:List')).toBe(false)
    expect(store.hasCode('system:user')).toBe(false)
    expect(store.hasCode('system:user:list:extra')).toBe(false)
  })

  it('权限码包含 * 时任意码短路通过（超管）', () => {
    const store = useAccessStore()
    store.setAccessCodes(['*'])

    expect(store.hasCode('anything:at:all')).toBe(true)
    expect(store.hasCode('')).toBe(true)
  })

  it('通配只认整条 "*"，"system:*" 这类前缀通配不生效', () => {
    const store = useAccessStore()
    store.setAccessCodes(['system:*'])

    expect(store.hasCode('system:user:list')).toBe(false)
    expect(store.hasCode('system:*')).toBe(true)
  })

  it('权限码为空数组时任何码都判定为无权限', () => {
    const store = useAccessStore()
    store.setAccessCodes([])

    expect(store.hasCode('system:user:list')).toBe(false)
    expect(store.hasCode('*')).toBe(false)
  })

  it('中文与 emoji 权限码按原样全等比较', () => {
    const store = useAccessStore()
    store.setAccessCodes(['系统:用户:列表', '🚀:deploy'])

    expect(store.hasCode('系统:用户:列表')).toBe(true)
    expect(store.hasCode('🚀:deploy')).toBe(true)
    expect(store.hasCode('系统:用户')).toBe(false)
  })

  it('setAccessCodes 整体替换而不是追加，旧权限码立即失效', () => {
    const store = useAccessStore()
    store.setAccessCodes(['a'])

    store.setAccessCodes(['b'])

    expect(store.hasCode('a')).toBe(false)
    expect(store.hasCode('b')).toBe(true)
  })
})

describe('路由装载与 homePath 派生', () => {
  it('setAccessRoutes 会把 isRoutesLoaded 置为 true —— 即使传入空数组也算「已拉取」', () => {
    const store = useAccessStore()

    store.setAccessRoutes([])

    expect(store.isRoutesLoaded).toBe(true)
    expect(store.homePath).toBe(HOME_PATH)
  })

  it('homePath 取第一条可导航路由的完整路径', () => {
    const store = useAccessStore()

    store.setAccessRoutes([menu('/system', 'System', {
      children: [menu('user', 'SystemUser')],
    })])

    expect(store.homePath).toBe('/system/user')
  })

  it('隐藏路由被跳过，homePath 落到第一条可见路由', () => {
    const store = useAccessStore()

    store.setAccessRoutes([
      { path: '/hidden', name: 'Hidden', meta: { title: 'x', hidden: true } },
      menu('/visible', 'Visible'),
    ])

    expect(store.homePath).toBe('/visible')
  })

  it('全部路由都隐藏且无可见子路由时回落到 HOME_PATH', () => {
    const store = useAccessStore()

    store.setAccessRoutes([
      { path: '/hidden', name: 'Hidden', meta: { title: 'x', hidden: true } },
    ])

    expect(store.homePath).toBe(HOME_PATH)
  })

  it('homePath 是响应式派生：路由变更后立即反映新首页', () => {
    const store = useAccessStore()
    store.setAccessRoutes([menu('/first', 'First')])
    expect(store.homePath).toBe('/first')

    store.setAccessRoutes([menu('/second', 'Second')])

    expect(store.homePath).toBe('/second')
  })
})

describe('登录过期标记与 $reset', () => {
  it('setLoginExpired 只切换标记，不动令牌', () => {
    const store = useAccessStore()
    store.setAccessToken('a')

    store.setLoginExpired(true)

    expect(store.loginExpired).toBe(true)
    expect(store.accessToken).toBe('a')
  })

  it('$reset 清空全部内存状态并删除两枚令牌的本地键', () => {
    const store = useAccessStore()
    store.setAccessToken('a')
    store.setRefreshToken('r')
    store.setAccessCodes(['x'])
    store.setAccessRoutes([menu('/a', 'A')])
    store.setLoginExpired(true)

    store.$reset()

    expect(store.accessToken).toBeNull()
    expect(store.refreshToken).toBeNull()
    expect(store.accessCodes).toEqual([])
    expect(store.accessRoutes).toEqual([])
    expect(store.isRoutesLoaded).toBe(false)
    expect(store.loginExpired).toBe(false)
    expect(localStorage.getItem(TOKEN_KEY)).toBeNull()
    expect(localStorage.getItem(REFRESH_TOKEN_KEY)).toBeNull()
    expect(store.homePath).toBe(HOME_PATH)
  })

  it('$reset 后权限码通配也一并失效', () => {
    const store = useAccessStore()
    store.setAccessCodes(['*'])

    store.$reset()

    expect(store.hasCode('any')).toBe(false)
  })
})
