/**
 * 布局桥接 Store（layout-bridge）单元测试。
 * 职责边界：它只是一组「请求计数器」——顶栏等外部组件把动作请求投递给布局层，
 * 布局层 watch 版本号自增来响应。这里锁住每个请求各自独立自增、互不串号，
 * 以及计数器从 0 起步（watch 首次触发靠的是「值变了」而不是「值为真」）。
 */
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { nextTick, watch } from 'vue'
import { useLayoutBridgeStore } from './layout-bridge'

beforeEach(() => {
  setActivePinia(createPinia())
})

describe('初始版本号', () => {
  it('五个请求版本号一律从 0 起步', () => {
    const store = useLayoutBridgeStore()

    expect(store.sidebarToggleVersion).toBe(0)
    expect(store.preferenceDrawerVersion).toBe(0)
    expect(store.globalSearchVersion).toBe(0)
    expect(store.lockScreenVersion).toBe(0)
    expect(store.tabOverviewVersion).toBe(0)
  })
})

describe('每个请求各自自增且互不干扰', () => {
  it('requestSidebarToggle 只推进侧栏版本号', () => {
    const store = useLayoutBridgeStore()

    store.requestSidebarToggle()

    expect(store.sidebarToggleVersion).toBe(1)
    expect(store.preferenceDrawerVersion).toBe(0)
    expect(store.globalSearchVersion).toBe(0)
    expect(store.lockScreenVersion).toBe(0)
    expect(store.tabOverviewVersion).toBe(0)
  })

  it('requestOpenPreferenceDrawer 只推进偏好抽屉版本号', () => {
    const store = useLayoutBridgeStore()

    store.requestOpenPreferenceDrawer()

    expect(store.preferenceDrawerVersion).toBe(1)
    expect(store.sidebarToggleVersion).toBe(0)
  })

  it('requestOpenGlobalSearch 只推进全局搜索版本号', () => {
    const store = useLayoutBridgeStore()

    store.requestOpenGlobalSearch()

    expect(store.globalSearchVersion).toBe(1)
    expect(store.lockScreenVersion).toBe(0)
  })

  it('requestLockScreen 只推进锁屏版本号', () => {
    const store = useLayoutBridgeStore()

    store.requestLockScreen()

    expect(store.lockScreenVersion).toBe(1)
    expect(store.globalSearchVersion).toBe(0)
  })

  it('requestOpenTabOverview 只推进标签总览版本号', () => {
    const store = useLayoutBridgeStore()

    store.requestOpenTabOverview()

    expect(store.tabOverviewVersion).toBe(1)
    expect(store.sidebarToggleVersion).toBe(0)
  })

  it('连续请求逐次累加，不做去重或合帧', () => {
    const store = useLayoutBridgeStore()

    store.requestSidebarToggle()
    store.requestSidebarToggle()
    store.requestSidebarToggle()

    expect(store.sidebarToggleVersion).toBe(3)
  })
})

describe('布局层订阅语义', () => {
  it('每次请求都能让 watch 收到一次变更（重复动作不会被吞掉）', async () => {
    const store = useLayoutBridgeStore()
    const handler = vi.fn()
    const stop = watch(() => store.globalSearchVersion, handler)

    store.requestOpenGlobalSearch()
    await nextTick()
    store.requestOpenGlobalSearch()
    await nextTick()

    expect(handler).toHaveBeenCalledTimes(2)
    stop()
  })

  it('停止订阅后再请求不再回调（布局卸载后不残留监听）', async () => {
    const store = useLayoutBridgeStore()
    const handler = vi.fn()
    const stop = watch(() => store.lockScreenVersion, handler)

    store.requestLockScreen()
    await nextTick()
    stop()
    store.requestLockScreen()
    await nextTick()

    expect(handler).toHaveBeenCalledTimes(1)
    expect(store.lockScreenVersion).toBe(2)
  })
})
