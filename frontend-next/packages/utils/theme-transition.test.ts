/**
 * packages/utils/theme-transition.ts 单元测试。
 *
 * 职责边界：明暗切换的圆形扩散动画编排。重点锁定源码注释里写明的三条「坑」：
 * 1) 后台标签页（document.hidden）必须绕开 ViewTransition，否则远端推来的偏好会卡在 ready 上；
 * 2) clipPath 一律用百分比而非 px，写 px 会被页面缩放整体压缩导致圆心偏移；
 * 3) 调用方只等 commit 落地，不等 450ms 动画播完，否则期间的本机改动会丢失上行。
 * 不验证真实视觉效果，ViewTransition 与 Web Animations 在 jsdom 中均由替身提供。
 */
import { afterEach, describe, expect, it, vi } from 'vitest'
import { runThemeTransition } from './theme-transition'

interface FakeAnimation {
  onfinish: (() => void) | null
}

interface FakeTransition {
  ready: Promise<void>
  finished: Promise<void>
  skipTransition: ReturnType<typeof vi.fn>
}

const restorers: Array<() => void> = []

function defineTemporary(target: object, key: string, value: unknown): void {
  const original = Object.getOwnPropertyDescriptor(target, key)
  Object.defineProperty(target, key, { value, configurable: true, writable: true })
  restorers.push(() => {
    if (original) {
      Object.defineProperty(target, key, original)
    }
    else {
      Reflect.deleteProperty(target, key)
    }
  })
}

/**
 * 让已排入队列的微任务与宏任务跑完（playReveal 是游离的异步分支）。
 *
 * 连排多轮而不是一轮：playReveal 内部有多层 await，一个 setTimeout(0) 只推进一层。
 * 排不干净时，上一个用例遗留的分支会在下一个用例执行期间才 resolve，
 * 而它 remove 的 theme-switching 挂在共享的 documentElement 上 —— 表现为乱序执行时
 * 「类应为 true 却是 false」的随机失败。
 */
async function flush(): Promise<void> {
  for (let turn = 0; turn < 5; turn += 1) {
    await new Promise(resolve => setTimeout(resolve, 0))
  }
}

/** 安装 startViewTransition 与 documentElement.animate 的替身。 */
function installViewTransition() {
  let resolveReady = (): void => {}
  let rejectReady = (_reason?: unknown): void => {}
  const ready = new Promise<void>((resolve, reject) => {
    resolveReady = resolve
    rejectReady = reject
  })
  // finished 故意永不结算：用于证明调用方不会等动画播完
  const finished = new Promise<void>(() => {})
  const skipTransition = vi.fn()
  const transition: FakeTransition = { ready, finished, skipTransition }

  const startViewTransition = vi.fn((callback: () => Promise<void>) => {
    void callback()
    return transition
  })
  defineTemporary(document, 'startViewTransition', startViewTransition)

  const animation: FakeAnimation = { onfinish: null }
  const animate = vi.fn(() => animation)
  defineTemporary(document.documentElement, 'animate', animate)

  return { startViewTransition, animate, animation, skipTransition, resolveReady, rejectReady }
}

/** 从 circle(R% at X% Y%) 里取出三个百分比数值。 */
function parseCircle(value: string): { radius: number, x: number, y: number } {
  const matched = /^circle\(([\d.]+)% at ([\d.]+)% ([\d.]+)%\)$/.exec(value)
  if (!matched) {
    throw new Error(`clipPath 不是预期的百分比 circle 形式: ${value}`)
  }
  return { radius: Number(matched[1]), x: Number(matched[2]), y: Number(matched[3]) }
}

afterEach(async () => {
  await flush()
  while (restorers.length > 0) {
    restorers.pop()?.()
  }
  document.documentElement.className = ''
  vi.restoreAllMocks()
})

describe('降级直切分支', () => {
  it('enabled 为 false 时不启动 ViewTransition，直接提交主题', async () => {
    const { startViewTransition } = installViewTransition()
    const commit = vi.fn()

    await runThemeTransition({ toDark: true, commit, enabled: false })

    expect(startViewTransition).not.toHaveBeenCalled()
    expect(commit).toHaveBeenCalledTimes(1)
  })

  it('提交期间挂上 theme-switching 抑制 CSS 过渡，下一帧才摘掉', async () => {
    installViewTransition()
    let classDuringCommit = false

    await runThemeTransition({
      toDark: false,
      enabled: false,
      commit: () => {
        classDuringCommit = document.documentElement.classList.contains('theme-switching')
      },
    })

    expect(classDuringCommit).toBe(true)
    // 摘除排在 requestAnimationFrame 回调里，等到它真正跑完再断言
    await vi.waitFor(() => {
      expect(document.documentElement.classList.contains('theme-switching')).toBe(false)
    })
  })

  it('浏览器不支持 startViewTransition 时走直切分支并如常提交', async () => {
    const commit = vi.fn()

    await runThemeTransition({ toDark: true, commit })

    expect(commit).toHaveBeenCalledTimes(1)
  })

  it('页面处于后台标签页时绕开 ViewTransition，避免 ready 长期不结算卡住偏好落地', async () => {
    const { startViewTransition } = installViewTransition()
    defineTemporary(document, 'hidden', true)
    const commit = vi.fn()

    await runThemeTransition({ toDark: true, commit })

    expect(startViewTransition).not.toHaveBeenCalled()
    expect(commit).toHaveBeenCalledTimes(1)
  })

  it('直切分支等待异步 commit 完成后才返回', async () => {
    installViewTransition()
    let committed = false

    await runThemeTransition({
      toDark: true,
      enabled: false,
      commit: async () => {
        await Promise.resolve()
        committed = true
      },
    })

    expect(committed).toBe(true)
  })
})

describe('视图过渡动画分支', () => {
  it('commit 在 ViewTransition 回调内执行，只调用一次', async () => {
    const { startViewTransition } = installViewTransition()
    const commit = vi.fn()

    await runThemeTransition({ toDark: false, commit })

    expect(startViewTransition).toHaveBeenCalledTimes(1)
    expect(commit).toHaveBeenCalledTimes(1)
  })

  it('ready 尚未结算时调用方就已返回，只等值落地不等动画播完', async () => {
    const { animate } = installViewTransition()
    const commit = vi.fn()

    await runThemeTransition({ toDark: false, commit })

    expect(commit).toHaveBeenCalledTimes(1)
    // ready 仍挂起，动画还没开始，但 await 已经返回了
    expect(animate).not.toHaveBeenCalled()
  })

  it('切亮色时新层由 0 扩散到全屏，作用在 view-transition-new 伪元素上', async () => {
    const { animate, resolveReady } = installViewTransition()

    await runThemeTransition({ toDark: false, commit: () => {} })
    resolveReady()
    await flush()

    const [keyframes, options] = animate.mock.calls[0] as unknown as [
      { clipPath: string[] },
      KeyframeAnimationOptions,
    ]
    expect(parseCircle(keyframes.clipPath[0] ?? '').radius).toBe(0)
    expect(parseCircle(keyframes.clipPath[1] ?? '').radius).toBeGreaterThan(0)
    expect(options.pseudoElement).toBe('::view-transition-new(root)')
  })

  it('切暗色时旧层由全屏收缩到 0，作用在 view-transition-old 伪元素上', async () => {
    const { animate, resolveReady } = installViewTransition()

    await runThemeTransition({ toDark: true, commit: () => {} })
    resolveReady()
    await flush()

    const [keyframes, options] = animate.mock.calls[0] as unknown as [
      { clipPath: string[] },
      KeyframeAnimationOptions,
    ]
    expect(parseCircle(keyframes.clipPath[0] ?? '').radius).toBeGreaterThan(0)
    expect(parseCircle(keyframes.clipPath[1] ?? '').radius).toBe(0)
    expect(options.pseudoElement).toBe('::view-transition-old(root)')
  })

  it('clipPath 一律使用百分比，出现 px 会在页面缩放时把圆心压向左上', async () => {
    const { animate, resolveReady } = installViewTransition()

    await runThemeTransition({ toDark: false, commit: () => {}, origin: { clientX: 100, clientY: 60 } })
    resolveReady()
    await flush()

    const [keyframes] = animate.mock.calls[0] as unknown as [{ clipPath: string[] }]
    for (const frame of keyframes.clipPath) {
      expect(frame).not.toMatch(/px/)
      expect(frame).toMatch(/^circle\([\d.]+% at [\d.]+% [\d.]+%\)$/)
    }
  })

  it('省略 origin 时从视口中心扩散，半径按对角线参照解析为约 70.71%', async () => {
    const { animate, resolveReady } = installViewTransition()

    await runThemeTransition({ toDark: false, commit: () => {} })
    resolveReady()
    await flush()

    const [keyframes] = animate.mock.calls[0] as unknown as [{ clipPath: string[] }]
    const end = parseCircle(keyframes.clipPath[1] ?? '')
    expect(end.x).toBe(50)
    expect(end.y).toBe(50)
    // 1024x768 视口下中心到最远角为 640，参照长度 1280/√2≈905.10
    expect(end.radius).toBeCloseTo(70.7107, 3)
  })

  it('起点取到视口角落时半径超过 100%，仍能覆盖整屏', async () => {
    const { animate, resolveReady } = installViewTransition()

    await runThemeTransition({ toDark: false, commit: () => {}, origin: { clientX: 0, clientY: 0 } })
    resolveReady()
    await flush()

    const [keyframes] = animate.mock.calls[0] as unknown as [{ clipPath: string[] }]
    const end = parseCircle(keyframes.clipPath[1] ?? '')
    expect(end.x).toBe(0)
    expect(end.y).toBe(0)
    expect(end.radius).toBeCloseTo(141.4214, 3)
  })

  it('动画时长与缓动固定为 450ms / ease-in', async () => {
    const { animate, resolveReady } = installViewTransition()

    await runThemeTransition({ toDark: true, commit: () => {} })
    resolveReady()
    await flush()

    const [, options] = animate.mock.calls[0] as unknown as [unknown, KeyframeAnimationOptions]
    expect(options.duration).toBe(450)
    expect(options.easing).toBe('ease-in')
  })

  it('动画播完后跳过剩余 ViewTransition 并摘掉 theme-switching，消除尾帧闪烁', async () => {
    const { animation, skipTransition, resolveReady } = installViewTransition()

    await runThemeTransition({ toDark: true, commit: () => {} })
    resolveReady()
    await flush()

    expect(document.documentElement.classList.contains('theme-switching')).toBe(true)
    animation.onfinish?.()

    expect(skipTransition).toHaveBeenCalledTimes(1)
    expect(document.documentElement.classList.contains('theme-switching')).toBe(false)
  })

  it('ready 被拒绝时兜底摘掉 theme-switching，页面不会永久停在禁用过渡状态', async () => {
    const { animate, rejectReady } = installViewTransition()

    await runThemeTransition({ toDark: false, commit: () => {} })
    rejectReady(new Error('transition aborted'))
    await flush()

    expect(animate).not.toHaveBeenCalled()
    expect(document.documentElement.classList.contains('theme-switching')).toBe(false)
  })

  it('commit 抛错时异常向调用方冒泡，而不是被静默吞掉', async () => {
    installViewTransition()

    await expect(
      runThemeTransition({
        toDark: true,
        enabled: false,
        commit: () => {
          throw new Error('保存偏好失败')
        },
      }),
    ).rejects.toThrow(/保存偏好失败/)
  })
})
