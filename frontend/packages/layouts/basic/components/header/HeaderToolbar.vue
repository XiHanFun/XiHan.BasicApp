<script setup lang="ts">
import type { HeaderToolbarPropsContract } from '../../contracts'
import type { NotificationItem } from '~/stores'

import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import LocaleSwitcher from '~/components/common/LocaleSwitcher.vue'
import TimezoneSwitcher from '~/components/common/TimezoneSwitcher.vue'
import { Icon } from '~/iconify'
import { useShellExtensions } from '~/stores'
import { useWidgetPlacement } from '../../composables'
import XihanIconButton from '../XihanIconButton.vue'
import HeaderContextMenu from './HeaderContextMenu.vue'
import HeaderSearchTrigger from './HeaderSearchTrigger.vue'
import HeaderUserMenu from './HeaderUserMenu.vue'
import NotificationPopover from './NotificationPopover.vue'

defineOptions({ name: 'HeaderToolbar' })

const props = defineProps<HeaderToolbarPropsContract & {
  notificationAllItems?: NotificationItem[]
  notificationMentionedItems?: NotificationItem[]
  notificationUnreadAll?: NotificationItem[]
  notificationUnreadMentioned?: NotificationItem[]
  notificationUnreadCount?: number
  notificationLoading?: boolean
}>()

const emit = defineEmits<{
  themeToggle: [event: MouseEvent]
  notificationMarkRead: [id: string]
  notificationConfirm: [id: string]
  notificationMarkAllRead: []
  notificationViewAll: []
  notificationRefresh: []
  fullscreenToggle: []
  preferencesOpen: []
  userAction: [key: string]
}>()

// 壳层扩展按钮（可选模块注册的顶栏入口）
const shellHeaderItems = computed(() => useShellExtensions().flatMap(extension => extension.headerToolbarItems ?? []))

const { t } = useI18n()
// 命令面板、语言、时区、主题、全屏按各自的位置落到顶栏或悬浮组（AppFloatToolbar），只有落到顶栏的在这里画。
// 小屏（< 768）顶栏放不下，除隐藏的以外一律回落悬浮组，右上角的账号入口不被挤掉；上下文名称另由断点收起
const placement = useWidgetPlacement()
</script>

<template>
  <div class="flex h-full min-w-0 shrink-0 items-center">
    <!-- 命令面板入口（面板本身由布局层常驻） -->
    <HeaderSearchTrigger v-if="placement.search.value === 'header'" class="mr-1" :compact="props.searchCompact" />

    <!-- 语言切换 -->
    <LocaleSwitcher
      v-if="placement.language.value === 'header'"
      variant="dropdown"
      apply
    >
      <XihanIconButton class="mr-1" :tooltip="t('header.toolbar.switch_language')">
        <Icon icon="lucide:languages" width="16" height="16" />
      </XihanIconButton>
    </LocaleSwitcher>

    <!-- 时区切换 -->
    <TimezoneSwitcher
      v-if="placement.timezone.value === 'header'"
      variant="dropdown"
      apply
    >
      <XihanIconButton class="mr-1" :tooltip="t('header.toolbar.switch_timezone')">
        <Icon icon="lucide:clock-3" width="16" height="16" />
      </XihanIconButton>
    </TimezoneSwitcher>

    <!-- 主题切换 -->
    <XihanIconButton
      v-if="placement.theme.value === 'header'"
      class="mr-1"
      :tooltip="props.isDark ? t('header.toolbar.theme_to_light') : t('header.toolbar.theme_to_dark')"
      @mousedown.prevent
      @click="(event: MouseEvent) => emit('themeToggle', event)"
    >
      <span class="icon-swap">
        <Transition name="icon-swap">
          <Icon
            :key="props.isDark ? 'sun' : 'moon'"
            :icon="props.isDark ? 'lucide:sun' : 'lucide:moon'"
            width="16"
            height="16"
          />
        </Transition>
      </span>
    </XihanIconButton>

    <!-- 全屏 -->
    <XihanIconButton
      v-if="placement.fullscreen.value === 'header'"
      class="mr-1"
      :tooltip="props.isFullscreen ? t('header.toolbar.fullscreen_exit') : t('header.toolbar.fullscreen_enter')"
      @click="emit('fullscreenToggle')"
    >
      <span class="icon-swap">
        <Transition name="icon-swap">
          <Icon
            :key="props.isFullscreen ? 'minimize' : 'maximize'"
            :icon="props.isFullscreen ? 'lucide:minimize' : 'lucide:maximize'"
            width="16"
            height="16"
          />
        </Transition>
      </span>
    </XihanIconButton>

    <!-- 偏好设置 -->
    <XihanIconButton
      v-if="props.showPreferencesInHeader !== false"
      class="mr-1"
      :tooltip="t('header.toolbar.preferences')"
      @mousedown.prevent
      @click="emit('preferencesOpen')"
    >
      <Icon icon="lucide:settings-2" width="16" height="16" />
    </XihanIconButton>

    <!-- 分割线 -->
    <div class="mx-1 h-4 w-px bg-border" />

    <!-- 壳层扩展按钮（可选模块注册的顶栏入口，如聊天） -->
    <component :is="item" v-for="(item, index) in shellHeaderItems" :key="index" />

    <!-- 通知弹窗 -->
    <NotificationPopover
      v-if="props.appStore.widgetNotification"
      :all-items="props.notificationAllItems ?? []"
      :mentioned-items="props.notificationMentionedItems ?? []"
      :unread-all="props.notificationUnreadAll ?? []"
      :unread-mentioned="props.notificationUnreadMentioned ?? []"
      :unread-count="props.notificationUnreadCount ?? 0"
      :loading="props.notificationLoading ?? false"
      @mark-read="(id) => emit('notificationMarkRead', id)"
      @confirm="(id) => emit('notificationConfirm', id)"
      @mark-all-read="emit('notificationMarkAllRead')"
      @view-all="emit('notificationViewAll')"
      @refresh="emit('notificationRefresh')"
    />

    <!-- 当前上下文：平台或租户名，悬停展开可切换的租户，末项进控制中心 -->
    <HeaderContextMenu
      v-if="props.contextLabel"
      :label="props.contextLabel"
      :is-platform="props.contextIsPlatform ?? false"
      @control-center="emit('userAction', 'control-center')"
    />

    <!-- 账号菜单：触发钮只有头像，名字在浮层第一行 -->
    <HeaderUserMenu
      :avatar="props.userStore.avatar"
      :nickname="props.userStore.nickname"
      :username="props.userStore.username"
      :options="props.userOptions"
      @select="(key: string) => emit('userAction', key)"
    />
  </div>
</template>
