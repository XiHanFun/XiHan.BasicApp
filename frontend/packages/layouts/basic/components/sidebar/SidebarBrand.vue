<script setup lang="ts">
import { computed } from 'vue'
import AppBrand from '../AppBrand.vue'

defineOptions({ name: 'SidebarBrand' })

const props = defineProps<SidebarBrandProps>()
const emit = defineEmits<{ click: [] }>()

interface SidebarBrandProps {
  collapsed: boolean
  appTitle: string
  appLogo: string
  sidebarCollapsedShowTitle: boolean
}

const showCollapsed = computed(() => props.collapsed && !props.sidebarCollapsedShowTitle)
</script>

<template>
  <div class="flex h-14 shrink-0">
    <AppBrand
      :app-title="props.appTitle"
      :app-logo="props.appLogo"
      :collapsed="showCollapsed"
      @click="emit('click')"
    />
  </div>
</template>
