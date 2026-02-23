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
        <kbd class="kbd">Ctrl K</kbd>
        <PrefTip :content="t('preference.shortcut.search_tip')" />
      </div>
      <NSwitch v-model:value="appStore.shortcutSearch" :disabled="!appStore.shortcutEnable" />
    </div>
    <div class="pref-row" :class="{ 'opacity-50': !appStore.shortcutEnable }">
      <div class="flex items-center gap-1.5">
        <span>{{ t('preference.shortcut.lock') }}</span>
        <kbd class="kbd">Alt L</kbd>
        <PrefTip :content="t('preference.shortcut.lock_tip')" />
      </div>
      <NSwitch v-model:value="appStore.shortcutLock" :disabled="!appStore.shortcutEnable" />
    </div>
    <div class="pref-row" :class="{ 'opacity-50': !appStore.shortcutEnable }">
      <div class="flex items-center gap-1.5">
        <span>{{ t('preference.shortcut.logout') }}</span>
        <kbd class="kbd">Alt Q</kbd>
        <PrefTip :content="t('preference.shortcut.logout_tip')" />
      </div>
      <NSwitch v-model:value="appStore.shortcutLogout" :disabled="!appStore.shortcutEnable" />
    </div>
  </NCard>
</template>

<style scoped>
.section-title {
  margin-bottom: 4px;
  padding: 0 6px;
  font-weight: 600;
  font-size: 13px;
  color: hsl(var(--foreground));
}

kbd.kbd {
  display: inline-flex;
  align-items: center;
  padding: 1px 6px;
  font-size: 11px;
  font-family: ui-monospace, 'SFMono-Regular', monospace;
  color: hsl(var(--muted-foreground));
  background: hsl(var(--muted));
  border: 1px solid hsl(var(--border));
  border-radius: 4px;
  line-height: 1.6;
  white-space: nowrap;
}
</style>
