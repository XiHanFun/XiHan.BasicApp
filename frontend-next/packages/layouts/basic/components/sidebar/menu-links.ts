import type { RouteLocationRaw } from 'vue-router'
import type { MenuRoute } from '~/types'
import { computed } from 'vue'
import { useAccessStore } from '~/stores'
import { resolveRouteFullPath } from '~/utils'

/**
 * 菜单条目的去处。条目渲染成真链接：点击、Enter、中键与 Ctrl+点击都走浏览器的链接语义。
 * 站内页面给 to，交给 RouterLink；外链菜单（meta.link）不生成路由，给 href 走原生新标签。
 */
export type MenuLinkTarget = { to: RouteLocationRaw, href?: undefined } | { href: string, to?: undefined }

/** 菜单键 → 去处 */
export type MenuLinkResolver = (key: string) => MenuLinkTarget

/**
 * 菜单键 → 外链地址。与菜单域里点击开外链那张表同一口径：按全路径索引后端下发的菜单树。
 * 要的是不带副作用的查询（渲染 href 用），菜单域只给了查到即打开的那一个入口
 */
export function useMenuExternalLinks(): (key: string) => string | undefined {
  const accessStore = useAccessStore()
  const linkByKey = computed(() => {
    const map = new Map<string, string>()
    const walk = (nodes: MenuRoute[], parentPath = '') => {
      for (const node of nodes) {
        const fullPath = resolveRouteFullPath(node.path, parentPath)
        if (node.meta?.link && fullPath) {
          map.set(fullPath, node.meta.link)
        }
        if (node.children?.length) {
          walk(node.children, fullPath)
        }
      }
    }
    walk(accessStore.accessRoutes)
    return map
  })
  return key => linkByKey.value.get(key)
}
