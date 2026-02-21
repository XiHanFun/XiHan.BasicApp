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

defineOptions({ name: 'AppSidebar' })
const props = withDefaults(defineProps<{ collapsed?: boolean }>(), {
  collapsed: undefined,
})

const router = useRouter()
const route = useRoute()
const appStore = useAppStore()
const accessStore = useAccessStore()
const { t } = useI18n()
const appTitle = import.meta.env.VITE_APP_TITLE || 'XiHan Admin'
const appLogo = import.meta.env.VITE_APP_LOGO || '/favicon.png'

const collapsed = computed(() => props.collapsed ?? appStore.sidebarCollapsed)

const activeKey = computed(() => route.name as string)

function renderIcon(icon: string) {
  return () => h(NIcon, null, { default: () => h(Icon, { icon }) })
}

function buildMenuOptions(routeList: any[]): MenuOption[] {
  const result: MenuOption[] = []
  for (const r of routeList) {
    if (r.meta?.hidden) {
      continue
    }
    const key = r.name as string
    const label = r.meta?.title ? t(r.meta.title, r.meta.title) : r.name
    const icon = r.meta?.icon

    if (r.children?.filter((c: any) => !c.meta?.hidden).length) {
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

const appRoutes = routes.find(r => r.path === '/')?.children ?? []
const menuSource = computed(() => {
  return accessStore.accessRoutes.length ? accessStore.accessRoutes : (appRoutes as any[])
})
const menuOptions = computed(() => buildMenuOptions(menuSource.value))
const sidebarPinned = computed(() => !appStore.sidebarExpandOnHover)
const sidebarCurrentWidth = computed(() => (collapsed.value ? 64 : appStore.sidebarWidth))

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
  <div class="relative flex h-full min-h-0 flex-col">
    <!-- Logo 区域 -->
    <div
      class="flex h-16 shrink-0 cursor-pointer items-center overflow-hidden border-b border-gray-100 px-4 transition-colors hover:bg-gray-50 dark:border-gray-800 dark:hover:bg-gray-800/60"
      :class="collapsed ? 'justify-center' : 'gap-2'"
      @click="handleBrandClick"
    >
      <div
        class="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-white/90 p-1.5 shadow-sm"
      >
        <img :src="appLogo" :alt="appTitle" class="h-8 w-8 object-contain">
      </div>
      <Transition name="fade">
        <span v-if="!collapsed" class="text-xl font-semibold text-gray-800 dark:text-gray-100">
          {{ appTitle }}
        </span>
      </Transition>
    </div>

    <!-- 菜单 -->
    <div class="flex-1 min-h-0 overflow-y-auto overflow-x-hidden py-2 pb-12">
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
      class="fixed bottom-2 left-3 z-40 flex h-7 w-7 items-center justify-center rounded-sm border-0 bg-gray-100 p-1 text-gray-500 outline-none transition-all duration-300 hover:bg-gray-200 hover:text-gray-700 focus:outline-none dark:bg-gray-800 dark:text-gray-400 dark:hover:bg-gray-700 dark:hover:text-gray-200"
      :title="collapsed ? '展开侧边栏' : '收起侧边栏'"
      @click.stop="handleToggleCollapse"
    >
      <NIcon size="15">
        <Icon :icon="collapsed ? 'lucide:chevrons-right' : 'lucide:chevrons-left'" />
      </NIcon>
    </button>

    <button
      v-if="appStore.sidebarFixedButton && !collapsed"
      type="button"
      :style="{ left: `${Math.max(12, sidebarCurrentWidth - 40)}px` }"
      class="fixed bottom-2 z-40 flex h-7 w-7 items-center justify-center rounded-sm border-0 bg-gray-100 p-[5px] text-gray-500 outline-none transition-all duration-300 hover:bg-gray-200 hover:text-gray-700 focus:outline-none dark:bg-gray-800 dark:text-gray-400 dark:hover:bg-gray-700 dark:hover:text-gray-200"
      :title="sidebarPinned ? '取消固定侧边栏' : '固定侧边栏'"
      @click.stop="handleTogglePin"
    >
      <NIcon size="14">
        <Icon :icon="sidebarPinned ? 'lucide:pin-off' : 'lucide:pin'" />
      </NIcon>
    </button>
  </div>
</template>
