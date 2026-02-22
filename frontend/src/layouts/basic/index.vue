<script lang="ts" setup>
import { NLayout, NLayoutContent, NLayoutHeader, NLayoutSider } from 'naive-ui'
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useAppStore, useLayoutPreferences, useTabbarStore } from '~/stores'
import AppHeader from './components/AppHeader.vue'
import AppPreferenceDrawer from './components/AppPreferenceDrawer.vue'
import AppSidebar from './components/AppSidebar.vue'
import AppTabbar from './components/AppTabbar.vue'

defineOptions({ name: 'BasicLayout' })

const appStore = useAppStore()
const layoutPreferences = useLayoutPreferences()
const tabbarStore = useTabbarStore()
const route = useRoute()
const collapsed = computed(() => layoutPreferences.sidebarCollapsed.value)
const viewportWidth = ref(typeof window !== 'undefined' ? window.innerWidth : 1200)
const isNarrowScreen = computed(() => viewportWidth.value < 960)
const mobileSidebarOpen = ref(false)
const sidebarHoverExpand = ref(false)
const contentMaximized = ref(false)
const isTopOnlyLayout = computed(() => layoutPreferences.layoutMode.value === 'top')
const isFullContentLayout = computed(() => layoutPreferences.layoutMode.value === 'full')
const compactSidebarLayout = computed(() =>
  ['side-mixed', 'header-mix'].includes(appStore.layoutMode),
)
const canHoverExpand = computed(() => {
  return (
    !isNarrowScreen.value
    && layoutPreferences.sidebarExpandOnHover
    && (collapsed.value || compactSidebarLayout.value)
  )
})
const effectiveCollapsed = computed(() => {
  if (isNarrowScreen.value) {
    return false
  }
  if (canHoverExpand.value) {
    return !sidebarHoverExpand.value
  }
  if (compactSidebarLayout.value) {
    return true
  }
  return collapsed.value
})
const showSider = computed(
  () =>
    !contentMaximized.value
    && !isFullContentLayout.value
    && !isTopOnlyLayout.value
    && appStore.sidebarShow
    && (isNarrowScreen.value ? mobileSidebarOpen.value : true),
)
const siderFollowContent = computed(() => !layoutPreferences.sidebarExpandOnHover)
const floatingSidebarMode = computed(() => !siderFollowContent.value && canHoverExpand.value)
const floatingSidebarExpand = computed(() => canHoverExpand.value && sidebarHoverExpand.value)
const expandedSidebarWidth = computed(() =>
  layoutPreferences.layoutMode.value === 'mix' ? 208 : layoutPreferences.sidebarWidth.value,
)
const siderWidth = computed(() => {
  if (isNarrowScreen.value) {
    return expandedSidebarWidth.value
  }
  if (floatingSidebarMode.value && floatingSidebarExpand.value) {
    return expandedSidebarWidth.value
  }
  if (compactSidebarLayout.value) {
    return 80
  }
  if (!siderFollowContent.value && canHoverExpand.value) {
    return 64
  }
  if (effectiveCollapsed.value) {
    return 64
  }
  return layoutPreferences.layoutMode.value === 'mix' ? 208 : layoutPreferences.sidebarWidth.value
})
const transitionName = computed(() => (appStore.transitionEnable ? appStore.transitionName : ''))
const contentStyle = computed(() => {
  if (isFullContentLayout.value) {
    return { maxWidth: '100%', margin: '0' }
  }
  if (!appStore.contentCompact) {
    return {}
  }
  return {
    maxWidth: `${appStore.contentMaxWidth}px`,
    margin: '0 auto',
  }
})

function updateViewportWidth() {
  viewportWidth.value = window.innerWidth
  if (!isNarrowScreen.value) {
    mobileSidebarOpen.value = false
  }
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
  window.addEventListener('xihan-toggle-sidebar-request', handleSidebarToggleRequest)
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', updateViewportWidth)
  window.removeEventListener('xihan-toggle-content-maximize', handleContentMaximizeToggle)
  window.removeEventListener('xihan-sync-content-maximize-state', handleContentMaximizeSync)
  window.removeEventListener('xihan-toggle-sidebar-request', handleSidebarToggleRequest)
})

function handleSiderMouseEnter() {
  if (canHoverExpand.value && !isNarrowScreen.value) {
    sidebarHoverExpand.value = true
  }
}

function handleSiderMouseLeave() {
  if (canHoverExpand.value && !isNarrowScreen.value) {
    sidebarHoverExpand.value = false
  }
}

function handleSidebarToggleRequest() {
  if (isNarrowScreen.value) {
    mobileSidebarOpen.value = !mobileSidebarOpen.value
    return
  }
  layoutPreferences.sidebarCollapsed.value = !layoutPreferences.sidebarCollapsed.value
}

function closeMobileSidebar() {
  mobileSidebarOpen.value = false
}

watch(
  () => route.fullPath,
  () => {
    if (isNarrowScreen.value) {
      mobileSidebarOpen.value = false
    }
  },
)
</script>

<template>
  <NLayout :has-sider="showSider && !isNarrowScreen" class="relative h-full bg-[var(--bg-base)]">
    <div
      v-if="isNarrowScreen && mobileSidebarOpen"
      class="fixed inset-0 z-40 bg-black/35 backdrop-blur-[1px]"
      @click="closeMobileSidebar"
    />
    <!-- 侧边栏 -->
    <NLayoutSider
      v-if="showSider"
      :width="siderWidth"
      :collapsed-width="64"
      :collapsed="effectiveCollapsed"
      collapse-mode="width"
      :native-scrollbar="false"
      :style="{ backgroundColor: 'var(--sidebar-bg)' }"
      :class="[
        'layout-sider-root relative overflow-visible transition-all duration-300',
        appStore.sidebarDark ? 'sidebar-dark-overlay' : '',
      ]"
      :class="
        isNarrowScreen
          ? 'fixed left-0 top-0 z-50 h-full shadow-xl'
          : floatingSidebarMode
            ? 'z-30'
            : ''
      "
      @mouseenter="handleSiderMouseEnter"
      @mouseleave="handleSiderMouseLeave"
    >
      <AppSidebar
        :collapsed="effectiveCollapsed"
        :floating-mode="isNarrowScreen ? false : floatingSidebarMode"
        :floating-expand="floatingSidebarExpand"
        :compact-menu="compactSidebarLayout"
        :expanded-width="expandedSidebarWidth"
      />
    </NLayoutSider>

    <!-- 主内容区 -->
    <NLayout class="flex flex-col bg-[var(--bg-base)]">
      <div
        class="layout-header-shell shrink-0"
        :class="appStore.headerMode === 'fixed' ? 'sticky top-0 z-20' : ''"
      >
        <!-- 顶部导航 -->
        <NLayoutHeader
          v-if="appStore.headerShow && !contentMaximized && !isFullContentLayout"
          :class="[
            'bg-[var(--header-bg)]',
            appStore.headerDark ? 'header-dark-overlay' : '',
          ]"
        >
          <AppHeader />
        </NLayoutHeader>
        <AppTabbar />
      </div>

      <!-- 页面内容 -->
      <NLayoutContent
        class="flex-1 overflow-auto"
        :native-scrollbar="false"
        :content-style="{ padding: '16px', minHeight: '100%' }"
      >
        <div class="min-h-full rounded-lg" :style="contentStyle">
          <RouterView v-slot="{ Component, route: currentRoute }">
            <Transition :name="transitionName" mode="out-in">
              <KeepAlive :include="currentRoute.meta?.keepAlive ? [currentRoute.name as string] : []">
                <component
                  :is="Component"
                  :key="`${currentRoute.fullPath}_${tabbarStore.getRefreshSeed(currentRoute.fullPath)}`"
                />
              </KeepAlive>
            </Transition>
          </RouterView>
        </div>
      </NLayoutContent>
      <div
        v-if="appStore.footerEnable"
        class="flex flex-wrap items-center justify-center gap-x-3 border-t border-[hsl(var(--border))] px-4 py-2 text-xs text-[hsl(var(--muted-foreground))]"
        :class="appStore.footerFixed ? 'sticky bottom-0 bg-[var(--header-bg)]' : ''"
      >
        <span v-if="appStore.copyrightEnable">
          Copyright © {{ appStore.copyrightDate || new Date().getFullYear() }}
          <a
            v-if="appStore.copyrightSite"
            :href="appStore.copyrightSite"
            target="_blank"
            class="ml-1 hover:underline"
          >
            {{ appStore.copyrightCompany }}
          </a>
          <span v-else class="ml-1">{{ appStore.copyrightCompany }}</span>
        </span>
        <a
          v-if="appStore.copyrightIcp"
          :href="appStore.copyrightIcpUrl || '#'"
          target="_blank"
          class="hover:underline"
        >
          {{ appStore.copyrightIcp }}
        </a>
      </div>
    </NLayout>
  </NLayout>
  <AppPreferenceDrawer />
</template>

<style scoped>
.layout-sider-root {
  border-right: 1px solid var(--border-color);
}

.layout-header-shell {
  background: var(--header-bg);
}

/* 深色侧边栏/顶栏：在浅色主题下强制深色背景 */
.sidebar-dark-overlay :deep(*) {
  --sidebar-bg: hsl(220 16% 16%);
  --n-item-color-active: hsl(0 0% 100% / 0.15);
  color-scheme: dark;
}
.sidebar-dark-overlay {
  background-color: hsl(220 16% 16%) !important;
}

.header-dark-overlay {
  background-color: hsl(220 16% 16%) !important;
  color-scheme: dark;
}
.header-dark-overlay :deep(*) {
  --header-bg: hsl(220 16% 16%);
  color-scheme: dark;
}
</style>
