<script setup lang="ts">
import { Icon } from '@iconify/vue'
import { NAvatar, NDropdown } from 'naive-ui'
import type { HeaderToolbarPropsContract } from '../../contracts'
import AppGlobalSearch from '../AppGlobalSearch.vue'
import XihanIconButton from '../XihanIconButton.vue'

defineOptions({ name: 'HeaderToolbar' })

const props = defineProps<HeaderToolbarPropsContract>()

const emit = defineEmits<{
  localeChange: [key: string]
  timezoneChange: [key: string]
  themeToggle: [event: MouseEvent]
  notification: []
  fullscreenToggle: []
  preferencesOpen: []
  userAction: [key: string]
}>()

</script>

<template>
  <div class="flex h-full min-w-0 shrink-0 items-center">
    <!-- 全局搜索 -->
    <AppGlobalSearch v-if="props.appStore.searchEnabled" class="mr-1" />

    <!-- 语言切换 -->
    <NDropdown
      v-if="props.appStore.widgetLanguageToggle"
      :options="props.localeOptions"
      @select="(key) => emit('localeChange', String(key))"
    >
      <XihanIconButton class="mr-1" tooltip="切换语言">
        <Icon icon="lucide:languages" width="16" height="16" />
      </XihanIconButton>
    </NDropdown>

    <!-- 时区切换 -->
    <NDropdown
      v-if="props.appStore.widgetTimezone"
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

    <!-- 全屏 -->
    <XihanIconButton
      v-if="props.appStore.widgetFullscreen"
      class="mr-1"
      :tooltip="props.isFullscreen ? '退出全屏' : '全屏'"
      @click="emit('fullscreenToggle')"
    >
      <Icon
        :icon="props.isFullscreen ? 'lucide:minimize-2' : 'lucide:maximize-2'"
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

    <!-- 通知 -->
    <XihanIconButton
      v-if="props.appStore.widgetNotification"
      class="mr-1"
      tooltip="通知"
      @click="emit('notification')"
    >
      <Icon icon="lucide:bell" width="16" height="16" />
    </XihanIconButton>

    <!-- 用户菜单 -->
    <NDropdown :options="props.userOptions" @select="(key) => emit('userAction', String(key))">
      <button
        type="button"
        class="user-btn ml-1 flex cursor-pointer items-center gap-2 rounded-lg px-2 py-1"
      >
        <NAvatar
          round
          :size="28"
          :src="props.userStore.avatar"
          :fallback-src="`https://api.dicebear.com/9.x/initials/svg?seed=${props.userStore.nickname}`"
        />
        <span class="hidden max-w-[96px] truncate text-sm text-foreground sm:block">
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
