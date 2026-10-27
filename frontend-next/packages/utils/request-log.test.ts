/**
 * packages/utils/request-log.ts 单元测试。
 *
 * 职责边界：前端请求日志的模块级环形缓冲——追加顺序、300 条上限裁剪、按 requestId 增量更新、
 * 清空，以及对外暴露的只读视图不可被消费方改写。
 * 该状态是模块单例，用例前后必须显式清空以保证任意顺序执行。
 */
import type { FrontendRequestLog } from '~/types'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { appendRequestLog, clearRequestLogs, updateRequestLog, useRequestLogs } from './request-log'

function makeLog(requestId: string, overrides: Partial<FrontendRequestLog> = {}): FrontendRequestLog {
  return {
    requestId,
    method: 'GET',
    url: `/api/${requestId}`,
    startedAt: 1000,
    status: 'pending',
    ...overrides,
  }
}

beforeEach(() => {
  clearRequestLogs()
})

afterEach(() => {
  clearRequestLogs()
  vi.restoreAllMocks()
})

describe('appendRequestLog', () => {
  it('新日志插到队首，最近的请求排在最前', () => {
    appendRequestLog(makeLog('r1'))
    appendRequestLog(makeLog('r2'))

    expect(useRequestLogs().value.map(log => log.requestId)).toEqual(['r2', 'r1'])
  })

  it('保留的条目是传入的原对象，字段不被改写', () => {
    const log = makeLog('r1', { method: 'POST', traceId: 'trace-1' })
    appendRequestLog(log)

    expect(useRequestLogs().value[0]).toEqual(log)
  })

  it('超过 300 条时裁掉最旧的，长度恒定为 300', () => {
    for (let i = 0; i < 301; i++) {
      appendRequestLog(makeLog(`r${i}`))
    }

    const logs = useRequestLogs().value
    expect(logs).toHaveLength(300)
    expect(logs[0]?.requestId).toBe('r300')
    expect(logs.at(-1)?.requestId).toBe('r1')
    expect(logs.some(log => log.requestId === 'r0')).toBe(false)
  })

  it('恰好 300 条时不发生裁剪', () => {
    for (let i = 0; i < 300; i++) {
      appendRequestLog(makeLog(`r${i}`))
    }

    const logs = useRequestLogs().value
    expect(logs).toHaveLength(300)
    expect(logs.at(-1)?.requestId).toBe('r0')
  })

  it('每次追加都替换整个数组引用，便于 Vue 侦测变更', () => {
    appendRequestLog(makeLog('r1'))
    const before = useRequestLogs().value
    appendRequestLog(makeLog('r2'))

    expect(useRequestLogs().value).not.toBe(before)
  })

  it('requestId 重复时两条都会保留，去重不是本层职责', () => {
    appendRequestLog(makeLog('same'))
    appendRequestLog(makeLog('same'))

    expect(useRequestLogs().value).toHaveLength(2)
  })
})

describe('updateRequestLog', () => {
  it('按 requestId 合并补丁字段，未提及的字段保持原值', () => {
    appendRequestLog(makeLog('r1', { traceId: 'trace-1' }))
    updateRequestLog('r1', { status: 'success', statusCode: 200, duration: 42 })

    expect(useRequestLogs().value[0]).toEqual({
      requestId: 'r1',
      method: 'GET',
      url: '/api/r1',
      startedAt: 1000,
      traceId: 'trace-1',
      status: 'success',
      statusCode: 200,
      duration: 42,
    })
  })

  it('只影响命中的那一条，其余日志原样不动', () => {
    appendRequestLog(makeLog('r1'))
    appendRequestLog(makeLog('r2'))
    updateRequestLog('r1', { status: 'error', message: '网络异常' })

    const logs = useRequestLogs().value
    expect(logs.find(log => log.requestId === 'r2')?.status).toBe('pending')
    expect(logs.find(log => log.requestId === 'r1')?.message).toBe('网络异常')
  })

  it('更新不改变条目顺序', () => {
    appendRequestLog(makeLog('r1'))
    appendRequestLog(makeLog('r2'))
    updateRequestLog('r1', { status: 'success' })

    expect(useRequestLogs().value.map(log => log.requestId)).toEqual(['r2', 'r1'])
  })

  it('requestId 不存在时静默跳过，既不新增也不抛错', () => {
    appendRequestLog(makeLog('r1'))
    expect(() => updateRequestLog('未知', { status: 'error' })).not.toThrow()
    expect(useRequestLogs().value).toHaveLength(1)
  })

  it('空补丁不改变原值', () => {
    appendRequestLog(makeLog('r1'))
    updateRequestLog('r1', {})

    expect(useRequestLogs().value[0]?.status).toBe('pending')
  })

  it('requestId 重复时两条都被同一次更新命中', () => {
    appendRequestLog(makeLog('same'))
    appendRequestLog(makeLog('same'))
    updateRequestLog('same', { status: 'success' })

    expect(useRequestLogs().value.every(log => log.status === 'success')).toBe(true)
  })

  it('补丁里的 requestId 会覆盖原 requestId', () => {
    appendRequestLog(makeLog('r1'))
    updateRequestLog('r1', { requestId: 'renamed' })

    expect(useRequestLogs().value[0]?.requestId).toBe('renamed')
  })
})

describe('clearRequestLogs 与只读视图', () => {
  it('清空后列表长度归零', () => {
    appendRequestLog(makeLog('r1'))
    clearRequestLogs()

    expect(useRequestLogs().value).toEqual([])
  })

  it('多次取用得到的是同一份共享状态，后续追加对已取的视图可见', () => {
    const first = useRequestLogs()
    const second = useRequestLogs()
    appendRequestLog(makeLog('r1'))

    expect(first.value.map(log => log.requestId)).toEqual(['r1'])
    expect(second.value).toEqual(first.value)
  })

  it('消费方无法通过视图改写日志，写入被只读代理拦下', () => {
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => {})
    appendRequestLog(makeLog('r1'))

    const view = useRequestLogs()
    const escaped = view as unknown as { value: FrontendRequestLog[] }
    escaped.value = []

    expect(view.value.map(log => log.requestId)).toEqual(['r1'])
    expect(warn).toHaveBeenCalled()
  })
})
