/**
 * setupContainerTransform 容器变形转场单元测试。
 * 职责：锁定「安装幂等且在捕获期记录点击源」「点击源与弹窗必须落在 800ms 关联窗口内」
 * 「开关关闭 / 系统减弱动效时整体跳过」「幽灵容器动画结束后自我清理」以及关闭时的反向播放。
 *
 * 该模块是应用级一次性安装，全文件共用同一次安装，用例之间靠时间轴推进使上一次点击源过期来隔离；
 * 卸载入口（setupContainerTransform 的返回值）单独在文末一组用例里验证并当场重装还原。
 * WAAPI / DOMMatrix / rAF 在 jsdom 缺失，全部用替身注入并在收尾还原。
 */
import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it } from 'vitest'

interface FakeAnimation {
  keyframes: Array<Record<string, unknown>>
  options: Record<string, unknown>
  finish: () => void
}

const originalRaf = globalThis.requestAnimationFrame
const originalMatchMedia = window.matchMedia
const originalAnimate = Object.getOwnPropertyDescriptor(Element.prototype, 'animate')
const originalDateNow = Date.now

let frames: FrameRequestCallback[] = []
let animations: FakeAnimation[] = []
let reduceMotion = false
let enabled = true
let clock = 1_000_000
const addSpyCalls: Array<[string, unknown, unknown]> = []
/**
 * beforeAll 里那三次幂等安装刚做完时的监听登记快照。
 *
 * 断言安装幂等性必须看这份快照而不是 addSpyCalls 本身：文末「卸载后重装」那组用例会
 * 再装一次，往 addSpyCalls 里追加新的 pointerdown。按声明顺序执行时它排在最后、影响不到
 * 前面的断言，一旦乱序（vitest --sequence.shuffle）先跑到它，前面的
 * toHaveLength(1) 就会看到 2 条而失败 —— 用例通过与否取决于执行顺序。
 */
let installSnapshot: Array<[string, unknown, unknown]> = []
let setup: typeof import('./useContainerTransform').setupContainerTransform

beforeAll(async () => {
  globalThis.requestAnimationFrame = ((cb: FrameRequestCallback) => {
    frames.push(cb)
    return frames.length
  }) as typeof globalThis.requestAnimationFrame

  Object.defineProperty(globalThis, 'DOMMatrixReadOnly', {
    writable: true,
    configurable: true,
    value: class {
      a = 1
      d = 1
      constructor(_init?: string) {}
    },
  })

  Object.defineProperty(Element.prototype, 'animate', {
    writable: true,
    configurable: true,
    value(keyframes: Array<Record<string, unknown>>, options: Record<string, unknown>) {
      let finish = () => {}
      const finished = new Promise<void>((resolve) => {
        finish = () => resolve()
      })
      animations.push({ keyframes, options, finish })
      return { finished }
    },
  })

  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    configurable: true,
    value: (query: string) => ({
      matches: query.includes('reduced-motion') ? reduceMotion : false,
      media: query,
      onchange: null,
      addListener: () => {},
      removeListener: () => {},
      addEventListener: () => {},
      removeEventListener: () => {},
      dispatchEvent: () => false,
    }),
  })

  Date.now = () => clock

  const realAdd = document.addEventListener.bind(document)
  document.addEventListener = ((type: string, listener: unknown, options: unknown) => {
    addSpyCalls.push([type, listener, options])
    return realAdd(type as keyof DocumentEventMap, listener as EventListener, options as boolean)
  }) as typeof document.addEventListener

  const mod = await import('./useContainerTransform')
  setup = mod.setupContainerTransform
  // 幂等：连装三次也只应留下一份监听
  setup({ enabled: () => enabled })
  setup({ enabled: () => false })
  setup()
  installSnapshot = [...addSpyCalls]
})

beforeEach(() => {
  frames = []
  animations = []
  reduceMotion = false
  enabled = true
  // 推进时间轴，让上一个用例记录的点击源必然过期
  clock += 1_000_000
})

afterEach(async () => {
  // 在环境仍然存活时把移除记录消化掉：模块级 MutationObserver 没有卸载入口
  document.body.innerHTML = ''
  await new Promise(resolve => setTimeout(resolve, 0))
  animations = []
})

// 覆盖了 rAF / DOMMatrixReadOnly / Element.animate / matchMedia / Date.now / addEventListener，逐一还原
afterAll(async () => {
  document.body.innerHTML = ''
  await new Promise(resolve => setTimeout(resolve, 0))
  globalThis.requestAnimationFrame = originalRaf
  Reflect.deleteProperty(globalThis as unknown as Record<string, unknown>, 'DOMMatrixReadOnly')
  if (originalAnimate) {
    Object.defineProperty(Element.prototype, 'animate', originalAnimate)
  }
  else {
    Reflect.deleteProperty(Element.prototype as unknown as Record<string, unknown>, 'animate')
  }
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    configurable: true,
    value: originalMatchMedia,
  })
  Date.now = originalDateNow
  Reflect.deleteProperty(document as unknown as Record<string, unknown>, 'addEventListener')
})

/** 跑完当前排队的帧回调（源码用了双层 rAF） */
function flushFrames(): void {
  for (let round = 0; round < 3; round++) {
    const pending = frames
    frames = []
    for (const cb of pending) {
      cb(0)
    }
  }
}

function rect(left: number, top: number, width: number, height: number): DOMRect {
  return new DOMRect(left, top, width, height)
}

function sized(el: HTMLElement, box: DOMRect): HTMLElement {
  el.getBoundingClientRect = () => box
  return el
}

function ghosts(): HTMLElement[] {
  return [...document.body.children].filter(
    (child): child is HTMLElement => child instanceof HTMLElement && child.style.position === 'fixed',
  )
}

/** 建一个可被识别的点击源并派发 pointerdown */
function clickSource(box = rect(10, 20, 100, 40)): HTMLElement {
  const button = sized(document.createElement('button'), box)
  document.body.appendChild(button)
  button.dispatchEvent(new Event('pointerdown', { bubbles: true }))
  return button
}

/** 挂载一个符合弹窗选择器的元素 */
function openModal(box = rect(200, 100, 400, 300)): HTMLElement {
  const modal = sized(document.createElement('div'), box)
  modal.setAttribute('data-scope', 'dialog')
  modal.setAttribute('data-part', 'content')
  document.body.appendChild(modal)
  return modal
}

/** 等 MutationObserver 的回调派发完 */
async function flushObserver(): Promise<void> {
  await new Promise(resolve => setTimeout(resolve, 0))
}

describe('setupContainerTransform 安装', () => {
  it('重复调用只装一次全局 pointerdown 监听', () => {
    expect(installSnapshot.filter(call => call[0] === 'pointerdown')).toHaveLength(1)
  })

  it('点击源在捕获期记录，早于任何 UI 框架的处理', () => {
    const call = installSnapshot.find(item => item[0] === 'pointerdown')

    expect(call?.[2]).toBe(true)
  })

  it('首次安装的开关生效，后续重复安装传入的开关被忽略（幂等语义）', async () => {
    clickSource()
    openModal()
    await flushObserver()
    flushFrames()

    // 第二次安装传的是恒 false 的开关；若它生效，这里就不会有动画
    expect(animations).toHaveLength(1)
  })
})

describe('容器变形的触发条件', () => {
  it('点击按钮后弹窗出现时生成幽灵容器，从点击源飞向弹窗', async () => {
    clickSource(rect(10, 20, 100, 40))
    openModal(rect(200, 100, 400, 300))
    await flushObserver()
    flushFrames()

    expect(animations).toHaveLength(1)
    const ghost = ghosts()[0]
    expect(ghost?.style.left).toBe('200px')
    expect(ghost?.style.top).toBe('100px')
    expect(ghost?.style.width).toBe('400px')
    expect(ghost?.style.height).toBe('300px')
    // 起始帧把幽灵位移回点击源位置并缩到点击源尺寸
    expect(animations[0]?.keyframes[0]?.transform)
      .toBe('translate(-190px, -80px) scale(0.25, 0.13333333333333333)')
    expect(animations[0]?.options.duration).toBe(300)
  })

  it('幽灵不参与命中测试，且压在最上层', async () => {
    clickSource()
    openModal()
    await flushObserver()
    flushFrames()

    const ghost = ghosts()[0]
    expect(ghost?.style.pointerEvents).toBe('none')
    expect(ghost?.style.zIndex).toBe('9999')
  })

  it('动画结束后幽灵自我移除，不在页面上留残骸', async () => {
    clickSource()
    openModal()
    await flushObserver()
    flushFrames()
    expect(ghosts()).toHaveLength(1)

    animations[0]?.finish()
    await flushObserver()

    expect(ghosts()).toHaveLength(0)
  })

  it('没有点击过任何源就出现弹窗时不做变形', async () => {
    openModal()
    await flushObserver()
    flushFrames()

    expect(animations).toHaveLength(0)
  })

  it('点击与弹窗出现相隔超过 800ms 关联窗口时不做变形', async () => {
    clickSource()
    clock += 801
    openModal()
    await flushObserver()
    flushFrames()

    expect(animations).toHaveLength(0)
  })

  it('恰好 800ms 仍在关联窗口内', async () => {
    clickSource()
    clock += 800
    openModal()
    await flushObserver()
    flushFrames()

    expect(animations).toHaveLength(1)
  })

  it('开关关闭时整体跳过', async () => {
    enabled = false
    clickSource()
    openModal()
    await flushObserver()
    flushFrames()

    expect(animations).toHaveLength(0)
  })

  it('系统开启减弱动效时整体跳过，即使开关为开', async () => {
    reduceMotion = true
    clickSource()
    openModal()
    await flushObserver()
    flushFrames()

    expect(animations).toHaveLength(0)
  })

  it('点击的不是可识别的容器元素时不记录点击源', async () => {
    const plain = sized(document.createElement('span'), rect(10, 20, 100, 40))
    document.body.appendChild(plain)
    plain.dispatchEvent(new Event('pointerdown', { bubbles: true }))

    openModal()
    await flushObserver()
    flushFrames()

    expect(animations).toHaveLength(0)
  })

  it('表格行也是可识别的点击源', async () => {
    const row = sized(document.createElement('div'), rect(0, 0, 300, 48))
    row.setAttribute('data-scope', 'table')
    row.setAttribute('data-part', 'row')
    document.body.appendChild(row)
    row.dispatchEvent(new Event('pointerdown', { bubbles: true }))

    openModal()
    await flushObserver()
    flushFrames()

    expect(animations).toHaveLength(1)
  })

  it('弹窗测得宽度为 0 时放弃变形，避免除零缩放', async () => {
    clickSource()
    openModal(rect(0, 0, 0, 0))
    await flushObserver()
    flushFrames()

    expect(animations).toHaveLength(0)
  })

  it('弹窗在测量前就被移除时不再变形', async () => {
    clickSource()
    const modal = openModal()
    await flushObserver()
    modal.remove()
    flushFrames()

    expect(animations).toHaveLength(0)
  })

  it('包在容器里挂载的弹窗同样被识别', async () => {
    clickSource()
    const host = document.createElement('div')
    const modal = sized(document.createElement('div'), rect(200, 100, 400, 300))
    modal.setAttribute('data-scope', 'dialog')
    modal.setAttribute('data-part', 'content')
    host.appendChild(modal)
    document.body.appendChild(host)
    await flushObserver()
    flushFrames()

    expect(animations).toHaveLength(1)
  })
})

describe('弹窗关闭时的反向播放', () => {
  it('关闭时幽灵从弹窗矩形收回点击源位置，时长更短', async () => {
    clickSource(rect(10, 20, 100, 40))
    const modal = openModal(rect(200, 100, 400, 300))
    await flushObserver()
    flushFrames()
    animations = []
    for (const ghost of ghosts()) {
      ghost.remove()
    }

    modal.remove()
    await flushObserver()

    expect(animations).toHaveLength(1)
    expect(animations[0]?.options.duration).toBe(240)
    const ghost = ghosts()[0]
    expect(ghost?.style.left).toBe('10px')
    expect(ghost?.style.top).toBe('20px')
    expect(ghost?.style.width).toBe('100px')
  })

  it('没有关联过的弹窗被移除时不播反向动画', async () => {
    const modal = openModal()
    await flushObserver()
    flushFrames()
    animations = []

    modal.remove()
    await flushObserver()

    expect(animations).toHaveLength(0)
  })

  it('同一个弹窗只反向播放一次，重复挂载移除不再入账', async () => {
    clickSource()
    const modal = openModal()
    await flushObserver()
    flushFrames()
    animations = []

    modal.remove()
    await flushObserver()
    expect(animations).toHaveLength(1)

    animations = []
    clock += 1_000_000
    document.body.appendChild(modal)
    await flushObserver()
    flushFrames()
    modal.remove()
    await flushObserver()

    expect(animations).toHaveLength(0)
  })

  it('开关关闭时连反向动画也不播', async () => {
    clickSource()
    const modal = openModal()
    await flushObserver()
    flushFrames()
    animations = []
    enabled = false

    modal.remove()
    await flushObserver()

    expect(animations).toHaveLength(0)
  })
})

// 放在文件末尾：这组用例会真的卸载再重装，避免影响前面共用同一次安装的用例。
describe('卸载入口', () => {
  // 回归锚点（清单条目 46）：修复前 setupContainerTransform 返回 void，
  // pointerdown 监听与 MutationObserver 装上后没有任何卸载入口，
  // 微前端 / 多实例卸载场景会残留监听，lastSource 也长期握着 DOMRect。
  it('卸载后 pointerdown 不再被记录、弹窗挂载也不再变形，重装后恢复', async () => {
    const dispose = setup({ enabled: () => enabled })
    expect(typeof dispose).toBe('function')

    dispose()

    clickSource()
    openModal()
    await flushObserver()
    flushFrames()
    expect(animations).toHaveLength(0)

    // 重装后重新生效（installed 标志已随卸载复位）
    const reinstalled = setup({ enabled: () => enabled })
    clock += 1_000_000
    clickSource()
    openModal()
    await flushObserver()
    flushFrames()

    expect(animations).toHaveLength(1)
    expect(typeof reinstalled).toBe('function')
  })

  it('重复安装拿到的是同一个卸载函数，重复卸载不误伤后来的安装', async () => {
    const first = setup({ enabled: () => enabled })
    const second = setup({ enabled: () => false })

    expect(second).toBe(first)

    first()
    // 卸载后重装，再拿旧句柄卸一次：不应把新安装也卸掉
    setup({ enabled: () => enabled })
    first()

    clock += 1_000_000
    clickSource()
    openModal()
    await flushObserver()
    flushFrames()

    expect(animations).toHaveLength(1)
  })
})
