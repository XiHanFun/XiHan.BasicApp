<script lang="ts" setup>
import type { DropdownOption, MenuOption } from 'naive-ui'
import { Icon } from '@iconify/vue'
import {
  useMessage,
} from 'naive-ui'
import { computed, h, onBeforeUnmount, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { routes } from '@/router/routes'
import { useAuthStore } from '@/store/auth'
import { useLocale, useTheme } from '~/hooks'
import { useAccessStore, useAppStore, useUserStore } from '~/stores'
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
const { t } = useI18n()
const message = useMessage()
const { isDark, toggleThemeWithTransition } = useTheme()
const { setLocale } = useLocale()
const isFullscreen = ref(false)
const currentTimezone = ref(
  typeof Intl !== 'undefined' ? Intl.DateTimeFormat().resolvedOptions().timeZone : 'UTC',
)

const showTopMenu = computed(() =>
  ['top', 'header-sidebar', 'header-mix'].includes(appStore.layoutMode),
)

const topMenuSource = computed<HeaderRouteItem[]>(() => {
  if (accessStore.accessRoutes.length) {
    return accessStore.accessRoutes as unknown as HeaderRouteItem[]
  }
  return (routes.find(item => item.path === '/')?.children ?? []) as HeaderRouteItem[]
})

const topMenuOptions = computed<MenuOption[]>(() => {
  return topMenuSource.value
    .filter(item => !item.meta?.hidden)
    .map((item) => {
      const firstChild = item.children?.find(child => !child.meta?.hidden)
      const iconName = item.meta?.icon
      const menuPath = firstChild
        ? firstChild.path.startsWith('/')
          ? firstChild.path
          : `${item.path}/${firstChild.path}`
        : item.path
      return {
        key: menuPath,
        label: t(String(item.meta?.title ?? item.name), String(item.meta?.title ?? item.name)),
        icon: iconName ? () => h(Icon, { icon: iconName }) : undefined,
      } as MenuOption
    })
})

const topMenuActive = computed(() => {
  const path = route.path
  const matched = topMenuOptions.value.find(
    item => path === String(item.key) || path.startsWith(`${String(item.key)}/`),
  )
  return matched?.key as string | undefined
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
        label: t(String(item.meta?.title), String(item.meta?.title)),
        icon: item.meta?.icon ? () => h(Icon, { icon: item.meta?.icon as string }) : undefined,
      }))

    return {
      title: t(String(r.meta.title), String(r.meta.title)),
      path: r.path,
      icon: appStore.breadcrumbShowIcon ? (r.meta.icon as string | undefined) : undefined,
      siblings,
    }
  })
})

const userOptions = computed<DropdownOption[]>(() => {
  return [
    {
      label: '个人中心',
      key: 'profile',
      icon: () => h(Icon, { icon: 'lucide:user' }),
    },
    ...(appStore.widgetLockScreen
      ? [
          {
            label: '锁屏',
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
      label: '退出登录',
      key: 'logout',
      icon: () => h(Icon, { icon: 'lucide:log-out' }),
    },
  ]
})

const localeOptions = [
  { label: '简体中文', key: 'zh-CN' },
  { label: 'English', key: 'en-US' },
]

const timezoneOptions = computed<DropdownOption[]>(() => {
  return [
    { label: 'UTC', key: 'UTC' },
    { label: '北京时间 (Asia/Shanghai)', key: 'Asia/Shanghai' },
    { label: '东京时间 (Asia/Tokyo)', key: 'Asia/Tokyo' },
    { label: '伦敦时间 (Europe/London)', key: 'Europe/London' },
    { label: '纽约时间 (America/New_York)', key: 'America/New_York' },
    { label: '洛杉矶时间 (America/Los_Angeles)', key: 'America/Los_Angeles' },
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
  message.success(`已切换时区：${timezone}`)
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
  if (path && path !== route.path) {
    router.push(path)
  }
}

function handleLockScreen() {
  message.info('锁屏功能待接入')
}

function handleSidebarToggle() {
  window.dispatchEvent(new CustomEvent('xihan-toggle-sidebar-request'))
}

function handleRefreshCurrentTab() {
  window.dispatchEvent(new CustomEvent('xihan-refresh-current-tab'))
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

onMounted(() => {
  const savedTimezone = localStorage.getItem('xihan_app_timezone')
  if (savedTimezone) {
    currentTimezone.value = savedTimezone
  }
  syncFullscreenState()
  document.addEventListener('fullscreenchange', syncFullscreenState)
})

onBeforeUnmount(() => {
  document.removeEventListener('fullscreenchange', syncFullscreenState)
})
</script>

<template>
  <div class="app-header-root flex h-14 min-w-0 items-center justify-between gap-2 bg-[var(--header-bg)] px-3">
    <HeaderNav
      :app-store="appStore"
      :show-top-menu="showTopMenu"
      :breadcrumbs="breadcrumbs"
      :top-menu-active="topMenuActive"
      :top-menu-options="topMenuOptions"
      @sidebar-toggle="handleSidebarToggle"
      @breadcrumb-select="handleBreadcrumbSelect"
      @top-menu-select="handleTopMenuSelect"
      @home-click="router.push('/')"
    />

    <HeaderToolbar
      :app-store="appStore"
      :user-store="userStore"
      :is-dark="isDark"
      :is-fullscreen="isFullscreen"
      :timezone-options="timezoneOptions"
      :locale-options="localeOptions"
      :user-options="userOptions"
      @locale-change="handleLocaleChange"
      @timezone-change="handleTimezoneChange"
      @theme-toggle="handleThemeToggle"
      @refresh="handleRefreshCurrentTab"
      @notification="message.info('通知功能待接入')"
      @fullscreen-toggle="toggleFullscreen"
      @preferences-open="openPreferenceDrawer"
      @user-action="handleUserAction"
    />
  </div>
</template>

<style scoped>
.app-header-root {
  border-bottom: 1px solid var(--border-color);
}
</style>
