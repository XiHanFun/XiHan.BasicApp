import type { RouteRecordRaw } from 'vue-router'
import type { MenuNode } from '~/stores'
import { resolveView } from './views'

/**
 * 后端菜单树 → 路由记录（方案 6.5）：每个页面节点一条顶层路由（路径是绝对路径，子节点同样平铺），名字是页面码，
 * 组件按 `component`（`<包>/<路径>/index`）精确解析，`meta.permission` 由守卫校验。目录只做分组、外链在新标签打开，二者不生成路由；
 * 隐藏的页面照常生成（可直接访问，导航里不显示）。
 */
export function menuRoutes(menus: readonly MenuNode[]): RouteRecordRaw[] {
  const records: RouteRecordRaw[] = []
  const walk = (nodes: readonly MenuNode[]) => {
    for (const node of nodes) {
      if (node.kind === 'Page') {
        records.push({
          path: node.route,
          name: node.code,
          component: resolveView(node.component),
          // 壳层（标签栏、面包屑、全局搜索）读 meta.title / icon：标题给文案键，由壳层按 te / t 翻译；运营改过的标题是原文
          meta: {
            permission: node.permission ?? undefined,
            titleKey: node.title ? undefined : node.titleKey,
            title: node.title || node.titleKey,
            icon: node.icon ?? undefined,
            keepAlive: node.keepAlive || undefined,
          },
        })
      }
      walk(node.children)
    }
  }
  walk(menus)
  return records
}

/** 登录后的落点：第一个在导航里显示的页面（深度优先），都隐藏时取第一个页面；没有页面即 null。 */
export function firstRoute(menus: readonly MenuNode[]): string | null {
  const pages: MenuNode[] = []
  const walk = (nodes: readonly MenuNode[]) => {
    for (const node of nodes) {
      if (node.kind === 'Page' && node.route)
        pages.push(node)
      walk(node.children)
    }
  }
  walk(menus)
  return (pages.find(page => !page.hidden) ?? pages[0])?.route ?? null
}
