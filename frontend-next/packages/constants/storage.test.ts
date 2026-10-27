/**
 * packages/constants/storage.ts 契约测试。
 *
 * 职责边界：存储 key 是跨版本的持久化契约——重复取值会让两项设置互相覆盖，
 * 漏前缀会污染同域下其它应用的 localStorage。这里逐条锁定前缀、唯一性与关键 key 的字面量，
 * 不涉及读写行为（那在 packages/utils/storage.test.ts）。
 */
import { describe, expect, it } from 'vitest'
import {
  LOCALE_KEY,
  LOCK_REASON_KEY,
  LOCK_STATE_KEY,
  REFRESH_TOKEN_KEY,
  STORAGE_PREFIX,
  THEME_MODE_KEY,
  TOKEN_KEY,
  USER_INFO_KEY,
} from './storage'
import * as storageKeys from './storage'

/** 除前缀本身外的全部导出 key。 */
const allKeys = Object.entries(storageKeys)
  .filter(([name]) => name !== 'STORAGE_PREFIX')
  .map(([name, value]) => [name, value as string] as const)

describe('存储 key 命名契约', () => {
  it('前缀固定为 xihan_，用于与同域其它应用隔离', () => {
    expect(STORAGE_PREFIX).toBe('xihan_')
  })

  it('导出的每一个 key 都带统一前缀', () => {
    const missing = allKeys.filter(([, value]) => !value.startsWith(STORAGE_PREFIX))
    expect(missing).toEqual([])
  })

  it('每一个 key 都是非空字符串且前缀之后还有实际名称', () => {
    const invalid = allKeys.filter(([, value]) => value.length <= STORAGE_PREFIX.length)
    expect(invalid).toEqual([])
  })

  it('key 全部使用小写加下划线，不出现驼峰或连字符', () => {
    const invalid = allKeys.filter(([, value]) => !/^xihan_[a-z0-9_]+$/.test(value))
    expect(invalid).toEqual([])
  })
})

describe('存储 key 唯一性', () => {
  it('不存在两个常量指向同一个 key，避免设置互相覆盖', () => {
    const seen = new Map<string, string>()
    const collisions: Array<[string, string, string]> = []

    for (const [name, value] of allKeys) {
      const previous = seen.get(value)
      if (previous) {
        collisions.push([value, previous, name])
      }
      else {
        seen.set(value, name)
      }
    }

    expect(collisions).toEqual([])
  })

  it('导出的 key 数量与去重后的数量相同', () => {
    expect(new Set(allKeys.map(([, value]) => value)).size).toBe(allKeys.length)
  })

  it('导出项全部为字符串常量，没有混入函数或对象', () => {
    const notString = Object.entries(storageKeys).filter(([, value]) => typeof value !== 'string')
    expect(notString).toEqual([])
  })
})

describe('关键 key 的字面量稳定性', () => {
  it('鉴权相关 key 取值固定，改动会让已登录用户的会话全部失效', () => {
    expect(TOKEN_KEY).toBe('xihan_access_token')
    expect(REFRESH_TOKEN_KEY).toBe('xihan_refresh_token')
    expect(USER_INFO_KEY).toBe('xihan_user_info')
  })

  it('锁屏标记与锁屏原因是两个独立 key，不共用一条记录', () => {
    expect(LOCK_STATE_KEY).toBe('xihan_locked')
    expect(LOCK_REASON_KEY).toBe('xihan_lock_reason')
    expect(LOCK_REASON_KEY).not.toBe(LOCK_STATE_KEY)
  })

  it('未定义任何用于保存锁屏口令的 key，口令一律不在客户端留存', () => {
    const passwordLike = allKeys.filter(([name, value]) =>
      /password|passcode|pwd|secret/i.test(name) || /password|passcode|pwd|secret/i.test(value),
    )

    expect(passwordLike).toEqual([])
  })

  it('语言与主题模式 key 取值固定，刷新后偏好才能被读回', () => {
    expect(LOCALE_KEY).toBe('xihan_locale')
    expect(THEME_MODE_KEY).toBe('xihan_theme_mode')
  })
})
