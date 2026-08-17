import type { Component } from 'vue'
import type { RouteRecordRaw } from 'vue-router'
import type { MenuRoute } from '~/types'
import { defineComponent, h } from 'vue'
import { useAppContext } from '~/stores/app-context'
import {
  resolveFirstNavigableRouteListPath,
  routeListContainsVisiblePath,
} from '~/utils'

const fallbackView = () => import('~/views/_core/fallback/not-found.vue')

// 后端 Component 路径（PascalCase）→ 前端实际文件路径（kebab-case）的别名映射
const componentAliasMap: Record<string, string> = {
  // _core 特殊映射（packages/views/_core 不在 src/views 目录下）
  '_core/about/index': '_core/about/index',
  '_core/profile/index': '_core/profile/index',
}

// packages 自身的 _core 视图映射（使用 ~/ 引用，无需从 src 注入）
const coreComponentMap: Record<string, () => Promise<unknown>> = {
  '_core/about/index': () => import('~/views/_core/about/index.vue'),
  '_core/profile/index': () => import('~/views/_core/profile/index.vue'),
}

function toKebabCase(input: string) {
  return input
    .replace(/([A-Z]+)([A-Z][a-z])/g, '$1-$2')
    .replace(/([a-z0-9])([A-Z])/g, '$1-$2')
    .replace(/_/g, '-')
    .toLowerCase()
}

function resolveView(component?: string) {
  if (!component) {
    return null
  }

  const normalized = component
    .replace(/^\/+/, '')
    .replace(/^views\//, '')
    .replace(/\.vue$/, '')

  const rawPath = normalized
  const lowerPath = normalized.toLowerCase()
  const kebabPath = normalized
    .split('/')
    .map(segment => toKebabCase(segment))
    .join('/')
  const aliasPath = componentAliasMap[lowerPath] ?? componentAliasMap[kebabPath] ?? ''

  const removeIndexSuffix = (path: string) => path.replace(/\/index$/i, '')
  const candidates = new Set([
    lowerPath,
    kebabPath,
    aliasPath,
    removeIndexSuffix(rawPath),
    removeIndexSuffix(lowerPath),
    removeIndexSuffix(kebabPath),
    removeIndexSuffix(aliasPath),
  ])

  // 优先匹配 packages 自身的 _core 视图
  for (const candidate of candidates) {
    if (!candidate)
      continue
    const core = coreComponentMap[candidate]
    if (core)
      return core
  }

  // 然后查 src 注册的显式映射（优先级高于 glob）
  const ctx = useAppContext()
  for (const candidate of candidates) {
    if (!candidate)
      continue
    const explicit = ctx.explicitComponentMap[candidate]
    if (explicit)
      return explicit
  }

  // 最后查 src 注册的 viewModules glob
  const keys = Array.from(candidates).flatMap(path => [
    `/src/views/${path}.vue`,
    `/src/views/${path}/index.vue`,
  ])

  for (const key of keys) {
    const matched = ctx.viewModules[key]
    if (matched)
      return matched
  }

  return null
}

/**
 * 用「名字 = 路由名」的壳组件承载页面，使路由名成为唯一的缓存标识。
 *
 * KeepAlive 的 include 按「组件名」匹配，而页面组件自带的 defineOptions name（如 PlatformUserPage）
 * 与后端菜单路由名（如 IdentityUser）并不一致——若直接拿页面名做缓存，名字对不上就静默失效。
 * 这里把页面统一包进一层壳，壳的 name 恒等于路由名：页面组件自身的名字从此完全不参与匹配，
 * 路由名一条线贯穿 tab.name → cachedTabNames → include → 缓存标识，结构上杜绝「名字对不上」的隐藏 bug；
 * 即便多个菜单指向同一页面组件，也因壳各自独立而互不串缓存。
 */
// 壳按路由名缓存：同一路由名永远返回同一个壳组件（稳定的组件标识是 KeepAlive 命中缓存的前提），
// 不依赖 vue-router 是否缓存异步解析结果。
const routeNameWrappers = new Map<string, Component>()

function withRouteName(
  loader: () => Promise<unknown>,
  routeName?: string,
): () => Promise<unknown> {
  if (!routeName) {
    return loader
  }
  return async () => {
    const cached = routeNameWrappers.get(routeName)
    if (cached) {
      return cached
    }
    const mod = await loader()
    const raw = (mod && typeof mod === 'object' && 'default' in mod
      ? (mod as { default: Component }).default
      : mod) as Component
    const wrapper = defineComponent({
      name: routeName,
      setup() {
        return () => h(raw)
      },
    })
    routeNameWrappers.set(routeName, wrapper)
    return wrapper
  }
}

export function mapMenuToRoutes(menuRoutes: MenuRoute[]): RouteRecordRaw[] {
  return menuRoutes
    // 外链菜单（meta.link）不生成路由——由侧边栏/顶栏菜单点击新标签直接打开
    .filter(item => !!item.path && !item.meta?.link)
    .map((item) => {
      const component = resolveView(item.component)
      const route = {
        path: item.path,
        name: item.name,
        meta: item.meta as unknown as Record<string, unknown>,
      } as unknown as RouteRecordRaw

      if (item.children?.length) {
        route.children = mapMenuToRoutes(item.children)
      }

      if (route.children?.length) {
        const firstChildPath = resolveFirstNavigableRouteListPath(route.children)
        const redirect = item.redirect && item.redirect !== item.path
          && routeListContainsVisiblePath(route.children, item.redirect)
          ? item.redirect
          : firstChildPath
        if (redirect && redirect !== item.path) {
          route.redirect = redirect
        }
      }
      else if (item.redirect && item.redirect !== item.path) {
        route.redirect = item.redirect
      }

      if (component) {
        // 包一层「名字 = 路由名」的壳，使 KeepAlive 的 include（路由名列表）稳定匹配，页面组件自身名字不参与
        route.component = withRouteName(component, item.name ? String(item.name) : undefined)
      }

      if (!route.component && (!route.children || route.children.length === 0) && fallbackView) {
        route.component = fallbackView
      }

      return route
    })
}
