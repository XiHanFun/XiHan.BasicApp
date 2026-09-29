import { describe, expect, it } from 'vitest'
import { EnableStatus } from '@/api'
import { canTogglePick, diffUserFormSecurity } from './user-form-access'

const effective = new Set(['r1'])

describe('canTogglePick', () => {
  it('撤掉已生效的一项要撤销权限', () => {
    expect(canTogglePick('r1', ['r1'], effective, { grant: true, revoke: false })).toBe(false)
    expect(canTogglePick('r1', ['r1'], effective, { grant: false, revoke: true })).toBe(true)
  })

  it('新勾一项要授予权限', () => {
    expect(canTogglePick('r2', [], effective, { grant: false, revoke: true })).toBe(false)
    expect(canTogglePick('r2', [], effective, { grant: true, revoke: false })).toBe(true)
  })

  it('本次改动内的来回切换不产生请求，随时可切', () => {
    const none = { grant: false, revoke: false }
    // 已生效的取消后再勾回
    expect(canTogglePick('r1', [], effective, none)).toBe(true)
    // 本次新勾的再取消
    expect(canTogglePick('r2', ['r2'], effective, none)).toBe(true)
  })
})

describe('diffUserFormSecurity', () => {
  const original = { status: EnableStatus.Enabled, isLocked: false, multiLogin: true, maxDev: 0 }

  it('没改的项不提交', () => {
    expect(diffUserFormSecurity({ ...original }, original)).toEqual({ status: false, lock: false, loginPolicy: false })
  })

  it('各项分别判定', () => {
    expect(diffUserFormSecurity({ ...original, status: EnableStatus.Disabled }, original)).toEqual({ status: true, lock: false, loginPolicy: false })
    expect(diffUserFormSecurity({ ...original, isLocked: true }, original)).toEqual({ status: false, lock: true, loginPolicy: false })
    expect(diffUserFormSecurity({ ...original, maxDev: 3 }, original)).toEqual({ status: false, lock: false, loginPolicy: true })
  })
})
