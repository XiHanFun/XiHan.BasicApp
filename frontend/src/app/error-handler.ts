import type { App } from 'vue'

/** 全局错误来源 */
type ErrorSource = 'vue' | 'window' | 'promise'

/**
 * 统一错误收口：当前输出到控制台，并预留上报钩子。
 * 后续可在此接入后端日志接口 / Sentry 等错误上报，无需改动各调用点。
 */
function reportError(source: ErrorSource, error: unknown, info?: string) {
  console.error(`[GlobalError:${source}]`, info ?? '', error)
  // TODO: 接入上报通道（如 POST 到后端日志接口），失败需静默避免二次抛错
}

/**
 * 仍在使用 window 监听器的应用实例数。
 * window 是全局单例，监听器只挂一份：多实例挂载（微前端 / 热更新重挂）时重复注册会让同一个错误被上报 N 次，
 * 接入上报通道后就是成倍的上报量。计数归零才真正摘掉监听器。
 */
let activeInstallCount = 0
/** 摘除 window 监听器的闭包；为 null 表示当前没挂 */
let removeWindowListeners: (() => void) | null = null

/**
 * 注册全局错误边界：Vue 渲染/生命周期错误 + 未捕获 JS 错误 + 未处理的 Promise 拒绝。
 * 应在 createApp 之后、mount 之前调用，保证启动早期的异常也能被捕获。
 *
 * 对 window 监听器幂等：重复调用不会重复注册，也就不会重复上报同一个错误。
 *
 * @param app Vue 应用实例
 * @returns 卸载函数，摘掉本次注册（app.unmount 时调用）；重复调用无副作用
 */
export function setupGlobalErrorHandler(app: App) {
  // 1. Vue 组件渲染/生命周期/侦听器内抛出的错误（按实例挂，不同 app 各自一份）
  const handleVueError: NonNullable<App['config']['errorHandler']> = (error, _instance, info) => {
    reportError('vue', error, info)
  }
  app.config.errorHandler = handleVueError

  if (!removeWindowListeners) {
    // 2. 全局未捕获的同步错误
    const handleWindowError = (event: ErrorEvent) => {
      reportError('window', event.error ?? event.message)
    }
    // 3. 未处理的 Promise 拒绝
    const handleUnhandledRejection = (event: PromiseRejectionEvent) => {
      reportError('promise', event.reason)
    }

    window.addEventListener('error', handleWindowError)
    window.addEventListener('unhandledrejection', handleUnhandledRejection)
    removeWindowListeners = () => {
      window.removeEventListener('error', handleWindowError)
      window.removeEventListener('unhandledrejection', handleUnhandledRejection)
    }
  }
  activeInstallCount += 1

  let disposed = false
  return () => {
    if (disposed) {
      return
    }
    disposed = true

    if (app.config.errorHandler === handleVueError) {
      app.config.errorHandler = undefined
    }
    activeInstallCount -= 1
    if (activeInstallCount === 0) {
      removeWindowListeners?.()
      removeWindowListeners = null
    }
  }
}
