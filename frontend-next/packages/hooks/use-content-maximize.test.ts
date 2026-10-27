/**
 * useContentMaximize 内容最大化单元测试。
 * 职责：锁定「模块级单例」这一关键约定——Header / Tabbar / 页面各自调用必须共享同一份状态，
 * 以及 maximize / restore / toggleMaximize 的幂等性与只读暴露。
 */
import { afterEach, describe, expect, it } from 'vitest'
import { useContentMaximize } from './useContentMaximize'

// 模块级单例状态跨用例可见，必须还原，否则用例顺序会影响结果
afterEach(() => {
  useContentMaximize().restore()
})

describe('useContentMaximize 状态迁移', () => {
  it('初始为非最大化', () => {
    const { contentIsMaximize } = useContentMaximize()

    expect(contentIsMaximize.value).toBe(false)
  })

  it('maximize 进入最大化，restore 退出', () => {
    const { contentIsMaximize, maximize, restore } = useContentMaximize()

    maximize()
    expect(contentIsMaximize.value).toBe(true)

    restore()
    expect(contentIsMaximize.value).toBe(false)
  })

  it('重复 maximize 不会把状态翻回去', () => {
    const { contentIsMaximize, maximize } = useContentMaximize()

    maximize()
    maximize()
    maximize()

    expect(contentIsMaximize.value).toBe(true)
  })

  it('未最大化时 restore 保持原状，不产生副作用', () => {
    const { contentIsMaximize, restore } = useContentMaximize()

    restore()
    restore()

    expect(contentIsMaximize.value).toBe(false)
  })

  it('toggleMaximize 连续两次回到原状态', () => {
    const { contentIsMaximize, toggleMaximize } = useContentMaximize()

    toggleMaximize()
    expect(contentIsMaximize.value).toBe(true)

    toggleMaximize()
    expect(contentIsMaximize.value).toBe(false)
  })
})

describe('useContentMaximize 单例共享', () => {
  it('两处独立调用共享同一份状态，一处最大化另一处立即可见', () => {
    const header = useContentMaximize()
    const page = useContentMaximize()

    header.maximize()

    expect(page.contentIsMaximize.value).toBe(true)

    page.restore()

    expect(header.contentIsMaximize.value).toBe(false)
  })

  it('不同调用方拿到的是同一个只读 ref 实例', () => {
    const a = useContentMaximize()
    const b = useContentMaximize()

    expect(a.contentIsMaximize).toBe(b.contentIsMaximize)
  })

  it('第三方 toggle 后新取得的实例读到的是最新值而非初始值', () => {
    useContentMaximize().toggleMaximize()

    expect(useContentMaximize().contentIsMaximize.value).toBe(true)
  })
})
