<script setup lang="ts">
import type { MenuOption } from 'naive-ui'
import { NMenu } from 'naive-ui'

defineOptions({ name: 'SidebarMenu' })

const props = defineProps<SidebarMenuProps>()

const emit = defineEmits<{ menuUpdate: [key: string] }>()

interface SidebarMenuProps {
  activeKey: string
  collapsed: boolean
  menuOptions: MenuOption[]
  navigationStyle: 'rounded' | 'plain'
  accordion?: boolean
}
</script>

<template>
  <div class="app-sidebar-menu flex-1 min-h-0 overflow-y-auto overflow-x-hidden py-2 pb-12">
    <NMenu
      :value="props.activeKey"
      :collapsed="props.collapsed"
      :collapsed-width="64"
      :indent="18"
      :options="props.menuOptions"
      :class="props.navigationStyle === 'rounded' ? 'sidebar-menu-rounded' : 'sidebar-menu-plain'"
      :accordion="props.accordion ?? true"
      @update:value="key => emit('menuUpdate', String(key))"
    />
  </div>
</template>
