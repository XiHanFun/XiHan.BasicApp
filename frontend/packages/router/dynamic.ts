/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

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
 * 按页面登记的组件路径取视图加载器，取不到返回 null
 * 组件路径形如 `<包>/<路径>/index`；null 由调用方套兜底视图
 * 配了却找不到说明菜单与前端模块不一致，必须报错，否则点进去是 404 却无从查起
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
 * 用「名字 = 路由名」的壳组件承载页面，让路由名成为唯一的缓存标识
 * KeepAlive 的 include 按组件名匹配，页面自带的 name 与路由名对不上会静默失效；多个菜单指向同一页面也各有壳，不串缓存
 */
// 壳按路由名缓存：同一路由名永远返回同一个壳组件（稳定的组件标识是 KeepAlive 命中缓存的前提），
// 不依赖 vue-router 是否缓存异步解析结果
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
 * 壳层菜单模型（accessStore.accessRoutes）转成路由记录
 * 目录重定向到第一个可导航子页，页面按组件路径解析视图，外链不生成路由
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
        route.component = withRouteName(component, item.name
          ? String(item.name)
          : undefined)
      }

      if (!route.component && (!route.children || route.children.length === 0) && fallbackView) {
        route.component = fallbackView
      }

      return route
    })
}
