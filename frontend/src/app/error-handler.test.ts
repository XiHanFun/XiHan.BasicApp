/**
 * 全局错误边界（src/app/error-handler.ts）单元测试。
 *
 * 职责边界：验证三条错误通道（Vue 渲染错误 / window 未捕获错误 / 未处理的 Promise 拒绝）
 * 都被接进同一个上报出口，日志前缀能区分来源，以及注册本身对 window 监听器幂等、可卸载。
 * window 监听器的挂载计数是模块级状态，afterEach 必须调用每次注册返回的卸载函数复位，
 * 否则会污染同文件后续用例。
 */
import type { App } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { setupGlobalErrorHandler } from './error-handler'

type WindowListener = Parameters<typeof window.addEventListener>[1]

/** 本文件内注册过的 window 监听器（由 addEventListener 探针记录），afterEach 清空 */
const registered: [string, WindowListener][] = []
/** 本文件内每次注册返回的卸载函数，afterEach 全部调用，复位模块级挂载计数 */
const disposers: (() => void)[] = []
const originalAddEventListener = window.addEventListener.bind(window)

let consoleError: ReturnType<typeof vi.spyOn>

/** 造一个只带 config 的最小 Vue 应用替身 */
function createFakeApp() {
  return { config: {} } as unknown as App
}

function setup() {
  const app = createFakeApp()
  disposers.push(setupGlobalErrorHandler(app))
  return app
}

beforeEach(() => {
  consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
  vi.spyOn(window, 'addEventListener').mockImplementation((type, listener, options) => {
    registered.push([String(type), listener])
    originalAddEventListener(type as never, listener as never, options as never)
  })
})

afterEach(() => {
  for (const dispose of disposers.splice(0)) {
    dispose()
  }
  // 卸载函数已经把监听器摘干净，这里兜底防止用例中途抛错导致残留
  for (const [type, listener] of registered) {
    window.removeEventListener(type, listener)
  }
  registered.length = 0
})

function dispatchWindowError(init: { error?: unknown, message?: string }) {
  const event = new Event('error') as Event & { error?: unknown, message?: string }
  event.error = init.error
  event.message = init.message
  window.dispatchEvent(event)
}

function dispatchRejection(reason: unknown) {
  const event = new Event('unhandledrejection') as Event & { reason?: unknown }
  event.reason = reason
  window.dispatchEvent(event)
}

describe('注册全局错误边界', () => {
  it('接管 Vue 应用的 errorHandler，并挂上 error 与 unhandledrejection 两个 window 监听器', () => {
    const app = setup()

    expect(typeof app.config.errorHandler).toBe('function')
    expect(registered.map(([type]) => type)).toEqual(['error', 'unhandledrejection'])
  })

  // 回归锚点：修复前每次调用都无条件 addEventListener，重复挂载（微前端多实例 / 热更新重挂）
  // 会让同一个错误被上报 N 次；现在 window 监听器全局只挂一份。
  it('重复调用不会重复注册 window 监听器，同一个错误只上报一次', () => {
    setup()
    setup()
    dispatchWindowError({ error: new Error('重复') })

    expect(registered.map(([type]) => type)).toEqual(['error', 'unhandledrejection'])
    expect(consoleError).toHaveBeenCalledTimes(1)
  })

  it('每个应用实例各自接管自己的 errorHandler——window 监听器共用不影响 Vue 通道', () => {
    const first = setup()
    const second = setup()

    expect(typeof first.config.errorHandler).toBe('function')
    expect(typeof second.config.errorHandler).toBe('function')
    expect(first.config.errorHandler).not.toBe(second.config.errorHandler)
  })
})

describe('组件渲染错误通道', () => {
  it('用 GlobalError:vue 前缀上报，并带上 Vue 给的 info 定位串', () => {
    const app = setup()
    const error = new Error('渲染炸了')
    app.config.errorHandler?.(error, null, 'render function')

    expect(consoleError).toHaveBeenCalledWith('[GlobalError:vue]', 'render function', error)
  })

  it('info 缺省时以空串占位，不打印 undefined', () => {
    const app = setup()
    const error = new Error('无 info')
    app.config.errorHandler?.(error, null, undefined as unknown as string)

    expect(consoleError).toHaveBeenCalledWith('[GlobalError:vue]', '', error)
  })

  it('抛出的不是 Error 实例（字符串 / null）时同样原样上报，不做二次包装', () => {
    const app = setup()
    app.config.errorHandler?.('纯字符串错误', null, 'setup')
    app.config.errorHandler?.(null, null, 'setup')

    expect(consoleError).toHaveBeenNthCalledWith(1, '[GlobalError:vue]', 'setup', '纯字符串错误')
    expect(consoleError).toHaveBeenNthCalledWith(2, '[GlobalError:vue]', 'setup', null)
  })
})

describe('window 未捕获错误通道', () => {
  it('优先上报 event.error 对象，前缀为 GlobalError:window', () => {
    setup()
    const error = new Error('脚本错误')
    dispatchWindowError({ error, message: '不该用到的文案' })

    expect(consoleError).toHaveBeenCalledWith('[GlobalError:window]', '', error)
  })

  it('跨域脚本拿不到 error 对象时回退到 message 文案', () => {
    setup()
    dispatchWindowError({ error: undefined, message: 'Script error.' })

    expect(consoleError).toHaveBeenCalledWith('[GlobalError:window]', '', 'Script error.')
  })

  it('error 为 null 时同样回退 message——?? 只在 null/undefined 时回落', () => {
    setup()
    dispatchWindowError({ error: null, message: 'null 错误' })

    expect(consoleError).toHaveBeenCalledWith('[GlobalError:window]', '', 'null 错误')
  })

  it('未调用注册函数时不挂任何监听器，window 错误无人接管', () => {
    dispatchWindowError({ message: '无人接管' })

    expect(registered).toEqual([])
    expect(consoleError).not.toHaveBeenCalled()
  })
})

describe('未处理的 Promise 拒绝通道', () => {
  it('以 GlobalError:promise 前缀上报 reason', () => {
    setup()
    const reason = new Error('接口挂了')
    dispatchRejection(reason)

    expect(consoleError).toHaveBeenCalledWith('[GlobalError:promise]', '', reason)
  })

  it('reason 为 undefined（裸 reject()）时也照常上报，不吞掉这类拒绝', () => {
    setup()
    dispatchRejection(undefined)

    expect(consoleError).toHaveBeenCalledWith('[GlobalError:promise]', '', undefined)
  })

  it('三条通道互不串扰：一次拒绝只产生一条 promise 日志', () => {
    setup()
    dispatchRejection('拒绝原因')

    expect(consoleError).toHaveBeenCalledTimes(1)
    expect(consoleError.mock.calls[0]?.[0]).toBe('[GlobalError:promise]')
  })
})

describe('监听器的摘除', () => {
  it('手动摘掉监听器后再派发事件不再上报——确认监听器确实挂在 window 上而非别处', () => {
    setup()
    for (const [type, listener] of registered) {
      window.removeEventListener(type, listener)
    }
    registered.length = 0

    dispatchWindowError({ message: '已摘除' })
    dispatchRejection('已摘除')

    expect(consoleError).not.toHaveBeenCalled()
  })

  // 回归锚点：修复前函数返回 void，装上的监听器没有任何卸载入口。
  it('注册函数返回卸载入口：调用后监听器被摘掉，Vue 通道也一并交还', () => {
    const app = createFakeApp()
    const dispose = setupGlobalErrorHandler(app)
    dispose()

    dispatchWindowError({ message: '已卸载' })
    dispatchRejection('已卸载')

    expect(consoleError).not.toHaveBeenCalled()
    expect(app.config.errorHandler).toBeUndefined()
  })

  it('卸载函数可重复调用且不误摘其它实例：仍在用的实例继续收到上报', () => {
    setup()
    const app = createFakeApp()
    const dispose = setupGlobalErrorHandler(app)
    dispose()
    dispose()

    dispatchWindowError({ message: '仍有实例在用' })

    expect(consoleError).toHaveBeenCalledTimes(1)
  })

  it('全部实例卸载后重新注册，监听器会重新挂上', () => {
    const dispose = setupGlobalErrorHandler(createFakeApp())
    dispose()
    setup()

    dispatchWindowError({ message: '重新挂载' })

    expect(consoleError).toHaveBeenCalledTimes(1)
  })
})
