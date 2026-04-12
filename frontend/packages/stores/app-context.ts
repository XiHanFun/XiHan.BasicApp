import type { Router, RouteRecordRaw } from 'vue-router'
import { reactive } from 'vue'

/**
 * 应用上下文：packages（核心包）不直接依赖 src（应用层），
 * 而是由 src 在启动时注册 API 实现、路由实例、视图模块等。
 *
 * 这样 packages 只通过 ~/ 引用自身，src 通过 @/ 引用自身并注入到此处。
 */
export interface AppContext {
  /** src 注册的 API 函数集合，packages 按名称取用 */
  apis: Record<string, any>
  /** 获取路由实例（惰性，避免循环依赖） */
  getRouter: () => Promise<Router>
  /** 获取静态路由定义（用于静态路由模式） */
  getStaticRoutes: () => RouteRecordRaw[]
  /** src/views 下 import.meta.glob 的结果 */
  viewModules: Record<string, () => Promise<unknown>>
  /** src 注册的显式组件映射（PascalCase 路径 → 懒加载函数） */
  explicitComponentMap: Record<string, () => Promise<unknown>>
}

const context = reactive<AppContext>({
  apis: {},
  getRouter: () => Promise.reject(new Error('[app-context] Router not registered')),
  getStaticRoutes: () => [],
  viewModules: {},
  explicitComponentMap: {},
})

/** src 启动时调用，注入 API 实现和路由等 */
export function registerAppContext(partial: Partial<AppContext>) {
  if (partial.apis) {
    Object.assign(context.apis, partial.apis)
  }
  if (partial.getRouter) {
    context.getRouter = partial.getRouter
  }
  if (partial.getStaticRoutes) {
    context.getStaticRoutes = partial.getStaticRoutes
  }
  if (partial.viewModules) {
    context.viewModules = partial.viewModules
  }
  if (partial.explicitComponentMap) {
    context.explicitComponentMap = partial.explicitComponentMap
  }
}

/** packages 内部使用，获取应用上下文 */
export function useAppContext(): AppContext {
  return context
}
