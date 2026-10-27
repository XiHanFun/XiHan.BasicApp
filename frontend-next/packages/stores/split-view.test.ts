/**
 * 分屏对照 Store（split-view）单元测试。
 * 职责边界：分屏开启/关闭、锚定与副标签的互斥约束、宽度占比夹取、视觉反转与路径互换、
 * SessionStorage 持久化往返，以及「小屏禁用分屏」这条硬约束。
 * useIsMobile 被替换为可控 ref —— jsdom 的 matchMedia 替身恒为 false，无法驱动断点。
 */
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { nextTick } from 'vue'
import { SPLIT_VIEW_KEY } from '~/constants'
import { useSplitViewStore } from './split-view'

const mobileRef = vi.hoisted(() => ({ current: null as null | { value: boolean } }))

vi.mock('~/composables/useIsMobile', async () => {
  const vue = await import('vue')
  const isMobileRef = vue.ref(false)
  mobileRef.current = isMobileRef
  return {
    MOBILE_BREAKPOINT: 768,
    useIsMobile: () => ({ isMobile: vue.readonly(isMobileRef) }),
  }
})

function setMobile(value: boolean): void {
  const target = mobileRef.current
  if (!target) {
    throw new Error('useIsMobile 替身未初始化')
  }
  target.value = value
}

function readPersisted(): Record<string, unknown> | null {
  const raw = sessionStorage.getItem(SPLIT_VIEW_KEY)
  return raw ? (JSON.parse(raw) as Record<string, unknown>) : null
}

beforeEach(() => {
  setMobile(false)
  setActivePinia(createPinia())
})

describe('初始状态与会话恢复', () => {
  it('无会话缓存时分屏关闭、两侧路径为空、占比为 0.5', () => {
    const store = useSplitViewStore()

    expect(store.active).toBe(false)
    expect(store.leftPath).toBe('')
    expect(store.rightPath).toBe('')
    expect(store.ratio).toBe(0.5)
    expect(store.reversed).toBe(false)
  })

  it('会话缓存存在时按缓存恢复分屏（刷新页面不消失）', () => {
    sessionStorage.setItem(SPLIT_VIEW_KEY, JSON.stringify({
      active: true,
      left: '/a',
      right: '/b',
      ratio: 0.3,
      reversed: true,
    }))
    setActivePinia(createPinia())

    const store = useSplitViewStore()

    expect(store.active).toBe(true)
    expect(store.leftPath).toBe('/a')
    expect(store.rightPath).toBe('/b')
    expect(store.ratio).toBe(0.3)
    expect(store.reversed).toBe(true)
  })

  it('会话缓存是损坏 JSON 时回落默认值，不抛异常', () => {
    sessionStorage.setItem(SPLIT_VIEW_KEY, '不是JSON')
    setActivePinia(createPinia())

    const store = useSplitViewStore()

    expect(store.active).toBe(false)
    expect(store.ratio).toBe(0.5)
  })

  it('会话缓存缺字段时逐项回落默认值', () => {
    sessionStorage.setItem(SPLIT_VIEW_KEY, JSON.stringify({ active: true, left: '/a' }))
    setActivePinia(createPinia())

    const store = useSplitViewStore()

    expect(store.leftPath).toBe('/a')
    expect(store.rightPath).toBe('')
    expect(store.ratio).toBe(0.5)
    expect(store.reversed).toBe(false)
  })
})

describe('开启分屏的前置约束', () => {
  it('正常开启：锚定在左、副标签在右、视觉未反转', () => {
    const store = useSplitViewStore()

    store.open('/a', '/b')

    expect(store.active).toBe(true)
    expect(store.leftPath).toBe('/a')
    expect(store.rightPath).toBe('/b')
    expect(store.reversed).toBe(false)
  })

  it('左右同路径时拒绝开启（分屏两侧必须不同）', () => {
    const store = useSplitViewStore()

    store.open('/a', '/a')

    expect(store.active).toBe(false)
  })

  it('任一路径为空串时拒绝开启', () => {
    const store = useSplitViewStore()

    store.open('', '/b')
    store.open('/a', '')

    expect(store.active).toBe(false)
    expect(store.leftPath).toBe('')
  })

  it('小屏下拒绝开启分屏（两栏并排过窄）', async () => {
    setMobile(true)
    await nextTick()
    const store = useSplitViewStore()

    store.open('/a', '/b')

    expect(store.canSplit).toBe(false)
    expect(store.active).toBe(false)
  })

  it('open 会重置上一次的视觉反转', () => {
    const store = useSplitViewStore()
    store.open('/a', '/b')
    store.toggleReversed()

    store.open('/c', '/d')

    expect(store.reversed).toBe(false)
  })
})

describe('两侧路径切换的互斥约束', () => {
  it('setRightPath 换成与锚定相同的路径时被拒绝', () => {
    const store = useSplitViewStore()
    store.open('/a', '/b')

    store.setRightPath('/a')

    expect(store.rightPath).toBe('/b')
  })

  it('setRightPath 传空串时被拒绝', () => {
    const store = useSplitViewStore()
    store.open('/a', '/b')

    store.setRightPath('')

    expect(store.rightPath).toBe('/b')
  })

  it('setLeftPath 换成与副标签相同的路径时被拒绝', () => {
    const store = useSplitViewStore()
    store.open('/a', '/b')

    store.setLeftPath('/b')

    expect(store.leftPath).toBe('/a')
  })

  it('setLeftPath / setRightPath 换到新路径时同步落地会话缓存', () => {
    const store = useSplitViewStore()
    store.open('/a', '/b')

    store.setLeftPath('/x')
    store.setRightPath('/y')

    expect(readPersisted()).toMatchObject({ left: '/x', right: '/y' })
  })

  it('setLeftPath / setRightPath 在分屏未开启时也会改路径并落地（不受 active 门控）', () => {
    const store = useSplitViewStore()

    store.setLeftPath('/a')

    expect(store.leftPath).toBe('/a')
    expect(readPersisted()).toMatchObject({ active: false, left: '/a' })
  })
})

describe('宽度占比夹取到 [0.2, 0.8]', () => {
  it('区间内的值原样保留', () => {
    const store = useSplitViewStore()

    store.setRatio(0.35)

    expect(store.ratio).toBe(0.35)
  })

  it('低于下界夹到 0.2，高于上界夹到 0.8', () => {
    const store = useSplitViewStore()

    store.setRatio(0)
    expect(store.ratio).toBe(0.2)

    store.setRatio(1)
    expect(store.ratio).toBe(0.8)
  })

  it('负数与超大值同样被夹到边界', () => {
    const store = useSplitViewStore()

    store.setRatio(-999)
    expect(store.ratio).toBe(0.2)

    store.setRatio(Number.MAX_SAFE_INTEGER)
    expect(store.ratio).toBe(0.8)
  })

  it('非数值（NaN）会穿透夹取逻辑写入 ratio，Math.min/max 对它无能为力', () => {
    const store = useSplitViewStore()

    store.setRatio(Number.NaN)

    expect(Number.isNaN(store.ratio)).toBe(true)
  })

  it('setRatio 立刻落地会话缓存', () => {
    const store = useSplitViewStore()

    store.setRatio(0.25)

    expect(readPersisted()).toMatchObject({ ratio: 0.25 })
  })
})

describe('视觉反转与路径互换', () => {
  it('未开启分屏时 toggleReversed 无效', () => {
    const store = useSplitViewStore()

    store.toggleReversed()

    expect(store.reversed).toBe(false)
  })

  it('toggleReversed 只翻转视觉顺序，两侧路径与占比都不变', () => {
    const store = useSplitViewStore()
    store.open('/a', '/b')
    store.setRatio(0.3)

    store.toggleReversed()

    expect(store.reversed).toBe(true)
    expect(store.leftPath).toBe('/a')
    expect(store.rightPath).toBe('/b')
    expect(store.ratio).toBe(0.3)
  })

  it('未开启分屏时 swapPaths 无效', () => {
    const store = useSplitViewStore()
    store.setLeftPath('/a')

    store.swapPaths()

    expect(store.leftPath).toBe('/a')
  })

  it('swapPaths 交换锚定与副标签并复位视觉反转', () => {
    const store = useSplitViewStore()
    store.open('/a', '/b')
    store.toggleReversed()

    store.swapPaths()

    expect(store.leftPath).toBe('/b')
    expect(store.rightPath).toBe('/a')
    expect(store.reversed).toBe(false)
  })
})

describe('标签归属判定', () => {
  it('分屏开启时锚定路径是分屏标签、副路径是被合并标签', () => {
    const store = useSplitViewStore()
    store.open('/a', '/b')

    expect(store.isSplitTab('/a')).toBe(true)
    expect(store.isMergedTab('/b')).toBe(true)
    expect(store.isSplitTab('/b')).toBe(false)
    expect(store.isMergedTab('/a')).toBe(false)
  })

  it('分屏关闭后两个判定一律为 false（副标签恢复可见）', () => {
    const store = useSplitViewStore()
    store.open('/a', '/b')

    store.close()

    expect(store.isSplitTab('/a')).toBe(false)
    expect(store.isMergedTab('/b')).toBe(false)
  })

  it('视觉反转不影响归属判定 —— 判的是锚定关系而非左右位置', () => {
    const store = useSplitViewStore()
    store.open('/a', '/b')

    store.toggleReversed()

    expect(store.isSplitTab('/a')).toBe(true)
    expect(store.isMergedTab('/b')).toBe(true)
  })
})

describe('关闭与持久化', () => {
  it('close 清空两侧路径、复位反转并落地会话缓存', () => {
    const store = useSplitViewStore()
    store.open('/a', '/b')
    store.toggleReversed()

    store.close()

    expect(store.active).toBe(false)
    expect(store.leftPath).toBe('')
    expect(store.rightPath).toBe('')
    expect(store.reversed).toBe(false)
    expect(readPersisted()).toEqual({ active: false, left: '', right: '', ratio: 0.5, reversed: false })
  })

  it('close 不重置宽度占比 —— 下次开启沿用用户拖出来的分割线位置', () => {
    const store = useSplitViewStore()
    store.open('/a', '/b')
    store.setRatio(0.7)

    store.close()

    expect(store.ratio).toBe(0.7)
  })

  it('持久化载荷字段名是 left/right 而非 leftPath/rightPath，可被下一次会话原样读回', () => {
    const store = useSplitViewStore()

    store.open('/a', '/b')

    expect(readPersisted()).toEqual({ active: true, left: '/a', right: '/b', ratio: 0.5, reversed: false })
  })
})

describe('小屏收敛', () => {
  it('分屏开启后缩到小屏会立即自动关闭', async () => {
    const store = useSplitViewStore()
    store.open('/a', '/b')

    setMobile(true)
    await nextTick()

    expect(store.active).toBe(false)
    expect(store.leftPath).toBe('')
  })

  it('启动即处于小屏时，会话里恢复的分屏被立即收敛为关闭态', async () => {
    sessionStorage.setItem(SPLIT_VIEW_KEY, JSON.stringify({
      active: true,
      left: '/a',
      right: '/b',
      ratio: 0.5,
      reversed: false,
    }))
    setMobile(true)
    await nextTick()
    setActivePinia(createPinia())

    const store = useSplitViewStore()
    await nextTick()

    expect(store.active).toBe(false)
  })

  it('小屏下分屏本就关闭时不会重复写会话缓存', async () => {
    setMobile(true)
    await nextTick()
    setActivePinia(createPinia())
    // 先前用例创建的 store 实例并不会随 setActivePinia 销毁（createPinia 用的是 detached
    // effect scope），它们的 watcher 仍挂在本文件共享的 mobileRef 上，上一行的 setMobile(true)
    // 会让这些遗留实例各写一次 sessionStorage。清掉它们的写入，本用例才只测新建 store 的行为。
    // 不清的话用例通过与否取决于此前跑过哪些用例 —— 乱序执行时会随机失败。
    sessionStorage.removeItem(SPLIT_VIEW_KEY)

    useSplitViewStore()
    await nextTick()

    expect(sessionStorage.getItem(SPLIT_VIEW_KEY)).toBeNull()
  })

  it('从小屏回到大屏后重新允许开启分屏', async () => {
    setMobile(true)
    await nextTick()
    setActivePinia(createPinia())
    const store = useSplitViewStore()

    setMobile(false)
    await nextTick()
    store.open('/a', '/b')

    expect(store.canSplit).toBe(true)
    expect(store.active).toBe(true)
  })
})
