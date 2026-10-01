<script setup lang="ts">
import type { HeaderToolbarPropsContract } from '../../contracts'
import type { NotificationItem } from '~/stores'

import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { XDropdown, XUserAvatar } from '~/components'
import LocaleSwitcher from '~/components/common/LocaleSwitcher.vue'
import TimezoneSwitcher from '~/components/common/TimezoneSwitcher.vue'
import { Icon } from '~/iconify'
import { useShellExtensions } from '~/stores'
import { useWidgetPlacement } from '../../composables'
import XihanIconButton from '../XihanIconButton.vue'
import HeaderSearchTrigger from './HeaderSearchTrigger.vue'
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
// 小屏（< 768）顶栏放不下，除隐藏的以外一律回落悬浮组，右上角的账号入口不被挤掉；用户名文字另由断点收起
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
      <Icon
        :icon="props.isDark ? 'lucide:sun' : 'lucide:moon'"
        width="16"
        height="16"
      />
    </XihanIconButton>

    <!-- 全屏 -->
    <XihanIconButton
      v-if="placement.fullscreen.value === 'header'"
      class="mr-1"
      :tooltip="props.isFullscreen ? t('header.toolbar.fullscreen_exit') : t('header.toolbar.fullscreen_enter')"
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

    <!-- 当前上下文：平台或租户名，点击进控制中心切换。皮肤与其它顶栏图标钮同一套，只是带上文字 -->
    <XihanIconButton
      v-if="props.contextLabel"
      class="xihan-icon-btn--labeled mr-1"
      :tooltip="t('header.context.switch', { name: props.contextLabel })"
      @click="emit('userAction', 'control-center')"
    >
      <Icon :icon="props.contextIsPlatform ? 'lucide:shield-check' : 'lucide:building-2'" width="16" height="16" class="shrink-0" />
      <span class="hidden max-w-32 truncate text-sm text-foreground md:block">{{ props.contextLabel }}</span>
      <Icon icon="lucide:chevron-down" width="13" height="13" class="shrink-0 text-muted-foreground" />
    </XihanIconButton>

    <!-- 用户菜单 -->
    <XDropdown :options="props.userOptions" @select="(key: string) => emit('userAction', key)">
      <!-- 窄屏只剩头像，名字收进 aria-label：菜单触发钮任何宽度下都有名字 -->
      <button
        type="button"
        class="user-btn ml-1 flex cursor-pointer items-center gap-2 rounded-lg px-2 py-1"
        :aria-label="props.userStore.nickname || props.userStore.username"
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
    </XDropdown>
  </div>
</template>

<style scoped>
.user-btn {
  border: none;
  background: transparent;
  outline: none;
  transition: background var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

.user-btn:hover {
  background: hsl(var(--accent));
}
</style>
