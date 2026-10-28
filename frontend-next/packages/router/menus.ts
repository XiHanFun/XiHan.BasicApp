import type { MenuNode } from '~/stores'

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
