/**
 * @vitest-environment jsdom
 * @vitest-environment-options { "url": "https://app.example.com/" }
 */
/**
 * useCheckUpdates 发版检查单元测试（部署站点这一侧）。
 * 职责：锁定「线上发布标记与当前产物不同才提示」「就是当前这一版时不提示」
 * 「按掉之后同一版不再追问、换版再提醒」这几条约定——它们决定用户到底看不看得到更新弹窗。
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

/** 造一份发布清单：构建标记换了就等价于线上发了新的一版 */
function stubFetchWithManifest(stamp: () => string) {
  const fetchMock = vi.fn((url: string) => Promise.resolve(url.includes('version.json')
    ? {
        ok: true,
        json: () => Promise.resolve({ version: '9.9.9', buildStamp: stamp() }),
      } as unknown as Response
    : { ok: false, status: 404 } as unknown as Response))
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
  // 挂载即比一次，这一步是异步的，等它落地再往下走
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
  it('线上发布标记与当前产物不同就提示：页面是发版之后打开的也认得出来', async () => {
    vi.stubEnv('DEV', false)
    vi.useFakeTimers()
    stubFetchWithManifest(() => '2026-09-22T14:00:00.000Z')

    // 挂载即比一次，不必等到第一个轮询周期
    await mountChecker()
    await vi.advanceTimersByTimeAsync(0)

    expect(confirmSpy).toHaveBeenCalledTimes(1)
  })

  it('线上没有发布清单（老产物 / 别处托管）时不提示', async () => {
    vi.stubEnv('DEV', false)
    vi.useFakeTimers()
    const fetchMock = vi.fn(() => Promise.resolve({ ok: false, status: 404 } as unknown as Response))
    vi.stubGlobal('fetch', fetchMock)

    await mountChecker()
    await vi.advanceTimersByTimeAsync(60_000)

    expect(fetchMock).toHaveBeenCalled()
    expect(confirmSpy).not.toHaveBeenCalled()
  })

  it('线上跑的就是当前这一版时不提示', async () => {
    vi.stubEnv('DEV', false)
    vi.useFakeTimers()
    stubFetchWithManifest(() => __APP_BUILD_STAMP__)

    await mountChecker()
    await vi.advanceTimersByTimeAsync(60_000)

    expect(confirmSpy).not.toHaveBeenCalled()
  })

  it('按掉之后同一版不再追问，再发一版才重新提醒', async () => {
    vi.stubEnv('DEV', false)
    vi.useFakeTimers()
    let stamp = '2026-09-22T14:00:00.000Z'
    stubFetchWithManifest(() => stamp)
    await mountChecker()

    await vi.advanceTimersByTimeAsync(30_000)
    expect(confirmSpy).toHaveBeenCalledTimes(1)

    // 用户按了取消（桩里 confirm 恒 resolve(false)）：同一版再轮几次都不该再弹
    await vi.advanceTimersByTimeAsync(90_000)
    expect(confirmSpy).toHaveBeenCalledTimes(1)

    stamp = '2026-09-22T15:00:00.000Z'
    await vi.advanceTimersByTimeAsync(30_000)
    expect(confirmSpy).toHaveBeenCalledTimes(2)
  })

  it('开发环境不轮询：一次请求都不发', async () => {
    vi.stubEnv('DEV', true)
    const fetchMock = stubFetchWithManifest(() => '2026-09-22T14:00:00.000Z')

    await mountChecker()

    expect(fetchMock).not.toHaveBeenCalled()
  })
})
