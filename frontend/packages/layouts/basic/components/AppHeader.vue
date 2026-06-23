<script lang="ts" setup>
import type { DropdownOption, MenuGroupOption, MenuOption } from 'naive-ui'
import type { VNodeChild } from 'vue'
import type { LayoutRouteRecord } from '../contracts'
import { NMenu } from 'naive-ui'
import { computed, h, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useTheme } from '~/hooks'
import { Icon } from '~/iconify'
import { useAppContext, useAppStore, useAuthStore, useLayoutBridgeStore, useNotificationStore, useUserStore } from '~/stores'
import { NotificationStatus } from '~/types/enums'
import { useLayoutMenuDomain, usePreferenceEntry } from '../composables'
import HeaderNav from './header/HeaderNav.vue'
import HeaderToolbar from './header/HeaderToolbar.vue'
import { renderHorizontalBadgeLabel } from './MenuBadge.vue'
import XihanIconButton from './XihanIconButton.vue'

interface Props {
  theme?: string
}

defineOptions({ name: 'AppHeader' })
withDefaults(defineProps<Props>(), { theme: 'light' })

const appStore = useAppStore()
const userStore = useUserStore()
const authStore = useAuthStore()
const layoutBridgeStore = useLayoutBridgeStore()
const notificationStore = useNotificationStore()
const appContext = useAppContext()
const { t, te } = useI18n()
const { isDark, toggleThemeWithTransition } = useTheme()
const {
  route,
  router,
  baseMenuSource,
  toLayoutMeta,
  resolveFullPath,
  resolveFirstNavigablePath,
  buildMenuOptionsFromRoutes,
  findMatchedRoutePath,
  openExternalIfMatch,
} = useLayoutMenuDomain()

// 偏好设置入口可见性：头部按钮与悬浮 FAB 互斥（auto 模式窄屏走 FAB，头部按钮隐藏）
const { showHeaderButton: showPreferencesInHeader } = usePreferenceEntry()

const hasBack = ref(false)
const hasForward = ref(false)

function updateHistoryState() {
  const state = window.history.state
  hasBack.value = state?.back != null
  hasForward.value = state?.forward != null
}

const canGoBack = computed(() => hasBack.value)
const canGoForward = computed(() => hasForward.value)

const isFullscreen = ref(false)

const isTopNavLayout = computed(() => appStore.layoutMode === 'top')
const isMixedNavLayout = computed(() => appStore.layoutMode === 'mix')
const isHeaderMixedLayout = computed(() => appStore.layoutMode === 'header-mix')
const showTopMenu = computed(
  () => isTopNavLayout.value || isMixedNavLayout.value || isHeaderMixedLayout.value,
)
const showBreadcrumb = computed(() => !showTopMenu.value && appStore.breadcrumbEnabled)

const isSplitMode = computed(
  () => (appStore.navigationSplit && isMixedNavLayout.value) || isHeaderMixedLayout.value,
)

const topMenuSource = computed<LayoutRouteRecord[]>(() => baseMenuSource.value)

function resolveIcon(icon: string) {
  if (!icon)
    return icon
  return icon.includes(':') ? icon : `lucide:${icon}`
}

function renderRouteIcon(icon: string) {
  return () => h(Icon, { icon: resolveIcon(icon) })
}

function renderTopMenuLabel(option: MenuOption | MenuGroupOption): VNodeChild {
  const rawLabel = option.label
  const label = typeof rawLabel === 'function' ? rawLabel(option) : rawLabel
  const children = (option as MenuOption).children
  const hasChildren = Array.isArray(children) && children.length > 0
  const key = (option as MenuOption).key
  // eslint-disable-next-line ts/no-use-before-define
  const isTopLevel = key != null && topLevelKeys.value.has(key)
  if (!hasChildren || !isTopLevel) {
    return label
  }
  return h('span', { class: 'inline-flex items-center gap-1' }, [
    label,
    h(Icon, {
      icon: 'lucide:chevron-down',
      class: 'size-6 shrink-0 opacity-70',
    }),
  ])
}

function translateMenuTitle(title: string, _fallback: string) {
  return te(title) ? t(title) : title
}

const topMenuOptions = computed<MenuOption[]>(() => {
  const options = buildMenuOptionsFromRoutes(topMenuSource.value, {
    keyBy: 'path',
    translate: translateMenuTitle,
    iconRenderer: renderRouteIcon,
    badgeLabelRenderer: renderHorizontalBadgeLabel,
  })
  if (isSplitMode.value) {
    return options.map(item => ({ ...item, children: undefined }))
  }
  return options
})

const topLevelKeys = computed(() => new Set(
  topMenuOptions.value.map((opt: MenuOption) => opt.key).filter(Boolean),
))

const topMenuActive = computed(() => {
  if (!isSplitMode.value) {
    return String(route.meta?.activePath || route.path || '')
  }
  return (
    findMatchedRoutePath(topMenuSource.value)
    ?? resolveFullPath(topMenuSource.value.find(item => !toLayoutMeta(item).hidden)?.path ?? '')
  )
})

const breadcrumbs = computed(() => {
  const matched = route.matched.filter(item => item.meta?.title && !item.meta?.hidden)
  if (appStore.breadcrumbHideOnlyOne && matched.length <= 1) {
    return []
  }
  return matched.map((item, index) => {
    const parent = index > 0 ? matched[index - 1] : null
    const siblings = (parent?.children ?? [])
      .filter(sibling => sibling.meta?.title && !sibling.meta?.hidden)
      .map((sibling) => {
        const siblingTitle = String(sibling.meta?.title ?? '')
        const siblingIcon = sibling.meta?.icon as string | undefined
        return {
          key: resolveFullPath(sibling.path, parent?.path ?? ''),
          label: te(siblingTitle) ? t(siblingTitle) : siblingTitle,
          icon: siblingIcon ? () => h(Icon, { icon: resolveIcon(siblingIcon) }) : undefined,
        }
      })
    const titleKey = String(item.meta?.title)
    return {
      title: te(titleKey) ? t(titleKey) : titleKey,
      path: item.path,
      icon: appStore.breadcrumbShowIcon ? (item.meta?.icon as string | undefined) : undefined,
      siblings,
    }
  })
})

const shortcutKbdStyle = [
  'display:inline-flex',
  'align-items:center',
  'padding:1px 6px',
  'font-size:11px',
  'font-family:ui-monospace,SFMono-Regular,monospace',
  'color:hsl(var(--muted-foreground))',
  'background:hsl(var(--muted))',
  'border:1px solid hsl(var(--border))',
  'border-radius:4px',
  'line-height:1.6',
  'white-space:nowrap',
].join(';')

const userOptions = computed<DropdownOption[]>(() => [
  {
    label: t('header.user.profile'),
    key: 'profile',
    icon: () => h(Icon, { icon: 'lucide:user' }),
  },
  {
    // 切换租户 / 平台管理：打开控制中心（独立公共页，不进标签栏）
    label: t('header.user.switch_tenant'),
    key: 'control-center',
    icon: () => h(Icon, { icon: 'lucide:building-2' }),
  },
  ...(appStore.widgetLockScreen
    ? [
        {
          label: () =>
            h('span', { style: 'display:inline-flex;align-items:center;gap:6px' }, [
              h('span', t('header.user.lock')),
              ...(appStore.shortcutEnable && appStore.shortcutLock
                ? [h('kbd', { style: shortcutKbdStyle }, 'Alt L')]
                : []),
            ]),
          key: 'lock',
          icon: () => h(Icon, { icon: 'lucide:lock' }),
        } as DropdownOption,
      ]
    : []),
  { type: 'divider', key: 'divider' },
  {
    label: () =>
      h('span', { style: 'display:inline-flex;align-items:center;gap:6px' }, [
        h('span', t('header.user.logout')),
        ...(appStore.shortcutEnable && appStore.shortcutLogout
          ? [h('kbd', { style: shortcutKbdStyle }, 'Alt Q')]
          : []),
      ]),
    key: 'logout',
    icon: () => h(Icon, { icon: 'lucide:log-out' }),
  },
])

async function handleUserAction(key: string) {
  if (key === 'logout') {
    await authStore.logout()
    return
  }
  if (key === 'profile') {
    router.push('/workbench/profile')
    return
  }
  if (key === 'control-center') {
    router.push('/control-center')
    return
  }
  if (key === 'lock') {
    layoutBridgeStore.requestLockScreen()
  }
}

function handleThemeToggle(event: MouseEvent) {
  toggleThemeWithTransition(event)
}

function handleBreadcrumbSelect(path: string) {
  if (path && path !== route.path) {
    router.push(path)
  }
}

function handleTopMenuSelect(path: string) {
  if (!path || path === route.path) {
    return
  }
  if (openExternalIfMatch(path)) {
    return
  }
  if (!isSplitMode.value) {
    router.push(path)
    return
  }
  const rootMenu = topMenuSource.value.find(item => resolveFullPath(item.path) === path)
  if (!rootMenu) {
    return
  }
  const targetPath = resolveFirstNavigablePath(rootMenu)
  if (targetPath && targetPath !== route.path) {
    router.push(targetPath)
  }
}

/** 整页刷新（等效浏览器刷新 / F5），区别于标签页级的软刷新 */
function handleReloadPage() {
  window.location.reload()
}

function openPreferenceDrawer() {
  layoutBridgeStore.requestOpenPreferenceDrawer()
}

// ===== 通知弹窗 =====
const NOTIFICATION_RECEIVED_EVENT = 'xihan:notification-received'
let signalRThrottleTimer: ReturnType<typeof setTimeout> | null = null

async function loadNotifications() {
  const userId = userStore.userInfo?.basicId
  if (!userId)
    return
  notificationStore.loading = true
  try {
    const list = await appContext.apis.userInboxApi.list(userId, true, userStore.userInfo?.tenantId)
    notificationStore.setItems(list.map(n => ({
      basicId: n.basicId,
      title: n.title,
      content: n.content ?? undefined,
      notificationType: n.notificationType,
      notificationStatus: n.notificationStatus,
      sendTime: n.sendTime,
      readTime: n.readTime ?? undefined,
      confirmTime: n.confirmTime ?? undefined,
      isGlobal: n.isGlobal,
      needConfirm: n.needConfirm,
      icon: n.icon ?? undefined,
      link: n.link ?? undefined,
    })))
  }
  catch {
    // 静默失败，不阻塞主流程
  }
  finally {
    notificationStore.loading = false
  }
}

async function handleNotificationMarkRead(id: string) {
  const userId = userStore.userInfo?.basicId
  if (!userId)
    return
  const prev = notificationStore.items.find(n => n.basicId === id)
  const prevStatus = prev?.notificationStatus
  const prevReadTime = prev?.readTime
  notificationStore.markItemRead(id)
  try {
    await appContext.apis.userInboxApi.markRead(id, userId, userStore.userInfo?.tenantId)
  }
  catch {
    if (prev && prevStatus !== undefined) {
      prev.notificationStatus = prevStatus
      prev.readTime = prevReadTime
    }
  }
}

async function handleNotificationConfirm(id: string) {
  const userId = userStore.userInfo?.basicId
  if (!userId)
    return
  notificationStore.markItemConfirmed(id)
  try {
    await appContext.apis.userInboxApi.confirm(id, userId, userStore.userInfo?.tenantId)
  }
  catch {
    await loadNotifications()
  }
}

async function handleNotificationMarkAllRead() {
  const userId = userStore.userInfo?.basicId
  if (!userId)
    return
  const snapshot = notificationStore.items
    .filter(n => n.notificationStatus === NotificationStatus.Unread)
    .map(n => ({ id: n.basicId, status: n.notificationStatus, readTime: n.readTime }))
  notificationStore.markAllRead()
  try {
    await appContext.apis.userInboxApi.markAllRead(userId, userStore.userInfo?.tenantId)
  }
  catch {
    for (const s of snapshot) {
      const item = notificationStore.items.find(n => n.basicId === s.id)
      if (item) {
        item.notificationStatus = s.status
        item.readTime = s.readTime
      }
    }
  }
}

function handleNotificationViewAll() {
  router.push('/workbench/inbox')
}

// SignalR 推送节流：2 秒内多次推送只触发一次全量刷新
function handleSignalRNotification() {
  if (signalRThrottleTimer)
    return
  signalRThrottleTimer = setTimeout(() => {
    signalRThrottleTimer = null
    loadNotifications()
  }, 2000)
}

function syncFullscreenState() {
  isFullscreen.value = Boolean(document.fullscreenElement)
}

function toggleFullscreen() {
  if (document.fullscreenElement) {
    document.exitFullscreen()
    return
  }
  document.documentElement.requestFullscreen()
}

onMounted(() => {
  syncFullscreenState()
  document.addEventListener('fullscreenchange', syncFullscreenState)
  updateHistoryState()
  window.addEventListener('popstate', updateHistoryState)
  // 加载通知 & 监听 SignalR 推送
  loadNotifications()
  window.addEventListener(NOTIFICATION_RECEIVED_EVENT, handleSignalRNotification)
})

onBeforeUnmount(() => {
  document.removeEventListener('fullscreenchange', syncFullscreenState)
  window.removeEventListener('popstate', updateHistoryState)
  window.removeEventListener(NOTIFICATION_RECEIVED_EVENT, handleSignalRNotification)
})

watch(() => route.fullPath, () => {
  nextTick(() => updateHistoryState())
})
</script>

<template>
  <!-- Back / Forward buttons -->
  <template v-if="appStore.breadcrumbNavButtons">
    <XihanIconButton
      class="my-0 rounded-md"
      :disabled="!canGoBack"
      @click="router.back()"
    >
      <Icon icon="lucide:arrow-left" class="size-4" />
    </XihanIconButton>
    <XihanIconButton
      class="my-0 rounded-md"
      :disabled="!canGoForward"
      @click="router.forward()"
    >
      <Icon icon="lucide:arrow-right" class="size-4" />
    </XihanIconButton>
  </template>

  <!-- Refresh button (left widget) — 整页刷新，等效浏览器 F5 -->
  <XihanIconButton
    v-if="appStore.widgetRefresh"
    class="my-0 mr-1 rounded-md"
    :tooltip="t('header.toolbar.refresh_page')"
    @click="handleReloadPage"
  >
    <Icon icon="lucide:refresh-cw" class="size-4" />
  </XihanIconButton>

  <!-- Breadcrumb -->
  <div v-if="showBreadcrumb" class="hidden flex-center lg:block">
    <HeaderNav
      :app-store="appStore"
      :breadcrumbs="breadcrumbs"
      @breadcrumb-select="handleBreadcrumbSelect"
      @home-click="router.push('/')"
    />
  </div>

  <!-- Menu area -->
  <div :class="`menu-align-${appStore.headerMenuAlign}`" class="flex flex-1 items-center min-w-0">
    <div v-if="showTopMenu" class="hidden items-center min-w-0 xihan-top-menu lg:flex">
      <NMenu
        mode="horizontal"
        :value="topMenuActive"
        :options="topMenuOptions"
        :render-label="renderTopMenuLabel"
        @update:value="(key: string | number) => handleTopMenuSelect(String(key))"
      />
    </div>
  </div>

  <!-- Right toolbar widgets -->
  <HeaderToolbar
    :app-store="appStore"
    :user-store="userStore"
    :is-dark="isDark"
    :is-fullscreen="isFullscreen"
    :show-preferences-in-header="showPreferencesInHeader"
    :user-options="userOptions"
    :notification-all-items="notificationStore.allItems"
    :notification-mentioned-items="notificationStore.mentionedItems"
    :notification-unread-all="notificationStore.unreadAll"
    :notification-unread-mentioned="notificationStore.unreadMentioned"
    :notification-unread-count="notificationStore.unreadCount"
    :notification-loading="notificationStore.loading"
    @theme-toggle="handleThemeToggle"
    @notification-mark-read="handleNotificationMarkRead"
    @notification-confirm="handleNotificationConfirm"
    @notification-mark-all-read="handleNotificationMarkAllRead"
    @notification-view-all="handleNotificationViewAll"
    @notification-refresh="loadNotifications"
    @fullscreen-toggle="toggleFullscreen"
    @preferences-open="openPreferenceDrawer"
    @user-action="handleUserAction"
  />
</template>

<style>
.menu-align-start {
  justify-content: flex-start;
}

.menu-align-center {
  justify-content: center;
}

.menu-align-end {
  justify-content: flex-end;
}

.xihan-top-menu .n-menu.n-menu--horizontal {
  --n-item-height: 40px;
  --n-item-font-size-horizontal: 14px;
  --n-item-text-color-horizontal: hsl(var(--foreground) / 80%);
  --n-item-text-color-hover-horizontal: hsl(var(--foreground));
  --n-item-text-color-active-horizontal: hsl(var(--primary));
  --n-item-color-active-horizontal: hsl(var(--primary) / 15%);
  --n-item-color-hover-horizontal: hsl(var(--accent));
  height: auto;
  align-items: center;
  background: transparent;
}

.xihan-top-menu .n-menu.n-menu--horizontal > .n-submenu,
.xihan-top-menu .n-menu.n-menu--horizontal > .n-menu-item,
.xihan-top-menu .n-menu.n-menu--horizontal > .n-submenu > .n-menu-item {
  display: flex;
  align-items: center;
}

.xihan-top-menu .n-menu.n-menu--horizontal .n-menu-item-content {
  display: flex;
  align-items: center;
}

.xihan-top-menu
  .n-menu.n-menu--horizontal
  > .n-submenu
  > .n-menu-item
  > .n-menu-item-content.n-menu-item-content--child-active,
.xihan-top-menu
  .n-menu.n-menu--horizontal
  > .n-submenu
  > .n-menu-item
  > .n-menu-item-content.n-menu-item-content--selected,
.xihan-top-menu .n-menu.n-menu--horizontal > .n-menu-item > .n-menu-item-content.n-menu-item-content--selected {
  background-color: transparent;
  border-radius: 6px;
}

.xihan-top-menu
  .n-menu.n-menu--horizontal
  > .n-submenu
  > .n-menu-item
  > .n-menu-item-content.n-menu-item-content--child-active::before,
.xihan-top-menu
  .n-menu.n-menu--horizontal
  > .n-submenu
  > .n-menu-item
  > .n-menu-item-content.n-menu-item-content--selected::before,
.xihan-top-menu .n-menu.n-menu--horizontal > .n-menu-item > .n-menu-item-content.n-menu-item-content--selected::before {
  background-color: hsl(var(--primary) / 0.15) !important;
  border-radius: 6px !important;
  box-shadow: none !important;
}
</style>
