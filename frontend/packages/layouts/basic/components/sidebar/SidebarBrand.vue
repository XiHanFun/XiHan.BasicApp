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

// Keep logo behavior consistent with vben:
// collapsed menu still uses collapsed logo text state.
const showCollapsed = computed(() => props.collapsed)
</script>

<template>
  <div class="flex h-[50px] w-full min-w-0 shrink-0">
    <AppBrand
      :app-title="props.appTitle"
      :app-logo="props.appLogo"
      :collapsed="showCollapsed"
      @click="emit('click')"
    />
  </div>
</template>
