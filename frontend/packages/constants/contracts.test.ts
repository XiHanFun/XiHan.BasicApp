/**
 * packages/constants 跨模块契约测试（http / route / layout-events / user-setting /
 * notification / tenant / index 汇总）。
 *
 * 职责边界：这些常量都是与后端或与 shell 其它层的**约定**——状态码与后端一致、
 * 场景枚举与后端 UserSettingScene 一致、静态选项覆盖枚举的全部成员、
 * 事件名不与其它自定义事件撞名、汇总入口不丢导出。逐条锁定，改一处即失败。
 */
import { afterEach, describe, expect, it, vi } from 'vitest'
import { NotificationType, TenantMemberType } from '~/types/enums'
import { BIZ_CODE, HTTP_STATUS } from './http'
import * as barrel from './index'
import {
  LAYOUT_EVENT_LOCK_SCREEN,
  LAYOUT_EVENT_NAMES,
  LAYOUT_EVENT_OPEN_GLOBAL_SEARCH,
  LAYOUT_EVENT_OPEN_PREFERENCE_DRAWER,
  LAYOUT_EVENT_TOGGLE_SIDEBAR_REQUEST,
} from './layout-events'
import { NOTIFICATION_TYPE_OPTIONS } from './notification'
import {
  AUTH_PATH,
  CODE_LOGIN_PATH,
  EMAIL_LOGIN_PATH,
  FORBIDDEN_PATH,
  HOME_PATH,
  LOGIN_PATH,
  NOT_FOUND_PATH,
  QRCODE_LOGIN_PATH,
  SERVER_ERROR_PATH,
} from './route'
import { MEMBER_TYPE_OPTIONS } from './tenant'
import {
  FAVORITES_SETTING_KEY,
  PREFERENCE_SETTING_KEY,
  UserSettingScene,
} from './user-setting'

// 多条用例通过 resetModules + 动态导入重新求值模块，必须逐条还原，保证任意顺序执行
afterEach(() => {
  vi.unstubAllEnvs()
  vi.unstubAllGlobals()
  vi.resetModules()
})

describe('状态码契约（HTTP 与业务码）', () => {
  it('九个 HTTP 状态码取标准值', () => {
    expect(HTTP_STATUS).toEqual({
      OK: 200,
      CREATED: 201,
      NO_CONTENT: 204,
      BAD_REQUEST: 400,
      UNAUTHORIZED: 401,
      FORBIDDEN: 403,
      NOT_FOUND: 404,
      LOCKED: 423,
      INTERNAL_SERVER_ERROR: 500,
    })
  })

  it('锁屏用 423 而不是 401，客户端据此展示锁屏遮罩而非跳登录页', () => {
    expect(HTTP_STATUS.LOCKED).toBe(423)
    expect(HTTP_STATUS.LOCKED).not.toBe(HTTP_STATUS.UNAUTHORIZED)
  })

  it('业务码中的锁屏与 HTTP 423 对齐', () => {
    expect(BIZ_CODE.LOCKED).toBe(HTTP_STATUS.LOCKED)
  })

  it('业务码里与 HTTP 同名的三项取值一致，避免两套语义', () => {
    expect(BIZ_CODE.UNAUTHORIZED).toBe(HTTP_STATUS.UNAUTHORIZED)
    expect(BIZ_CODE.FORBIDDEN).toBe(HTTP_STATUS.FORBIDDEN)
    expect(BIZ_CODE.SUCCESS).toBe(HTTP_STATUS.OK)
  })

  it('令牌过期与刷新令牌过期是两个不同的业务码，拦截器据此决定刷新还是登出', () => {
    expect(BIZ_CODE.TOKEN_EXPIRED).toBe(4001)
    expect(BIZ_CODE.REFRESH_TOKEN_EXPIRED).toBe(4002)
    expect(BIZ_CODE.TOKEN_EXPIRED).not.toBe(BIZ_CODE.REFRESH_TOKEN_EXPIRED)
  })

  it('两组状态码内部各自取值唯一', () => {
    const httpValues = Object.values(HTTP_STATUS)
    const bizValues = Object.values(BIZ_CODE)
    expect(new Set(httpValues).size).toBe(httpValues.length)
    expect(new Set(bizValues).size).toBe(bizValues.length)
  })
})

describe('布局事件名', () => {
  it('清单恰好收录四个事件且与各自常量一致', () => {
    expect(LAYOUT_EVENT_NAMES).toEqual([
      LAYOUT_EVENT_TOGGLE_SIDEBAR_REQUEST,
      LAYOUT_EVENT_OPEN_PREFERENCE_DRAWER,
      LAYOUT_EVENT_OPEN_GLOBAL_SEARCH,
      LAYOUT_EVENT_LOCK_SCREEN,
    ])
  })

  it('事件名互不重复，否则一次派发会触发多个监听', () => {
    expect(new Set(LAYOUT_EVENT_NAMES).size).toBe(LAYOUT_EVENT_NAMES.length)
  })

  it('全部以 xihan- 前缀命名，避免与第三方库的自定义事件撞名', () => {
    const wrong = LAYOUT_EVENT_NAMES.filter(name => !name.startsWith('xihan-'))
    expect(wrong).toEqual([])
  })

  it('事件名可直接用于 DOM 自定义事件的派发与监听', () => {
    const received: string[] = []
    const handler = (event: Event): void => {
      received.push(event.type)
    }

    for (const name of LAYOUT_EVENT_NAMES) {
      window.addEventListener(name, handler)
    }
    for (const name of LAYOUT_EVENT_NAMES) {
      window.dispatchEvent(new CustomEvent(name))
    }
    for (const name of LAYOUT_EVENT_NAMES) {
      window.removeEventListener(name, handler)
    }

    expect(received).toEqual([...LAYOUT_EVENT_NAMES])

    // 移除监听后再派发不应再收到，确认清理生效
    for (const name of LAYOUT_EVENT_NAMES) {
      window.dispatchEvent(new CustomEvent(name))
    }
    expect(received).toHaveLength(LAYOUT_EVENT_NAMES.length)
  })
})

describe('路由路径常量', () => {
  it('全部登录入口都挂在 /auth 之下', () => {
    for (const path of [LOGIN_PATH, CODE_LOGIN_PATH, EMAIL_LOGIN_PATH, QRCODE_LOGIN_PATH]) {
      expect(path.startsWith(`${AUTH_PATH}/`)).toBe(true)
    }
  })

  it('错误页路径与 HTTP 状态码一一对应', () => {
    expect(NOT_FOUND_PATH).toBe(`/${HTTP_STATUS.NOT_FOUND}`)
    expect(FORBIDDEN_PATH).toBe(`/${HTTP_STATUS.FORBIDDEN}`)
    expect(SERVER_ERROR_PATH).toBe(`/${HTTP_STATUS.INTERNAL_SERVER_ERROR}`)
  })

  it('全部路径以斜杠开头且互不重复', () => {
    const paths = [
      AUTH_PATH,
      LOGIN_PATH,
      CODE_LOGIN_PATH,
      EMAIL_LOGIN_PATH,
      QRCODE_LOGIN_PATH,
      NOT_FOUND_PATH,
      FORBIDDEN_PATH,
      SERVER_ERROR_PATH,
    ]

    expect(paths.filter(path => !path.startsWith('/'))).toEqual([])
    expect(new Set(paths).size).toBe(paths.length)
  })

  it('首页兜底路径由 .env 的 VITE_HOME_PATH 提供，没被降级成根路径', () => {
    expect(HOME_PATH).toBe('/workbench/dashboard')
  })

  it('环境变量 VITE_HOME_PATH 缺省时兜底为根路径', async () => {
    vi.resetModules()
    vi.stubEnv('VITE_HOME_PATH', '')
    const mod = await import('./route')

    expect(mod.HOME_PATH).toBe('/')

    vi.unstubAllEnvs()
    vi.resetModules()
  })
})

describe('用户设置场景', () => {
  it('场景取值与后端 UserSettingScene 一致：偏好 0、页面 1', () => {
    expect(UserSettingScene.Preference).toBe(0)
    expect(UserSettingScene.Page).toBe(1)
  })

  it('场景取值互不相同，(scene, settingKey) 才能唯一定位一条记录', () => {
    const values = Object.values(UserSettingScene)
    expect(new Set(values).size).toBe(values.length)
  })

  it('偏好与收藏夹是同一场景下的两个不同 settingKey', () => {
    expect(PREFERENCE_SETTING_KEY).toBe('global')
    expect(FAVORITES_SETTING_KEY).toBe('favorites')
    expect(PREFERENCE_SETTING_KEY).not.toBe(FAVORITES_SETTING_KEY)
  })

  it('本端会话标识是标准 UUID，且同一模块实例内多次取用保持不变', async () => {
    vi.resetModules()
    const first = await import('./user-setting')
    const second = await import('./user-setting')

    expect(first.USER_SETTING_CLIENT_ID).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/,
    )
    expect(second.USER_SETTING_CLIENT_ID).toBe(first.USER_SETTING_CLIENT_ID)
  })

  it('每次模块求值产出不同的会话标识，两个标签页不会互相误判为自身回显', async () => {
    vi.resetModules()
    const first = (await import('./user-setting')).USER_SETTING_CLIENT_ID
    vi.resetModules()
    const second = (await import('./user-setting')).USER_SETTING_CLIENT_ID

    expect(second).not.toBe(first)
  })

  it('crypto.randomUUID 不可用时退回时间戳加随机串，仍然产出非空标识', async () => {
    vi.resetModules()
    vi.stubGlobal('crypto', undefined)
    const fallback = (await import('./user-setting')).USER_SETTING_CLIENT_ID

    expect(fallback).toMatch(/^\d+-[a-z0-9]+$/)
  })
})

describe('静态选项与后端枚举对齐', () => {
  it('通知类型选项覆盖 NotificationType 的全部成员，不多不少', () => {
    expect(NOTIFICATION_TYPE_OPTIONS.map(option => option.value)).toEqual(
      Object.values(NotificationType),
    )
  })

  it('通知类型选项的兜底中文标签非空且互不重复', () => {
    const labels = NOTIFICATION_TYPE_OPTIONS.map(option => option.label)
    expect(labels.filter(label => label.trim() === '')).toEqual([])
    expect(new Set(labels).size).toBe(labels.length)
  })

  it('租户成员类型选项覆盖 TenantMemberType 的全部成员，不多不少', () => {
    expect(MEMBER_TYPE_OPTIONS.map(option => option.value)).toEqual(
      Object.values(TenantMemberType),
    )
  })

  it('租户成员类型选项的兜底中文标签非空且互不重复', () => {
    const labels = MEMBER_TYPE_OPTIONS.map(option => option.label)
    expect(labels.filter(label => label.trim() === '')).toEqual([])
    expect(new Set(labels).size).toBe(labels.length)
  })

  it('两组选项的 value 都是后端序列化用的字符串枚举值，不是数字下标', () => {
    const values = [
      ...NOTIFICATION_TYPE_OPTIONS.map(option => option.value),
      ...MEMBER_TYPE_OPTIONS.map(option => option.value),
    ]
    expect(values.filter(value => typeof value !== 'string')).toEqual([])
  })

  it('平台管理员是租户成员类型中的一员，配额统计需要能识别它', () => {
    expect(MEMBER_TYPE_OPTIONS.map(option => option.value)).toContain(
      TenantMemberType.PlatformAdmin,
    )
  })
})

describe('汇总入口', () => {
  it('index 转出全部子模块导出，没有被同名导出静默覆盖', async () => {
    vi.resetModules()
    const modules = await Promise.all([
      import('./app'),
      import('./http'),
      import('./layout-events'),
      import('./notification'),
      import('./route'),
      import('./storage'),
      import('./tenant'),
      import('./user-setting'),
    ])

    const missing: string[] = []
    for (const mod of modules) {
      for (const name of Object.keys(mod)) {
        if (!(name in barrel)) {
          missing.push(name)
        }
      }
    }

    expect(missing).toEqual([])
  })

  it('汇总入口转出的是子模块的同一份实例，而不是复制或改写后的值', async () => {
    vi.resetModules()
    const [combined, http, storage] = await Promise.all([
      import('./index'),
      import('./http'),
      import('./storage'),
    ])

    expect(combined.HTTP_STATUS).toBe(http.HTTP_STATUS)
    expect(combined.TOKEN_KEY).toBe(storage.TOKEN_KEY)
  })
})
