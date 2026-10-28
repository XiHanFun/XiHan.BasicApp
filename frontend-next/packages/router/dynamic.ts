import type { Component } from 'vue'
import type { RouteRecordRaw } from 'vue-router'
import type { MenuRoute } from '~/types'
import { defineComponent, h } from 'vue'
import {
  resolveFirstNavigableRouteListPath,
  routeListContainsVisiblePath,
} from '~/utils'
import { resolveView } from './views'

const fallbackView = () => import('~/views/_core/fallback/not-found.vue')

/**
 * 按页面登记的组件路径（`<包>/<路径>/index`）取视图加载器；没配或登记里没有返回 null，由调用方套兜底视图。
 * 配了却找不到是菜单与前端模块不一致，必须出声：侧栏照样渲染出条目，点进去却是 404，静默的话配错的人无从查起。
 */
function loadView(component?: string): (() => Promise<unknown>) | null {
  if (!component) {
    return null
  }
  try {
    return resolveView(component)
  }
  catch (error) {
    console.error('[router] 菜单的组件在前端模块里找不到，该页面回落到 404', component, error)
    return null
  }
}

/**
 * 用「名字 = 路由名」的壳组件承载页面，使路由名成为唯一的缓存标识。
 *
 * KeepAlive 的 include 按「组件名」匹配，而页面组件自带的 defineOptions name（如 IdentityUsersPage）
 * 与后端菜单路由名（页面码，如 identity.users）并不一致——若直接拿页面名做缓存，名字对不上就静默失效。
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

/**
 * 壳层菜单模型（accessStore.accessRoutes，由 auth.permissions 的菜单树投影而来）→ 路由记录。
 * 目录节点带子路由并自动重定向到第一个可导航子页面，页面节点按组件路径解析视图，外链不生成路由。
 */
export function mapMenuToRoutes(menuRoutes: MenuRoute[]): RouteRecordRaw[] {
  return menuRoutes
    // 外链菜单（meta.link）不生成路由——由侧边栏/顶栏菜单点击新标签直接打开
    .filter(item => !!item.path && !item.meta?.link)
    .map((item) => {
      const component = loadView(item.component)
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
