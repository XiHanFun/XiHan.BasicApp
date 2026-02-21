<script lang="ts" setup>
import { h, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  NButton,
  NBreadcrumb,
  NBreadcrumbItem,
  NDropdown,
  NAvatar,
  NFlex,
  NTag,
  NSpace,
  NTooltip,
  NIcon,
} from 'naive-ui'
import { Icon } from '@iconify/vue'
import { useI18n } from 'vue-i18n'
import { useAppStore, useUserStore } from '~/stores'
import { useTheme, useLocale } from '~/hooks'
import { useAuthStore } from '@/store/auth'
import AppGlobalSearch from './AppGlobalSearch.vue'
import AppPreferenceDrawer from './AppPreferenceDrawer.vue'

defineOptions({ name: 'AppHeader' })

const route = useRoute()
const router = useRouter()
const appStore = useAppStore()
const userStore = useUserStore()
const authStore = useAuthStore()
const { t } = useI18n()
const { isDark, toggleThemeWithTransition } = useTheme()
const { setLocale } = useLocale()

const breadcrumbs = computed(() => {
  const matched = route.matched
    .filter((r) => r.meta?.title && !r.meta?.hidden)
  return matched.map((r, index) => {
      const parent = index > 0 ? matched[index - 1] : null
      const siblings = (parent?.children ?? [])
        .filter((item) => item.meta?.title && !item.meta?.hidden)
        .map((item) => ({
          key: item.path.startsWith('/') ? item.path : `${parent?.path ?? ''}/${item.path}`,
          label: t(String(item.meta?.title), String(item.meta?.title)),
          icon: item.meta?.icon
            ? () => h(Icon, { icon: item.meta?.icon as string })
            : undefined,
        }))

      return {
        title: t(String(r.meta.title), String(r.meta.title)),
        path: r.path,
        icon: r.meta.icon as string | undefined,
        siblings,
      }
    })
})

const userOptions = [
  {
    label: '个人中心',
    key: 'profile',
    icon: () => h(Icon, { icon: 'lucide:user' }),
  },
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

const localeOptions = [
  { label: '简体中文', key: 'zh-CN' },
  { label: 'English', key: 'en-US' },
]

function handleUserAction(key: string) {
  if (key === 'logout') {
    authStore.logout()
  } else if (key === 'profile') {
    router.push('/profile')
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
</script>

<template>
  <div
    class="flex h-14 items-center justify-between border-b border-gray-100 bg-white px-4 dark:border-gray-800 dark:bg-gray-900"
  >
    <!-- 左侧：折叠按钮 + 面包屑 -->
    <div class="flex items-center gap-3">
      <NTooltip placement="bottom" :delay="500">
        <template #trigger>
          <NButton
            quaternary
            circle
            size="small"
            @click="appStore.toggleSidebar"
          >
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

      <NBreadcrumb v-if="appStore.breadcrumbEnabled">
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
          <NFlex v-else align="center" :size="6">
            <Icon v-if="item.icon" :icon="item.icon" width="14" />
            <span>{{ item.title }}</span>
          </NFlex>
        </NBreadcrumbItem>
      </NBreadcrumb>
    </div>

    <!-- 右侧：工具栏 -->
    <NSpace align="center" :size="4">
      <AppGlobalSearch v-if="appStore.searchEnabled" />

      <!-- 语言切换 -->
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

      <!-- 主题切换 -->
      <NTooltip placement="bottom" :delay="500">
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

      <AppPreferenceDrawer />

      <!-- 用户头像 -->
      <NDropdown :options="userOptions" @select="handleUserAction">
        <div class="flex cursor-pointer items-center gap-2 rounded-lg px-2 py-1 hover:bg-gray-100 dark:hover:bg-gray-800">
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
