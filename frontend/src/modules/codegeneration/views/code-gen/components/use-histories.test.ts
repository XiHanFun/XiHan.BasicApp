import { describe, expect, it } from 'vitest'
import { formatDuration, formatSize, hasPackage } from './use-histories'

describe('生成历史的显示', () => {
  it('耗时满一秒按秒、否则按毫秒', () => {
    expect(formatDuration(850)).toBe('850ms')
    expect(formatDuration(1234)).toBe('1.23s')
  })

  it('大小按 B / KB / MB', () => {
    expect(formatSize(512)).toBe('512 B')
    expect(formatSize(2048)).toBe('2.00 KB')
    expect(formatSize(3 * 1024 * 1024)).toBe('3.00 MB')
  })

  it('有产物包且没到期才能下载', () => {
    const now = Date.parse('2026-11-03T10:00:00Z')
    expect(hasPackage({ packageExpiresAt: '2026-11-10T10:00:00Z' }, now)).toBe(true)
    expect(hasPackage({ packageExpiresAt: '2026-11-01T10:00:00Z' }, now)).toBe(false)
    expect(hasPackage({ packageExpiresAt: null }, now)).toBe(false)
  })
})
