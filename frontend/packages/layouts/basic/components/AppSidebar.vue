<script lang="ts" setup>
import type { CSSProperties } from 'vue'
import type { LayoutRouteRecord } from '../contracts'
import type { MenuLinkTarget } from './sidebar/menu-links'
import type { AppMenuOption } from '~/types'

import { useHoverIntent } from '@xihan-ui/vue/behavior'
import { computed, h, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { HOME_PATH } from '~/constants'
import { Icon } from '~/iconify'
import { useAccessStore, useAppStore } from '~/stores'
import { useEffectiveLayoutMode, useLayoutMenuDomain } from '../composables'
import { renderSidebarBadgeLabel } from './MenuBadge.vue'
import { useMenuExternalLinks } from './sidebar/menu-links'
import SidebarBrand from './sidebar/SidebarBrand.vue'
import SidebarCollapseButton from './sidebar/SidebarCollapseButton.vue'
import SidebarFixedButton from './sidebar/SidebarFixedButton.vue'
import SidebarMenu from './sidebar/SidebarMenu.vue'

defineOptions({ name: 'AppSidebar' })

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
  sidebarSubTheme: 'light',
})

const emit = defineEmits<{
  'update:collapse': [value: boolean]
  'update:expandOnHovering': [value: boolean]
  'update:extraVisible': [value: boolean]
  'update:extraCollapse': [value: boolean]
  'sidebarMouseEnter': []
  'sidebarMouseLeave': []
}>()

interface Props {
  mode?: 'full' | 'header-logo'
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
  sidebarSubTheme?: string
}

const appStore = useAppStore()
const accessStore = useAccessStore()
const { t, te } = useI18n()
const {
  route,
  router,
  baseMenuSource,
  visibleRootRoutes,
  activeRootRoute,
  toLayoutMeta,
  resolveFullPath,
  resolveFirstNavigablePath,
  buildMenuOptionsFromRoutes,
  findMatchedRoutePath,
} = useLayoutMenuDomain()
const externalLinkOf = useMenuExternalLinks()

const appTitle = computed(
  () => appStore.brandTitle || import.meta.env.VITE_APP_TITLE || 'XiHan Admin',
)
const appLogo = computed(
  () => appStore.brandLogo || import.meta.env.VITE_APP_LOGO || '/favicon.png',
)

const activeKey = computed(() => String(route.meta?.activePath || route.path || ''))
const effectiveLayoutMode = useEffectiveLayoutMode()
const isSideMixedLayout = computed(() => effectiveLayoutMode.value === 'side-mixed')
const isHeaderMixLayout = computed(() => effectiveLayoutMode.value === 'header-mix')
const isSplitMenuLayout = computed(() => appStore.navigationSplit && effectiveLayoutMode.value === 'mix')

const extraMenuTheme = computed<'dark' | 'light'>(() => {
  return props.sidebarSubTheme === 'dark' ? 'dark' : 'light'
})

function resolveIcon(icon: string) {
  if (!icon)
    return icon
  return icon.includes(':') ? icon : `lucide:${icon}`
}

function renderIcon(icon: string) {
  return () => h(Icon, { icon: resolveIcon(icon) })
}

function translateTitle(title: string, _fallback: string) {
  return te(title) ? t(title) : title
}

const menuBuildConfig = {
  keyBy: 'path' as const,
  translate: translateTitle,
  iconRenderer: renderIcon,
  badgeLabelRenderer: renderSidebarBadgeLabel,
  linkIcon: () => h(Icon, { icon: 'lucide:external-link', width: 13, height: 13, style: 'opacity:0.5;flex-shrink:0' }),
}

function toPrimaryOptions(routeList: LayoutRouteRecord[], parentPath = '') {
  return buildMenuOptionsFromRoutes(routeList, menuBuildConfig, parentPath)
    .map(item => ({ ...item, children: undefined }))
}

// --- Standard menu ---
const menuSource = computed<LayoutRouteRecord[]>(() => {
  if (isSideMixedLayout.value || isHeaderMixLayout.value)
    return []
  if (!isSplitMenuLayout.value)
    return baseMenuSource.value
  return activeRootRoute.value?.children?.filter(child => !toLayoutMeta(child).hidden) ?? []
})

const menuOptions = computed(() => {
  const parentPath
    = isSplitMenuLayout.value && activeRootRoute.value
      ? resolveFullPath(activeRootRoute.value.path)
      : ''
  return buildMenuOptionsFromRoutes(menuSource.value, menuBuildConfig, parentPath)
})

// --- Hover tracking for dual-column primary menus ---
const sideMixedHoverKey = ref('')
const headerMixHoverKey = ref('')

// --- Side-mixed menu ---
const sideMixedPrimaryRoutes = computed(() =>
  isSideMixedLayout.value ? visibleRootRoutes.value : [],
)
const sideMixedPrimaryOptions = computed<AppMenuOption[]>(() =>
  toPrimaryOptions(sideMixedPrimaryRoutes.value),
)
const sideMixedActiveTopKey = computed(() => {
  if (!isSideMixedLayout.value)
    return ''
  return (
    findMatchedRoutePath(sideMixedPrimaryRoutes.value)
    ?? (sideMixedPrimaryRoutes.value[0]
      ? resolveFullPath(sideMixedPrimaryRoutes.value[0].path)
      : '')
    ?? ''
  )
})
const sideMixedEffectiveTopKey = computed(
  () => sideMixedHoverKey.value || sideMixedActiveTopKey.value,
)
const sideMixedSecondarySource = computed<LayoutRouteRecord[]>(() => {
  if (!isSideMixedLayout.value)
    return []
  const activeTopRoute = sideMixedPrimaryRoutes.value.find(
    item => resolveFullPath(item.path) === sideMixedEffectiveTopKey.value,
  )
  if (!activeTopRoute)
    return []
  return activeTopRoute.children?.filter(child => !toLayoutMeta(child).hidden) ?? []
})
const sideMixedSecondaryOptions = computed(() =>
  buildMenuOptionsFromRoutes(
    sideMixedSecondarySource.value,
    menuBuildConfig,
    sideMixedEffectiveTopKey.value,
  ),
)

// --- Header-mix menu ---
const headerMixParentPath = computed(() => {
  if (!activeRootRoute.value)
    return ''
  return resolveFullPath(activeRootRoute.value.path)
})
const headerMixPrimaryRoutes = computed(() => {
  if (!isHeaderMixLayout.value)
    return []
  return activeRootRoute.value?.children?.filter(child => !toLayoutMeta(child).hidden) ?? []
})
const headerMixPrimaryOptions = computed<AppMenuOption[]>(() =>
  toPrimaryOptions(headerMixPrimaryRoutes.value, headerMixParentPath.value),
)
const headerMixActivePrimaryKey = computed(() => {
  if (!isHeaderMixLayout.value)
    return ''
  return (
    findMatchedRoutePath(headerMixPrimaryRoutes.value, headerMixParentPath.value)
    ?? (headerMixPrimaryRoutes.value[0]
      ? resolveFullPath(headerMixPrimaryRoutes.value[0].path, headerMixParentPath.value)
      : '')
    ?? ''
  )
})
const headerMixEffectivePrimaryKey = computed(
  () => headerMixHoverKey.value || headerMixActivePrimaryKey.value,
)
const headerMixSecondarySource = computed<LayoutRouteRecord[]>(() => {
  if (!isHeaderMixLayout.value)
    return []
  const activePrimary = headerMixPrimaryRoutes.value.find(
    item =>
      resolveFullPath(item.path, headerMixParentPath.value) === headerMixEffectivePrimaryKey.value,
  )
  if (!activePrimary)
    return []
  return activePrimary.children?.filter(child => !toLayoutMeta(child).hidden) ?? []
})
const headerMixSecondaryOptions = computed(() =>
  buildMenuOptionsFromRoutes(
    headerMixSecondarySource.value,
    menuBuildConfig,
    headerMixEffectivePrimaryKey.value,
  ),
)

// --- Sidebar styles ---
/**
 * 桌面端侧栏收起（内容最大化、顶栏开关藏起侧栏）：整块照原宽度滑出画面并打 inert，
 * 占位列直接让出、正文一步到位——侧栏走 translate3d(-100%)，不逐帧重排正文
 */
const stowed = computed(() => !props.isMobile && !props.showSidebar)

// 收起与放出的那一帧占位列不走过渡，宽度一步到位；等这一帧真正画出来，再交还给原有的折叠过渡。
// 只等一帧不够：rAF 回调排在本帧绘制之前，那时撤掉 none，宽度变化照样会被过渡接住
const placeholderSnap = ref(false)
let snapFrame = 0
watch(stowed, () => {
  placeholderSnap.value = true
  cancelAnimationFrame(snapFrame)
  snapFrame = requestAnimationFrame(() => {
    snapFrame = requestAnimationFrame(() => {
      placeholderSnap.value = false
    })
  })
}, { flush: 'sync' })

const placeholderStyle = computed((): CSSProperties => {
  let widthValue = `${props.sidebarWidth}px`

  if (props.expandOnHovering && !appStore.sidebarExpandOnHover) {
    widthValue = `${props.sidebarCollapseWidth}px`
  }

  if (props.isDualColumn && appStore.sidebarExpandOnHover && props.extraVisible) {
    widthValue = `${props.sidebarWidth + props.sidebarExtraWidth}px`
  }

  if (props.sidebarWidth === 0 || stowed.value) {
    widthValue = '0px'
  }

  return {
    ...(placeholderSnap.value ? { transition: 'none' } : {}),
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
  const extraW
    = isMixed && appStore.sidebarExpandOnHover && props.extraVisible ? props.sidebarExtraWidth : 0
  const totalW = props.sidebarWidth + extraW
  return {
    '--scroll-shadow': 'var(--sidebar)',
    'flex': `0 0 ${totalW}px`,
    'maxWidth': `${totalW}px`,
    'minWidth': `${totalW}px`,
    'width': `${totalW}px`,
    'height': props.isMobile ? '100%' : `calc(100% - ${props.sidebarMarginTop}px)`,
    'marginTop': props.isMobile ? '0' : `${props.sidebarMarginTop}px`,
    'marginLeft': props.isMobile && !props.showSidebar ? `-${totalW}px` : '0',
    'overflow': props.isMobile && !props.showSidebar ? 'hidden' : undefined,
    'zIndex': props.sidebarZIndex,
    'transform': stowed.value ? 'translate3d(-100%, 0, 0)' : undefined,
    ...(isMixed && props.extraVisible ? { transition: 'none' } : {}),
  }
})

const sidebarContentStyle = computed(
  (): CSSProperties => ({
    height: `calc(100% - ${props.headerHeight + 42}px)`,
    paddingTop: '8px',
  }),
)

const logoAreaStyle = computed((): CSSProperties => {
  const isMixed = props.isDualColumn
  return {
    ...(isMixed ? { display: 'flex', justifyContent: 'center' } : {}),
    height: `${props.headerHeight - 1}px`,
  }
})

const extraStyle = computed(
  (): CSSProperties => ({
    left: `${props.sidebarWidth}px`,
    width: props.extraVisible && props.showSidebar ? `${props.sidebarExtraWidth}px` : '0',
    zIndex: props.sidebarZIndex,
  }),
)

const extraTitleStyle = computed(
  (): CSSProperties => ({
    height: `${props.headerHeight - 1}px`,
  }),
)

const extraContentStyle = computed((): CSSProperties => {
  const titleH = props.headerHeight > 0 ? props.headerHeight : 0
  return { height: `calc(100% - ${titleH + 42}px)` }
})

// --- Links ---
// 菜单条目渲染成真链接，点击、Enter、中键与 Ctrl+点击的跳转都由链接本身完成；这里只算去处
function menuLinkOf(key: string): MenuLinkTarget {
  const href = externalLinkOf(key)
  if (href)
    return { href }
  return { to: key.startsWith('/') ? key : { name: key } }
}

/** 双列主列的条目是一级目录，去处是它下面第一个可达的页面 */
function primaryLinkOf(routes: LayoutRouteRecord[], key: string, parentPath = ''): MenuLinkTarget {
  const href = externalLinkOf(key)
  if (href)
    return { href }
  const target = routes.find(item => resolveFullPath(item.path, parentPath) === key)
  return { to: (target && resolveFirstNavigablePath(target, parentPath)) || key }
}

function sideMixedPrimaryLinkOf(key: string): MenuLinkTarget {
  return primaryLinkOf(sideMixedPrimaryRoutes.value, key)
}

function headerMixPrimaryLinkOf(key: string): MenuLinkTarget {
  return primaryLinkOf(headerMixPrimaryRoutes.value, key, headerMixParentPath.value)
}

// --- Actions ---
// 主列点选只决定副栏开合：有可见下级才展开；跳转已由链接完成
function handleSideMixedPrimaryUpdate(key: string) {
  const target = sideMixedPrimaryRoutes.value.find(item => resolveFullPath(item.path) === key)
  if (target) {
    const hasChildren
      = (target.children?.filter(child => !toLayoutMeta(child).hidden) ?? []).length > 0
    emit('update:extraVisible', hasChildren)
  }
}

function handleHeaderMixPrimaryUpdate(key: string) {
  const target = headerMixPrimaryRoutes.value.find(
    item => resolveFullPath(item.path, headerMixParentPath.value) === key,
  )
  if (target) {
    const hasChildren
      = (target.children?.filter(child => !toLayoutMeta(child).hidden) ?? []).length > 0
    emit('update:extraVisible', hasChildren)
  }
}

function handleBrandClick() {
  const targetPath = accessStore.homePath || HOME_PATH
  if (route.path !== targetPath)
    router.push(targetPath)
}

function handlePrimaryColumnHover(e: MouseEvent, mode: 'header-mix' | 'side-mixed') {
  if (appStore.sidebarExpandOnHover)
    return
  // 主列的条目是侧栏导航的链接节点，DOM 上带 data-value=选项键；按键找回选项序号，不数 DOM 位置
  const itemEl = (e.target as HTMLElement).closest<HTMLElement>('[data-scope="side-nav"][data-part="link"]')
  if (!itemEl)
    return
  const hoveredKey = itemEl.dataset.value
  if (!hoveredKey)
    return
  const options = mode === 'side-mixed' ? sideMixedPrimaryOptions.value : headerMixPrimaryOptions.value
  const idx = options.findIndex(option => String(option.key) === hoveredKey)
  const routes = mode === 'side-mixed' ? sideMixedPrimaryRoutes.value : headerMixPrimaryRoutes.value
  if (idx < 0 || idx >= routes.length)
    return
  const parentPath = mode === 'header-mix' ? headerMixParentPath.value : ''
  const target = routes[idx]
  if (!target)
    return
  const key = resolveFullPath(target.path, parentPath)
  if (mode === 'side-mixed') {
    sideMixedHoverKey.value = key
  }
  else {
    headerMixHoverKey.value = key
  }
  const hasChildren
    = (target.children?.filter(child => !toLayoutMeta(child).hidden) ?? []).length > 0
  emit('update:extraVisible', hasChildren)
}

function handleAsideMouseLeave() {
  emit('sidebarMouseLeave')
  if (!appStore.sidebarExpandOnHover) {
    sideMixedHoverKey.value = ''
    headerMixHoverKey.value = ''
    syncExtraVisibility()
  }
}

const asideRef = ref<HTMLElement | null>(null)

// 触摸设备没有真正的悬停：轻点也会走 pointerenter/pointerleave，交给悬停意图会一抬指就收起
const hoverCapable = typeof window !== 'undefined' && (window.matchMedia?.('(hover: hover)').matches ?? true)

// 折叠侧栏的悬停展开：指针在侧栏停够 100ms 才报展开意图，扫过不展开；展开后的面板就是这个 aside 自己，没有独立浮层元素。
// 必须在 setup 顶层调用：它自己挂 onMounted 等模板 ref 就位，放进 onMounted 回调里那一层永远不会执行。
// 触摸设备交给下面的 mouseenter / mouseleave，这里取不到触发元素即不跟踪
useHoverIntent({
  getTriggerEl: () => (hoverCapable ? asideRef.value : null),
  getContentEl: () => null,
  openDelay: 100,
  closeDelay: 0,
  onOpenIntent: () => emit('sidebarMouseEnter'),
  onCloseIntent: handleAsideMouseLeave,
})

// 悬停意图接管的设备上这两条不再重复触发
function onAsideMouseEnter() {
  if (!hoverCapable)
    emit('sidebarMouseEnter')
}

function onAsideMouseLeave() {
  if (!hoverCapable)
    handleAsideMouseLeave()
}

function syncExtraVisibility() {
  if (!props.isDualColumn)
    return
  if (!appStore.sidebarExpandOnHover) {
    emit('update:extraVisible', false)
    return
  }
  const hasSecondary = isSideMixedLayout.value
    ? sideMixedSecondarySource.value.length > 0
    : isHeaderMixLayout.value
      ? headerMixSecondarySource.value.length > 0
      : false
  emit('update:extraVisible', hasSecondary)
}

onMounted(syncExtraVisibility)

watch(() => [props.isDualColumn, appStore.sidebarExpandOnHover], syncExtraVisibility)

watch(
  () => route.path,
  () => {
    if (props.isDualColumn)
      syncExtraVisibility()
  },
)
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
      ref="asideRef"
      :inert="stowed || undefined"
      :style="asideStyle"
      class="fixed left-0 top-0 h-full transition-all duration-150"
      @mouseenter="onAsideMouseEnter"
      @mouseleave="onAsideMouseLeave"
    >
      <!-- Primary sidebar panel -->
      <div
        class="relative h-full bg-sidebar"
        :class="[sidebarTheme, isDualColumn ? '' : 'border-r border-border']"
        :style="{ width: `${sidebarWidth}px` }"
      >
        <!-- Fixed (pin) button, hidden on mobile -->
        <SidebarFixedButton
          v-if="!isMobile && !collapse && !isDualColumn && appStore.sidebarFixedButton"
          v-model:expand-on-hover="appStore.sidebarExpandOnHover"
        />

        <!-- Side-mixed layout: collapsed logo + menu -->
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
          <div
            :style="sidebarContentStyle"
            class="mixed-primary-menu overflow-y-auto overflow-x-hidden"
            @mouseover="(e: MouseEvent) => handlePrimaryColumnHover(e, 'side-mixed')"
          >
            <SidebarMenu
              :active-key="sideMixedEffectiveTopKey"
              :collapsed="true"
              :collapsed-width="sidebarWidth"
              :sidebar-collapsed-show-title="appStore.sidebarCollapsedShowTitle"
              :menu-options="sideMixedPrimaryOptions"
              :link-of="sideMixedPrimaryLinkOf"
              :navigation-style="appStore.navigationStyle"
              :accordion="true"
              :no-top-padding="true"
              @menu-update="handleSideMixedPrimaryUpdate"
            />
          </div>
          <div style="height: 42px" />
        </template>

        <!-- Header-mix layout: collapsed logo + menu -->
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
          <div
            :style="sidebarContentStyle"
            class="mixed-primary-menu overflow-y-auto overflow-x-hidden"
            @mouseover="(e: MouseEvent) => handlePrimaryColumnHover(e, 'header-mix')"
          >
            <SidebarMenu
              :active-key="headerMixEffectivePrimaryKey"
              :collapsed="true"
              :collapsed-width="sidebarWidth"
              :sidebar-collapsed-show-title="appStore.sidebarCollapsedShowTitle"
              :menu-options="headerMixPrimaryOptions"
              :link-of="headerMixPrimaryLinkOf"
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
          <div
            v-if="isSideMode && !isMixedNav"
            :style="logoAreaStyle"
            :class="{ 'sidebar-logo-align-with-header': !effectiveCollapsed }"
          >
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
              :no-top-padding="isMixedNav"
              :menu-options="menuOptions"
              :link-of="menuLinkOf"
              :navigation-style="appStore.navigationStyle"
              :accordion="appStore.navigationAccordion"
            />
          </div>

          <!-- Collapse button spacer + button (hidden on mobile) -->
          <template v-if="!isMobile">
            <div style="height: 42px" />
            <SidebarCollapseButton
              v-if="appStore.sidebarCollapseButton && !isDualColumn"
              :collapsed="collapse"
              @update:collapsed="(v: boolean) => emit('update:collapse', v)"
            />
          </template>
        </template>
      </div>

      <!-- Extra panel for dual-column modes -->
      <div
        v-if="isDualColumn"
        :inert="stowed || undefined"
        :class="[sidebarSubTheme, { 'border-l': extraVisible }]"
        :data-theme="extraMenuTheme"
        :style="extraStyle"
        class="fixed top-0 h-full overflow-hidden border-r border-border bg-sidebar transition-all duration-200"
      >
        <div class="h-full">
          <SidebarCollapseButton
            v-if="isDualColumn && appStore.sidebarExpandOnHover"
            :collapsed="extraCollapse"
            @update:collapsed="(v: boolean) => emit('update:extraCollapse', v)"
          />
          <SidebarFixedButton
            v-if="!extraCollapse"
            v-model:expand-on-hover="appStore.sidebarExpandOnHover"
          />
          <div
            v-if="!extraCollapse && headerHeight > 0"
            :style="extraTitleStyle"
            class="flex items-center justify-center"
          >
            <span class="extra-brand-title truncate">{{ appTitle }}</span>
          </div>
          <div
            :style="extraContentStyle"
            class="overflow-y-auto overflow-x-hidden border-border py-2"
          >
            <SidebarMenu
              :active-key="activeKey"
              :collapsed="extraCollapse"
              :menu-options="
                isSideMixedLayout ? sideMixedSecondaryOptions : headerMixSecondaryOptions
              "
              :link-of="menuLinkOf"
              :navigation-style="appStore.navigationStyle"
              :accordion="appStore.navigationAccordion"
              :no-top-padding="true"
            />
          </div>
        </div>
      </div>
    </aside>
  </template>
</template>

<style scoped>
/*
 * Mixed primary column — match xihan NormalMenu
 * Light: accent-foreground text, primary hover-text, primary-foreground active-text on primary bg
 * Dark: foreground/80% text, foreground hover-text, primary-foreground active-text on primary bg
 */

.extra-brand-title {
  color: hsl(var(--foreground));
  font-size: 16px;
  font-weight: 600;
  letter-spacing: 0;
  line-height: 1.2;
}

.sidebar-logo-align-with-header {
  /* Header logo has an extra pl-2 container offset; keep side mode aligned with it. */
  padding-left: 0.5rem;
}
</style>

<style>

</style>
