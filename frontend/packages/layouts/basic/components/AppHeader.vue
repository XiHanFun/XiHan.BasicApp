<script lang="ts" setup>
import type { DropdownOption, MenuOption } from 'naive-ui'
import type { LayoutRouteRecord } from '../contracts'
import { Icon } from '@iconify/vue'
import { NMenu, useMessage } from 'naive-ui'
import { computed, h, onBeforeUnmount, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useLocale, useRefresh, useTheme } from '~/hooks'
import { useAppStore, useAuthStore, useLayoutBridgeStore, useUserStore } from '~/stores'
import { useLayoutMenuDomain } from '../composables'
import HeaderNav from './header/HeaderNav.vue'
import HeaderToolbar from './header/HeaderToolbar.vue'
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
const { t, te } = useI18n()
const message = useMessage()
const { isDark, toggleThemeWithTransition } = useTheme()
const { setLocale } = useLocale()
const { refresh: doRefresh } = useRefresh()
const {
  route,
  router,
  baseMenuSource,
  toLayoutMeta,
  resolveFullPath,
  buildMenuOptionsFromRoutes,
  findMatchedRoutePath,
} = useLayoutMenuDomain()

const isFullscreen = ref(false)
const currentTimezone = ref(
  typeof Intl !== 'undefined' ? Intl.DateTimeFormat().resolvedOptions().timeZone : 'UTC',
)

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

function renderRouteIcon(icon: string) {
  return () => h(Icon, { icon })
}

function translateMenuTitle(title: string, fallback: string) {
  return te(title) ? t(title) : fallback
}

const topMenuOptions = computed<MenuOption[]>(() => {
  const options = buildMenuOptionsFromRoutes(topMenuSource.value, {
    keyBy: 'path',
    translate: translateMenuTitle,
    iconRenderer: renderRouteIcon,
  })
  if (isSplitMode.value) {
    return options.map((item) => ({ ...item, children: undefined }))
  }
  return options
})

function resolveFirstVisiblePath(routeItem: LayoutRouteRecord, parentPath = ''): string {
  const fullPath = resolveFullPath(routeItem.path, parentPath)
  const firstVisibleChild = routeItem.children?.find((child) => !toLayoutMeta(child).hidden)
  if (!firstVisibleChild) {
    return fullPath
  }
  return resolveFirstVisiblePath(firstVisibleChild, fullPath)
}

const topMenuActive = computed(() => {
  if (!isSplitMode.value) {
    return String(route.meta?.activePath || route.path || '')
  }
  return (
    findMatchedRoutePath(topMenuSource.value) ??
    resolveFullPath(topMenuSource.value.find((item) => !toLayoutMeta(item).hidden)?.path ?? '')
  )
})

const breadcrumbs = computed(() => {
  const matched = route.matched.filter((item) => item.meta?.title && !item.meta?.hidden)
  if (appStore.breadcrumbHideOnlyOne && matched.length <= 1) {
    return []
  }
  return matched.map((item, index) => {
    const parent = index > 0 ? matched[index - 1] : null
    const siblings = (parent?.children ?? [])
      .filter((sibling) => sibling.meta?.title && !sibling.meta?.hidden)
      .map((sibling) => {
        const siblingTitle = String(sibling.meta?.title ?? '')
        const siblingIcon = sibling.meta?.icon as string | undefined
        return {
          key: resolveFullPath(sibling.path, parent?.path ?? ''),
          label: te(siblingTitle) ? t(siblingTitle) : siblingTitle,
          icon: siblingIcon ? () => h(Icon, { icon: siblingIcon }) : undefined,
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

const localeOptions = computed(() => [
  { label: t('header.locale.zh_cn'), key: 'zh-CN' },
  { label: t('header.locale.en_us'), key: 'en-US' },
])

const timezoneOptions = computed<DropdownOption[]>(() => [
  { label: t('header.timezone.utc'), key: 'UTC' },
  { label: t('header.timezone.shanghai'), key: 'Asia/Shanghai' },
  { label: t('header.timezone.tokyo'), key: 'Asia/Tokyo' },
  { label: t('header.timezone.london'), key: 'Europe/London' },
  { label: t('header.timezone.new_york'), key: 'America/New_York' },
  { label: t('header.timezone.los_angeles'), key: 'America/Los_Angeles' },
])

function handleUserAction(key: string) {
  if (key === 'logout') {
    authStore.logout()
    return
  }
  if (key === 'profile') {
    router.push('/profile')
    return
  }
  if (key === 'lock') {
    layoutBridgeStore.requestLockScreen()
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
  if (!isSplitMode.value) {
    router.push(path)
    return
  }
  const rootMenu = topMenuSource.value.find((item) => resolveFullPath(item.path) === path)
  if (!rootMenu) {
    return
  }
  const targetPath = resolveFirstVisiblePath(rootMenu)
  if (targetPath && targetPath !== route.path) {
    router.push(targetPath)
  }
}

function handleRefreshCurrentTab() {
  doRefresh()
}

function openPreferenceDrawer() {
  layoutBridgeStore.requestOpenPreferenceDrawer()
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
  <!-- Refresh button (left widget) -->
  <XihanIconButton
    v-if="appStore.widgetRefresh"
    class="my-0 mr-1 rounded-md"
    @click="handleRefreshCurrentTab"
  >
    <Icon icon="lucide:refresh-cw" class="size-4" />
  </XihanIconButton>

  <!-- Breadcrumb -->
  <div v-if="showBreadcrumb" class="flex-center hidden lg:block">
    <HeaderNav
      :app-store="appStore"
      :breadcrumbs="breadcrumbs"
      @breadcrumb-select="handleBreadcrumbSelect"
      @home-click="router.push('/')"
    />
  </div>

  <!-- Menu area -->
  <div :class="`menu-align-${appStore.headerMenuAlign}`" class="flex min-w-0 flex-1 items-center">
    <div v-if="showTopMenu" class="xihan-top-menu hidden min-w-0 items-center lg:flex">
      <NMenu
        mode="horizontal"
        :value="topMenuActive"
        :options="topMenuOptions"
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
    :show-preferences-in-header="true"
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
.xihan-top-menu
  .n-menu.n-menu--horizontal
  > .n-menu-item
  > .n-menu-item-content.n-menu-item-content--selected {
  background-color: hsl(var(--primary) / 15%);
  border-radius: 6px;
}
</style>
