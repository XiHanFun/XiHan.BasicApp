<script lang="ts" setup>
import {
  darkTheme,
  NConfigProvider,
  NLayout,
  NLayoutContent,
  NLayoutHeader,
  NLayoutSider,
} from 'naive-ui'
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useContentMaximize, useTheme } from '~/hooks'
import { useAppStore, useLayoutPreferences, useTabbarStore } from '~/stores'
import AppHeader from './components/AppHeader.vue'
import AppPreferenceDrawer from './components/AppPreferenceDrawer.vue'
import AppSidebar from './components/AppSidebar.vue'
import AppTabbar from './components/AppTabbar.vue'
import XihanBackTop from './components/XihanBackTop.vue'

defineOptions({ name: 'BasicLayout' })

const appStore = useAppStore()
const { isDark, themeOverrides } = useTheme()

// 深色侧边栏/顶栏 —— 只有在浅色主题下才需要局部深色，暗色主题下已经全局深色
const sidebarForceDark = computed(() => appStore.sidebarDark && !isDark.value)
const headerForceDark = computed(() => appStore.headerDark && !isDark.value)
const layoutPreferences = useLayoutPreferences()
const tabbarStore = useTabbarStore()
const route = useRoute()
const { contentIsMaximize: contentMaximized } = useContentMaximize()
const collapsed = computed(() => layoutPreferences.sidebarCollapsed.value)
const viewportWidth = ref(typeof window !== 'undefined' ? window.innerWidth : 1200)
const isNarrowScreen = computed(() => viewportWidth.value < 960)
const mobileSidebarOpen = ref(false)
const sidebarHoverExpand = ref(false)
const isTopOnlyLayout = computed(() => layoutPreferences.layoutMode.value === 'top')
const isFullContentLayout = computed(() => layoutPreferences.layoutMode.value === 'full')
const isMixLayout = computed(() => layoutPreferences.layoutMode.value === 'mix')
const isSideMixedLayout = computed(() => appStore.layoutMode === 'side-mixed')
const isHeaderMixLayout = computed(() => appStore.layoutMode === 'header-mix')
const isMultiColumnLayout = computed(() => isSideMixedLayout.value || isHeaderMixLayout.value)
const canHoverExpand = computed(() => {
  return (
    !isNarrowScreen.value
    && layoutPreferences.sidebarExpandOnHover
    && collapsed.value
  )
})
const effectiveCollapsed = computed(() => {
  if (isNarrowScreen.value) {
    return false
  }
  if (isMultiColumnLayout.value) {
    return false
  }
  if (canHoverExpand.value) {
    return !sidebarHoverExpand.value
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
  if (isMultiColumnLayout.value) {
    return 64 + appStore.sidebarWidth
  }
  if (floatingSidebarMode.value && floatingSidebarExpand.value) {
    return expandedSidebarWidth.value
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

// 内容区滚动追踪，用于头部阴影
const contentRef = ref<InstanceType<typeof NLayoutContent> | null>(null)
const layoutScrollY = ref(0)
const headerHasShadow = computed(() => layoutScrollY.value > 20)

function attachContentScrollListener() {
  const inst = contentRef.value as { $el?: Element } | null
  const container = inst?.$el?.querySelector?.('.n-scrollbar-container')
  if (container) {
    container.addEventListener('scroll', () => {
      layoutScrollY.value = container.scrollTop
    }, { passive: true })
  }
}
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

onMounted(() => {
  updateViewportWidth()
  attachContentScrollListener()
  window.addEventListener('resize', updateViewportWidth)
  window.addEventListener('xihan-toggle-sidebar-request', handleSidebarToggleRequest)
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', updateViewportWidth)
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
  <NLayout class="relative h-full bg-background-deep">
    <!-- 移动端遮罩 -->
    <div
      v-if="isNarrowScreen && mobileSidebarOpen"
      class="fixed inset-0 z-40 bg-black/35 backdrop-blur-[1px]"
      @click="closeMobileSidebar"
    />

    <!-- mix 布局：头部打通全宽 -->
    <div
      v-if="isMixLayout"
      class="shrink-0 bg-header"
      :class="appStore.headerMode === 'fixed' ? 'sticky top-0 z-20' : ''"
    >
      <NLayoutHeader
        v-if="appStore.headerShow && !contentMaximized && !isFullContentLayout"
        class="bg-header"
        :class="[headerForceDark ? 'dark' : '', headerHasShadow ? 'layout-header-shadow' : '']"
      >
        <NConfigProvider
          :theme="headerForceDark ? darkTheme : undefined"
          :theme-overrides="themeOverrides"
        >
          <AppHeader />
        </NConfigProvider>
      </NLayoutHeader>
    </div>

    <NLayout :has-sider="showSider && !isNarrowScreen" class="min-h-0 flex-1 bg-background-deep">
      <!-- 侧边栏 -->
      <NLayoutSider
        v-if="showSider"
        :width="siderWidth"
        :collapsed-width="64"
        :collapsed="effectiveCollapsed"
        collapse-mode="width"
        :native-scrollbar="false"
        class="layout-sider-root relative overflow-visible bg-sidebar transition-[transform] duration-300"
        :class="[
          sidebarForceDark ? 'dark' : '',
          isNarrowScreen
            ? 'fixed left-0 top-0 z-50 h-full shadow-xl'
            : floatingSidebarMode
              ? 'z-30'
              : '',
        ]"
        @mouseenter="handleSiderMouseEnter"
        @mouseleave="handleSiderMouseLeave"
      >
        <NConfigProvider
          :theme="sidebarForceDark ? darkTheme : undefined"
          :theme-overrides="themeOverrides"
        >
          <AppSidebar
            :collapsed="effectiveCollapsed"
            :floating-mode="isNarrowScreen ? false : floatingSidebarMode"
            :floating-expand="floatingSidebarExpand"
            :expanded-width="expandedSidebarWidth"
          />
        </NConfigProvider>
      </NLayoutSider>

      <!-- 主内容区 -->
      <NLayout class="flex min-h-0 flex-col bg-background-deep">
        <!-- 非 mix 布局头部 -->
        <div
          v-if="!isMixLayout"
          class="shrink-0 bg-header"
          :class="appStore.headerMode === 'fixed' ? 'sticky top-0 z-20' : ''"
        >
          <NLayoutHeader
            v-if="appStore.headerShow && !contentMaximized && !isFullContentLayout"
            class="bg-header"
            :class="[headerForceDark ? 'dark' : '', headerHasShadow ? 'layout-header-shadow' : '']"
          >
            <NConfigProvider
              :theme="headerForceDark ? darkTheme : undefined"
              :theme-overrides="themeOverrides"
            >
              <AppHeader />
            </NConfigProvider>
          </NLayoutHeader>
        </div>
        <AppTabbar v-if="appStore.tabbarEnabled && !contentMaximized && !isFullContentLayout" />

        <!-- 页面内容 -->
        <NLayoutContent
          ref="contentRef"
          class="flex-1 overflow-auto"
          :native-scrollbar="false"
          :content-style="{ padding: '16px', minHeight: '100%' }"
        >
          <div class="min-h-full rounded-lg" :style="contentStyle">
            <RouterView v-slot="{ Component, route: currentRoute }">
              <Transition :name="transitionName" mode="out-in">
                <KeepAlive
                  :include="currentRoute.meta?.keepAlive ? [currentRoute.name as string] : []"
                >
                  <component
                    :is="Component"
                    :key="`${currentRoute.fullPath}_${tabbarStore.getRefreshSeed(currentRoute.fullPath)}`"
                  />
                </KeepAlive>
              </Transition>
            </RouterView>
          </div>
        </NLayoutContent>

        <!-- 页脚 -->
        <div
          v-if="appStore.footerEnable && !isFullContentLayout"
          class="flex flex-wrap items-center justify-center gap-x-3 border-t border-border px-4 py-2 text-xs text-muted-foreground"
          :class="appStore.footerFixed ? 'sticky bottom-0 bg-header' : ''"
        >
          <span v-if="appStore.copyrightEnable">
            Copyright © {{ appStore.copyrightDate || new Date().getFullYear() }}
            <a
              v-if="appStore.copyrightSite"
              :href="appStore.copyrightSite"
              target="_blank"
              class="ml-1 hover:underline"
            >
              {{ appStore.copyrightName }}
            </a>
            <span v-else class="ml-1">{{ appStore.copyrightName }}</span>
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
  </NLayout>
  <AppPreferenceDrawer />
  <!-- 回到顶部 -->
  <XihanBackTop :scroll-y="layoutScrollY" :content-ref="contentRef" />
</template>

<style scoped>
.layout-sider-root {
  border-right: 1px solid hsl(var(--border));
}

.layout-header-shadow {
  box-shadow: 0 8px 24px hsl(var(--background) / 70%);
}
</style>
