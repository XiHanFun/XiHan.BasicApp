<script setup lang="ts">
import { NMenu } from 'naive-ui'
import type { SidebarMenuPropsContract } from '../../contracts'

defineOptions({ name: 'SidebarMenu' })

const props = defineProps<SidebarMenuPropsContract>()

const emit = defineEmits<{ menuUpdate: [key: string] }>()

</script>

<template>
  <div class="app-sidebar-menu flex-1 min-h-0">
    <NMenu
      :value="props.activeKey"
      :collapsed="props.collapsed"
      :collapsed-width="props.collapsedWidth ?? 60"
      :indent="16"
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
:deep(.app-sidebar-menu .n-menu) {
  --n-item-height: 38px;
  --n-item-text-color: hsl(var(--foreground) / 80%);
  --n-item-text-color-hover: hsl(var(--foreground));
  --n-item-icon-color: hsl(var(--foreground) / 72%);
  --n-item-icon-color-hover: hsl(var(--foreground));
  --n-item-color-hover: hsl(var(--accent));
  --n-item-color-active: hsl(var(--primary) / 15%);
  --n-item-text-color-active: hsl(var(--primary));
  --n-item-icon-color-active: hsl(var(--primary));
  --n-arrow-color: hsl(var(--foreground) / 55%);
  --n-font-size: 14px;
  background: transparent;
}

:deep(.app-sidebar-menu .n-menu .n-menu-item-content),
:deep(.app-sidebar-menu .n-menu .n-submenu .n-menu-item-content) {
  margin: 2px 0;
  border-radius: 0;
}

:deep(.app-sidebar-menu .n-menu .n-menu-item-content .n-menu-item-content-header),
:deep(.app-sidebar-menu .n-menu .n-submenu .n-menu-item-content .n-menu-item-content-header) {
  font-size: 14px;
}

:deep(.app-sidebar-menu .n-menu .n-menu-item-content::before),
:deep(.app-sidebar-menu .n-menu .n-submenu .n-menu-item-content::before) {
  left: 0;
  right: 0;
  border-radius: inherit;
}

:deep(.app-sidebar-menu .n-menu .n-menu-item-content .n-menu-item-content__icon),
:deep(.app-sidebar-menu .n-menu .n-submenu .n-menu-item-content .n-menu-item-content__icon) {
  transition: transform 0.25s ease;
}

:deep(.app-sidebar-menu .n-menu .n-menu-item-content:hover .n-menu-item-content__icon),
:deep(.app-sidebar-menu .n-menu .n-submenu .n-menu-item-content:hover .n-menu-item-content__icon) {
  transform: scale(1.2);
}

:deep(.sidebar-menu-rounded.n-menu .n-menu-item-content),
:deep(.sidebar-menu-rounded.n-menu .n-submenu .n-menu-item-content) {
  margin-left: 8px;
  margin-right: 8px;
  border-radius: 8px !important;
}

:deep(.sidebar-menu-plain.n-menu .n-menu-item-content),
:deep(.sidebar-menu-plain.n-menu .n-submenu .n-menu-item-content) {
  margin-left: 0;
  margin-right: 0;
  border-radius: 0 !important;
}

:deep(.sidebar-menu-collapsed-icon-center.n-menu.n-menu--collapsed > .n-menu-item),
:deep(.sidebar-menu-collapsed-icon-center.n-menu.n-menu--collapsed > .n-submenu),
:deep(.sidebar-menu-collapsed-icon-center.n-menu.n-menu--collapsed > .n-submenu > .n-menu-item) {
  height: auto !important;
  margin: 4px 0 !important;
  overflow: visible !important;
}

:deep(.sidebar-menu-collapsed-icon-center.n-menu.n-menu--collapsed .n-menu-item-content) {
  display: flex !important;
  align-items: center;
  justify-content: center;
  height: auto !important;
  padding: 12px 0 !important;
  margin: 0 6px !important;
  overflow: visible !important;
}

:deep(.sidebar-menu-collapsed-icon-center.n-menu.n-menu--collapsed .n-menu-item-content::before) {
  left: 0;
  right: 0;
  border-radius: 6px;
}

:deep(.sidebar-menu-collapsed-icon-center.n-menu.n-menu--collapsed .n-menu-item-content .n-menu-item-content__icon) {
  margin-right: 0 !important;
}

:deep(.sidebar-menu-collapsed-icon-center.n-menu.n-menu--collapsed .n-menu-item-content .n-menu-item-content-header),
:deep(.sidebar-menu-collapsed-icon-center.n-menu.n-menu--collapsed .n-menu-item-content .n-menu-item-content__arrow) {
  display: none !important;
}

/*
 * Parent wrappers need height:auto to prevent NMenu's 38px constraint → overflow/overlap
 */
:deep(.sidebar-menu-collapsed-show-title.n-menu.n-menu--collapsed > .n-menu-item),
:deep(.sidebar-menu-collapsed-show-title.n-menu.n-menu--collapsed > .n-submenu),
:deep(.sidebar-menu-collapsed-show-title.n-menu.n-menu--collapsed > .n-submenu > .n-menu-item) {
  height: auto !important;
  margin: 4px 0 !important;
  overflow: visible !important;
}

:deep(.sidebar-menu-collapsed-show-title.n-menu.n-menu--collapsed .n-menu-item-content) {
  display: flex !important;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: auto !important;
  margin: 0 8px !important;
  padding: 9px 0 !important;
  overflow: visible !important;
  line-height: normal;
}

:deep(.sidebar-menu-collapsed-show-title.n-menu.n-menu--collapsed .n-menu-item-content::before) {
  left: 0;
  right: 0;
  border-radius: 6px;
}

:deep(.sidebar-menu-collapsed-show-title.n-menu.n-menu--collapsed .n-menu-item-content .n-menu-item-content__icon) {
  margin-right: 0 !important;
  font-size: 20px !important;
  width: 20px;
  height: 20px;
  max-height: 20px;
  transition: all 0.25s ease;
}

:deep(.sidebar-menu-collapsed-show-title.n-menu.n-menu--collapsed .n-menu-item-content .n-menu-item-content-header) {
  display: block !important;
  width: 100% !important;
  height: auto !important;
  margin-top: 8px;
  margin-bottom: 0;
  opacity: 1 !important;
  transform: none !important;
  overflow: hidden !important;
  text-overflow: ellipsis;
  white-space: nowrap;
  text-align: center;
  font-size: 12px;
  font-weight: 400;
  line-height: 1.2;
}

:deep(.sidebar-menu-collapsed-show-title.n-menu.n-menu--collapsed .n-menu-item-content .n-menu-item-content__arrow) {
  display: none !important;
}

:deep(.sidebar-menu-collapsed-show-title.n-menu.n-menu--collapsed .n-menu-item-content:hover .n-menu-item-content__icon) {
  transform: scale(1.2);
}

:deep(.sidebar-menu-collapsed-show-title.n-menu.n-menu--collapsed .n-menu-item.n-menu-item--selected .n-menu-item-content-header) {
  font-weight: 600;
}
</style>
