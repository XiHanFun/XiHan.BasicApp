<script lang="ts" setup>
import { computed } from 'vue'
import { NTag } from 'naive-ui'
import { useRoute, useRouter } from 'vue-router'
import { useAppStore, useTabbarStore } from '~/stores'

defineOptions({ name: 'AppTabbar' })

const route = useRoute()
const router = useRouter()
const appStore = useAppStore()
const tabbarStore = useTabbarStore()

const visibleTabs = computed(() => tabbarStore.tabs)

function handleJump(path: string) {
  tabbarStore.setActiveTab(path)
  if (route.fullPath !== path) {
    router.push(path)
  }
}

function handleClose(path: string, e: MouseEvent) {
  e.stopPropagation()
  tabbarStore.removeTab(path)
  if (route.fullPath === path) {
    router.push(tabbarStore.activeTab)
  }
}

function handleCloseOthers(path: string) {
  tabbarStore.closeOthers(path)
  if (route.fullPath !== path) {
    router.push(path)
  }
}
</script>

<template>
  <div
    v-if="appStore.tabbarEnabled"
    class="flex items-center gap-2 overflow-x-auto border-b border-gray-100 bg-white px-4 py-2 dark:border-gray-800 dark:bg-gray-900"
  >
    <NTag
      v-for="item in visibleTabs"
      :key="item.key"
      :type="route.fullPath === item.path ? 'primary' : 'default'"
      :closable="item.closable"
      round
      class="cursor-pointer shrink-0"
      @click="handleJump(item.path)"
      @close="handleClose(item.path, $event)"
      @contextmenu.prevent="handleCloseOthers(item.path)"
    >
      {{ item.title }}
    </NTag>
  </div>
</template>
