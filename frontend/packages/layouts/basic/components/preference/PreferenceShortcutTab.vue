<script setup lang="ts">
import type { useAppStore } from '~/stores'
import { NCard, NSwitch } from 'naive-ui'
import { useI18n } from 'vue-i18n'
import PrefTip from './PrefTip.vue'

defineOptions({ name: 'PreferenceShortcutTab' })
const props = defineProps<{ appStore: ReturnType<typeof useAppStore> }>()
const appStore = props.appStore
const { t } = useI18n()
</script>

<template>
  <NCard size="small" :bordered="false">
    <div class="section-title">
      {{ t('preference.shortcut.global') }}
    </div>
    <div class="pref-row">
      <div class="flex items-center gap-1">
        <span>{{ t('preference.shortcut.enabled') }}</span>
        <PrefTip :content="t('preference.shortcut.enabled_tip')" />
      </div>
      <NSwitch v-model:value="appStore.shortcutEnable" />
    </div>
    <div class="pref-row" :class="{ 'opacity-50': !appStore.shortcutEnable }">
      <div class="flex items-center gap-1.5">
        <span>{{ t('preference.shortcut.search') }}</span>
        <kbd class="kbd">Ctrl+K</kbd>
        <PrefTip :content="t('preference.shortcut.search_tip')" />
      </div>
      <NSwitch v-model:value="appStore.shortcutSearch" :disabled="!appStore.shortcutEnable" />
    </div>
    <div class="pref-row" :class="{ 'opacity-50': !appStore.shortcutEnable }">
      <div class="flex items-center gap-1.5">
        <span>{{ t('preference.shortcut.lock') }}</span>
        <kbd class="kbd">Alt+L</kbd>
        <PrefTip :content="t('preference.shortcut.lock_tip')" />
      </div>
      <NSwitch v-model:value="appStore.shortcutLock" :disabled="!appStore.shortcutEnable" />
    </div>
    <div class="pref-row" :class="{ 'opacity-50': !appStore.shortcutEnable }">
      <div class="flex items-center gap-1.5">
        <span>{{ t('preference.shortcut.logout') }}</span>
        <kbd class="kbd">Alt+Q</kbd>
        <PrefTip :content="t('preference.shortcut.logout_tip')" />
      </div>
      <NSwitch v-model:value="appStore.shortcutLogout" :disabled="!appStore.shortcutEnable" />
    </div>
  </NCard>
</template>
