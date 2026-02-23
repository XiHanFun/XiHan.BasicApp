<script lang="ts" setup>
import type { MenuOption } from 'naive-ui'
import type { RouteRecordRaw } from 'vue-router'
import type { MenuRoute } from '~/types'
import { Icon } from '@iconify/vue'
import { NIcon, NMenu } from 'naive-ui'
import { computed, h } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { HOME_PATH } from '~/constants'
import { useAccessStore, useAppStore } from '~/stores'
import SidebarActions from './sidebar/SidebarActions.vue'
import SidebarBrand from './sidebar/SidebarBrand.vue'
import SidebarMenu from './sidebar/SidebarMenu.vue'

interface AppSidebarProps {
  collapsed?: boolean
  floatingMode?: boolean
  floatingExpand?: boolean
  expandedWidth?: number
}

defineOptions({ name: 'AppSidebar' })
const props = withDefaults(defineProps<AppSidebarProps>(), {
  collapsed: undefined,
  floatingMode: false,
  floatingExpand: false,
  expandedWidth: 224,
})
const router = useRouter()
const route = useRoute()
const appStore = useAppStore()
const accessStore = useAccessStore()
const { t } = useI18n()
const appTitle = computed(
  () => appStore.brandTitle || import.meta.env.VITE_APP_TITLE || 'XiHan Admin',
)
const appLogo = computed(
  () => appStore.brandLogo || import.meta.env.VITE_APP_LOGO || '/favicon.png',
)

interface SidebarRouteMeta {
  hidden?: boolean
  title?: string
  icon?: string
}

type SidebarRouteRecord = RouteRecordRaw & {
  redirect?: RouteRecordRaw['redirect']
  children?: SidebarRouteRecord[]
}
type SidebarRouteRecordBase = Pick<RouteRecordRaw, 'path' | 'name' | 'meta'> & {
  redirect?: RouteRecordRaw['redirect']
  children?: SidebarRouteRecord[]
}

const collapsed = computed(() => props.collapsed ?? appStore.sidebarCollapsed)

const activeKey = computed(() => (route.name ? String(route.name) : ''))

function renderIcon(icon: string) {
  return () => h(NIcon, null, { default: () => h(Icon, { icon }) })
}

function toRouteNameKey(name: RouteRecordRaw['name']) {
  return typeof name === 'string' || typeof name === 'number' ? String(name) : undefined
}

function toSidebarMeta(record: RouteRecordRaw): SidebarRouteMeta {
  return (record.meta ?? {}) as SidebarRouteMeta
}

function normalizeMenuRoutes(menuRoutes: MenuRoute[]): SidebarRouteRecord[] {
  return menuRoutes.map((route) => {
    const normalized: SidebarRouteRecordBase = {
      path: route.path,
      name: route.name,
      meta: route.meta as unknown as RouteRecordRaw['meta'],
    }
    if (route.redirect) {
      normalized.redirect = route.redirect
    }
    if (route.children?.length) {
      normalized.children = normalizeMenuRoutes(route.children)
    }
    return normalized as SidebarRouteRecord
  })
}

function buildMenuOptions(routeList: SidebarRouteRecord[]): MenuOption[] {
  const result: MenuOption[] = []
  for (const r of routeList) {
    const meta = toSidebarMeta(r)
    if (meta.hidden) {
      continue
    }
    const firstVisibleChild = r.children?.find(child => !toSidebarMeta(child).hidden)
    const key = toRouteNameKey(r.name ?? firstVisibleChild?.name)
    if (!key) {
      continue
    }
    const fallbackName = toRouteNameKey(r.name) ?? key
    const label = meta.title ? t(meta.title, meta.title) : fallbackName
    const icon = meta.icon

    if (r.children?.some(c => !toSidebarMeta(c).hidden)) {
      result.push({
        key,
        label,
        icon: icon ? renderIcon(icon) : undefined,
        children: buildMenuOptions(r.children),
      })
    }
    else {
      result.push({
        key,
        label,
        icon: icon ? renderIcon(icon) : undefined,
      })
    }
  }
  return result
}

const appRoutes = (router.options.routes.find(r => r.path === '/')?.children ?? []) as SidebarRouteRecord[]
const isSideMixedLayout = computed(() => appStore.layoutMode === 'side-mixed')
const isHeaderMixLayout = computed(() => appStore.layoutMode === 'header-mix')
const isMixedNavLayout = computed(() => ['mix', 'header-sidebar'].includes(appStore.layoutMode))
const isSplitMenuLayout = computed(
  () => appStore.navigationSplit && ['mix', 'header-sidebar'].includes(appStore.layoutMode),
)
const baseMenuSource = computed<SidebarRouteRecord[]>(() => {
  return accessStore.accessRoutes.length
    ? normalizeMenuRoutes(accessStore.accessRoutes)
    : appRoutes
})
const visibleRootRoutes = computed(() => baseMenuSource.value.filter(item => !toSidebarMeta(item).hidden))

function findMatchedRouteNameKey(candidates: SidebarRouteRecord[]) {
  for (const record of route.matched) {
    const matchedName = toRouteNameKey(record.name)
    if (matchedName && candidates.some(item => toRouteNameKey(item.name) === matchedName)) {
      return matchedName
    }
  }
  return undefined
}

function resolveFullPath(path: string, parentPath = '') {
  if (!path) {
    return parentPath || '/'
  }
  if (path.startsWith('/')) {
    return path
  }
  return `${parentPath.replace(/\/$/, '')}/${path}`.replace(/\/{2,}/g, '/')
}

function routeTreeContainsMatched(
  node: SidebarRouteRecord,
  matchedNames: Set<string>,
  parentPath = '',
): boolean {
  const selfName = toRouteNameKey(node.name)
  if (selfName && matchedNames.has(selfName)) {
    return true
  }

  const fullPath = resolveFullPath(node.path, parentPath)
  if (fullPath && (route.path === fullPath || route.path.startsWith(`${fullPath}/`))) {
    return true
  }

  const children = node.children ?? []
  return children.some(child => routeTreeContainsMatched(child, matchedNames, fullPath))
}

const activeRootKey = computed<string>(() => {
  const matchedNames = new Set(
    route.matched
      .map(item => toRouteNameKey(item.name))
      .filter((item): item is string => Boolean(item)),
  )
  const nestedMatchedRoot = visibleRootRoutes.value.find(item =>
    routeTreeContainsMatched(item, matchedNames),
  )
  return findMatchedRouteNameKey(visibleRootRoutes.value)
    ?? toRouteNameKey(nestedMatchedRoot?.name)
    ?? toRouteNameKey(visibleRootRoutes.value[0]?.name)
    ?? ''
})
const activeRootRoute = computed(() => {
  return visibleRootRoutes.value.find(item => toRouteNameKey(item.name) === activeRootKey.value)
})

const menuSource = computed<SidebarRouteRecord[]>(() => {
  if (isSideMixedLayout.value || isHeaderMixLayout.value) {
    return []
  }
  if (!isSplitMenuLayout.value) {
    return baseMenuSource.value
  }
  const visibleChildren = activeRootRoute.value?.children?.filter(child => !toSidebarMeta(child).hidden) ?? []
  return visibleChildren.length ? visibleChildren : baseMenuSource.value
})
const menuOptions = computed(() => buildMenuOptions(menuSource.value))
const sideMixedPrimaryRoutes = computed(() => {
  if (!isSideMixedLayout.value) {
    return []
  }
  return visibleRootRoutes.value
})
const sideMixedPrimaryOptions = computed<MenuOption[]>(() => {
  return sideMixedPrimaryRoutes.value
    .map((item) => {
      const key = toRouteNameKey(item.name)
      if (!key) {
        return undefined
      }
      const meta = toSidebarMeta(item)
      const label = meta.title ? t(meta.title, meta.title) : key
      return {
        key,
        label,
        icon: meta.icon ? renderIcon(meta.icon) : undefined,
      } as MenuOption
    })
    .filter(Boolean) as MenuOption[]
})
const sideMixedActiveTopKey = computed(() => {
  if (!isSideMixedLayout.value) {
    return ''
  }
  return findMatchedRouteNameKey(sideMixedPrimaryRoutes.value)
    ?? toRouteNameKey(sideMixedPrimaryRoutes.value[0]?.name)
    ?? ''
})
const sideMixedSecondarySource = computed<SidebarRouteRecord[]>(() => {
  if (!isSideMixedLayout.value) {
    return []
  }
  const activeTopRoute = sideMixedPrimaryRoutes.value.find(
    item => toRouteNameKey(item.name) === sideMixedActiveTopKey.value,
  )
  const children = activeTopRoute?.children?.filter(child => !toSidebarMeta(child).hidden) ?? []
  if (children.length) {
    return children
  }
  return activeTopRoute ? [activeTopRoute] : []
})
const sideMixedSecondaryOptions = computed(() => buildMenuOptions(sideMixedSecondarySource.value))
const headerMixPrimaryRoutes = computed(() => {
  if (!isHeaderMixLayout.value) {
    return []
  }
  return activeRootRoute.value?.children?.filter(child => !toSidebarMeta(child).hidden) ?? []
})
const headerMixPrimaryOptions = computed<MenuOption[]>(() => {
  return headerMixPrimaryRoutes.value
    .map((item) => {
      const key = toRouteNameKey(item.name)
      if (!key) {
        return undefined
      }
      const meta = toSidebarMeta(item)
      const label = meta.title ? t(meta.title, meta.title) : key
      return {
        key,
        label,
        icon: meta.icon ? renderIcon(meta.icon) : undefined,
      } as MenuOption
    })
    .filter(Boolean) as MenuOption[]
})
const headerMixActivePrimaryKey = computed(() => {
  if (!isHeaderMixLayout.value) {
    return ''
  }
  return findMatchedRouteNameKey(headerMixPrimaryRoutes.value)
    ?? toRouteNameKey(headerMixPrimaryRoutes.value[0]?.name)
    ?? ''
})
const headerMixSecondarySource = computed(() => {
  if (!isHeaderMixLayout.value) {
    return []
  }
  const activePrimary = headerMixPrimaryRoutes.value.find(
    item => toRouteNameKey(item.name) === headerMixActivePrimaryKey.value,
  )
  const grandchildren = activePrimary?.children?.filter(child => !toSidebarMeta(child).hidden) ?? []
  if (grandchildren.length > 0) {
    return grandchildren
  }
  return headerMixPrimaryRoutes.value
})
const headerMixSecondaryOptions = computed(() => buildMenuOptions(headerMixSecondarySource.value))
const sidebarPinned = computed(() => !appStore.sidebarExpandOnHover)
const sidebarCurrentWidth = computed(() => (collapsed.value ? 64 : appStore.sidebarWidth))
const floatingSidebarStyle = computed(() => {
  if (!props.floatingMode) {
    return undefined
  }
  return {
    width: `${props.floatingExpand ? props.expandedWidth : 64}px`,
  }
})

function handleMenuUpdate(key: string) {
  router.push({ name: key })
}

function handleSideMixedPrimaryUpdate(key: string) {
  const target = sideMixedPrimaryRoutes.value.find(item => toRouteNameKey(item.name) === key)
  if (!target) {
    return
  }
  const firstVisibleChild = target.children?.find(child => !toSidebarMeta(child).hidden)
  const targetName = toRouteNameKey(firstVisibleChild?.name ?? target.name)
  if (targetName) {
    router.push({ name: targetName })
  }
}

function handleHeaderMixPrimaryUpdate(key: string) {
  const target = headerMixPrimaryRoutes.value.find(item => toRouteNameKey(item.name) === key)
  if (!target) {
    return
  }
  const firstVisibleChild = target.children?.find(child => !toSidebarMeta(child).hidden)
  const targetName = toRouteNameKey(firstVisibleChild?.name ?? target.name)
  if (targetName) {
    router.push({ name: targetName })
  }
}

function handleBrandClick() {
  if (route.path !== HOME_PATH) {
    router.push(HOME_PATH)
  }
}

function handleToggleCollapse() {
  if (typeof window !== 'undefined' && window.innerWidth < 960) {
    window.dispatchEvent(new CustomEvent('xihan-toggle-sidebar-request'))
    return
  }
  appStore.toggleSidebar()
}

function handleTogglePin() {
  appStore.setSidebarExpandOnHover(!appStore.sidebarExpandOnHover)
}
</script>

<template>
  <div
    class="app-sidebar-root relative flex h-full min-h-0 flex-col transition-[transform,width] duration-300"
    :class="props.floatingMode ? 'absolute left-0 top-0 z-40 bg-[var(--sidebar-bg)]' : ''"
    :style="floatingSidebarStyle"
  >
    <template v-if="isSideMixedLayout">
      <div class="flex min-h-0 flex-1">
        <div class="side-mixed-primary flex w-16 shrink-0 flex-col border-r border-[var(--border-color)]">
          <SidebarBrand
            :collapsed="true"
            :app-title="appTitle"
            :app-logo="appLogo"
            :sidebar-collapsed-show-title="false"
            @click="handleBrandClick"
          />
          <SidebarMenu
            :active-key="sideMixedActiveTopKey"
            :collapsed="true"
            :sidebar-collapsed-show-title="false"
            :menu-options="sideMixedPrimaryOptions"
            :navigation-style="appStore.navigationStyle"
            :accordion="true"
            :no-top-padding="true"
            @menu-update="handleSideMixedPrimaryUpdate"
          />
        </div>
        <div class="side-mixed-secondary min-w-0 flex-1">
          <SidebarMenu
            :active-key="activeKey"
            :collapsed="false"
            :menu-options="sideMixedSecondaryOptions"
            :navigation-style="appStore.navigationStyle"
            :accordion="appStore.navigationAccordion"
            :no-top-padding="true"
            @menu-update="handleMenuUpdate"
          />
        </div>
      </div>
    </template>
    <template v-else-if="isHeaderMixLayout">
      <div class="flex min-h-0 flex-1">
        <div class="header-mix-primary flex w-16 shrink-0 flex-col border-r border-[var(--border-color)]">
          <SidebarBrand
            :collapsed="true"
            :app-title="appTitle"
            :app-logo="appLogo"
            :sidebar-collapsed-show-title="false"
            @click="handleBrandClick"
          />
          <SidebarMenu
            :active-key="headerMixActivePrimaryKey"
            :collapsed="true"
            :sidebar-collapsed-show-title="false"
            :menu-options="headerMixPrimaryOptions"
            :navigation-style="appStore.navigationStyle"
            :accordion="true"
            :no-top-padding="true"
            @menu-update="handleHeaderMixPrimaryUpdate"
          />
        </div>
        <div class="header-mix-secondary min-w-0 flex-1">
          <SidebarMenu
            :active-key="activeKey"
            :collapsed="false"
            :menu-options="headerMixSecondaryOptions"
            :navigation-style="appStore.navigationStyle"
            :accordion="appStore.navigationAccordion"
            :no-top-padding="true"
            @menu-update="handleMenuUpdate"
          />
        </div>
      </div>
    </template>
    <template v-else>
      <SidebarBrand
        v-if="!isMixedNavLayout"
        :collapsed="collapsed"
        :app-title="appTitle"
        :app-logo="appLogo"
        :sidebar-collapsed-show-title="appStore.sidebarCollapsedShowTitle"
        @click="handleBrandClick"
      />

      <SidebarMenu
        :active-key="activeKey"
        :collapsed="collapsed"
        :sidebar-collapsed-show-title="appStore.sidebarCollapsedShowTitle"
        :no-top-padding="isMixedNavLayout"
        :menu-options="menuOptions"
        :navigation-style="appStore.navigationStyle"
        :accordion="appStore.navigationAccordion"
        @menu-update="handleMenuUpdate"
      />

      <SidebarActions
        :collapsed="collapsed"
        :sidebar-collapse-button="appStore.sidebarCollapseButton"
        :sidebar-fixed-button="appStore.sidebarFixedButton"
        :floating-mode="props.floatingMode"
        :floating-expand="props.floatingExpand"
        :sidebar-pinned="sidebarPinned"
        :sidebar-current-width="sidebarCurrentWidth"
        @toggle-collapse="handleToggleCollapse"
        @toggle-pin="handleTogglePin"
      />
    </template>
  </div>
</template>

<style scoped>
:deep(.sidebar-menu-rounded .n-menu-item-content) {
  border-radius: 8px;
  margin: 2px 8px;
}

:deep(.sidebar-menu-plain .n-menu-item-content) {
  border-radius: 0;
  margin: 0;
}
</style>
