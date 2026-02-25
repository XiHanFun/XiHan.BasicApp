<script lang="ts" setup>
import type { CSSProperties } from 'vue'
import type { MenuOption } from 'naive-ui'
import { Icon } from '@iconify/vue'
import { computed, h, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { HOME_PATH } from '~/constants'
import { useAppStore, useLayoutBridgeStore } from '~/stores'
import type { LayoutRouteRecord } from '../contracts'
import { useLayoutMenuDomain } from '../composables'
import SidebarActions from './sidebar/SidebarActions.vue'
import SidebarBrand from './sidebar/SidebarBrand.vue'
import SidebarCollapseButton from './sidebar/SidebarCollapseButton.vue'
import SidebarFixedButton from './sidebar/SidebarFixedButton.vue'
import SidebarMenu from './sidebar/SidebarMenu.vue'

defineOptions({ name: 'AppSidebar' })

interface Props {
  mode?: 'full' | 'header-logo' | 'extra-menu'
  collapse?: boolean
  expandOnHovering?: boolean
  extraVisible?: boolean
  extraCollapse?: boolean
  isMobile?: boolean
  isNarrowScreen?: boolean
  mobileSidebarOpen?: boolean
  showSidebar?: boolean
  sidebarWidth?: number
  sidebarCollapseWidth?: number
  sidebarMarginTop?: number
  sidebarZIndex?: number
  sidebarExtraWidth?: number
  headerHeight?: number
  isSideMode?: boolean
  isMixedNav?: boolean
  isDualColumn?: boolean
  floatingMode?: boolean
  floatingExpand?: boolean
  expandedWidth?: number
  effectiveCollapsed?: boolean
  sidebarTheme?: string
}

const props = withDefaults(defineProps<Props>(), {
  mode: 'full',
  collapse: false,
  expandOnHovering: false,
  extraVisible: false,
  extraCollapse: false,
  isMobile: false,
  isNarrowScreen: false,
  mobileSidebarOpen: false,
  showSidebar: true,
  sidebarWidth: 224,
  sidebarCollapseWidth: 60,
  sidebarMarginTop: 0,
  sidebarZIndex: 200,
  sidebarExtraWidth: 224,
  headerHeight: 50,
  isSideMode: true,
  isMixedNav: false,
  isDualColumn: false,
  floatingMode: false,
  floatingExpand: false,
  expandedWidth: 224,
  effectiveCollapsed: false,
  sidebarTheme: 'light',
})

const emit = defineEmits<{
  'update:collapse': [value: boolean]
  'update:expandOnHovering': [value: boolean]
  'update:extraVisible': [value: boolean]
  'update:extraCollapse': [value: boolean]
  'sidebarMouseEnter': [event: MouseEvent]
  'sidebarMouseLeave': []
}>()

const appStore = useAppStore()
const layoutBridgeStore = useLayoutBridgeStore()
const { t, te } = useI18n()
const {
  route,
  router,
  baseMenuSource,
  visibleRootRoutes,
  activeRootRoute,
  toLayoutMeta,
  resolveFullPath,
  buildMenuOptionsFromRoutes,
  findMatchedRoutePath,
} = useLayoutMenuDomain()

const appTitle = computed(() => appStore.brandTitle || import.meta.env.VITE_APP_TITLE || 'XiHan Admin')
const appLogo = computed(() => appStore.brandLogo || import.meta.env.VITE_APP_LOGO || '/favicon.png')

const activeKey = computed(() => String(route.meta?.activePath || route.path || ''))
const isSideMixedLayout = computed(() => appStore.layoutMode === 'side-mixed')
const isHeaderMixLayout = computed(() => appStore.layoutMode === 'header-mix')
const isMixedNavLayout = computed(() => ['mix', 'header-sidebar'].includes(appStore.layoutMode))
const isSplitMenuLayout = computed(() =>
  appStore.navigationSplit && ['mix', 'header-sidebar'].includes(appStore.layoutMode),
)

const extraMenuTheme = computed<'dark' | 'light'>(() => {
  if (props.sidebarTheme === 'dark') return 'dark'
  return (appStore.sidebarSubDark || appStore.sidebarDark) ? 'dark' : 'light'
})

function renderIcon(icon: string) {
  return () => h(Icon, { icon })
}

function translateTitle(title: string, fallback: string) {
  return te(title) ? t(title) : fallback
}

function toPrimaryOptions(routeList: LayoutRouteRecord[], parentPath = '') {
  return buildMenuOptionsFromRoutes(routeList, {
    keyBy: 'path',
    translate: translateTitle,
    iconRenderer: renderIcon,
  }, parentPath).map(item => ({ ...item, children: undefined }))
}

// --- Standard menu ---
const menuSource = computed<LayoutRouteRecord[]>(() => {
  if (isSideMixedLayout.value || isHeaderMixLayout.value) return []
  if (!isSplitMenuLayout.value) return baseMenuSource.value
  return activeRootRoute.value?.children?.filter(child => !toLayoutMeta(child).hidden) ?? []
})

const menuOptions = computed(() => {
  const parentPath = isSplitMenuLayout.value && activeRootRoute.value
    ? resolveFullPath(activeRootRoute.value.path)
    : ''
  return buildMenuOptionsFromRoutes(menuSource.value, {
    keyBy: 'path',
    translate: translateTitle,
    iconRenderer: renderIcon,
  }, parentPath)
})

// --- Side-mixed menu ---
const sideMixedPrimaryRoutes = computed(() => isSideMixedLayout.value ? visibleRootRoutes.value : [])
const sideMixedPrimaryOptions = computed<MenuOption[]>(() => toPrimaryOptions(sideMixedPrimaryRoutes.value))
const sideMixedActiveTopKey = computed(() => {
  if (!isSideMixedLayout.value) return ''
  return findMatchedRoutePath(sideMixedPrimaryRoutes.value)
    ?? (sideMixedPrimaryRoutes.value[0] ? resolveFullPath(sideMixedPrimaryRoutes.value[0].path) : '')
    ?? ''
})
const sideMixedSecondarySource = computed<LayoutRouteRecord[]>(() => {
  if (!isSideMixedLayout.value) return []
  const activeTopRoute = sideMixedPrimaryRoutes.value.find(
    item => resolveFullPath(item.path) === sideMixedActiveTopKey.value,
  )
  if (!activeTopRoute) return []
  return activeTopRoute.children?.filter(child => !toLayoutMeta(child).hidden) ?? []
})
const sideMixedSecondaryOptions = computed(() =>
  buildMenuOptionsFromRoutes(sideMixedSecondarySource.value, {
    keyBy: 'path',
    translate: translateTitle,
    iconRenderer: renderIcon,
  }, sideMixedActiveTopKey.value),
)

// --- Header-mix menu ---
const headerMixParentPath = computed(() => {
  if (!activeRootRoute.value) return ''
  return resolveFullPath(activeRootRoute.value.path)
})
const headerMixPrimaryRoutes = computed(() => {
  if (!isHeaderMixLayout.value) return []
  return activeRootRoute.value?.children?.filter(child => !toLayoutMeta(child).hidden) ?? []
})
const headerMixPrimaryOptions = computed<MenuOption[]>(() => toPrimaryOptions(headerMixPrimaryRoutes.value, headerMixParentPath.value))
const headerMixActivePrimaryKey = computed(() => {
  if (!isHeaderMixLayout.value) return ''
  return findMatchedRoutePath(headerMixPrimaryRoutes.value, headerMixParentPath.value)
    ?? (headerMixPrimaryRoutes.value[0]
      ? resolveFullPath(headerMixPrimaryRoutes.value[0].path, headerMixParentPath.value)
      : '')
    ?? ''
})
const headerMixSecondarySource = computed<LayoutRouteRecord[]>(() => {
  if (!isHeaderMixLayout.value) return []
  const activePrimary = headerMixPrimaryRoutes.value.find(
    item => resolveFullPath(item.path, headerMixParentPath.value) === headerMixActivePrimaryKey.value,
  )
  if (!activePrimary) return []
  return activePrimary.children?.filter(child => !toLayoutMeta(child).hidden) ?? []
})
const headerMixSecondaryOptions = computed(() =>
  buildMenuOptionsFromRoutes(headerMixSecondarySource.value, {
    keyBy: 'path',
    translate: translateTitle,
    iconRenderer: renderIcon,
  }, headerMixActivePrimaryKey.value),
)

// --- Sidebar styles ---
const SIDEBAR_COLLAPSE_WIDTH = 60

const placeholderStyle = computed((): CSSProperties => {
  let widthValue = `${props.sidebarWidth}px`

  if (props.expandOnHovering && !appStore.sidebarExpandOnHover) {
    widthValue = `${props.sidebarCollapseWidth}px`
  }

  if (props.isDualColumn && appStore.sidebarExpandOnHover && props.extraVisible) {
    widthValue = `${props.sidebarWidth + props.sidebarExtraWidth}px`
  }

  if (props.sidebarWidth === 0) {
    widthValue = '0px'
  }

  return {
    flex: `0 0 ${widthValue}`,
    maxWidth: widthValue,
    minWidth: widthValue,
    width: widthValue,
    ...(widthValue === '0px' ? { overflow: 'hidden' } : {}),
    marginLeft: props.showSidebar ? '0' : `-${widthValue}`,
  }
})

const asideStyle = computed((): CSSProperties => {
  const isMixed = props.isDualColumn
  const extraW = isMixed && appStore.sidebarExpandOnHover && props.extraVisible
    ? props.sidebarExtraWidth
    : 0
  const totalW = props.sidebarWidth + extraW
  return {
    '--scroll-shadow': 'var(--sidebar)',
    flex: `0 0 ${totalW}px`,
    maxWidth: `${totalW}px`,
    minWidth: `${totalW}px`,
    width: `${totalW}px`,
    height: `calc(100% - ${props.sidebarMarginTop}px)`,
    marginTop: `${props.sidebarMarginTop}px`,
    zIndex: props.sidebarZIndex,
    ...(isMixed && props.extraVisible ? { transition: 'none' } : {}),
  }
})

const sidebarContentStyle = computed((): CSSProperties => ({
  height: `calc(100% - ${props.headerHeight + 42}px)`,
  paddingTop: '8px',
}))

const logoAreaStyle = computed((): CSSProperties => {
  const isMixed = props.isDualColumn
  return {
    ...(isMixed ? { display: 'flex', justifyContent: 'center' } : {}),
    height: `${props.headerHeight - 1}px`,
  }
})

const extraStyle = computed((): CSSProperties => ({
  left: `${props.sidebarWidth}px`,
  width: props.extraVisible && props.showSidebar ? `${props.sidebarExtraWidth}px` : '0',
  zIndex: props.sidebarZIndex,
}))

const extraTitleStyle = computed((): CSSProperties => ({
  height: `${props.headerHeight - 1}px`,
}))

const extraContentStyle = computed((): CSSProperties => ({
  height: `calc(100% - ${props.headerHeight + 42}px)`,
}))

// --- Actions ---
function handleMenuUpdate(key: string) {
  if (!key) return
  if (key.startsWith('/')) {
    if (key !== route.path) router.push(key)
    return
  }
  if (String(route.name ?? '') !== key) router.push({ name: key })
}

function resolveFirstVisiblePath(target: LayoutRouteRecord, parentPath = ''): string {
  const currentPath = resolveFullPath(target.path, parentPath)
  const visibleChildren = target.children?.filter(child => !toLayoutMeta(child).hidden) ?? []
  if (!visibleChildren.length) return currentPath
  return resolveFirstVisiblePath(visibleChildren[0], currentPath)
}

function jumpToFirstVisibleChild(target: LayoutRouteRecord, parentPath = '') {
  const targetPath = resolveFirstVisiblePath(target, parentPath)
  if (targetPath && targetPath !== route.path) router.push(targetPath)
}

function handleSideMixedPrimaryUpdate(key: string) {
  const target = sideMixedPrimaryRoutes.value.find(item => resolveFullPath(item.path) === key)
  if (target) {
    const hasChildren = (target.children?.filter(child => !toLayoutMeta(child).hidden) ?? []).length > 0
    emit('update:extraVisible', hasChildren)
    jumpToFirstVisibleChild(target)
  }
}

function handleHeaderMixPrimaryUpdate(key: string) {
  const target = headerMixPrimaryRoutes.value.find(
    item => resolveFullPath(item.path, headerMixParentPath.value) === key,
  )
  if (target) {
    const hasChildren = (target.children?.filter(child => !toLayoutMeta(child).hidden) ?? []).length > 0
    emit('update:extraVisible', hasChildren)
    jumpToFirstVisibleChild(target, headerMixParentPath.value)
  }
}

function handleBrandClick() {
  if (route.path !== HOME_PATH) router.push(HOME_PATH)
}

function handleToggleCollapse() {
  if (typeof window !== 'undefined' && window.innerWidth < 960) {
    layoutBridgeStore.requestSidebarToggle()
    return
  }
  appStore.toggleSidebar()
}

function handleTogglePin() {
  appStore.setSidebarExpandOnHover(!appStore.sidebarExpandOnHover)
}

function syncExtraVisibility() {
  if (!props.isDualColumn) return
  const hasSecondary = isSideMixedLayout.value
    ? sideMixedSecondarySource.value.length > 0
    : isHeaderMixLayout.value
      ? headerMixSecondarySource.value.length > 0
      : false
  emit('update:extraVisible', hasSecondary)
}

onMounted(syncExtraVisibility)

watch(() => [props.isDualColumn, appStore.sidebarExpandOnHover], syncExtraVisibility)

watch(() => route.path, () => {
  if (props.isDualColumn) syncExtraVisibility()
})
</script>

<template>
  <!-- Header logo mode: only renders the brand -->
  <template v-if="mode === 'header-logo'">
    <SidebarBrand
      :collapsed="effectiveCollapsed"
      :app-title="appTitle"
      :app-logo="appLogo"
      :sidebar-collapsed-show-title="false"
      @click="handleBrandClick"
    />
  </template>

  <!-- Extra menu mode: renders extra panel menu content -->
  <template v-else-if="mode === 'extra-menu'">
    <SidebarMenu
      :active-key="activeKey"
      :collapsed="extraCollapse"
      :sidebar-theme="extraMenuTheme"
      :menu-options="isSideMixedLayout ? sideMixedSecondaryOptions : headerMixSecondaryOptions"
      :navigation-style="appStore.navigationStyle"
      :accordion="appStore.navigationAccordion"
      :no-top-padding="true"
      @menu-update="handleMenuUpdate"
    />
  </template>

  <!-- Full sidebar mode: complete sidebar with placeholder + fixed aside -->
  <template v-else>
    <!-- Placeholder div (takes space in flex flow, non-mobile only) -->
    <div
      v-if="!isMobile"
      :class="sidebarTheme"
      :style="placeholderStyle"
      class="h-full transition-all duration-150"
    />

    <!-- Fixed sidebar aside -->
    <aside
      :style="asideStyle"
      class="fixed left-0 top-0 h-full transition-all duration-150"
      @mouseenter="(e: MouseEvent) => emit('sidebarMouseEnter', e)"
      @mouseleave="emit('sidebarMouseLeave')"
    >
      <!-- Primary sidebar panel -->
      <div
        class="relative h-full"
        :class="[
          sidebarTheme,
          isDualColumn ? 'bg-sidebar-deep' : 'border-r border-border bg-sidebar',
        ]"
        :style="{ width: `${sidebarWidth}px` }"
      >
        <!-- Fixed (pin) button -->
        <SidebarFixedButton
          v-if="!collapse && !isDualColumn && appStore.sidebarFixedButton"
          v-model:expand-on-hover="appStore.sidebarExpandOnHover"
        />

        <!-- Side-mixed layout: collapsed logo + icon+text menu -->
        <template v-if="isSideMixedLayout">
          <div :style="logoAreaStyle">
            <SidebarBrand
              :collapsed="true"
              :app-title="appTitle"
              :app-logo="appLogo"
              :sidebar-collapsed-show-title="false"
              @click="handleBrandClick"
            />
          </div>
          <div :style="sidebarContentStyle" class="mixed-primary-menu overflow-y-auto overflow-x-hidden">
            <SidebarMenu
              :active-key="sideMixedActiveTopKey"
              :collapsed="true"
              :collapsed-width="sidebarWidth"
              :sidebar-collapsed-show-title="true"
              :sidebar-theme="sidebarTheme"
              :menu-options="sideMixedPrimaryOptions"
              :navigation-style="appStore.navigationStyle"
              :accordion="true"
              :no-top-padding="true"
              @menu-update="handleSideMixedPrimaryUpdate"
            />
          </div>
          <div style="height: 42px" />
        </template>

        <!-- Header-mix layout: collapsed logo + icon+text menu -->
        <template v-else-if="isHeaderMixLayout">
          <div :style="logoAreaStyle">
            <SidebarBrand
              :collapsed="true"
              :app-title="appTitle"
              :app-logo="appLogo"
              :sidebar-collapsed-show-title="false"
              @click="handleBrandClick"
            />
          </div>
          <div :style="sidebarContentStyle" class="mixed-primary-menu overflow-y-auto overflow-x-hidden">
            <SidebarMenu
              :active-key="headerMixActivePrimaryKey"
              :collapsed="true"
              :collapsed-width="sidebarWidth"
              :sidebar-collapsed-show-title="true"
              :sidebar-theme="sidebarTheme"
              :menu-options="headerMixPrimaryOptions"
              :navigation-style="appStore.navigationStyle"
              :accordion="true"
              :no-top-padding="true"
              @menu-update="handleHeaderMixPrimaryUpdate"
            />
          </div>
          <div style="height: 42px" />
        </template>

        <!-- Standard layout -->
        <template v-else>
          <!-- Logo -->
          <div v-if="isSideMode && !isMixedNav" :style="logoAreaStyle">
            <SidebarBrand
              :collapsed="effectiveCollapsed"
              :app-title="appTitle"
              :app-logo="appLogo"
              :sidebar-collapsed-show-title="appStore.sidebarCollapsedShowTitle"
              @click="handleBrandClick"
            />
          </div>

          <!-- Scrollable menu area -->
          <div :style="sidebarContentStyle" class="overflow-y-auto overflow-x-hidden">
            <SidebarMenu
              :active-key="activeKey"
              :collapsed="effectiveCollapsed"
              :collapsed-width="sidebarCollapseWidth"
              :sidebar-collapsed-show-title="appStore.sidebarCollapsedShowTitle"
              :sidebar-theme="sidebarTheme"
              :no-top-padding="isMixedNav"
              :menu-options="menuOptions"
              :navigation-style="appStore.navigationStyle"
              :accordion="appStore.navigationAccordion"
              @menu-update="handleMenuUpdate"
            />
          </div>

          <!-- Collapse button spacer + button -->
          <div style="height: 42px" />
          <SidebarCollapseButton
            v-if="appStore.sidebarCollapseButton && !isDualColumn"
            :collapsed="collapse"
            @update:collapsed="(v: boolean) => emit('update:collapse', v)"
          />
        </template>
      </div>

      <!-- Extra panel for dual-column modes -->
      <div
        v-if="isDualColumn"
        :class="[
          (appStore.sidebarSubDark || appStore.sidebarDark) ? 'dark' : 'light',
          { 'border-l': extraVisible },
        ]"
        :style="extraStyle"
        class="fixed top-0 h-full overflow-hidden border-r border-border bg-sidebar transition-all duration-200"
      >
        <SidebarCollapseButton
          v-if="isDualColumn && appStore.sidebarExpandOnHover"
          :collapsed="extraCollapse"
          @update:collapsed="(v: boolean) => emit('update:extraCollapse', v)"
        />
        <SidebarFixedButton
          v-if="!extraCollapse"
          v-model:expand-on-hover="appStore.sidebarExpandOnHover"
        />
        <div v-if="!extraCollapse" :style="extraTitleStyle" class="flex items-center pl-2">
          <span class="truncate text-base font-semibold text-foreground">{{ appTitle }}</span>
        </div>
        <div
          :style="extraContentStyle"
          class="overflow-y-auto overflow-x-hidden border-border py-2"
        >
          <SidebarMenu
            :active-key="activeKey"
            :collapsed="extraCollapse"
            :sidebar-theme="extraMenuTheme"
            :menu-options="isSideMixedLayout ? sideMixedSecondaryOptions : headerMixSecondaryOptions"
            :navigation-style="appStore.navigationStyle"
            :accordion="appStore.navigationAccordion"
            :no-top-padding="true"
            @menu-update="handleMenuUpdate"
          />
        </div>
      </div>
    </aside>
  </template>
</template>

<style scoped>
/*
 * Mixed primary column — match vben NormalMenu
 * Light: accent-foreground text, primary hover-text, primary-foreground active-text on primary bg
 * Dark: foreground/80% text, foreground hover-text, primary-foreground active-text on primary bg
 */
.mixed-primary-menu :deep(.menu-theme-light.n-menu.n-menu--collapsed) {
  --n-item-text-color: hsl(var(--accent-foreground));
  --n-item-text-color-hover: hsl(var(--primary));
  --n-item-icon-color: hsl(var(--accent-foreground));
  --n-item-icon-color-hover: hsl(var(--primary));
  --n-item-color-hover: hsl(var(--accent));
  --n-item-text-color-active: hsl(var(--primary-foreground));
  --n-item-text-color-active-hover: hsl(var(--primary-foreground));
  --n-item-icon-color-active: hsl(var(--primary-foreground));
  --n-item-icon-color-active-hover: hsl(var(--primary-foreground));
  --n-item-color-active: hsl(var(--primary));
  --n-item-color-active-hover: hsl(var(--primary));
}

.mixed-primary-menu :deep(.menu-theme-dark.n-menu.n-menu--collapsed) {
  --n-item-text-color: hsl(var(--foreground) / 80%);
  --n-item-text-color-hover: hsl(var(--foreground));
  --n-item-icon-color: hsl(var(--foreground) / 72%);
  --n-item-icon-color-hover: hsl(var(--foreground));
  --n-item-color-hover: hsl(var(--accent));
  --n-item-text-color-active: hsl(var(--primary-foreground));
  --n-item-text-color-active-hover: hsl(var(--primary-foreground));
  --n-item-icon-color-active: hsl(var(--primary-foreground));
  --n-item-icon-color-active-hover: hsl(var(--primary-foreground));
  --n-item-color-active: hsl(var(--primary));
  --n-item-color-active-hover: hsl(var(--primary));
}

.mixed-primary-menu :deep(.n-menu.n-menu--collapsed .n-menu-item) {
  height: auto !important;
  margin: 4px 0 !important;
  overflow: visible !important;
}

.mixed-primary-menu :deep(.n-menu.n-menu--collapsed .n-menu-item-content) {
  display: flex !important;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: auto !important;
  margin: 0 8px !important;
  padding: 9px 0 !important;
  overflow: visible !important;
}

.mixed-primary-menu :deep(.n-menu.n-menu--collapsed .n-menu-item-content::before) {
  left: 0;
  right: 0;
  border-radius: 6px;
}

.mixed-primary-menu :deep(.n-menu.n-menu--collapsed .n-menu-item-content .n-menu-item-content__icon) {
  font-size: 20px !important;
  width: 20px;
  height: 20px;
  max-height: 20px;
  margin-right: 0 !important;
  transition: all 0.25s ease;
}

.mixed-primary-menu :deep(.n-menu.n-menu--collapsed .n-menu-item-content .n-menu-item-content-header) {
  display: block !important;
  width: 100% !important;
  height: auto !important;
  margin-top: 8px;
  margin-bottom: 0;
  overflow: hidden !important;
  text-overflow: ellipsis;
  white-space: nowrap;
  opacity: 1 !important;
  transform: none !important;
  text-align: center;
  font-size: 12px;
  font-weight: 400;
  line-height: 1.2;
}

.mixed-primary-menu :deep(.n-menu.n-menu--collapsed .n-menu-item-content .n-menu-item-content__arrow) {
  display: none !important;
}

.mixed-primary-menu :deep(.n-menu.n-menu--collapsed .n-menu-item-content:hover .n-menu-item-content__icon) {
  transform: scale(1.2);
}

.mixed-primary-menu :deep(.n-menu.n-menu--collapsed .n-menu-item.n-menu-item--selected .n-menu-item-content-header) {
  font-weight: 600;
}
</style>
