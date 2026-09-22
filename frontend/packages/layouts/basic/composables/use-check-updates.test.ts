/**
 * @vitest-environment jsdom
 * @vitest-environment-options { "url": "https://app.example.com/" }
 */
/**
 * useCheckUpdates 前端资源更新检查单元测试（部署站点这一侧）。
 * 职责：锁定「挂载即记下首页指纹当基线」「指纹变了才弹刷新提示，且同一轮只弹一次」
 * 「关掉定时检查就停轮询」这几条约定——它们决定用户到底看不看得到更新弹窗。
 * 本机与开发环境的「一概不轮询」在 use-check-updates.localhost.test.ts 里锁，
 * 那条要跑在 localhost 这个域名下，只能另开一份环境。
 */
import { mount } from '@vue/test-utils'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h, nextTick, reactive } from 'vue'

const confirmSpy = vi.fn(() => Promise.resolve(false))
const appStore = reactive({ enableCheckUpdates: true, checkUpdatesInterval: 30 })

vi.mock('vue-i18n', () => ({ useI18n: () => ({ t: (key: string) => key }) }))
vi.mock('~/stores', () => ({ useAppStore: () => appStore }))
vi.mock('~/composables', () => ({ dialog: { confirm: (...args: unknown[]) => confirmSpy(...(args as [])) } }))

/** 造一个带 etag 的假首页：换掉返回值即等价于重新部署了一次 */
function stubFetch(tag: () => string | null) {
  const fetchMock = vi.fn(() => Promise.resolve({
    ok: true,
    headers: { get: (name: string) => (name === 'etag' ? tag() : null) },
    text: () => Promise.resolve('<html></html>'),
  } as unknown as Response))
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

/** 造一个不给校验头的假首页（CDN 压缩后常见）：版本只体现在正文里那批带哈希的资源名上 */
function stubFetchWithoutValidator(assetHash: () => string) {
  const fetchMock = vi.fn(() => Promise.resolve({
    ok: true,
    headers: { get: () => null },
    text: () => Promise.resolve(`<html><body><script src="/assets/js/index-${assetHash()}.js"></script></body></html>`),
  } as unknown as Response))
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

async function mountChecker() {
  const { useCheckUpdates } = await import('./use-check-updates')
  const wrapper = mount(defineComponent({
    setup() {
      useCheckUpdates()
      return () => h('div')
    },
  }))
  // 挂载里取基线是异步的，等它落地再往下走
  await nextTick()
  await Promise.resolve()
  return wrapper
}

afterEach(() => {
  confirmSpy.mockClear()
  appStore.enableCheckUpdates = true
  appStore.checkUpdatesInterval = 30
  vi.unstubAllGlobals()
  vi.unstubAllEnvs()
  vi.useRealTimers()
})

describe('useCheckUpdates 轮询与提示', () => {
  it('挂载时取一次首页指纹当基线，不弹提示', async () => {
    vi.stubEnv('DEV', false)
    const fetchMock = stubFetch(() => 'v1')

    await mountChecker()

    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(confirmSpy).not.toHaveBeenCalled()
  })

  it('指纹变了弹一次刷新提示，同一轮不重复弹', async () => {
    vi.stubEnv('DEV', false)
    vi.useFakeTimers()
    let tag = 'v1'
    stubFetch(() => tag)
    // 定时器要在挂载前接管，轮询才走得到假时钟上
    await mountChecker()

    tag = 'v2'
    // 缺省 30 秒一轮：连走两轮，提示只出一次
    await vi.advanceTimersByTimeAsync(30_000)
    await vi.advanceTimersByTimeAsync(30_000)

    expect(confirmSpy).toHaveBeenCalledTimes(1)
  })

  it('关掉定时检查后不再轮询', async () => {
    vi.stubEnv('DEV', false)
    vi.useFakeTimers()
    let tag = 'v1'
    const fetchMock = stubFetch(() => tag)
    await mountChecker()

    appStore.enableCheckUpdates = false
    await nextTick()
    const calls = fetchMock.mock.calls.length
    tag = 'v2'
    await vi.advanceTimersByTimeAsync(60_000)

    expect(fetchMock.mock.calls.length).toBe(calls)
    expect(confirmSpy).not.toHaveBeenCalled()
  })

  it('服务端不给校验头时改比正文指纹：资源哈希变了照样弹提示', async () => {
    vi.stubEnv('DEV', false)
    vi.useFakeTimers()
    let assetHash = 'AAAAAAAA'
    stubFetchWithoutValidator(() => assetHash)
    await mountChecker()

    // 没换版本：正文一样，不该弹
    await vi.advanceTimersByTimeAsync(30_000)
    expect(confirmSpy).not.toHaveBeenCalled()

    assetHash = 'BBBBBBBB'
    await vi.advanceTimersByTimeAsync(30_000)

    expect(confirmSpy).toHaveBeenCalledTimes(1)
  })

  it('开发环境不轮询：一次请求都不发', async () => {
    vi.stubEnv('DEV', true)
    const fetchMock = stubFetch(() => 'v1')

    await mountChecker()

    expect(fetchMock).not.toHaveBeenCalled()
  })
})
