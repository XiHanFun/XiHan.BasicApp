<script lang="ts" setup>
import type { MenuOption } from 'naive-ui'
import { Icon } from '@iconify/vue'
import { NIcon, NMenu } from 'naive-ui'
import { computed, h } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { routes } from '@/router/routes'
import { HOME_PATH } from '~/constants'
import { useAccessStore, useAppStore } from '~/stores'

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

interface SidebarRouteItem {
  name?: string
  meta?: {
    hidden?: boolean
    title?: string
    icon?: string
  }
  children?: SidebarRouteItem[]
}

const collapsed = computed(() => props.collapsed ?? appStore.sidebarCollapsed)

const activeKey = computed(() => route.name as string)

function renderIcon(icon: string) {
  return () => h(NIcon, null, { default: () => h(Icon, { icon }) })
}

function buildMenuOptions(routeList: SidebarRouteItem[]): MenuOption[] {
  const result: MenuOption[] = []
  for (const r of routeList) {
    if (r.meta?.hidden) {
      continue
    }
    const firstVisibleChild = r.children?.find((child) => !child.meta?.hidden)
    const key = (props.compactMenu ? firstVisibleChild?.name : r.name) as string
    if (!key) {
      continue
    }
    const label = r.meta?.title ? t(r.meta.title, r.meta.title) : r.name
    const icon = r.meta?.icon

    if (!props.compactMenu && r.children?.some((c) => !c.meta?.hidden)) {
      result.push({
        key,
        label,
        icon: icon ? renderIcon(icon) : undefined,
        children: buildMenuOptions(r.children),
      })
    } else {
      result.push({
        key,
        label,
        icon: icon ? renderIcon(icon) : undefined,
      })
    }
  }
  return result
}

const appRoutes = routes.find((r) => r.path === '/')?.children ?? []
const menuSource = computed(() => {
  return (
    accessStore.accessRoutes.length ? accessStore.accessRoutes : appRoutes
  ) as SidebarRouteItem[]
})
const menuOptions = computed(() => buildMenuOptions(menuSource.value))
const sidebarPinned = computed(() => !appStore.sidebarExpandOnHover)
const sidebarCurrentWidth = computed(() => (collapsed.value ? 64 : appStore.sidebarWidth))
const floatingSidebarStyle = computed(() => {
  if (!props.floatingMode) return undefined
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
    <!-- Logo 区域 -->
    <div
      class="app-sidebar-brand flex h-16 shrink-0 cursor-pointer items-center overflow-hidden px-3 transition-colors hover:bg-[hsl(var(--accent))]"
      @click="handleBrandClick"
    >
      <div class="relative h-12 w-full overflow-hidden">
        <div
          class="absolute left-0 top-1/2 flex h-12 w-12 -translate-y-1/2 items-center justify-center rounded-xl bg-[hsl(var(--card)/0.92)] p-1.5 shadow-sm transition-transform duration-300"
          :class="collapsed ? 'scale-100' : 'scale-90'"
        >
          <img :src="appLogo" :alt="appTitle" class="h-8 w-8 object-contain" />
        </div>
        <span
          class="absolute left-[52px] top-1/2 block -translate-y-1/2 overflow-hidden text-ellipsis whitespace-nowrap text-xl font-semibold leading-none text-[hsl(var(--foreground))] transition-all duration-300"
          :class="collapsed ? 'max-w-0 opacity-0 delay-0' : 'max-w-[220px] opacity-100 delay-100'"
        >
          {{ appTitle }}
        </span>
      </div>
    </div>

    <!-- 菜单 -->
    <div class="app-sidebar-menu flex-1 min-h-0 overflow-y-auto overflow-x-hidden py-2 pb-12">
      <NMenu
        :value="activeKey"
        :collapsed="collapsed"
        :collapsed-width="64"
        :indent="18"
        :options="menuOptions"
        accordion
        @update:value="handleMenuUpdate"
      />
    </div>

    <button
      v-if="appStore.sidebarCollapseButton"
      type="button"
      class="fixed bottom-2 left-3 z-40 flex h-7 w-7 items-center justify-center rounded-sm border-0 bg-[hsl(var(--muted))] p-1 text-[hsl(var(--muted-foreground))] outline-none transition-all duration-300 hover:bg-[hsl(var(--accent))] hover:text-[hsl(var(--accent-foreground))] focus:outline-none"
      :title="collapsed ? '展开侧边栏' : '收起侧边栏'"
      @click.stop="handleToggleCollapse"
    >
      <NIcon size="15">
        <Icon :icon="collapsed ? 'lucide:chevrons-right' : 'lucide:chevrons-left'" />
      </NIcon>
    </button>

    <Transition
      enter-active-class="transition-all duration-300 delay-100"
      enter-from-class="-translate-x-2 opacity-0"
      enter-to-class="translate-x-0 opacity-100"
      leave-active-class="transition-all duration-200"
      leave-from-class="translate-x-0 opacity-100"
      leave-to-class="-translate-x-1 opacity-0"
    >
      <button
        v-if="
          appStore.sidebarFixedButton && !collapsed && (!props.floatingMode || props.floatingExpand)
        "
        type="button"
        :style="{ left: `${Math.max(12, sidebarCurrentWidth - 40)}px` }"
        class="fixed bottom-2 z-40 flex h-7 w-7 items-center justify-center rounded-sm border-0 bg-[hsl(var(--muted))] p-[5px] text-[hsl(var(--muted-foreground))] outline-none transition-all duration-300 hover:bg-[hsl(var(--accent))] hover:text-[hsl(var(--accent-foreground))] focus:outline-none"
        :title="sidebarPinned ? '主体不跟随侧边栏' : '主体跟随侧边栏'"
        @click.stop="handleTogglePin"
      >
        <NIcon size="14">
          <Icon :icon="sidebarPinned ? 'lucide:pin-off' : 'lucide:pin'" />
        </NIcon>
      </button>
    </Transition>
  </div>
</template>

<style scoped>
.app-sidebar-brand {
  border-bottom: 1px solid var(--border-color);
}
</style>
