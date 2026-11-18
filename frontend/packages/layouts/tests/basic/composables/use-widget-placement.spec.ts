/**
 * 顶栏挤压里面包屑「因挤而截」的宽度（use-widget-placement.ts 的 squeezedTruncation）：
 * 文字超出可视宽的部分才算；顶到自身或上层的宽度上限而截的不算（组件库给每一级设了最大宽，长标题在空档很多时也会截），
 * 否则顶栏会一路把「自动」的工具全让到悬浮组
 */
import { afterEach, describe, expect, it, vi } from 'vitest'
import { squeezedTruncation } from '~/layouts/basic/composables/use-widget-placement'

interface Box {
  width: number
  scrollWidth?: number
  clientWidth?: number
  overflowX?: string
  maxInlineSize?: string
}

const styles = new Map<Element, Box>()

/** 造一个节点并记下它的几何：jsdom 没有排版，宽度与计算样式都靠这里给 */
function node(box: Box, ...children: HTMLElement[]): HTMLElement {
  const el = document.createElement('span')
  el.append(...children)
  styles.set(el, box)
  Object.defineProperty(el, 'scrollWidth', { value: box.scrollWidth ?? box.width })
  Object.defineProperty(el, 'clientWidth', { value: box.clientWidth ?? box.width })
  el.getBoundingClientRect = () => ({ width: box.width }) as DOMRect
  return el
}

function stubComputedStyle() {
  vi.spyOn(window, 'getComputedStyle').mockImplementation(el => ({
    overflowX: styles.get(el)?.overflowX ?? 'visible',
    maxInlineSize: styles.get(el)?.maxInlineSize ?? 'none',
  }) as CSSStyleDeclaration)
}

afterEach(() => {
  styles.clear()
  vi.restoreAllMocks()
})

describe('面包屑因挤而截的宽度', () => {
  it('一级顶到自身的最大宽而截：不算挤', () => {
    stubComputedStyle()
    const label = node({
      width: 120,
      scrollWidth: 190,
      clientWidth: 120,
      overflowX: 'hidden',
    })
    const link = node({ width: 128, maxInlineSize: '128px' }, label)
    const root = node({ width: 400 }, link)

    expect(squeezedTruncation(root)).toBe(0)
  })

  it('一级还没到最大宽就截了：是被挤的，截掉多少算多少', () => {
    stubComputedStyle()
    const label = node({
      width: 60,
      scrollWidth: 190,
      clientWidth: 60,
      overflowX: 'hidden',
    })
    const link = node({ width: 68, maxInlineSize: '128px' }, label)
    const root = node({ width: 200 }, link)

    expect(squeezedTruncation(root)).toBe(130)
  })

  it('没设最大宽的截断照算；没截、或可见溢出的不算', () => {
    stubComputedStyle()
    const clipped = node({
      width: 50,
      scrollWidth: 80,
      clientWidth: 50,
      overflowX: 'hidden',
    })
    const visible = node({
      width: 50,
      scrollWidth: 80,
      clientWidth: 50,
      overflowX: 'visible',
    })
    const whole = node({ width: 50, overflowX: 'hidden' })
    const root = node({ width: 300 }, clipped, visible, whole)

    expect(squeezedTruncation(root)).toBe(30)
  })
})
