<script lang="ts" setup>
import type { DropdownOption, MenuOption } from 'naive-ui'
import { Icon } from '@iconify/vue'
import {
  NAvatar,
  NBreadcrumb,
  NBreadcrumbItem,
  NButton,
  NDropdown,
  NFlex,
  NIcon,
  NMenu,
  NSpace,
  NTag,
  NTooltip,
  useMessage,
} from 'naive-ui'
import { computed, h, onBeforeUnmount, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { routes } from '@/router/routes'
import { useAuthStore } from '@/store/auth'
import { useLocale, useTheme } from '~/hooks'
import { useAccessStore, useAppStore, useUserStore } from '~/stores'
import AppGlobalSearch from './AppGlobalSearch.vue'
import AppPreferenceDrawer from './AppPreferenceDrawer.vue'

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

const showTopMenu = computed(() => appStore.layoutMode === 'top')

const topMenuSource = computed(() => {
  if (accessStore.accessRoutes.length) {
    return accessStore.accessRoutes
  }
  return routes.find(item => item.path === '/')?.children ?? []
})

const topMenuOptions = computed<MenuOption[]>(() => {
  return topMenuSource.value
    .filter((item: any) => !item.meta?.hidden)
    .map((item: any) => {
      const firstChild = item.children?.find((child: any) => !child.meta?.hidden)
      const menuPath = firstChild
        ? firstChild.path.startsWith('/')
          ? firstChild.path
          : `${item.path}/${firstChild.path}`
        : item.path
      return {
        key: menuPath,
        label: t(String(item.meta?.title ?? item.name), String(item.meta?.title ?? item.name)),
        icon: item.meta?.icon ? () => h(Icon, { icon: item.meta.icon as string }) : undefined,
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
  syncFullscreenState()
  document.addEventListener('fullscreenchange', syncFullscreenState)
})

onBeforeUnmount(() => {
  document.removeEventListener('fullscreenchange', syncFullscreenState)
})
</script>

<template>
  <div
    class="flex h-14 min-w-0 items-center justify-between gap-2 border-b border-gray-100 bg-white px-3 dark:border-gray-800 dark:bg-gray-900"
  >
    <!-- 左侧：折叠按钮 + 面包屑 -->
    <div
      class="flex min-w-0 flex-1 items-center gap-2"
      :class="appStore.headerMenuAlign === 'center' ? 'mx-auto' : ''"
    >
      <NTooltip v-if="!showTopMenu && appStore.widgetSidebarToggle" placement="bottom" :delay="500">
        <template #trigger>
          <NButton quaternary circle size="small" @click="appStore.toggleSidebar">
            <template #icon>
              <NIcon>
                <Icon
                  :icon="
                    appStore.sidebarCollapsed ? 'lucide:panel-left-open' : 'lucide:panel-left-close'
                  "
                  width="18"
                />
              </NIcon>
            </template>
          </NButton>
        </template>
        {{ appStore.sidebarCollapsed ? '展开侧边栏' : '收起侧边栏' }}
      </NTooltip>

      <NBreadcrumb
        v-if="appStore.breadcrumbEnabled && !showTopMenu"
        separator=">"
        class="hidden sm:flex"
        :class="
          appStore.breadcrumbStyle === 'background'
            ? 'rounded-md bg-gray-100 px-2 py-1 dark:bg-gray-800'
            : ''
        "
      >
        <NBreadcrumbItem v-if="appStore.breadcrumbShowHome">
          <NFlex align="center" :size="6" class="cursor-pointer" @click="router.push('/')">
            <Icon icon="lucide:house" width="14" />
            <span>Home</span>
          </NFlex>
        </NBreadcrumbItem>
        <NBreadcrumbItem v-for="item in breadcrumbs" :key="item.path">
          <NDropdown
            v-if="item.siblings.length > 1"
            :options="item.siblings"
            @select="(key) => handleBreadcrumbSelect(String(key))"
          >
            <NTag size="small" round :bordered="false" class="cursor-pointer">
              <NFlex align="center" :size="6">
                <Icon v-if="item.icon" :icon="item.icon" width="14" />
                <span>{{ item.title }}</span>
              </NFlex>
            </NTag>
          </NDropdown>
          <NFlex v-else align="center" :size="6" class="rounded px-1.5 py-0.5">
            <Icon v-if="item.icon" :icon="item.icon" width="14" />
            <span>{{ item.title }}</span>
          </NFlex>
        </NBreadcrumbItem>
      </NBreadcrumb>

      <NMenu
        v-if="showTopMenu"
        class="hidden lg:block"
        mode="horizontal"
        :value="topMenuActive"
        :options="topMenuOptions"
        @update:value="(key) => handleTopMenuSelect(String(key))"
      />
    </div>

    <!-- 右侧：工具栏 -->
    <NSpace
      align="center"
      :size="2"
      class="min-w-0 flex-nowrap"
      :class="appStore.headerMenuAlign === 'right' ? 'ml-auto' : ''"
    >
      <div v-if="appStore.searchEnabled">
        <AppGlobalSearch />
      </div>

      <!-- 语言切换 -->
      <div v-if="appStore.widgetLanguageToggle" class="hidden md:block">
        <NDropdown :options="localeOptions" @select="handleLocaleChange">
          <NTooltip placement="bottom" :delay="500">
            <template #trigger>
              <NButton quaternary circle size="small">
                <template #icon>
                  <NIcon>
                    <Icon icon="lucide:languages" width="18" />
                  </NIcon>
                </template>
              </NButton>
            </template>
            切换语言
          </NTooltip>
        </NDropdown>
      </div>

      <!-- 主题切换 -->
      <NTooltip v-if="appStore.widgetThemeToggle" placement="bottom" :delay="500">
        <template #trigger>
          <NButton quaternary circle size="small" @click="handleThemeToggle">
            <template #icon>
              <NIcon>
                <Icon :icon="isDark ? 'lucide:sun' : 'lucide:moon'" width="18" />
              </NIcon>
            </template>
          </NButton>
        </template>
        {{ isDark ? '切换亮色模式' : '切换暗色模式' }}
      </NTooltip>

      <div v-if="appStore.widgetRefresh" class="hidden md:block">
        <NTooltip placement="bottom" :delay="500">
          <template #trigger>
            <NButton quaternary circle size="small" @click="router.go(0)">
              <template #icon>
                <NIcon><Icon icon="lucide:refresh-cw" width="16" /></NIcon>
              </template>
            </NButton>
          </template>
          刷新
        </NTooltip>
      </div>

      <div v-if="appStore.widgetNotification" class="hidden lg:block">
        <NTooltip placement="bottom" :delay="500">
          <template #trigger>
            <NButton quaternary circle size="small">
              <template #icon>
                <NIcon><Icon icon="lucide:bell" width="16" /></NIcon>
              </template>
            </NButton>
          </template>
          通知
        </NTooltip>
      </div>

      <div v-if="appStore.widgetFullscreen" class="hidden sm:block">
        <NTooltip placement="bottom" :delay="500">
          <template #trigger>
            <NButton quaternary circle size="small" @click="toggleFullscreen">
              <template #icon>
                <NIcon>
                  <Icon :icon="isFullscreen ? 'lucide:minimize-2' : 'lucide:maximize-2'" width="16" />
                </NIcon>
              </template>
            </NButton>
          </template>
          {{ isFullscreen ? '退出全屏' : '全屏' }}
        </NTooltip>
      </div>

      <AppPreferenceDrawer />

      <!-- 用户头像 -->
      <NDropdown :options="userOptions" @select="handleUserAction">
        <div
          class="flex cursor-pointer items-center gap-2 rounded-lg px-2 py-1 hover:bg-gray-100 dark:hover:bg-gray-800"
        >
          <NAvatar
            round
            :size="30"
            :src="userStore.avatar"
            :fallback-src="`https://api.dicebear.com/9.x/initials/svg?seed=${userStore.nickname}`"
          />
          <span class="hidden text-sm text-gray-700 sm:block dark:text-gray-300">
            {{ userStore.nickname || userStore.username }}
          </span>
          <NIcon size="14" class="text-gray-400">
            <Icon icon="lucide:chevron-down" />
          </NIcon>
        </div>
      </NDropdown>
    </NSpace>
  </div>
</template>
