import type { MenuNode } from './auth'
import type { MenuRoute } from '~/types'
import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { HOME_PATH } from '~/constants'
import { firstRoute } from '~/router/menus'
import { useAuthStore } from './auth'
import { SetupStoreId } from './store-ids'

/**
 * 后端菜单树（auth.permissions 的 MenuNode）→ 壳层的菜单路由模型（MenuRoute）。
 * 标题：运营改过或运营建的节点给原文，其余给文案键，壳层按 te / t 翻译（各能力包的语言文件登记）。
 * 目录与外链没有路由，给它一条以节点码命名的占位路径（侧栏按它当展开键 / 外链键，不生成路由记录）；外链走 meta.link 新标签打开。
 */
export function toMenuRoutes(menus: readonly MenuNode[]): MenuRoute[] {
  return menus.map(node => ({
    path: node.route || `/${node.code}`,
    name: node.code,
    component: node.kind === 'Page' ? node.component || undefined : undefined,
    meta: {
      title: node.title || node.titleKey,
      icon: node.icon ?? undefined,
      order: node.order,
      hidden: node.hidden || undefined,
      keepAlive: node.keepAlive || undefined,
      affixTab: node.affix || undefined,
      link: node.kind === 'Link' ? node.externalUrl ?? undefined : undefined,
      permissions: node.permission ? [node.permission] : undefined,
    },
    children: node.children.length > 0 ? toMenuRoutes(node.children) : undefined,
  }))
}

/**
 * 访问态（壳层视角）：令牌、菜单路由、权限码都是 auth store 的投影，这里不另存一份。
 * 4.x 由登录流程写入这些字段；6.0 的真源是 auth.login / auth.permissions（见 auth.ts），壳层只读。
 */
export const useAccessStore = defineStore(SetupStoreId.Access, () => {
  const auth = useAuthStore()
  const accessToken = computed(() => auth.accessToken)
  const accessRoutes = computed<MenuRoute[]>(() => toMenuRoutes(auth.menus))
  const accessCodes = computed<string[]>(() => auth.permissions?.permissions ?? [])
  /** 6.0 没有单独的按钮码：按钮门控直接用操作的权限码 */
  const accessButtons = computed<string[]>(() => [])
  const isRoutesLoaded = computed(() => auth.permissions !== null)
  const loginExpired = ref(false)
  const homePath = computed(() => firstRoute(auth.menus) ?? HOME_PATH)

  function setLoginExpired(expired: boolean) {
    loginExpired.value = expired
  }

  /** 权限码精确匹配，`*` 不当通配（后端已展开） */
  function hasCode(code: string): boolean {
    return auth.can(code)
  }

  function $reset() {
    loginExpired.value = false
    auth.clear()
  }

  return { accessToken, accessRoutes, accessCodes, accessButtons, isRoutesLoaded, loginExpired, homePath, setLoginExpired, hasCode, $reset }
})
