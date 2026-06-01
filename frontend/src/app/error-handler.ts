import type { App } from 'vue'

/** 全局错误来源 */
type ErrorSource = 'vue' | 'window' | 'promise'

/**
 * 统一错误收口：当前输出到控制台，并预留上报钩子。
 * 后续可在此接入后端日志接口 / Sentry 等错误上报，无需改动各调用点。
 */
function reportError(source: ErrorSource, error: unknown, info?: string) {
  // eslint-disable-next-line no-console
  console.error(`[GlobalError:${source}]`, info ?? '', error)
  // TODO: 接入上报通道（如 POST 到后端日志接口），失败需静默避免二次抛错
}

/**
 * 注册全局错误边界：Vue 渲染/生命周期错误 + 未捕获 JS 错误 + 未处理的 Promise 拒绝。
 * 应在 createApp 之后、mount 之前调用，保证启动早期的异常也能被捕获。
 *
 * @param app Vue 应用实例
 */
export function setupGlobalErrorHandler(app: App) {
  // 1. Vue 组件渲染/生命周期/侦听器内抛出的错误
  app.config.errorHandler = (error, _instance, info) => {
    reportError('vue', error, info)
  }

  // 2. 全局未捕获的同步错误
  window.addEventListener('error', (event) => {
    reportError('window', event.error ?? event.message)
  })

  // 3. 未处理的 Promise 拒绝
  window.addEventListener('unhandledrejection', (event) => {
    reportError('promise', event.reason)
  })
}
