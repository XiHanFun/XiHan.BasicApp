<script lang="ts" setup>
import { computed, h } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { NMenu, NIcon, NText } from 'naive-ui'
import type { MenuOption } from 'naive-ui'
import { Icon } from '@iconify/vue'
import { useI18n } from 'vue-i18n'
import { useAccessStore, useAppStore } from '~/stores'
import { routes } from '@/router/routes'

defineOptions({ name: 'AppSidebar' })

const router = useRouter()
const route = useRoute()
const appStore = useAppStore()
const accessStore = useAccessStore()
const { t } = useI18n()

const collapsed = computed(() => appStore.sidebarCollapsed)

const activeKey = computed(() => route.name as string)

function renderIcon(icon: string) {
  return () => h(NIcon, null, { default: () => h(Icon, { icon }) })
}

function buildMenuOptions(routeList: any[], parent = ''): MenuOption[] {
  const result: MenuOption[] = []
  for (const r of routeList) {
    if (r.meta?.hidden) continue
    const key = r.name as string
    const label = r.meta?.title ? t(r.meta.title, r.meta.title) : r.name
    const icon = r.meta?.icon

    if (r.children?.filter((c: any) => !c.meta?.hidden).length) {
      result.push({
        key,
        label,
        icon: icon ? renderIcon(icon) : undefined,
        children: buildMenuOptions(r.children, key),
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
  return accessStore.accessRoutes.length ? accessStore.accessRoutes : (appRoutes as any[])
})
const menuOptions = computed(() => buildMenuOptions(menuSource.value))

function handleMenuUpdate(key: string) {
  router.push({ name: key })
}
</script>

<template>
  <div class="flex h-full flex-col">
    <!-- Logo 区域 -->
    <div
      class="flex h-14 shrink-0 items-center overflow-hidden border-b border-gray-100 px-4 dark:border-gray-800"
      :class="collapsed ? 'justify-center' : 'gap-2'"
    >
      <div class="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-primary-600">
        <Icon icon="lucide:zap" class="text-white" width="18" />
      </div>
      <Transition name="fade">
        <span v-if="!collapsed" class="text-base font-semibold text-gray-800 dark:text-gray-100">
          XiHan Admin
        </span>
      </Transition>
    </div>

    <!-- 菜单 -->
    <div class="flex-1 overflow-y-auto overflow-x-hidden py-2">
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
  </div>
</template>
