<script setup lang="ts">
import type { DropdownOption } from 'naive-ui'
import type { useAppStore, useUserStore } from '~/stores'
import { Icon } from '@iconify/vue'
import { NAvatar, NButton, NDropdown, NIcon, NSpace } from 'naive-ui'
import AppGlobalSearch from '../AppGlobalSearch.vue'

defineOptions({ name: 'HeaderToolbar' })

const props = defineProps<HeaderToolbarProps>()

const emit = defineEmits<{
  localeChange: [key: string]
  timezoneChange: [key: string]
  themeToggle: [event: MouseEvent]
  refresh: []
  notification: []
  fullscreenToggle: []
  preferencesOpen: []
  userAction: [key: string]
}>()

interface HeaderToolbarProps {
  appStore: ReturnType<typeof useAppStore>
  userStore: ReturnType<typeof useUserStore>
  isDark: boolean
  isFullscreen: boolean
  showPreferencesInHeader?: boolean
  timezoneOptions: DropdownOption[]
  localeOptions: Array<{ label: string, key: string }>
  userOptions: DropdownOption[]
}
</script>

<template>
  <NSpace
    align="center"
    :size="2"
    class="min-w-0 flex-nowrap"
    :class="props.appStore.headerMenuAlign === 'right' ? 'ml-auto' : ''"
  >
    <div class="flex items-center gap-1">
      <div v-if="props.appStore.searchEnabled">
        <AppGlobalSearch />
      </div>

      <div v-if="props.appStore.widgetLanguageToggle">
        <NDropdown
          :options="props.localeOptions"
          @select="(key) => emit('localeChange', String(key))"
        >
          <NButton quaternary circle size="small" @mousedown.prevent>
            <template #icon>
              <NIcon size="16">
                <Icon icon="lucide:languages" />
              </NIcon>
            </template>
          </NButton>
        </NDropdown>
      </div>

      <NDropdown
        :options="props.timezoneOptions"
        @select="(key) => emit('timezoneChange', String(key))"
      >
        <NButton quaternary circle size="small" @mousedown.prevent>
          <template #icon>
            <NIcon size="16">
              <Icon icon="lucide:clock-3" />
            </NIcon>
          </template>
        </NButton>
      </NDropdown>

      <NButton
        v-if="props.appStore.widgetThemeToggle"
        quaternary
        circle
        size="small"
        @mousedown.prevent
        @click="(event) => emit('themeToggle', event)"
      >
        <template #icon>
          <NIcon size="16">
            <Icon :icon="props.isDark ? 'lucide:sun' : 'lucide:moon'" />
          </NIcon>
        </template>
      </NButton>

      <div v-if="props.appStore.widgetFullscreen">
        <NButton quaternary circle size="small" @mousedown.prevent @click="emit('fullscreenToggle')">
          <template #icon>
            <NIcon size="16">
              <Icon :icon="props.isFullscreen ? 'lucide:minimize-2' : 'lucide:maximize-2'" />
            </NIcon>
          </template>
        </NButton>
      </div>

      <NButton
        v-if="props.showPreferencesInHeader !== false"
        quaternary
        circle
        size="small"
        @mousedown.prevent
        @click="emit('preferencesOpen')"
      >
        <template #icon>
          <NIcon size="16">
            <Icon icon="lucide:settings-2" />
          </NIcon>
        </template>
      </NButton>
    </div>

    <div class="flex items-center gap-1 pl-1">
      <div v-if="props.appStore.widgetNotification">
        <NButton quaternary circle size="small" @mousedown.prevent @click="emit('notification')">
          <template #icon>
            <NIcon size="16">
              <Icon icon="lucide:bell" />
            </NIcon>
          </template>
        </NButton>
      </div>

      <NDropdown :options="props.userOptions" @select="(key) => emit('userAction', String(key))">
        <div
          class="flex cursor-pointer items-center gap-2 rounded-lg px-2 py-1 hover:bg-[hsl(var(--accent))]"
        >
          <NAvatar
            round
            :size="30"
            :src="props.userStore.avatar"
            :fallback-src="`https://api.dicebear.com/9.x/initials/svg?seed=${props.userStore.nickname}`"
          />
          <span class="hidden text-sm text-[hsl(var(--foreground))] sm:block">
            {{ props.userStore.nickname || props.userStore.username }}
          </span>
          <NIcon size="14" class="text-[hsl(var(--muted-foreground))]">
            <Icon icon="lucide:chevron-down" />
          </NIcon>
        </div>
      </NDropdown>
    </div>
  </NSpace>
</template>
