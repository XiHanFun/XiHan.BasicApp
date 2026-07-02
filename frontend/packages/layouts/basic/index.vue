<script lang="ts" setup>
import { darkTheme, NConfigProvider, NDropdown } from 'naive-ui'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { setupContainerTransform } from '~/composables/useContainerTransform'
import { useRefresh, useTheme } from '~/hooks'
import { Icon } from '~/iconify'
import { useSplitViewStore, useTabbarStore } from '~/stores'
import AppFavorites from './components/AppFavorites.vue'
import AppHeader from './components/AppHeader.vue'
import AppPreferenceDrawer from './components/AppPreferenceDrawer.vue'
import AppSidebar from './components/AppSidebar.vue'
import AppTabbar from './components/AppTabbar.vue'
import AppTabOverview from './components/AppTabOverview.vue'
import NotificationBanner from './components/NotificationBanner.vue'
import NotificationGate from './components/NotificationGate.vue'
import SplitPane from './components/SplitPane.vue'
import XihanBackTop from './components/XihanBackTop.vue'
import XihanIconButton from './components/XihanIconButton.vue'
import { openTabInNewWindow, useLayoutShellAdapter } from './composables'
import { useChatIntegration } from './composables/use-chat-integration'
import { useCheckUpdates } from './composables/use-check-updates'
import { useSignalRIntegration } from './composables/use-signalr-integration'
import { LayoutContentRenderer } from './core'

defineOptions({ name: 'BasicLayout' })

const { isDark, themeOverrides } = useTheme()
const shell = useLayoutShellAdapter()
const route = useRoute()
const router = useRouter()
const { t, te } = useI18n()
const splitView = useSplitViewStore()
const tabbarStore = useTabbarStore()

// 初始化 SignalR 连接（实时通知 + 踢下线）
useSignalRIntegration()

// 初始化聊天实时链路（/hubs/chat 独立连接 + 会话预取供顶栏角标；无权限静默关闭）
useChatIntegration()

// 定时检查前端资源更新
useCheckUpdates()

// 容器变形转场：点击的行/按钮「长大」成弹窗、关闭收回原位（跟随「页面切换动画」偏好）
setupContainerTransform({ enabled: () => shell.appStore.transitionEnable })

// 仅当「当前路由 === 分屏锚定标签」时显示分屏（右标签已并入、从标签栏隐藏）
const showSplit = computed(() => splitView.active && route.fullPath === splitView.leftPath)

// 路由变化时对齐分屏状态（pre-flush，渲染前完成 → 不闪烁）：
// - 抵达副标签路径（用户经菜单直接导航）→ 锚定/副互换
// - 抵达「替换锚定页」的目标路径 → 把锚定切到新页
const pendingAnchorSwitch = ref('')
watch(() => route.fullPath, (path) => {
  if (!splitView.active) {
    return
  }
  if (path === splitView.rightPath) {
    splitView.swapPaths()
    return
  }
  if (pendingAnchorSwitch.value && path === pendingAnchorSwitch.value) {
    splitView.setLeftPath(path)
    pendingAnchorSwitch.value = ''
  }
})

// 分屏分隔条拖拽：拖拽期间用全屏遮罩接管指针（防 iframe 吞事件→卡顿），rAF 合帧更丝滑
const splitRowRef = ref<HTMLElement | null>(null)
const draggingDivider = ref(false)
function onDividerDown() {
  const el = splitRowRef.value
  if (!el) {
    return
  }
  const rect = el.getBoundingClientRect()
  draggingDivider.value = true
  document.body.style.userSelect = 'none'
  let raf = 0
  let pendingX = 0
  const move = (ev: PointerEvent) => {
    pendingX = ev.clientX
    if (raf) {
      return
    }
    raf = requestAnimationFrame(() => {
      raf = 0
      splitView.setRatio((pendingX - rect.left) / rect.width)
    })
  }
  const up = () => {
    draggingDivider.value = false
    document.body.style.userSelect = ''
    if (raf) {
      cancelAnimationFrame(raf)
    }
    window.removeEventListener('pointermove', move)
    window.removeEventListener('pointerup', up)
  }
  window.addEventListener('pointermove', move)
  window.addEventListener('pointerup', up)
}

// ── 分割线悬浮工具组（左右页面各自的替换/刷新/新窗口，分组显示）────
const splitPaneRef = ref<InstanceType<typeof SplitPane> | null>(null)
const { refresh: refreshCurrentTab } = useRefresh()

function trTab(title: string): string {
  return te(title) ? t(title) : title
}

/** 可替换为的标签：其它已打开标签（排除分屏中的两个页面） */
const splitTabOptions = computed(() =>
  tabbarStore.tabs
    .filter(item => item.path !== splitView.leftPath && item.path !== splitView.rightPath)
    .map(item => ({ key: item.path, label: trTab(item.title) })),
)

/** 视觉侧 → 是否为锚定 pane（锚定默认在左，reversed 后在右） */
function sideIsAnchor(side: 'left' | 'right'): boolean {
  return side === 'left' ? !splitView.reversed : splitView.reversed
}

function sidePath(side: 'left' | 'right'): string {
  return sideIsAnchor(side) ? splitView.leftPath : splitView.rightPath
}

/** 替换某一侧页面：锚定侧需导航（watcher 在路由抵达时对齐）；副侧直接换渲染组件 */
function onSideSelect(side: 'left' | 'right', key: string | number): void {
  const path = String(key)
  if (sideIsAnchor(side)) {
    pendingAnchorSwitch.value = path
    void router.push(path)
  }
  else {
    splitView.setRightPath(path)
  }
}

/** 刷新某一侧：锚定侧走标签刷新机制（当前路由），副侧重建渲染组件 */
function onSideReload(side: 'left' | 'right'): void {
  if (sideIsAnchor(side)) {
    refreshCurrentTab()
  }
  else {
    splitPaneRef.value?.reload()
  }
}

/** 新窗口打开某一侧页面 */
function onSideOpen(side: 'left' | 'right'): void {
  openTabInNewWindow(sidePath(side))
}

// ── 左右互换（收缩成页面大图标 → 图标交叉飞行 → 展开为交换后内容）──
// 互换 = 翻转视觉顺序（CSS order），两侧组件实例原地保留——不导航、不重挂载、不刷新。
// shrink：两屏向中心收缩淡出、各自中央弹出页面大图标 → fly：两枚图标互飞对方位置
// →（翻转 order，幕后瞬时完成）→ expand：图标弹出消失、两屏展开
const swapPhase = ref<'idle' | 'shrink' | 'fly' | 'expand'>('idle')
const swapCenters = ref({ left: { x: 0, y: 0 }, right: { x: 0, y: 0 } })
const swapIcons = ref({ left: 'lucide:file', right: 'lucide:file' })

function resolveTabIcon(path: string): string {
  const icon = tabbarStore.tabs.find(tab => tab.path === path)?.meta?.icon as string | undefined
  if (!icon) {
    return 'lucide:file'
  }
  return icon.includes(':') ? icon : `lucide:${icon}`
}

/** 取 pane 中心点（相对分屏行），作为图标的停靠/飞行坐标 */
function paneCenter(selector: string): { x: number, y: number } {
  const row = splitRowRef.value
  const el = row?.querySelector(selector)
  if (!row || !el) {
    return { x: 0, y: 0 }
  }
  const rowRect = row.getBoundingClientRect()
  const rect = el.getBoundingClientRect()
  return { x: rect.left - rowRect.left + rect.width / 2, y: rect.top - rowRect.top + rect.height / 2 }
}

/** 图标位置：shrink 停在本侧中心，fly/expand 飞到对侧中心（transition 负责插值飞行） */
function swapIconTransform(side: 'left' | 'right'): string {
  const own = swapCenters.value[side]
  const other = swapCenters.value[side === 'left' ? 'right' : 'left']
  const pos = swapPhase.value === 'shrink' ? own : other
  return `translate(${pos.x}px, ${pos.y}px)`
}

function wait(ms: number): Promise<void> {
  return new Promise(resolve => setTimeout(resolve, ms))
}

async function swapSplitPanes(): Promise<void> {
  if (swapPhase.value !== 'idle' || !splitView.rightPath) {
    return
  }
  // 视觉侧中心/图标（锚定 pane 可能在视觉右侧）
  const anchorCenter = paneCenter('.split-anchor')
  const secondaryCenter = paneCenter('.split-secondary')
  const visualLeftIsAnchor = anchorCenter.x <= secondaryCenter.x
  swapCenters.value = visualLeftIsAnchor
    ? { left: anchorCenter, right: secondaryCenter }
    : { left: secondaryCenter, right: anchorCenter }
  swapIcons.value = {
    left: resolveTabIcon(visualLeftIsAnchor ? splitView.leftPath : splitView.rightPath),
    right: resolveTabIcon(visualLeftIsAnchor ? splitView.rightPath : splitView.leftPath),
  }
  swapPhase.value = 'shrink'
  await wait(260)
  swapPhase.value = 'fly'
  await wait(360)
  // 纯视觉交换：翻转 order，两侧实例原地保留（零刷新）
  splitView.toggleReversed()
  swapPhase.value = 'expand'
  await wait(260)
  swapPhase.value = 'idle'
}

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
    shell.isMobile.value
    || (!shell.isHeaderNav.value && !shell.isFullContent.value && shell.appStore.sidebarShow),
)
</script>

<template>
  <div class="relative flex min-h-full w-full">
    <!-- ==================== Sidebar ==================== -->
    <NConfigProvider
      v-if="sidebarEnableState"
      v-show="!shell.contentMaximized.value"
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
        :style="shell.headerWrapperStyle.value"
        class="overflow-hidden"
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
            <Icon :icon="shell.showSider.value ? 'lucide:panel-left-close' : 'lucide:panel-left-open'" width="18" height="18" />
          </XihanIconButton>

          <!-- 收藏夹（收藏常用菜单，跨端同步；可在偏好设置中开关） -->
          <AppFavorites v-if="shell.appStore.widgetFavorites" />

          <!-- Header content (flex-1 fills remaining header space) -->
          <NConfigProvider
            :theme="headerForceDark ? darkTheme : undefined"
            :theme-overrides="themeOverrides"
            class="flex min-w-0 flex-1 items-center"
          >
            <AppHeader :theme="headerTheme" />
          </NConfigProvider>
        </header>

        <!-- Tabbar：随「深色顶栏」一起暗化（与 header 同款：本地 dark class + Naive 暗色） -->
        <div
          v-if="shell.appStore.tabbarEnabled && !shell.isFullContent.value"
          :class="headerTheme"
          :style="shell.tabbarStyle.value"
        >
          <NConfigProvider
            :theme="headerForceDark ? darkTheme : undefined"
            :theme-overrides="themeOverrides"
          >
            <AppTabbar />
          </NConfigProvider>
        </div>
      </div>

      <!-- 通知横幅（顶栏之下、页面内容之上，块级推下内容） -->
      <NotificationBanner />

      <!-- Page content -->
      <div class="flex-1 overflow-hidden transition-[margin-top] duration-200" :style="[{ scrollbarGutter: 'stable' }, shell.contentStyle.value]">
        <!-- 普通内容 -->
        <div
          v-if="!showSplit"
          class="h-full overflow-auto"
          :class="{ 'xihan-compact-layout': shell.appStore.contentCompact }"
          :style="
            shell.appStore.contentCompact
              ? { maxWidth: `${shell.appStore.contentMaxWidth}px`, margin: '0 auto' }
              : {}
          "
        >
          <LayoutContentRenderer :transition-name="shell.transitionName.value" />
        </div>
        <!-- 分屏对照：锚定标签（主视图）+ 副标签（应用内直接渲染）；reversed 时仅交换视觉顺序 -->
        <div
          v-else
          ref="splitRowRef"
          class="relative flex h-full w-full overflow-hidden"
          :class="{ 'split-collapsed': swapPhase === 'shrink' || swapPhase === 'fly' }"
        >
          <div
            class="split-anchor h-full min-w-0 overflow-auto"
            :style="{
              flexBasis: `calc((100% - 6px) * ${splitView.reversed ? 1 - splitView.ratio : splitView.ratio})`,
              order: splitView.reversed ? 3 : 1,
            }"
          >
            <LayoutContentRenderer :transition-name="shell.transitionName.value" />
          </div>
          <div
            class="split-divider"
            :class="{ 'is-dragging': draggingDivider }"
            style="order: 2"
            @pointerdown="onDividerDown"
          >
            <!-- 分割线悬浮工具组：左/右页面各自的替换、刷新、新窗口 + 中部共享操作 -->
            <div class="split-tools" @pointerdown.stop>
              <span class="split-tools__label">{{ t('tabbar.split_left_label') }}</span>
              <NDropdown trigger="click" :options="splitTabOptions" @select="(key: string | number) => onSideSelect('left', key)">
                <button type="button" class="split-tools__btn" :title="t('tabbar.split_switch_left')">
                  <Icon icon="lucide:replace" width="16" height="16" />
                </button>
              </NDropdown>
              <button type="button" class="split-tools__btn" :title="t('tabbar.split_reload_left')" @click="onSideReload('left')">
                <Icon icon="lucide:rotate-cw" width="16" height="16" />
              </button>
              <button type="button" class="split-tools__btn" :title="t('tabbar.split_open_left')" @click="onSideOpen('left')">
                <Icon icon="lucide:external-link" width="16" height="16" />
              </button>

              <span class="split-tools__sep" />
              <span class="split-tools__grip" @pointerdown="onDividerDown">
                <Icon icon="lucide:grip-vertical" width="16" height="16" />
              </span>
              <button type="button" class="split-tools__btn" :title="t('tabbar.split_swap')" @click="swapSplitPanes">
                <Icon icon="lucide:arrow-left-right" width="16" height="16" />
              </button>
              <button type="button" class="split-tools__btn split-tools__btn--close" :title="t('tabbar.split_close')" @click="splitView.close()">
                <Icon icon="lucide:x" width="17" height="17" />
              </button>
              <span class="split-tools__sep" />

              <span class="split-tools__label">{{ t('tabbar.split_right_label') }}</span>
              <NDropdown trigger="click" :options="splitTabOptions" @select="(key: string | number) => onSideSelect('right', key)">
                <button type="button" class="split-tools__btn" :title="t('tabbar.split_switch_right')">
                  <Icon icon="lucide:replace" width="16" height="16" />
                </button>
              </NDropdown>
              <button type="button" class="split-tools__btn" :title="t('tabbar.split_reload_right')" @click="onSideReload('right')">
                <Icon icon="lucide:rotate-cw" width="16" height="16" />
              </button>
              <button type="button" class="split-tools__btn" :title="t('tabbar.split_open_right')" @click="onSideOpen('right')">
                <Icon icon="lucide:external-link" width="16" height="16" />
              </button>
            </div>
          </div>
          <div class="split-secondary h-full min-w-0 flex-1" :style="{ order: splitView.reversed ? 1 : 3 }">
            <SplitPane ref="splitPaneRef" />
          </div>
          <!-- 拖拽遮罩：拖拽期间接管指针，拖动丝滑 -->
          <div v-if="draggingDivider" class="split-drag-overlay" />

          <!-- 互换动画：两枚页面大图标（收缩时弹出 → 交叉飞行 → 展开时消散） -->
          <template v-if="swapPhase !== 'idle'">
            <div class="swap-icon" :style="{ transform: swapIconTransform('left') }">
              <div class="swap-icon__center">
                <div class="swap-icon__card" :class="swapPhase === 'expand' ? 'is-out' : 'is-in'">
                  <Icon :icon="swapIcons.left" width="30" height="30" />
                </div>
              </div>
            </div>
            <div class="swap-icon" :style="{ transform: swapIconTransform('right') }">
              <div class="swap-icon__center">
                <div class="swap-icon__card" :class="swapPhase === 'expand' ? 'is-out' : 'is-in'">
                  <Icon :icon="swapIcons.right" width="30" height="30" />
                </div>
              </div>
            </div>
          </template>
        </div>
      </div>

      <!-- Footer -->
      <footer
        v-if="shell.appStore.footerEnable && !shell.contentMaximized.value"
        :style="{
          minHeight: `${shell.footerHeight.value}px`,
          marginBottom: shell.isFullContent.value ? `-${shell.footerHeight.value}px` : '0',
          position: shell.appStore.footerFixed ? 'fixed' : 'static',
          width: shell.footerWidth.value,
          zIndex: shell.appStore.footerFixed ? 199 : undefined,
        }"
        class="footer-bar bottom-0 flex w-full border-t border-border bg-background text-xs text-muted-foreground transition-all duration-200"
        :class="shell.isMobile.value ? 'flex-col items-center justify-center gap-0.5 px-3 py-1' : 'flex-row items-center px-4'"
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
    <AppTabOverview />
    <XihanBackTop :scroll-y="shell.scrollY.value" />

    <!-- 通知展示分级：登录后弹窗 + 强制阅读拦截（teleport 到 body，位置不敏感） -->
    <NotificationGate />

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
/* 分屏分隔条 */
.split-divider {
  position: relative;
  flex: 0 0 6px;
  cursor: col-resize;
  background: hsl(var(--border));
  transition: background 0.15s ease;
}

.split-divider:hover,
.split-divider.is-dragging {
  background: hsl(var(--primary) / 50%);
}

/* 分割线悬浮工具组：垂直胶囊，悬浮在分隔条中央，不占两侧空间。
   默认隐藏，悬停分割线（含工具组自身）或拖拽中才显示 */
.split-tools {
  position: absolute;
  top: 50%;
  left: 50%;
  z-index: 21;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 3px;
  padding: 9px 5px;
  border-radius: 9999px;
  background: hsl(var(--background));
  border: 1px solid hsl(var(--border));
  box-shadow: 0 6px 22px hsl(var(--foreground) / 12%);
  transform: translate(-50%, -50%);
  cursor: default;
  opacity: 0;
  pointer-events: none;
  transition: opacity 0.18s ease;
}

.split-divider:hover .split-tools,
.split-divider.is-dragging .split-tools {
  opacity: 1;
  pointer-events: auto;
}

/* 左/右分组小标签 */
.split-tools__label {
  padding: 2px 0;
  font-size: 12px;
  line-height: 1;
  color: hsl(var(--muted-foreground));
  user-select: none;
}

/* 分组分隔线 */
.split-tools__sep {
  width: 20px;
  height: 1px;
  margin: 4px 0;
  background: hsl(var(--border));
}

/* 拖拽手柄：工具组里也能拖（事件不 stop，转给分隔条逻辑） */
.split-tools__grip {
  display: inline-flex;
  padding: 2px 0;
  color: hsl(var(--muted-foreground));
  cursor: col-resize;
}

.split-tools__btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  border: none;
  border-radius: 9999px;
  background: transparent;
  color: hsl(var(--foreground) / 65%);
  cursor: pointer;
  transition:
    background 0.15s ease,
    color 0.15s ease;
}

.split-tools__btn:hover {
  background: hsl(var(--accent));
  color: hsl(var(--foreground));
}

.split-tools__btn--close:hover {
  background: hsl(var(--destructive) / 12%);
  color: hsl(var(--destructive));
}

/* 拖拽遮罩：覆盖整个分屏行，拖拽期间接管指针避免卡顿 */
.split-drag-overlay {
  position: absolute;
  inset: 0;
  z-index: 20;
  cursor: col-resize;
}

/* 左右互换动画：两屏向中心收缩淡出（split-collapsed），expand 时过渡回原状 */
.split-anchor,
.split-secondary {
  transform-origin: center;
  transition:
    transform 0.24s cubic-bezier(0.4, 0, 0.2, 1),
    opacity 0.24s cubic-bezier(0.4, 0, 0.2, 1);
}

.split-collapsed .split-anchor,
.split-collapsed .split-secondary {
  transform: scale(0.62);
  opacity: 0;
  pointer-events: none;
}

/* 飞行图标：外层（.swap-icon）负责定位与飞行（transition transform），
   内层卡片（__card）负责弹入/消散动画——分层避免 animation 覆盖 transform 定位 */
.swap-icon {
  position: absolute;
  left: 0;
  top: 0;
  z-index: 22;
  pointer-events: none;
  transition: transform 0.34s cubic-bezier(0.45, 0.05, 0.25, 1);
}

.swap-icon__center {
  transform: translate(-50%, -50%);
}

.swap-icon__card {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 64px;
  height: 64px;
  border-radius: 16px;
  background: hsl(var(--background));
  border: 1px solid hsl(var(--border));
  box-shadow: 0 12px 32px hsl(var(--foreground) / 15%);
  color: hsl(var(--primary));
}

.swap-icon__card.is-in {
  animation: swap-card-in 0.24s cubic-bezier(0.22, 1.3, 0.36, 1) both;
}

.swap-icon__card.is-out {
  animation: swap-card-out 0.22s ease both;
}

@keyframes swap-card-in {
  from {
    transform: scale(0.3);
    opacity: 0;
  }
}

@keyframes swap-card-out {
  to {
    transform: scale(0.4);
    opacity: 0;
  }
}

.footer-bar {
  gap: 8px;
}

.footer-bar.flex-col {
  gap: 4px;
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
