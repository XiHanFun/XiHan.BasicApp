/**
 * 手机号码工具单元测试。
 *
 * 职责边界：号码正规化为 E.164、E.164 反解为「国家 + 本地号码」、默认国家取值、
 * 国家选项列表的形状。号码规则本身由 libphonenumber-js 保证，这里只锁本仓库的约定。
 */
import { describe, expect, it } from 'vitest'
import { defaultPhoneCountry, normalizePhone, parsePhone, phoneCountryOptions } from './phone'

describe('normalizePhone', () => {
  it('本地号码按所选国家转成 E.164', () => {
    expect(normalizePhone('0912345678', 'TW')).toBe('+886912345678')
    expect(normalizePhone('13800138000', 'CN')).toBe('+8613800138000')
  })

  it('忽略空格与连字符', () => {
    expect(normalizePhone('09 1234-5678', 'TW')).toBe('+886912345678')
  })

  it('号码在该国家不成立时返回 null', () => {
    expect(normalizePhone('0912345', 'TW')).toBeNull()
    expect(normalizePhone('', 'TW')).toBeNull()
  })
})

describe('parsePhone', () => {
  it('反解 E.164 为国家与本地号码', () => {
    expect(parsePhone('+886912345678')).toEqual({ country: 'TW', national: '0912345678' })
  })

  it('空值或无法解析时返回 null', () => {
    expect(parsePhone(null)).toBeNull()
    expect(parsePhone('12345')).toBeNull()
  })
})

describe('defaultPhoneCountry', () => {
  it('取浏览器语言里第一个带地区的语言', () => {
    expect(defaultPhoneCountry(['zh-TW', 'zh', 'en-US'])).toBe('TW')
    expect(defaultPhoneCountry(['de-DE'])).toBe('DE')
  })

  it('没有地区信息时回退 CN', () => {
    expect(defaultPhoneCountry(['zh', 'en'])).toBe('CN')
    expect(defaultPhoneCountry([])).toBe('CN')
  })
})

describe('phoneCountryOptions', () => {
  it('每个选项都带国家码、本地化国家名与拨号前缀', () => {
    const options = phoneCountryOptions('zh-CN')
    const tw = options.find(option => option.value === 'TW')

    expect(tw).toBeDefined()
    expect(tw!.dialCode).toBe('+886')
    expect(tw!.label).toContain('+886')
    expect(options.length).toBeGreaterThan(200)
  })

  it('国家名跟随传入语言', () => {
    const de = phoneCountryOptions('de-DE').find(option => option.value === 'JP')
    expect(de!.label).toContain('Japan')
  })
})
