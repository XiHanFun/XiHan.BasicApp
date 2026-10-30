import type { CacheEntry, CacheNamespace } from './cache'
import { describe, expect, it } from 'vitest'
import { ApiError, configureRequest } from '~/request'
import {
  batchDeleteToast,
  buildKeyTree,
  canEditEntry,
  collectGroupKeys,
  deleteByPrefix,
  deleteFailureMessage,
  deleteKeys,
  formatSeconds,
  formatSize,
  goneKeys,
  GROUP_PREFIX,
  isJson,
  KEY_PAGE_SIZE,
  keyPrefix,
  leafKeys,
  loadNamespaces,
  mergeKeys,
  namespaceOf,
  namespaceOptions,
  packageLabel,
  prettyJson,
  readOnlyReason,
  singleDeleteToast,
  slidingText,
  timeToLiveText,
  toDeleteBatches,
  toKeysRequest,
  useCacheEntry,
  useCacheKeys,
} from './cache'

/** 假翻译：键原样返回，带参数时拼上参数便于断言 */
const t = (key: string, values?: Record<string, unknown>) => (values ? `${key}:${JSON.stringify(values)}` : key)

interface Call {
  method: string
  url: string
  body: unknown
}

function json(status: number, body: object, type = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': type } })
}

/** 逐个请求交给 respond 回答（可返回 Promise 以控制先后），记下方法、地址与请求体 */
function stub(respond: (call: Call, index: number) => Promise<Response> | Response): Call[] {
  const calls: Call[] = []
  configureRequest({
    baseUrl: '',
    getToken: () => 't',
    fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
      const call = { method: init?.method ?? 'GET', url: decodeURIComponent(String(url)), body: init?.body ? JSON.parse(String(init.body)) : undefined }
      calls.push(call)
      return respond(call, calls.length - 1)
    }) as unknown as typeof fetch,
  })
  return calls
}

/** 依次回答给定的响应体 */
function reply(...bodies: object[]): Call[] {
  return stub((_, index) => json(200, bodies[index] ?? {}))
}

function problem(status: number, code: string): Response {
  return json(status, { code, status, detail: `detail of ${code}`, traceId: 'x' }, 'application/problem+json')
}

/** 手动放行的响应：用来造「先发后到」 */
function deferred() {
  let resolve!: (response: Response) => void
  const promise = new Promise<Response>((done) => {
    resolve = done
  })
  return { promise, resolve }
}

const NAMESPACES: CacheNamespace[] = [
  { package: 'access', prefix: 'access:', editable: false },
  { package: 'notification', prefix: 'notification:', editable: true },
  { package: 'billing', prefix: 'billing:', editable: true },
]

function entry(patch: Partial<CacheEntry> = {}): CacheEntry {
  return { key: 'notification:telegram:1', size: 12, timeToLiveSeconds: 3700, slidingSeconds: null, text: '{"a":1}', editable: true, ...patch }
}

describe('缓存管理：命名空间', () => {
  it('包名写出文案（不认得的原样），选项值是前缀、文本是前缀加包名，不可改写的标只读', async () => {
    expect(packageLabel(t, 'access')).toBe('maintenance.caches.packages.access')
    expect(packageLabel(t, 'billing')).toBe('billing')
    expect(namespaceOptions(t, NAMESPACES)).toEqual([
      { value: 'access:', label: 'maintenance.caches.namespace_option_read_only:{"prefix":"access:","name":"maintenance.caches.packages.access"}' },
      { value: 'notification:', label: 'maintenance.caches.namespace_option:{"prefix":"notification:","name":"maintenance.caches.packages.notification"}' },
      { value: 'billing:', label: 'maintenance.caches.namespace_option:{"prefix":"billing:","name":"billing"}' },
    ])

    const calls = reply({ items: NAMESPACES })
    expect(await loadNamespaces()).toEqual(NAMESPACES)
    expect(calls).toEqual([{ method: 'GET', url: '/api/caches:namespaces', body: undefined }])
  })

  it('键或前缀按开头归到命名空间；不在任何开放命名空间里为 null', () => {
    expect(namespaceOf(NAMESPACES, 'access:policies:1')?.package).toBe('access')
    expect(namespaceOf(NAMESPACES, 'notification:')?.editable).toBe(true)
    expect(namespaceOf(NAMESPACES, 'locks:job')).toBeNull()
  })
})

describe('缓存管理：键列表请求', () => {
  it('完整前缀是命名空间前缀加去空白的子前缀；第一页不带续页标记，下一页带上', () => {
    expect(keyPrefix('access:', '  policies: ')).toBe('access:policies:')
    expect(keyPrefix('access:', '')).toBe('access:')
    expect(toKeysRequest('access:', null)).toEqual({ prefix: 'access:', limit: KEY_PAGE_SIZE })
    expect(toKeysRequest('access:', 'c1')).toEqual({ prefix: 'access:', limit: KEY_PAGE_SIZE, continuation: 'c1' })
  })

  it('并入下一页时跨页去重，并按序数排序', () => {
    expect(mergeKeys(['b:2', 'a:1'], ['a:1', 'B:3', 'b:2', 'c'])).toEqual(['B:3', 'a:1', 'b:2', 'c'])
  })

  it('第一页按前缀取，加载更多带上续页标记并去重并入；没有下一页时不再发请求', async () => {
    const calls = reply(
      { items: ['access:b', 'access:a'], continuation: 'c1' },
      { items: ['access:b', 'access:c'], continuation: null },
    )
    const list = useCacheKeys()
    expect(await list.load('access:')).toBe(true)
    expect(list.keys.value).toEqual(['access:a', 'access:b'])
    expect(list.continuation.value).toBe('c1')
    expect(list.prefix.value).toBe('access:')

    expect(await list.loadMore()).toBe(true)
    expect(list.keys.value).toEqual(['access:a', 'access:b', 'access:c'])
    expect(list.continuation.value).toBeNull()
    expect(await list.loadMore()).toBe(false)

    expect(calls.map(call => `${call.method} ${call.url}`)).toEqual([
      `GET /api/caches:keys?prefix=access:&limit=${KEY_PAGE_SIZE}`,
      `GET /api/caches:keys?prefix=access:&limit=${KEY_PAGE_SIZE}&continuation=c1`,
    ])
    expect(list.loading.value).toBe(false)
  })

  it('重查作废在途的上一次：先发后到的旧响应不覆盖新结果，它的失败也不抛出', async () => {
    const slow = deferred()
    stub((call) => {
      if (call.url.includes('prefix=access:'))
        return slow.promise
      return json(200, { items: ['notification:x'], continuation: null })
    })
    const list = useCacheKeys()
    const first = list.load('access:')
    expect(await list.load('notification:')).toBe(true)
    slow.resolve(problem(500, 'KERNEL_UNEXPECTED_000'))
    expect(await first).toBe(false)
    expect(list.keys.value).toEqual(['notification:x'])
    expect(list.prefix.value).toBe('notification:')
    expect(list.loading.value).toBe(false)
  })

  it('当前那次查询失败照常抛出；删掉的键从列表里拿掉，续页标记不动', async () => {
    stub(() => problem(403, 'MAINTENANCE_DENIED_002'))
    const list = useCacheKeys()
    await expect(list.load('locks:')).rejects.toMatchObject({ code: 'MAINTENANCE_DENIED_002' })
    expect(list.loading.value).toBe(false)

    reply({ items: ['access:a', 'access:b', 'access:c'], continuation: 'c9' })
    await list.load('access:')
    list.discard(['access:b', 'access:zzz'])
    expect(list.keys.value).toEqual(['access:a', 'access:c'])
    expect(list.continuation.value).toBe('c9')
  })
})

describe('缓存管理：键树（4.x 同）', () => {
  it('按 : 分组，分组带叶子数；同一路径既是键又是更深键的前缀时两者都在', () => {
    const tree = buildKeyTree(['a:b', 'a:b:c', 'a:d', 'solo'])
    expect(tree).toEqual([
      {
        value: `${GROUP_PREFIX}a`,
        label: 'a',
        leafCount: 3,
        children: [
          { value: 'a:b', label: 'b' },
          { value: `${GROUP_PREFIX}a:b`, label: 'b', leafCount: 1, children: [{ value: 'a:b:c', label: 'c' }] },
          { value: 'a:d', label: 'd' },
        ],
      },
      { value: 'solo', label: 'solo' },
    ])
    expect(collectGroupKeys(tree)).toEqual([`${GROUP_PREFIX}a`, `${GROUP_PREFIX}a:b`])
    expect(leafKeys([`${GROUP_PREFIX}a`, 'a:b', 'solo'])).toEqual(['a:b', 'solo'])
  })
})

describe('缓存管理：缓存项', () => {
  it('大小按 B / KB / MB（4.x 同），秒数取最大单位再带下一级', () => {
    expect(formatSize(512)).toBe('512 B')
    expect(formatSize(1536)).toBe('1.5 KB')
    expect(formatSize(3 * 1024 * 1024)).toBe('3.00 MB')

    expect(formatSeconds(t, 0)).toBe('maintenance.caches.units.second:{"n":0}')
    expect(formatSeconds(t, 45)).toBe('maintenance.caches.units.second:{"n":45}')
    expect(formatSeconds(t, 3725)).toBe('maintenance.caches.units.hour:{"n":1} maintenance.caches.units.minute:{"n":2}')
    // 下一级为零即不带，也不跳到再下一级
    expect(formatSeconds(t, 86400 + 30)).toBe('maintenance.caches.units.day:{"n":1}')
  })

  it('剩余存活为空即永不过期，滑动过期为空即无', () => {
    expect(timeToLiveText(t, entry({ timeToLiveSeconds: null }))).toBe('maintenance.caches.never_expire')
    expect(timeToLiveText(t, entry({ timeToLiveSeconds: 90 }))).toBe('maintenance.caches.units.minute:{"n":1} maintenance.caches.units.second:{"n":30}')
    expect(slidingText(t, entry())).toBe('maintenance.caches.no_sliding')
    expect(slidingText(t, entry({ slidingSeconds: 1800 }))).toBe('maintenance.caches.units.minute:{"n":30}')
  })

  it('改写要有权限、且后端判定可改且值是文本；不能改的原因区分二进制与只读命名空间', () => {
    expect(canEditEntry(entry(), true)).toBe(true)
    expect(canEditEntry(entry(), false)).toBe(false)
    expect(canEditEntry(entry({ editable: false }), true)).toBe(false)
    expect(canEditEntry(entry({ text: null, editable: false }), true)).toBe(false)
    expect(canEditEntry(null, true)).toBe(false)

    expect(readOnlyReason(entry())).toBeNull()
    expect(readOnlyReason(entry({ editable: false }))).toBe('read-only')
    expect(readOnlyReason(entry({ text: null, editable: false }))).toBe('binary')
  })

  it('只有对象或数组算 JSON；美化失败时原样', () => {
    expect(isJson(' {"a":1} ')).toBe(true)
    expect(isJson('[1,2]')).toBe(true)
    expect(isJson('42')).toBe(false)
    expect(isJson('{oops')).toBe(false)
    expect(prettyJson('{"a":1}')).toBe('{\n  "a": 1\n}')
    expect(prettyJson('plain')).toBe('plain')
  })

  it('查看按键取；键不存在或已过期只把详情置空，其余错误抛出', async () => {
    const calls = stub((call, index) => {
      if (index === 0)
        return json(200, entry())
      return call.url.includes('gone') ? problem(404, 'MAINTENANCE_NOTFOUND_003') : problem(503, 'MAINTENANCE_UNAVAILABLE_001')
    })
    const state = useCacheEntry()
    await state.open('notification:telegram:1')
    expect(state.entry.value).toEqual(entry())
    expect(calls[0]).toEqual({ method: 'GET', url: '/api/caches:inspect?key=notification:telegram:1', body: undefined })

    await state.open('notification:gone')
    expect(state.key.value).toBe('notification:gone')
    expect(state.entry.value).toBeNull()
    expect(state.loading.value).toBe(false)

    const failure = state.open('notification:other')
    await expect(failure).rejects.toBeInstanceOf(ApiError)
    expect(state.loading.value).toBe(false)
  })

  it('连点两个键只认后一个；保存按 PUT 写回并以返回的缓存项为准', async () => {
    const slow = deferred()
    const calls = stub((call) => {
      if (call.method === 'PUT')
        return json(200, entry({ key: 'notification:b', text: 'new', size: 3 }))
      return call.url.includes('notification:a') ? slow.promise : json(200, entry({ key: 'notification:b', text: 'old' }))
    })
    const state = useCacheEntry()
    const first = state.open('notification:a')
    await state.open('notification:b')
    slow.resolve(json(200, entry({ key: 'notification:a' })))
    await first
    expect(state.key.value).toBe('notification:b')
    expect(state.entry.value?.text).toBe('old')

    await state.save('new')
    expect(calls.at(-1)).toEqual({ method: 'PUT', url: '/api/caches', body: { key: 'notification:b', value: 'new' } })
    expect(state.entry.value).toMatchObject({ text: 'new', size: 3 })

    state.reset()
    expect([state.key.value, state.entry.value]).toEqual([null, null])
  })
})

describe('缓存管理：删除', () => {
  it('去重后每 100 个一批；既没删掉也没失败的键算本来就不在了', async () => {
    const keys = Array.from({ length: 205 }, (_, i) => `access:k${i}`)
    expect(toDeleteBatches([...keys, 'access:k0']).map(batch => batch.length)).toEqual([100, 100, 5])

    const calls = stub((call) => {
      const sent = new URL(call.url, 'http://x').searchParams.getAll('keys')
      // 每批删掉偶数号，k1 不在开放命名空间里（造一个失败），其余奇数号已过期
      return json(200, {
        removed: sent.filter(key => Number(key.slice(8)) % 2 === 0),
        failed: sent.includes('access:k1') ? [{ key: 'access:k1', code: 'MAINTENANCE_DENIED_002' }] : [],
      })
    })
    const outcome = await deleteKeys([...keys, 'access:k0'])
    expect(calls.map(call => call.method)).toEqual(['DELETE', 'DELETE', 'DELETE'])
    expect(calls[0]!.url.startsWith('/api/caches?keys=access:k0&keys=access:k1&')).toBe(true)
    expect(outcome.removed).toHaveLength(103)
    expect(outcome.failed).toEqual([{ key: 'access:k1', code: 'MAINTENANCE_DENIED_002' }])
    expect(outcome.missing).toHaveLength(101)
    expect(outcome.missing).not.toContain('access:k1')
    expect(goneKeys(outcome)).toHaveLength(204)
  })

  it('删除当前键：删掉即成功，本来就不在提醒一句，没删成报原因', () => {
    expect(singleDeleteToast(t, { removed: ['a:1'], missing: [], failed: [] })).toEqual({ tone: 'success', message: 'common.messages.delete_success' })
    expect(singleDeleteToast(t, { removed: [], missing: ['a:1'], failed: [] })).toEqual({ tone: 'warning', message: 'maintenance.caches.key_already_gone' })
    expect(singleDeleteToast(t, { removed: [], missing: [], failed: [{ key: 'a:1', code: 'MAINTENANCE_DENIED_002' }] }))
      .toEqual({ tone: 'danger', message: 'maintenance.caches.namespace_denied' })
    expect(deleteFailureMessage(t, 'X_UNKNOWN_1')).toBe('X_UNKNOWN_1')
  })

  it('批量删除：全部不在了即成功（已过期的单独说），有失败时报成功与失败个数并附去重后的原因', () => {
    expect(batchDeleteToast(t, { removed: ['a', 'b'], missing: [], failed: [] }))
      .toEqual({ tone: 'success', message: 'maintenance.caches.batch_deleted:{"count":2}' })
    expect(batchDeleteToast(t, { removed: ['a'], missing: ['b'], failed: [] }))
      .toEqual({ tone: 'success', message: 'maintenance.caches.batch_deleted_with_missing:{"count":1,"missing":1}' })
    expect(batchDeleteToast(t, {
      removed: ['a'],
      missing: ['b'],
      failed: [{ key: 'x', code: 'MAINTENANCE_DENIED_002' }, { key: 'y', code: 'MAINTENANCE_DENIED_002' }],
    })).toEqual({
      tone: 'warning',
      message: 'maintenance.caches.batch_delete_partial:{"success":2,"failed":2,"reason":"maintenance.caches.namespace_denied"}',
    })
  })

  it('按前缀删除走 POST，返回删掉的键数', async () => {
    const calls = reply({ removed: 7 })
    expect(await deleteByPrefix('notification:telegram:')).toBe(7)
    expect(calls).toEqual([{ method: 'POST', url: '/api/caches:delete-by-prefix', body: { prefix: 'notification:telegram:' } }])
  })
})
