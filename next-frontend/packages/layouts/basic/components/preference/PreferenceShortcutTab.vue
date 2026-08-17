<script setup lang="ts">
import type { useAppStore } from '~/stores'
import { XhSwitch } from '@xihan-ui/vue'
import { useI18n } from 'vue-i18n'
import { usePlatform } from '~/composables/usePlatform'
import PrefTip from './PrefTip.vue'

defineOptions({ name: 'PreferenceShortcutTab' })
const props = defineProps<{ appStore: ReturnType<typeof useAppStore> }>()
const appStore = props.appStore
const { t } = useI18n()

// 快捷键标签按平台显示（Mac 用 ⌘/⌥/⇧ 符号），复用共享 composable
const { formatShortcut: keys } = usePlatform()
</script>

<template>
  <section class="pref-card">
    <div class="section-title">
      {{ t('preference.shortcut.global') }}
    </div>
    <div class="pref-row">
      <div class="flex items-center gap-1">
        <span>{{ t('preference.shortcut.enabled') }}</span>
        <PrefTip :content="t('preference.shortcut.enabled_tip')" />
      </div>
      <XhSwitch v-model:checked="appStore.shortcutEnable" />
    </div>
    <div class="pref-row" :class="{ 'opacity-50': !appStore.shortcutEnable }">
      <div class="flex items-center gap-1.5">
        <span>{{ t('preference.shortcut.search') }}</span>
        <kbd class="kbd">{{ keys('Ctrl+K') }}</kbd>
        <PrefTip :content="t('preference.shortcut.search_tip')" />
      </div>
      <XhSwitch v-model:checked="appStore.shortcutSearch" :disabled="!appStore.shortcutEnable" />
    </div>
    <div class="pref-row" :class="{ 'opacity-50': !appStore.shortcutEnable }">
      <div class="flex items-center gap-1.5">
        <span>{{ t('preference.shortcut.tab_overview') }}</span>
        <kbd class="kbd">{{ keys('Alt+B') }}</kbd>
        <PrefTip :content="t('preference.shortcut.tab_overview_tip')" />
      </div>
      <XhSwitch v-model:checked="appStore.shortcutTabOverview" :disabled="!appStore.shortcutEnable" />
    </div>
    <div class="pref-row" :class="{ 'opacity-50': !appStore.shortcutEnable }">
      <div class="flex items-center gap-1.5">
        <span>{{ t('preference.shortcut.lock') }}</span>
        <kbd class="kbd">{{ keys('Alt+L') }}</kbd>
        <PrefTip :content="t('preference.shortcut.lock_tip')" />
      </div>
      <XhSwitch v-model:checked="appStore.shortcutLock" :disabled="!appStore.shortcutEnable" />
    </div>
    <div class="pref-row" :class="{ 'opacity-50': !appStore.shortcutEnable }">
      <div class="flex items-center gap-1.5">
        <span>{{ t('preference.shortcut.logout') }}</span>
        <kbd class="kbd">{{ keys('Alt+Q') }}</kbd>
        <PrefTip :content="t('preference.shortcut.logout_tip')" />
      </div>
      <XhSwitch v-model:checked="appStore.shortcutLogout" :disabled="!appStore.shortcutEnable" />
    </div>
  </section>
</template>
