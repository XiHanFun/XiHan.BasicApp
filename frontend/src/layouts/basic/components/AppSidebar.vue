<script lang="ts" setup>
import type { MenuOption } from 'naive-ui'
import type { RouteRecordRaw } from 'vue-router'
import type { MenuRoute } from '~/types'
import { Icon } from '@iconify/vue'
import { NIcon } from 'naive-ui'
import { computed, h } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { routes } from '@/router/routes'
import { HOME_PATH } from '~/constants'
import { useAccessStore, useAppStore } from '~/stores'
import SidebarActions from './sidebar/SidebarActions.vue'
import SidebarBrand from './sidebar/SidebarBrand.vue'
import SidebarMenu from './sidebar/SidebarMenu.vue'

interface AppSidebarProps {
  collapsed?: boolean
  floatingMode?: boolean
  floatingExpand?: boolean
  compactMenu?: boolean
  expandedWidth?: number
}

defineOptions({ name: 'AppSidebar' })
const props = withDefaults(defineProps<AppSidebarProps>(), {
  collapsed: undefined,
  floatingMode: false,
  floatingExpand: false,
  compactMenu: false,
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
    const keySource = props.compactMenu || appStore.sidebarAutoActivateChild
      ? firstVisibleChild?.name
      : r.name
    const key = toRouteNameKey(keySource)
    if (!key) {
      continue
    }
    const fallbackName = toRouteNameKey(r.name) ?? key
    const label = meta.title ? t(meta.title, meta.title) : fallbackName
    const icon = meta.icon

    if (!props.compactMenu && r.children?.some(c => !toSidebarMeta(c).hidden)) {
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

const appRoutes = (routes.find(r => r.path === '/')?.children ?? []) as SidebarRouteRecord[]
const menuSource = computed<SidebarRouteRecord[]>(() => {
  if (accessStore.accessRoutes.length) {
    return normalizeMenuRoutes(accessStore.accessRoutes)
  }
  return appRoutes
})
const menuOptions = computed(() => buildMenuOptions(menuSource.value))
const sidebarPinned = computed(() => !appStore.sidebarExpandOnHover)
const sidebarCurrentWidth = computed(() => (collapsed.value ? 64 : appStore.sidebarWidth))
const floatingSidebarStyle = computed(() => {
  if (!props.floatingMode)
    return undefined
  return {
    width: `${props.floatingExpand ? props.expandedWidth : 64}px`,
  }
})

function handleMenuUpdate(key: string) {
  router.push({ name: key })
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
    class="app-sidebar-root relative flex h-full min-h-0 flex-col bg-[var(--sidebar-bg)] transition-[transform,width] duration-300"
    :class="props.floatingMode ? 'absolute left-0 top-0 z-40' : ''"
    :style="floatingSidebarStyle"
  >
    <SidebarBrand
      :collapsed="collapsed"
      :app-title="appTitle"
      :app-logo="appLogo"
      :sidebar-collapsed-show-title="appStore.sidebarCollapsedShowTitle"
      @click="handleBrandClick"
    />

    <SidebarMenu
      :active-key="activeKey"
      :collapsed="collapsed"
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
  </div>
</template>

<style scoped>
.app-sidebar-brand {
  border-bottom: 1px solid var(--border-color);
}

:deep(.sidebar-menu-rounded .n-menu-item-content) {
  border-radius: 8px;
  margin: 2px 8px;
}

:deep(.sidebar-menu-plain .n-menu-item-content) {
  border-radius: 0;
  margin: 0;
}
</style>
