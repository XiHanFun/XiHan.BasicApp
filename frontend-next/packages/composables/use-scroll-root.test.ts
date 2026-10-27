/**
 * useScrollRoot 滚动源登记单元测试。
 * 职责：锁定「布局挂载时登记、卸载时撤销、无布局壳的页面取到 null」这条约定，
 * 以及登记本体必须是同一个可被 watch 的 ref（换滚动源时依赖方据此重新接线）。
 */
import { afterEach, describe, expect, it, vi } from 'vitest'
import { watch } from 'vue'
import { getScrollRoot, scrollRootRef, setScrollRoot } from './useScrollRoot'

// 模块级单例状态跨用例可见，必须还原
afterEach(() => {
  setScrollRoot(null)
})

describe('useScrollRoot 登记与读取', () => {
  it('未登记时取到 null，交由组件库自己探测', () => {
    expect(getScrollRoot()).toBeNull()
  })

  it('登记后取到同一个元素实例', () => {
    const el = document.createElement('div')

    setScrollRoot(el)

    expect(getScrollRoot()).toBe(el)
  })

  it('卸载时置 null，滚动源随之撤销', () => {
    setScrollRoot(document.createElement('div'))

    setScrollRoot(null)

    expect(getScrollRoot()).toBeNull()
  })

  it('换一个滚动源时后者覆盖前者', () => {
    const first = document.createElement('div')
    const second = document.createElement('section')

    setScrollRoot(first)
    setScrollRoot(second)

    expect(getScrollRoot()).toBe(second)
  })
})

describe('useScrollRoot 可观察性', () => {
  it('导出的是登记本体，可直接 watch 到滚动源换人', async () => {
    const seen: Array<null | string> = []
    const stop = watch(scrollRootRef, el => seen.push(el?.tagName ?? null), { flush: 'sync' })

    try {
      setScrollRoot(document.createElement('main'))
      setScrollRoot(null)
    }
    finally {
      stop()
    }

    expect(seen).toEqual(['MAIN', null])
  })

  it('重复登记同一个元素不触发变化通知（shallowRef 按引用比较）', () => {
    const el = document.createElement('div')
    const spy = vi.fn()
    const stop = watch(scrollRootRef, spy, { flush: 'sync' })

    try {
      setScrollRoot(el)
      setScrollRoot(el)
    }
    finally {
      stop()
    }

    expect(spy).toHaveBeenCalledTimes(1)
  })

  it('scrollRootRef 与 getScrollRoot 读到的是同一份状态', () => {
    const el = document.createElement('div')

    setScrollRoot(el)

    expect(scrollRootRef.value).toBe(getScrollRoot())
  })

  it('登记的元素不被 Vue 深层代理，取回的仍是原生元素本身', () => {
    const el = document.createElement('div')
    el.id = 'scroll-host'

    setScrollRoot(el)

    expect(getScrollRoot()).toBe(el)
    expect(getScrollRoot()?.id).toBe('scroll-host')
  })
})
