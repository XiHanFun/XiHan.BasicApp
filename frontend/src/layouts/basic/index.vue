<script lang="ts" setup>
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { NLayout, NLayoutSider, NLayoutHeader, NLayoutContent } from 'naive-ui'
import { useAppStore } from '~/stores'
import AppSidebar from './components/AppSidebar.vue'
import AppHeader from './components/AppHeader.vue'
import AppTabbar from './components/AppTabbar.vue'

defineOptions({ name: 'BasicLayout' })

const appStore = useAppStore()
const collapsed = computed(() => appStore.sidebarCollapsed)
const viewportWidth = ref(typeof window !== 'undefined' ? window.innerWidth : 1200)
const responsiveCollapse = computed(() => viewportWidth.value < 960)
const sidebarHoverExpand = ref(false)
const canHoverExpand = computed(() => {
  return appStore.sidebarExpandOnHover && (collapsed.value || responsiveCollapse.value)
})
const effectiveCollapsed = computed(() => {
  if (canHoverExpand.value) {
    return !sidebarHoverExpand.value
  }
  return collapsed.value || responsiveCollapse.value
})
const showSider = computed(() => appStore.layoutMode !== 'top' && appStore.sidebarShow)
const siderWidth = computed(() => {
  if (effectiveCollapsed.value) return 64
  return appStore.layoutMode === 'mix' ? 208 : appStore.sidebarWidth
})
const transitionName = computed(() => (appStore.transitionEnable ? appStore.transitionName : ''))
const contentStyle = computed(() => {
  if (!appStore.contentCompact) return {}
  return {
    maxWidth: `${appStore.contentMaxWidth}px`,
    margin: '0 auto',
  }
})

function updateViewportWidth() {
  viewportWidth.value = window.innerWidth
}

onMounted(() => {
  updateViewportWidth()
  window.addEventListener('resize', updateViewportWidth)
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', updateViewportWidth)
})

function handleSiderMouseEnter() {
  if (canHoverExpand.value) {
    sidebarHoverExpand.value = true
  }
}

function handleSiderMouseLeave() {
  if (canHoverExpand.value) {
    sidebarHoverExpand.value = false
  }
}
</script>

<template>
  <NLayout :has-sider="showSider" class="h-full bg-[var(--bg-base)]">
    <!-- 侧边栏 -->
    <NLayoutSider
      v-if="showSider"
      :width="siderWidth"
      :collapsed-width="64"
      :collapsed="effectiveCollapsed"
      collapse-mode="width"
      bordered
      :native-scrollbar="false"
      class="transition-all duration-300"
      @mouseenter="handleSiderMouseEnter"
      @mouseleave="handleSiderMouseLeave"
    >
      <AppSidebar :collapsed="effectiveCollapsed" />
    </NLayoutSider>

    <!-- 主内容区 -->
    <NLayout class="flex flex-col bg-[var(--bg-base)]">
      <!-- 顶部导航 -->
      <NLayoutHeader bordered class="shrink-0 bg-[var(--header-bg)]" :class="appStore.headerMode === 'fixed' ? 'sticky top-0 z-10' : ''">
        <AppHeader />
      </NLayoutHeader>
      <AppTabbar />

      <!-- 页面内容 -->
      <NLayoutContent
        class="flex-1 overflow-auto"
        :native-scrollbar="false"
        :content-style="{ padding: '16px', minHeight: '100%' }"
      >
        <div class="min-h-full rounded-lg" :style="contentStyle">
          <RouterView v-slot="{ Component, route }">
            <Transition :name="transitionName" mode="out-in">
              <KeepAlive :include="route.meta?.keepAlive ? [route.name as string] : []">
                <component :is="Component" :key="route.fullPath" />
              </KeepAlive>
            </Transition>
          </RouterView>
        </div>
      </NLayoutContent>
      <div
        v-if="appStore.footerEnable"
        class="border-t border-gray-100 px-4 py-2 text-xs text-gray-500 dark:border-gray-800 dark:text-gray-400"
        :class="appStore.footerFixed ? 'sticky bottom-0 bg-[var(--header-bg)]' : ''"
      >
        <span v-if="appStore.copyrightEnable">
          Copyright © {{ new Date().getFullYear() }}
          <a :href="appStore.copyrightSite" target="_blank" class="ml-1 hover:underline">{{ appStore.copyrightCompany }}</a>
        </span>
      </div>
    </NLayout>
  </NLayout>
</template>
