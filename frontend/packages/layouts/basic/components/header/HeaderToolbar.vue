<script setup lang="ts">
import type { HeaderToolbarPropsContract } from '../../contracts'
import type { NotificationItem } from '~/stores'
import { NDropdown } from 'naive-ui'
import { XUserAvatar } from '~/components'
import { useIsMobile } from '~/composables'
import { Icon } from '~/iconify'
import AppGlobalSearch from '../AppGlobalSearch.vue'
import XihanIconButton from '../XihanIconButton.vue'
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
  localeChange: [key: string]
  timezoneChange: [key: string]
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

// 小屏（<768）：隐藏次要工具（语言/时区/全屏）与用户名文字，避免头部溢出裁切头像菜单
const { isMobile } = useIsMobile()
</script>

<template>
  <div class="flex h-full min-w-0 shrink-0 items-center">
    <!-- 全局搜索 -->
    <AppGlobalSearch v-if="props.appStore.searchEnabled" class="mr-1" />

    <!-- 语言切换（小屏隐藏） -->
    <NDropdown
      v-if="props.appStore.widgetLanguageToggle && !isMobile"
      :options="props.localeOptions"
      @select="(key) => emit('localeChange', String(key))"
    >
      <XihanIconButton class="mr-1" tooltip="切换语言">
        <Icon icon="lucide:languages" width="16" height="16" />
      </XihanIconButton>
    </NDropdown>

    <!-- 时区切换（小屏隐藏） -->
    <NDropdown
      v-if="props.appStore.widgetTimezone && !isMobile"
      :options="props.timezoneOptions"
      @select="(key) => emit('timezoneChange', String(key))"
    >
      <XihanIconButton class="mr-1 mt-[2px]" tooltip="切换时区">
        <Icon icon="lucide:clock-3" width="16" height="16" />
      </XihanIconButton>
    </NDropdown>

    <!-- 主题切换 -->
    <XihanIconButton
      v-if="props.appStore.widgetThemeToggle"
      class="mr-1 mt-[2px]"
      :tooltip="props.isDark ? '切换浅色' : '切换深色'"
      @mousedown.prevent
      @click="(event: MouseEvent) => emit('themeToggle', event)"
    >
      <Icon
        :icon="props.isDark ? 'lucide:sun' : 'lucide:moon'"
        width="16"
        height="16"
      />
    </XihanIconButton>

    <!-- 全屏（小屏隐藏） -->
    <XihanIconButton
      v-if="props.appStore.widgetFullscreen && !isMobile"
      class="mr-1"
      :tooltip="props.isFullscreen ? '退出全屏' : '全屏'"
      @click="emit('fullscreenToggle')"
    >
      <Icon
        :icon="props.isFullscreen ? 'lucide:minimize' : 'lucide:maximize'"
        width="16"
        height="16"
      />
    </XihanIconButton>

    <!-- 偏好设置 -->
    <XihanIconButton
      v-if="props.showPreferencesInHeader !== false"
      class="mr-1"
      tooltip="偏好设置"
      @mousedown.prevent
      @click="emit('preferencesOpen')"
    >
      <Icon icon="lucide:settings-2" width="16" height="16" />
    </XihanIconButton>

    <!-- 分割线 -->
    <div class="mx-1 h-4 w-px bg-border" />

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

    <!-- 用户菜单 -->
    <NDropdown :options="props.userOptions" @select="(key) => emit('userAction', String(key))">
      <button
        type="button"
        class="user-btn ml-1 flex cursor-pointer items-center gap-2 rounded-lg px-2 py-1"
      >
        <XUserAvatar
          :size="28"
          :avatar="props.userStore.avatar"
          :name="props.userStore.nickname || props.userStore.username"
        />
        <span class="hidden max-w-[96px] truncate text-sm text-foreground md:block">
          {{ props.userStore.nickname || props.userStore.username }}
        </span>
        <Icon icon="lucide:chevron-down" width="13" height="13" class="shrink-0 text-muted-foreground" />
      </button>
    </NDropdown>
  </div>
</template>

<style scoped>
.user-btn {
  border: none;
  background: transparent;
  outline: none;
  transition: background 0.15s ease;
}

.user-btn:hover {
  background: hsl(var(--accent));
}
</style>
