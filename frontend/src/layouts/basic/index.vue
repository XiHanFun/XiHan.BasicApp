<script lang="ts" setup>
import { NLayout, NLayoutContent, NLayoutHeader, NLayoutSider } from 'naive-ui'
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useAppStore, useTabbarStore } from '~/stores'
import AppHeader from './components/AppHeader.vue'
import AppSidebar from './components/AppSidebar.vue'
import AppTabbar from './components/AppTabbar.vue'

defineOptions({ name: 'BasicLayout' })

const appStore = useAppStore()
const tabbarStore = useTabbarStore()
const collapsed = computed(() => appStore.sidebarCollapsed)
const viewportWidth = ref(typeof window !== 'undefined' ? window.innerWidth : 1200)
const responsiveCollapse = computed(() => viewportWidth.value < 960)
const sidebarHoverExpand = ref(false)
const contentMaximized = ref(false)
const isTopOnlyLayout = computed(() => appStore.layoutMode === 'top')
const isFullContentLayout = computed(() => appStore.layoutMode === 'full')
const compactSidebarLayout = computed(() =>
  ['side-mixed', 'header-mix'].includes(appStore.layoutMode),
)
const canHoverExpand = computed(() => {
  return (
    appStore.sidebarExpandOnHover &&
    (collapsed.value || responsiveCollapse.value || compactSidebarLayout.value)
  )
})
const effectiveCollapsed = computed(() => {
  if (canHoverExpand.value) {
    return !sidebarHoverExpand.value
  }
  if (compactSidebarLayout.value) {
    return true
  }
  return collapsed.value || responsiveCollapse.value
})
const showSider = computed(
  () =>
    !contentMaximized.value &&
    !isFullContentLayout.value &&
    !isTopOnlyLayout.value &&
    appStore.sidebarShow,
)
const siderFollowContent = computed(() => !appStore.sidebarExpandOnHover)
const floatingSidebarMode = computed(() => !siderFollowContent.value && canHoverExpand.value)
const floatingSidebarExpand = computed(() => canHoverExpand.value && sidebarHoverExpand.value)
const expandedSidebarWidth = computed(() =>
  appStore.layoutMode === 'mix' ? 208 : appStore.sidebarWidth,
)
const siderWidth = computed(() => {
  if (floatingSidebarMode.value && floatingSidebarExpand.value) {
    return expandedSidebarWidth.value
  }
  if (compactSidebarLayout.value) {
    return 80
  }
  if (!siderFollowContent.value && canHoverExpand.value) return 64
  if (effectiveCollapsed.value) return 64
  return appStore.layoutMode === 'mix' ? 208 : appStore.sidebarWidth
})
const transitionName = computed(() => (appStore.transitionEnable ? appStore.transitionName : ''))
const contentStyle = computed(() => {
  if (isFullContentLayout.value) {
    return { maxWidth: '100%', margin: '0' }
  }
  if (!appStore.contentCompact) return {}
  return {
    maxWidth: `${appStore.contentMaxWidth}px`,
    margin: '0 auto',
  }
})

function updateViewportWidth() {
  viewportWidth.value = window.innerWidth
}

function emitContentMaximizeState() {
  window.dispatchEvent(
    new CustomEvent('xihan-content-maximized-change', { detail: contentMaximized.value }),
  )
}

function handleContentMaximizeToggle() {
  contentMaximized.value = !contentMaximized.value
  emitContentMaximizeState()
}

function handleContentMaximizeSync() {
  emitContentMaximizeState()
}

onMounted(() => {
  updateViewportWidth()
  window.addEventListener('resize', updateViewportWidth)
  window.addEventListener('xihan-toggle-content-maximize', handleContentMaximizeToggle)
  window.addEventListener('xihan-sync-content-maximize-state', handleContentMaximizeSync)
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', updateViewportWidth)
  window.removeEventListener('xihan-toggle-content-maximize', handleContentMaximizeToggle)
  window.removeEventListener('xihan-sync-content-maximize-state', handleContentMaximizeSync)
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
      :native-scrollbar="false"
      :style="{ backgroundColor: 'var(--sidebar-bg)' }"
      class="layout-sider-root relative overflow-visible transition-all duration-300"
      :class="floatingSidebarMode ? 'z-30' : ''"
      @mouseenter="handleSiderMouseEnter"
      @mouseleave="handleSiderMouseLeave"
    >
      <AppSidebar
        :collapsed="effectiveCollapsed"
        :floating-mode="floatingSidebarMode"
        :floating-expand="floatingSidebarExpand"
        :compact-menu="compactSidebarLayout"
        :expanded-width="expandedSidebarWidth"
      />
    </NLayoutSider>

    <!-- 主内容区 -->
    <NLayout class="flex flex-col bg-[var(--bg-base)]">
      <!-- 顶部导航 -->
      <NLayoutHeader
        v-if="!contentMaximized && !isFullContentLayout"
        class="shrink-0 bg-[var(--header-bg)]"
        :class="appStore.headerMode === 'fixed' ? 'sticky top-0 z-10' : ''"
      >
        <AppHeader />
      </NLayoutHeader>
      <AppTabbar class="sticky z-10" :class="contentMaximized ? 'top-0' : 'top-14'" />

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
                <component
                  :is="Component"
                  :key="`${route.fullPath}_${tabbarStore.getRefreshSeed(route.fullPath)}`"
                />
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
          <a :href="appStore.copyrightSite" target="_blank" class="ml-1 hover:underline">
            {{ appStore.copyrightCompany }}
          </a>
        </span>
      </div>
    </NLayout>
  </NLayout>
</template>

<style scoped>
.layout-sider-root {
  border-right: 1px solid var(--border-color) !important;
}
</style>
