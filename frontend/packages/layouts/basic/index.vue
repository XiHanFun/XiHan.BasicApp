<script lang="ts" setup>
import { Icon } from '@iconify/vue'
import { darkTheme, NConfigProvider } from 'naive-ui'
import { computed } from 'vue'
import { useTheme } from '~/hooks'
import AppHeader from './components/AppHeader.vue'
import AppPreferenceDrawer from './components/AppPreferenceDrawer.vue'
import AppSidebar from './components/AppSidebar.vue'
import AppTabbar from './components/AppTabbar.vue'
import XihanBackTop from './components/XihanBackTop.vue'
import XihanIconButton from './components/XihanIconButton.vue'
import { useLayoutShellAdapter } from './composables'
import { LayoutContentRenderer } from './core'

defineOptions({ name: 'BasicLayout' })

const { isDark, themeOverrides } = useTheme()
const shell = useLayoutShellAdapter()

const appVersion = __APP_VERSION__
const appBuildTime = __APP_BUILD_TIME__
const appHomepage = __APP_HOMEPAGE__
const appName = __APP_NAME__
const appAuthorName = __APP_AUTHOR_NAME__
const appAuthorUrl = __APP_AUTHOR_URL__

const sidebarForceDark = computed(() => shell.appStore.sidebarDark && !isDark.value)
const headerForceDark = computed(() => shell.appStore.headerDark && !isDark.value)
const sidebarTheme = computed(() => (isDark.value || shell.appStore.sidebarDark ? 'dark' : 'light'))
const sidebarSubTheme = computed(() =>
  isDark.value || shell.appStore.sidebarSubDark ? 'dark' : 'light',
)
const headerTheme = computed(() => (isDark.value || shell.appStore.headerDark ? 'dark' : 'light'))

const sidebarEnableState = computed(
  () =>
    shell.isMobile.value ||
    (!shell.isHeaderNav.value && !shell.isFullContent.value && shell.appStore.sidebarShow),
)
</script>

<template>
  <div class="relative flex min-h-full w-full">
    <!-- ==================== Sidebar ==================== -->
    <NConfigProvider
      v-if="sidebarEnableState"
      :theme="sidebarForceDark ? darkTheme : undefined"
      :theme-overrides="themeOverrides"
    >
      <AppSidebar
        v-model:collapse="shell.sidebarCollapse.value"
        v-model:expand-on-hovering="shell.sidebarExpandOnHovering.value"
        v-model:extra-visible="shell.sidebarExtraVisible.value"
        v-model:extra-collapse="shell.sidebarExtraCollapse.value"
        :is-mobile="shell.isMobile.value"
        :is-narrow-screen="shell.isNarrowScreen.value"
        :mobile-sidebar-open="shell.mobileSidebarOpen.value"
        :show-sidebar="shell.showSider.value"
        :sidebar-width="shell.isMobile.value ? shell.siderWidth.value : shell.getSidebarWidth.value"
        :sidebar-collapse-width="shell.getSideCollapseWidth.value"
        :sidebar-margin-top="shell.sidebarMarginTop.value"
        :sidebar-z-index="shell.sidebarZIndex.value"
        :sidebar-extra-width="shell.sidebarExtraWidth.value"
        :header-height="shell.isMixedNav.value ? 0 : shell.headerHeight.value"
        :is-side-mode="shell.isSideMode.value"
        :is-mixed-nav="shell.isMixedNav.value"
        :is-dual-column="shell.isDualColumnMode.value"
        :floating-mode="shell.floatingSidebarMode.value"
        :floating-expand="shell.floatingSidebarExpand.value"
        :expanded-width="shell.expandedSidebarWidth.value"
        :effective-collapsed="shell.effectiveCollapsed.value"
        :sidebar-theme="sidebarTheme"
        :sidebar-sub-theme="sidebarSubTheme"
        @sidebar-mouse-enter="shell.handleSidebarMouseEnter"
        @sidebar-mouse-leave="shell.handleSidebarMouseLeave"
      />
    </NConfigProvider>

    <!-- ==================== Main Content ==================== -->
    <div class="flex flex-1 flex-col overflow-hidden transition-all duration-300 ease-in">
      <!-- Header + Tabbar wrapper -->
      <div
        :class="{ 'shadow-[0_16px_24px_hsl(var(--background))]': shell.scrollY.value > 20 }"
        :style="shell.headerWrapperStyle.value"
        class="overflow-hidden transition-all duration-200"
      >
        <!-- Header -->
        <header
          v-if="shell.appStore.headerShow"
          v-show="!shell.isFullContent.value && !shell.contentMaximized.value"
          :class="headerTheme"
          :style="{
            height: `${shell.headerHeight.value}px`,
            right: !shell.isSideMode.value ? 0 : undefined,
          }"
          class="top-0 flex w-full flex-[0_0_auto] items-center border-b border-border bg-header pl-2 transition-[margin-top] duration-200"
        >
          <!-- Logo in header (for header-nav / mixed-nav / mobile) -->
          <div
            v-if="shell.showHeaderLogo.value"
            :style="{ minWidth: `${shell.isMobile.value ? 40 : shell.appStore.sidebarWidth}px` }"
          >
            <NConfigProvider
              :theme="headerForceDark ? darkTheme : undefined"
              :theme-overrides="themeOverrides"
            >
              <AppSidebar mode="header-logo" :effective-collapsed="shell.isMobile.value" />
            </NConfigProvider>
          </div>

          <!-- Toggle sidebar button -->
          <XihanIconButton
            v-if="shell.showHeaderToggleButton.value"
            class="my-0 mr-1"
            @click="shell.handleHeaderToggle"
          >
            <Icon :icon="shell.showSider.value ? 'ep:fold' : 'ep:expand'" width="18" height="18" />
          </XihanIconButton>

          <!-- Header content (flex-1 fills remaining header space) -->
          <NConfigProvider
            :theme="headerForceDark ? darkTheme : undefined"
            :theme-overrides="themeOverrides"
            class="flex min-w-0 flex-1 items-center"
          >
            <AppHeader :theme="headerTheme" />
          </NConfigProvider>
        </header>

        <!-- Tabbar -->
        <div
          v-if="
            shell.appStore.tabbarEnabled &&
            !shell.contentMaximized.value &&
            !shell.isFullContent.value
          "
          :style="shell.tabbarStyle.value"
        >
          <AppTabbar />
        </div>
      </div>

      <!-- Page content -->
      <div class="transition-[margin-top] duration-200" :style="shell.contentStyle.value">
        <div
          class="min-h-full"
          :style="
            shell.appStore.contentCompact
              ? { maxWidth: `${shell.appStore.contentMaxWidth}px`, margin: '0 auto' }
              : {}
          "
        >
          <LayoutContentRenderer :transition-name="shell.transitionName.value" />
        </div>
      </div>

      <!-- Footer -->
      <footer
        v-if="shell.appStore.footerEnable"
        :style="{
          minHeight: `${shell.footerHeight.value}px`,
          marginBottom: shell.isFullContent.value ? `-${shell.footerHeight.value}px` : '0',
          position: shell.appStore.footerFixed ? 'fixed' : 'static',
          width: shell.footerWidth.value,
          zIndex: shell.appStore.footerFixed ? 199 : undefined,
        }"
        class="footer-bar bottom-0 flex w-full border-t border-border bg-background px-4 text-xs text-muted-foreground transition-all duration-200"
        :class="shell.isMobile.value ? 'flex-col items-center justify-center gap-1 py-2' : 'flex-row items-center'"
      >
        <!-- Left: Dev version info -->
        <div v-if="shell.appStore.footerShowDevInfo" class="footer-section-left" :class="{ 'text-center': shell.isMobile.value }">
          <a :href="appHomepage" target="_blank" class="hover:underline">{{ appName }}</a>
          v{{ appVersion }}({{ appBuildTime }})
          · by
          <a :href="appAuthorUrl" target="_blank" class="hover:underline">{{ appAuthorName }}</a>
        </div>
        <div v-else-if="!shell.isMobile.value" class="footer-section-left" />

        <!-- Center: Copyright -->
        <div v-if="shell.appStore.copyrightEnable" class="footer-section-center">
          <span>
            Copyright &copy; {{ shell.appStore.copyrightDate || new Date().getFullYear() }}-{{ new Date().getFullYear() }}
            <a
              v-if="shell.appStore.copyrightSite"
              :href="shell.appStore.copyrightSite"
              target="_blank"
              class="hover:underline"
            >{{ shell.appStore.copyrightName }}</a>
            <span v-else>{{ shell.appStore.copyrightName }}</span>.
            All Rights Reserved.
          </span>
        </div>
        <div v-else-if="!shell.isMobile.value" class="footer-section-center" />

        <!-- Right: ICP -->
        <div v-if="shell.appStore.copyrightEnable && shell.appStore.copyrightIcp" :class="shell.isMobile.value ? '' : 'footer-section-right'">
          <a
            :href="shell.appStore.copyrightIcpUrl || '#'"
            target="_blank"
            class="hover:underline"
          >{{ shell.appStore.copyrightIcp }}</a>
        </div>
        <div v-else-if="!shell.isMobile.value" class="footer-section-right" />
      </footer>
    </div>

    <!-- ==================== Extra ==================== -->
    <AppPreferenceDrawer />
    <XihanBackTop :scroll-y="shell.scrollY.value" />

    <!-- Mobile mask -->
    <div
      v-if="shell.maskVisible.value"
      class="fixed left-0 top-0 h-full w-full bg-overlay transition-[background-color] duration-200"
      :style="{ zIndex: 200 }"
      @click="shell.handleClickMask"
    />
  </div>
</template>

<style scoped>
.footer-bar {
  gap: 8px;
}

.footer-section-left {
  flex: 1;
  min-width: 0;
  text-align: left;
}

.footer-section-center {
  flex: 0 0 auto;
  text-align: center;
  white-space: nowrap;
}

.footer-section-right {
  flex: 1;
  min-width: 0;
  text-align: right;
}

.footer-bar :deep(a) {
  color: hsl(var(--foreground));
  text-decoration: none;
}
</style>
