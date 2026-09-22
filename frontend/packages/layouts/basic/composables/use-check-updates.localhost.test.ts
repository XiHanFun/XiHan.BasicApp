/**
 * useCheckUpdates 在本机地址下的单元测试。
 * 职责：锁定「跑在 localhost 上一概不轮询」——本机跑的产物不会被别人重新部署，
 * 比对首页指纹没有意义；开发服务器更是每改一次就换一次指纹。
 * 域名是 jsdom 环境自带的 localhost，只能单独一份文件（部署站点那一侧在 use-check-updates.test.ts）。
 */
import { mount } from '@vue/test-utils'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h, nextTick, reactive } from 'vue'

const confirmSpy = vi.fn(() => Promise.resolve(false))
const appStore = reactive({ enableCheckUpdates: true, checkUpdatesInterval: 30 })

vi.mock('vue-i18n', () => ({ useI18n: () => ({ t: (key: string) => key }) }))
vi.mock('~/stores', () => ({ useAppStore: () => appStore }))
vi.mock('~/composables', () => ({ dialog: { confirm: (...args: unknown[]) => confirmSpy(...(args as [])) } }))

afterEach(() => {
  confirmSpy.mockClear()
  appStore.enableCheckUpdates = true
  vi.unstubAllGlobals()
  vi.unstubAllEnvs()
  vi.useRealTimers()
})

describe('useCheckUpdates 本机地址', () => {
  it('挂载不取指纹，改了也不弹提示', async () => {
    vi.stubEnv('DEV', false)
    vi.useFakeTimers()
    expect(window.location.hostname).toBe('localhost')
    let tag = 'v1'
    const fetchMock = vi.fn(() => Promise.resolve({
      headers: { get: (name: string) => (name === 'etag' ? tag : null) },
    } as unknown as Response))
    vi.stubGlobal('fetch', fetchMock)

    const { useCheckUpdates } = await import('./use-check-updates')
    mount(defineComponent({
      setup() {
        useCheckUpdates()
        return () => h('div')
      },
    }))
    await nextTick()
    tag = 'v2'
    await vi.advanceTimersByTimeAsync(60_000)

    expect(fetchMock).not.toHaveBeenCalled()
    expect(confirmSpy).not.toHaveBeenCalled()
  })

  it('运行中打开定时检查也不会起轮询', async () => {
    vi.stubEnv('DEV', false)
    vi.useFakeTimers()
    appStore.enableCheckUpdates = false
    const fetchMock = vi.fn(() => Promise.resolve({
      headers: { get: () => 'v1' },
    } as unknown as Response))
    vi.stubGlobal('fetch', fetchMock)

    const { useCheckUpdates } = await import('./use-check-updates')
    mount(defineComponent({
      setup() {
        useCheckUpdates()
        return () => h('div')
      },
    }))
    appStore.enableCheckUpdates = true
    await nextTick()
    await vi.advanceTimersByTimeAsync(60_000)

    expect(fetchMock).not.toHaveBeenCalled()
  })
})
