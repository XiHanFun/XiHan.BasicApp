<script setup lang="ts">
import type { MenuOption } from 'naive-ui'
import { NMenu } from 'naive-ui'

defineOptions({ name: 'SidebarMenu' })

const props = defineProps<SidebarMenuProps>()

const emit = defineEmits<{ menuUpdate: [key: string] }>()

interface SidebarMenuProps {
  activeKey: string
  collapsed: boolean
  sidebarCollapsedShowTitle?: boolean
  noTopPadding?: boolean
  menuOptions: MenuOption[]
  navigationStyle: 'rounded' | 'plain'
  accordion?: boolean
}
</script>

<template>
  <div
    class="app-sidebar-menu flex-1 min-h-0 overflow-y-auto overflow-x-hidden pb-12"
    :class="props.noTopPadding ? 'py-0' : 'py-2'"
  >
    <NMenu
      :value="props.activeKey"
      :collapsed="props.collapsed"
      :collapsed-width="64"
      :indent="18"
      :options="props.menuOptions"
      :class="[
        props.navigationStyle === 'rounded' ? 'sidebar-menu-rounded' : 'sidebar-menu-plain',
        props.collapsed && props.sidebarCollapsedShowTitle ? 'sidebar-menu-collapsed-show-title' : '',
        props.collapsed && !props.sidebarCollapsedShowTitle ? 'sidebar-menu-collapsed-icon-center' : '',
      ]"
      :accordion="props.accordion ?? true"
      @update:value="key => emit('menuUpdate', String(key))"
    />
  </div>
</template>

<style scoped>
:deep(.sidebar-menu-collapsed-show-title.n-menu.n-menu--collapsed .n-menu-item-content) {
  display: flex !important;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  height: 56px;
  line-height: 1.1;
  padding: 6px 0 !important;
}

:deep(.sidebar-menu-collapsed-show-title.n-menu.n-menu--collapsed .n-menu-item-content .n-menu-item-content__icon) {
  margin-right: 0 !important;
}

:deep(.sidebar-menu-collapsed-show-title.n-menu.n-menu--collapsed .n-menu-item-content .n-menu-item-content-header) {
  display: block !important;
  margin-top: 4px;
  width: auto !important;
  max-width: 56px;
  opacity: 1 !important;
  transform: none !important;
  white-space: normal;
  text-align: center;
  font-size: 12px;
  line-height: 1.1;
}

:deep(.sidebar-menu-collapsed-show-title.n-menu.n-menu--collapsed .n-menu-item-content .n-menu-item-content__arrow) {
  display: none !important;
}

:deep(.sidebar-menu-collapsed-icon-center.n-menu.n-menu--collapsed .n-menu-item-content) {
  display: flex !important;
  justify-content: center;
  align-items: center;
  padding: 0 !important;
}

:deep(.sidebar-menu-collapsed-icon-center.n-menu.n-menu--collapsed .n-menu-item-content .n-menu-item-content__icon) {
  margin-right: 0 !important;
}

:deep(.sidebar-menu-collapsed-icon-center.n-menu.n-menu--collapsed .n-menu-item-content .n-menu-item-content-header),
:deep(.sidebar-menu-collapsed-icon-center.n-menu.n-menu--collapsed .n-menu-item-content .n-menu-item-content__arrow) {
  display: none !important;
}
</style>
