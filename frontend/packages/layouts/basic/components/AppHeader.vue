<script lang="ts" setup>
import type { DropdownOption, MenuOption } from 'naive-ui'
import { Icon } from '@iconify/vue'
import {
  useMessage,
} from 'naive-ui'
import { computed, h, onBeforeUnmount, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { useContentMaximize, useLocale, useRefresh, useTheme } from '~/hooks'
import { useAccessStore, useAppStore, useAuthStore, useUserStore } from '~/stores'
import HeaderNav from './header/HeaderNav.vue'
import HeaderToolbar from './header/HeaderToolbar.vue'

interface HeaderRouteItem {
  path: string
  name?: string
  meta?: {
    hidden?: boolean
    title?: string
    icon?: string
  }
  children?: HeaderRouteItem[]
}

defineOptions({ name: 'AppHeader' })

const route = useRoute()
const router = useRouter()
const accessStore = useAccessStore()
const appStore = useAppStore()
const userStore = useUserStore()
const authStore = useAuthStore()
const { t, te } = useI18n()
const message = useMessage()
const { isDark, toggleThemeWithTransition } = useTheme()
const { setLocale } = useLocale()
const { contentIsMaximize: contentMaximized } = useContentMaximize()
const { refresh: doRefresh } = useRefresh()
const isFullscreen = ref(false)
const viewportWidth = ref(typeof window !== 'undefined' ? window.innerWidth : 1200)
const isNarrowScreen = computed(() => viewportWidth.value < 960)
// 与 FAB 互斥：窄屏或内容最大化时 FAB 显示，头部按钮隐藏
const showPreferencesInHeader = computed(() => !isNarrowScreen.value && !contentMaximized.value)
const currentTimezone = ref(
  typeof Intl !== 'undefined' ? Intl.DateTimeFormat().resolvedOptions().timeZone : 'UTC',
)

const isTopNavLayout = computed(() => appStore.layoutMode === 'top')
const isMixedNavLayout = computed(() => appStore.layoutMode === 'mix')
const isHeaderMixedLayout = computed(() => appStore.layoutMode === 'header-mix')
const showTopMenu = computed(() =>
  isTopNavLayout.value || isMixedNavLayout.value || isHeaderMixedLayout.value,
)
const headerBrandTitle = computed(
  () => appStore.brandTitle || import.meta.env.VITE_APP_TITLE || 'XiHan Admin',
)
const headerBrandLogo = computed(
  () => appStore.brandLogo || import.meta.env.VITE_APP_LOGO || '/favicon.png',
)

const topMenuSource = computed<HeaderRouteItem[]>(() => {
  if (isTopNavLayout.value) {
    return (router.options.routes.find(item => item.path === '/')?.children ?? []) as HeaderRouteItem[]
  }
  if (accessStore.accessRoutes.length) {
    return accessStore.accessRoutes as unknown as HeaderRouteItem[]
  }
  return (router.options.routes.find(item => item.path === '/')?.children ?? []) as HeaderRouteItem[]
})

function toRouteNameKey(name: string | number | symbol | null | undefined) {
  return typeof name === 'string' || typeof name === 'number' ? String(name) : undefined
}

function findVisibleChild(routeItem: HeaderRouteItem) {
  return routeItem.children?.find(child => !child.meta?.hidden)
}

function resolveFullPath(path: string, parentPath = '') {
  if (!path) {
    return parentPath || '/'
  }
  if (path.startsWith('/')) {
    return path
  }
  return `${parentPath.replace(/\/$/, '')}/${path}`.replace(/\/{2,}/g, '/')
}

function buildTopTreeOptions(routeList: HeaderRouteItem[], parentPath = ''): MenuOption[] {
  return routeList
    .filter(item => !item.meta?.hidden)
    .map((item) => {
      const fullPath = resolveFullPath(item.path, parentPath)
      const iconName = item.meta?.icon
      const titleKey = String(item.meta?.title ?? toRouteNameKey(item.name) ?? fullPath)
      const visibleChildren = item.children?.filter(child => !child.meta?.hidden) ?? []
      return {
        key: fullPath,
        label: te(titleKey) ? t(titleKey) : titleKey,
        icon: iconName ? () => h(Icon, { icon: iconName }) : undefined,
        children: visibleChildren.length ? buildTopTreeOptions(visibleChildren, fullPath) : undefined,
      } as MenuOption
    })
    .filter(Boolean) as MenuOption[]
}

const topMenuOptions = computed<MenuOption[]>(() => {
  if (isTopNavLayout.value) {
    return buildTopTreeOptions(topMenuSource.value, '')
  }
  return topMenuSource.value
    .filter(item => !item.meta?.hidden)
    .map((item) => {
      const key = toRouteNameKey(item.name)
      if (!key) {
        return undefined
      }
      const iconName = item.meta?.icon
      const titleKey = String(item.meta?.title ?? key)
      return {
        key,
        label: te(titleKey) ? t(titleKey) : titleKey,
        icon: iconName ? () => h(Icon, { icon: iconName }) : undefined,
      } as MenuOption
    })
    .filter(Boolean) as MenuOption[]
})

const topMenuActive = computed(() => {
  if (isTopNavLayout.value) {
    return route.path
  }
  const routeMatched = route.matched.find((matchedRoute) => {
    const matchedName = toRouteNameKey(matchedRoute.name)
    return Boolean(
      matchedName
      && topMenuSource.value.some(item => toRouteNameKey(item.name) === matchedName),
    )
  })
  return toRouteNameKey(routeMatched?.name)
    ?? toRouteNameKey(topMenuSource.value.find(item => !item.meta?.hidden)?.name)
})

const breadcrumbs = computed(() => {
  const matched = route.matched.filter(r => r.meta?.title && !r.meta?.hidden)
  if (appStore.breadcrumbHideOnlyOne && matched.length <= 1) {
    return []
  }
  return matched.map((r, index) => {
    const parent = index > 0 ? matched[index - 1] : null
    const siblings = (parent?.children ?? [])
      .filter(item => item.meta?.title && !item.meta?.hidden)
      .map(item => ({
        key: item.path.startsWith('/') ? item.path : `${parent?.path ?? ''}/${item.path}`,
        label: te(String(item.meta?.title)) ? t(String(item.meta?.title)) : String(item.meta?.title),
        icon: item.meta?.icon ? () => h(Icon, { icon: item.meta?.icon as string }) : undefined,
      }))

    const titleKey = String(r.meta.title)
    return {
      title: te(titleKey) ? t(titleKey) : titleKey,
      path: r.path,
      icon: appStore.breadcrumbShowIcon ? (r.meta.icon as string | undefined) : undefined,
      siblings,
    }
  })
})

const userOptions = computed<DropdownOption[]>(() => {
  return [
    {
      label: t('header.user.profile'),
      key: 'profile',
      icon: () => h(Icon, { icon: 'lucide:user' }),
    },
    ...(appStore.widgetLockScreen
      ? [
          {
            label: () => h('span', { style: 'display:inline-flex;align-items:center;gap:6px' }, [
              h('span', t('header.user.lock')),
              ...(appStore.shortcutEnable && appStore.shortcutLock
                ? [h('kbd', {
                    style: 'display:inline-flex;align-items:center;padding:1px 6px;font-size:11px;'
                      + 'font-family:ui-monospace,SFMono-Regular,monospace;color:hsl(var(--muted-foreground));'
                      + 'background:hsl(var(--muted));border:1px solid hsl(var(--border));border-radius:4px;'
                      + 'line-height:1.6;white-space:nowrap;',
                  }, 'Alt L')]
                : []),
            ]),
            key: 'lock',
            icon: () => h(Icon, { icon: 'lucide:lock' }),
          } as DropdownOption,
        ]
      : []),
    {
      type: 'divider',
      key: 'divider',
    },
    {
      label: () => h('span', { style: 'display:inline-flex;align-items:center;gap:6px' }, [
        h('span', t('header.user.logout')),
        ...(appStore.shortcutEnable && appStore.shortcutLogout
          ? [h('kbd', {
              style: 'display:inline-flex;align-items:center;padding:1px 6px;font-size:11px;'
                + 'font-family:ui-monospace,SFMono-Regular,monospace;color:hsl(var(--muted-foreground));'
                + 'background:hsl(var(--muted));border:1px solid hsl(var(--border));border-radius:4px;'
                + 'line-height:1.6;white-space:nowrap;',
            }, 'Alt Q')]
          : []),
      ]),
      key: 'logout',
      icon: () => h(Icon, { icon: 'lucide:log-out' }),
    },
  ]
})

const localeOptions = computed(() => [
  { label: t('header.locale.zh_cn'), key: 'zh-CN' },
  { label: t('header.locale.en_us'), key: 'en-US' },
])

const timezoneOptions = computed<DropdownOption[]>(() => {
  return [
    { label: t('header.timezone.utc'), key: 'UTC' },
    { label: t('header.timezone.shanghai'), key: 'Asia/Shanghai' },
    { label: t('header.timezone.tokyo'), key: 'Asia/Tokyo' },
    { label: t('header.timezone.london'), key: 'Europe/London' },
    { label: t('header.timezone.new_york'), key: 'America/New_York' },
    { label: t('header.timezone.los_angeles'), key: 'America/Los_Angeles' },
  ]
})

function handleUserAction(key: string) {
  if (key === 'logout') {
    authStore.logout()
  }
  else if (key === 'profile') {
    router.push('/profile')
  }
  else if (key === 'lock') {
    handleLockScreen()
  }
}

function handleLocaleChange(key: string) {
  setLocale(key)
}

function handleTimezoneChange(timezone: string) {
  currentTimezone.value = timezone
  localStorage.setItem('xihan_app_timezone', timezone)
  message.success(t('header.timezone.switch_success', { timezone }))
}

function handleThemeToggle(e: MouseEvent) {
  toggleThemeWithTransition(e)
}

function handleBreadcrumbSelect(path: string) {
  if (path && path !== route.path) {
    router.push(path)
  }
}

function handleTopMenuSelect(path: string) {
  if (isTopNavLayout.value) {
    if (path && path !== route.path) {
      router.push(path)
    }
    return
  }
  const rootMenu = topMenuSource.value.find(item => toRouteNameKey(item.name) === path)
  if (!rootMenu) {
    return
  }
  const firstVisibleChild = findVisibleChild(rootMenu)
  const targetName = toRouteNameKey(firstVisibleChild?.name ?? rootMenu.name)
  if (targetName && targetName !== toRouteNameKey(route.name)) {
    router.push({ name: targetName })
  }
}

function handleLockScreen() {
  window.dispatchEvent(new CustomEvent('xihan-lock-screen'))
}

function handleSidebarToggle() {
  window.dispatchEvent(new CustomEvent('xihan-toggle-sidebar-request'))
}

function handleRefreshCurrentTab() {
  doRefresh()
}

function openPreferenceDrawer() {
  window.dispatchEvent(new CustomEvent('xihan-open-preference-drawer'))
}

function syncFullscreenState() {
  isFullscreen.value = Boolean(document.fullscreenElement)
}

function toggleFullscreen() {
  if (document.fullscreenElement) {
    document.exitFullscreen()
  }
  else {
    document.documentElement.requestFullscreen()
  }
}

function updateViewportWidth() {
  viewportWidth.value = window.innerWidth
}

onMounted(() => {
  const savedTimezone = localStorage.getItem('xihan_app_timezone')
  if (savedTimezone) {
    currentTimezone.value = savedTimezone
  }
  syncFullscreenState()
  document.addEventListener('fullscreenchange', syncFullscreenState)
  window.addEventListener('resize', updateViewportWidth)
})

onBeforeUnmount(() => {
  document.removeEventListener('fullscreenchange', syncFullscreenState)
  window.removeEventListener('resize', updateViewportWidth)
})
</script>

<template>
  <div class="flex h-14 min-w-0 items-center justify-between gap-2 border-b border-border bg-header px-3">
    <HeaderNav
      :app-store="appStore"
      :layout-mode="appStore.layoutMode"
      :app-title="headerBrandTitle"
      :app-logo="headerBrandLogo"
      :show-top-menu="showTopMenu"
      :breadcrumbs="breadcrumbs"
      :top-menu-active="topMenuActive"
      :top-menu-options="topMenuOptions"
      @sidebar-toggle="handleSidebarToggle"
      @refresh="handleRefreshCurrentTab"
      @breadcrumb-select="handleBreadcrumbSelect"
      @top-menu-select="handleTopMenuSelect"
      @home-click="router.push('/')"
    />

    <HeaderToolbar
      :app-store="appStore"
      :user-store="userStore"
      :is-dark="isDark"
      :is-fullscreen="isFullscreen"
      :show-preferences-in-header="showPreferencesInHeader"
      :timezone-options="timezoneOptions"
      :locale-options="localeOptions"
      :user-options="userOptions"
      @locale-change="handleLocaleChange"
      @timezone-change="handleTimezoneChange"
      @theme-toggle="handleThemeToggle"
      @notification="message.info(t('header.notification.pending'))"
      @fullscreen-toggle="toggleFullscreen"
      @preferences-open="openPreferenceDrawer"
      @user-action="handleUserAction"
    />
  </div>
</template>

