/**
 * 授予、拒绝与指派的有效期草稿
 * 锁定：两头都有时到期必须晚于生效（相等也不行），只有一头或都没有都成立；
 * 为空的字段不进请求、原因去掉首尾空白；已有条目的 ISO 时间换回时间戳，没有原因按空串
 */
import { describe, expect, it } from 'vitest'
import { emptyGrantWindow, grantWindowOf, isGrantWindowValid, toGrantWindowPayload, toPermissionWindowPayload } from '@/modules/access/views/roles/grant-window'

const from = Date.parse('2026-12-01T08:00:00Z')
const to = Date.parse('2027-01-01T00:00:00Z')

describe('有效期草稿', () => {
  it('两头都有时到期必须晚于生效', () => {
    expect(isGrantWindowValid({ effectiveAt: from, expiresAt: to })).toBe(true)
    expect(isGrantWindowValid({ effectiveAt: to, expiresAt: from })).toBe(false)
    expect(isGrantWindowValid({ effectiveAt: from, expiresAt: from })).toBe(false)
    expect(isGrantWindowValid({ effectiveAt: null, expiresAt: from })).toBe(true)
    expect(isGrantWindowValid(emptyGrantWindow())).toBe(true)
  })

  it('为空的字段不进请求，原因去掉首尾空白', () => {
    expect(toGrantWindowPayload(emptyGrantWindow())).toEqual({})
    expect(toPermissionWindowPayload({ effectiveAt: null, expiresAt: null, reason: '   ' })).toEqual({})
    expect(toPermissionWindowPayload({ effectiveAt: from, expiresAt: to, reason: ' 临时 ' })).toEqual({
      effectiveAt: '2026-12-01T08:00:00.000Z',
      expiresAt: '2027-01-01T00:00:00.000Z',
      reason: '临时',
    })
    // 指派没有原因：草稿里有也不带
    expect(toGrantWindowPayload({ effectiveAt: null, expiresAt: to, reason: '不带' } as never)).toEqual({
      expiresAt: '2027-01-01T00:00:00.000Z',
    })
  })

  it('已有条目的有效期换回草稿', () => {
    expect(grantWindowOf({ effectiveAt: '2026-12-01T08:00:00Z', expiresAt: null })).toEqual({
      effectiveAt: from,
      expiresAt: null,
      reason: '',
    })
    expect(grantWindowOf({ expiresAt: '2027-01-01T00:00:00Z', reason: '离岗' })).toEqual({
      effectiveAt: null,
      expiresAt: to,
      reason: '离岗',
    })
  })
})
