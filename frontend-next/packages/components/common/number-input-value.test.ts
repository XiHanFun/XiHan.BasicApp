/**
 * 数字输入的值换算：固定小数位的回舍、补齐与缺省步长，以及输入途中的草稿显示。
 *
 * 整数字段（人数上限、计费月数）不能提交出 1.5，价格要固定两位小数——
 * 组件库的数字字段没有 precision，这一层由封装自己给。
 */
import { describe, expect, it } from 'vitest'
import {
  assertPrecision,
  defaultStep,
  formatNumber,
  parseNumber,
  resolveDisplayText,
  roundToPrecision,
  toModelValue,
} from './number-input-value'

describe('roundToPrecision', () => {
  it('按位数回舍，0 位即取整', () => {
    expect(roundToPrecision(1.5, 0)).toBe(2)
    expect(roundToPrecision(1.4, 0)).toBe(1)
    expect(roundToPrecision(12.345, 2)).toBe(12.35)
    expect(roundToPrecision(0.1 + 0.2, 2)).toBe(0.3)
  })

  it('不给位数时原样', () => {
    expect(roundToPrecision(1.23456)).toBe(1.23456)
  })

  it('负零收成零，模型里不出现带符号的零', () => {
    expect(Object.is(roundToPrecision(-0), 0)).toBe(true)
    expect(Object.is(roundToPrecision(-0.001, 2), 0)).toBe(true)
  })
})

describe('formatNumber / parseNumber', () => {
  it('给了位数时补齐到固定小数位', () => {
    expect(formatNumber(12.5, 2)).toBe('12.50')
    expect(formatNumber(3, 1)).toBe('3.0')
    expect(formatNumber(2.5, 0)).toBe('3')
    expect(formatNumber(12.5)).toBe('12.5')
  })

  it('读回与组件库缺省同口径：严格 Number()，空串为 NaN', () => {
    expect(parseNumber(' 12.50 ')).toBe(12.5)
    expect(parseNumber('')).toBeNaN()
    expect(parseNumber('12abc')).toBeNaN()
  })

  it('format 的输出能被 parse 读回同一个数，按加号不漂', () => {
    for (const n of [0, 0.01, 1.5, 12.345, 99999.99]) {
      const shown = formatNumber(n, 2)
      expect(formatNumber(parseNumber(shown), 2)).toBe(shown)
    }
  })
})

describe('defaultStep', () => {
  it('不写 step 时按最小一位走', () => {
    expect(defaultStep(0)).toBe(1)
    expect(defaultStep(1)).toBe(0.1)
    expect(defaultStep(2)).toBe(0.01)
    expect(defaultStep(undefined)).toBeUndefined()
  })
})

describe('toModelValue', () => {
  it('空串为 null，成数的串按位数回舍', () => {
    expect(toModelValue('  ')).toBeNull()
    expect(toModelValue('1.5', 0)).toBe(2)
    expect(toModelValue('19.999', 2)).toBe(20)
    expect(toModelValue('-3')).toBe(-3)
  })

  it('还不成数的中间态不上抛', () => {
    expect(toModelValue('-')).toBeUndefined()
    expect(toModelValue('1e')).toBeUndefined()
  })
})

describe('resolveDisplayText', () => {
  it('草稿仍对应当前值时沿用草稿，保住输入途中的写法', () => {
    expect(resolveDisplayText(0, { text: '-0', value: 0 })).toBe('-0')
    expect(resolveDisplayText(1, { text: '1.', value: 1 })).toBe('1.')
    expect(resolveDisplayText(null, { text: '-', value: null })).toBe('-')
  })

  it('值被外部改成别的数时草稿作废，按值格式化', () => {
    expect(resolveDisplayText(5, { text: '1.', value: 1 }, 2)).toBe('5.00')
    expect(resolveDisplayText(null, { text: '3', value: 3 })).toBe('')
    expect(resolveDisplayText(12.5, null, 2)).toBe('12.50')
  })
})

describe('assertPrecision', () => {
  it('只收 0–20 的整数', () => {
    expect(() => assertPrecision(undefined)).not.toThrow()
    expect(() => assertPrecision(0)).not.toThrow()
    expect(() => assertPrecision(20)).not.toThrow()
    expect(() => assertPrecision(1.5)).toThrow(RangeError)
    expect(() => assertPrecision(-1)).toThrow(RangeError)
    expect(() => assertPrecision(21)).toThrow(RangeError)
  })
})
