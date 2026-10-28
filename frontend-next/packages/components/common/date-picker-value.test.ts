/**
 * 日期选择封装的值换算、区间草稿与段位组接线。
 *
 * 区间：组件库按位存放两端，只填了一端时是 [起, ''] 或 ['', 止]。封装此前只认两端齐备，
 * 填到一半的那次变化被丢掉、受控值又把它抹回去，在段位里键入的区间永远存不下来；
 * 这里钉住「一端时不上抛、交给草稿，两端齐备才上抛，清空上抛 null」。
 */
import { describe, expect, it } from 'vitest'
import {
  isoToTimestamp,
  rangeToDraft,
  resolveRangeChange,
  segmentLiteralBefore,
  timestampToIso,
} from './date-picker-value'
import { endSegmentGroupWiring, segmentGroupWiring } from './date-picker-wiring'

describe('时间戳与 ISO 串互换', () => {
  const ts = new Date(2026, 9, 1, 9, 30, 45).getTime()

  it('日期档只取本地日历日，带时刻档取到时:分', () => {
    expect(timestampToIso(ts, false)).toBe('2026-10-01')
    expect(timestampToIso(ts, true)).toBe('2026-10-01T09:30')
  })

  it('本地零点不因 UTC 偏移挪到前一天', () => {
    expect(timestampToIso(new Date(2026, 0, 1).getTime(), false)).toBe('2026-01-01')
  })

  it('日期串解为当日零点，日期时间串带上时刻', () => {
    expect(isoToTimestamp('2026-10-01')).toBe(new Date(2026, 9, 1).getTime())
    expect(isoToTimestamp('2026-10-01T09:30')).toBe(new Date(2026, 9, 1, 9, 30).getTime())
    expect(isoToTimestamp('2026-10-01T09:30:15')).toBe(new Date(2026, 9, 1, 9, 30, 15).getTime())
  })

  it('选过时刻再换算回来，时:分原样保留（重选不归零）', () => {
    const picked = isoToTimestamp('2026-10-01T18:05')!
    expect(timestampToIso(picked, true)).toBe('2026-10-01T18:05')
  })

  it('空串与写坏的串按无值', () => {
    expect(isoToTimestamp('')).toBeNull()
    expect(isoToTimestamp('2026-10')).toBeNull()
    expect(isoToTimestamp('not-a-date')).toBeNull()
  })
})

describe('段位分隔按段类型出', () => {
  it('日期段之间 -，日期与时刻之间空格，时刻段之间 :', () => {
    expect(segmentLiteralBefore('month')).toBe('-')
    expect(segmentLiteralBefore('day')).toBe('-')
    expect(segmentLiteralBefore('year')).toBe('-')
    expect(segmentLiteralBefore('hour')).toBe(' ')
    expect(segmentLiteralBefore('minute')).toBe(':')
    expect(segmentLiteralBefore('second')).toBe(':')
    expect(segmentLiteralBefore('dayPeriod')).toBe(' ')
  })
})

describe('区间草稿', () => {
  const start = new Date(2026, 8, 1).getTime()
  const end = new Date(2026, 8, 30).getTime()

  it('受控值转成两端日期串，无值是空数组', () => {
    expect(rangeToDraft([start, end])).toEqual(['2026-09-01', '2026-09-30'])
    expect(rangeToDraft(null)).toEqual([])
    expect(rangeToDraft(undefined)).toEqual([])
  })

  it('两端齐备才上抛 [起, 止]', () => {
    expect(resolveRangeChange(['2026-09-01', '2026-09-30'])).toEqual([start, end])
  })

  it('只填了起点或只填了终点时不上抛', () => {
    expect(resolveRangeChange(['2026-09-01'])).toBeUndefined()
    expect(resolveRangeChange(['2026-09-01', ''])).toBeUndefined()
    expect(resolveRangeChange(['', '2026-09-30'])).toBeUndefined()
  })

  it('两端皆空（清空）上抛 null', () => {
    expect(resolveRangeChange([])).toBeNull()
    expect(resolveRangeChange(['', ''])).toBeNull()
  })

  it('带时间的区间：草稿两端带上时:分，回传的时刻原样保留', () => {
    const from = new Date(2026, 8, 1, 8, 30).getTime()
    const to = new Date(2026, 8, 30, 18, 5).getTime()

    expect(rangeToDraft([from, to], true)).toEqual(['2026-09-01T08:30', '2026-09-30T18:05'])
    expect(resolveRangeChange(['2026-09-01T08:30', '2026-09-30T18:05'])).toEqual([from, to])
  })
})

describe('段位组接线', () => {
  const field = {
    'id': 'f-control',
    'aria-labelledby': 'f-label',
    'aria-describedby': 'f-desc',
    'aria-invalid': 'true',
    'aria-required': 'false',
    'aria-readonly': 'false',
    'data-disabled': undefined,
    'data-readonly': undefined,
    'data-invalid': '',
  }

  it('转交 id 与 aria-*，不转交段位组自己算的 data-* 状态位', () => {
    const wiring = segmentGroupWiring(field)
    expect(wiring).toMatchObject({ 'id': 'f-control', 'aria-labelledby': 'f-label', 'aria-describedby': 'f-desc' })
    expect(wiring).not.toHaveProperty('data-disabled')
    expect(wiring).not.toHaveProperty('data-invalid')
  })

  it('区间起点那组：名字链末尾接上组自己，字段标签与「开始日期」都读', () => {
    expect(segmentGroupWiring(field, true)['aria-labelledby']).toBe('f-label f-control')
  })

  it('区间终点那组：id 由字段派生，名字链接上自己，说明与校验照抄', () => {
    expect(endSegmentGroupWiring(field)).toEqual({
      'id': 'f-control-end',
      'aria-labelledby': 'f-label f-control-end',
      'aria-describedby': 'f-desc',
      'aria-invalid': 'true',
      'aria-required': 'false',
      'aria-readonly': 'false',
    })
  })

  it('不在字段里时终点那组什么都不挂，起点那组只带调用方自己的属性', () => {
    expect(endSegmentGroupWiring({})).toEqual({})
    expect(segmentGroupWiring({ 'aria-label': '生日' }, true)).toEqual({ 'aria-label': '生日' })
  })
})
