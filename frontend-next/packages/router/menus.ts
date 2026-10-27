import type { RouteRecordRaw } from 'vue-router'
import type { MenuNode } from '~/stores'
import { resolveView } from './views'

/**
 * 后端菜单树 → 路由记录（方案 6.5）：每个节点一条顶层路由（路径是绝对路径，子节点同样平铺），名字是页面码，
 * 组件按 `component`（`<包>/<路径>/index`）精确解析，`meta.permission` 由守卫校验。
 */
export function menuRoutes(menus: readonly MenuNode[]): RouteRecordRaw[] {
  const records: RouteRecordRaw[] = []
  const walk = (nodes: readonly MenuNode[]) => {
    for (const node of nodes) {
      records.push({
        path: node.route,
        name: node.code,
        component: resolveView(node.component),
        meta: { permission: node.permission ?? undefined, titleKey: node.titleKey },
      })
      walk(node.children)
    }
  }
  walk(menus)
  return records
}

/** 第一个可进入的页面路径（深度优先），没有菜单即 null。 */
export function firstRoute(menus: readonly MenuNode[]): string | null {
  for (const node of menus) {
    if (node.route)
      return node.route
    const child = firstRoute(node.children)
    if (child)
      return child
  }
  return null
}
