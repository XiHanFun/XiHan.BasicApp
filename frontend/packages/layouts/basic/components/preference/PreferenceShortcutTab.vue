<script setup lang="ts">
import type { useAppStore } from '~/stores'
import { XhKbd, XhSwitch } from '@xihan-ui/vue'
import { useI18n } from 'vue-i18n'
import { GLOBAL_HOTKEYS } from '~/composables/useGlobalShortcuts'
import PrefTip from './PrefTip.vue'

defineOptions({ name: 'PreferenceShortcutTab' })
const props = defineProps<{ appStore: ReturnType<typeof useAppStore> }>()
const appStore = props.appStore
const { t } = useI18n()

// 键帽与注册端读同一份键位声明；这里只显示，不接管按键
const keys = GLOBAL_HOTKEYS
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
        <XhKbd :keys="[...keys.search]" />
        <PrefTip :content="t('preference.shortcut.search_tip')" />
      </div>
      <XhSwitch v-model:checked="appStore.shortcutSearch" :disabled="!appStore.shortcutEnable" />
    </div>
    <div class="pref-row" :class="{ 'opacity-50': !appStore.shortcutEnable }">
      <div class="flex items-center gap-1.5">
        <span>{{ t('preference.shortcut.tab_overview') }}</span>
        <XhKbd :keys="[...keys.tabOverview]" />
        <PrefTip :content="t('preference.shortcut.tab_overview_tip')" />
      </div>
      <XhSwitch v-model:checked="appStore.shortcutTabOverview" :disabled="!appStore.shortcutEnable" />
    </div>
    <div class="pref-row" :class="{ 'opacity-50': !appStore.shortcutEnable }">
      <div class="flex items-center gap-1.5">
        <span>{{ t('preference.shortcut.lock') }}</span>
        <XhKbd :keys="[...keys.lock]" />
        <PrefTip :content="t('preference.shortcut.lock_tip')" />
      </div>
      <XhSwitch v-model:checked="appStore.shortcutLock" :disabled="!appStore.shortcutEnable" />
    </div>
    <div class="pref-row" :class="{ 'opacity-50': !appStore.shortcutEnable }">
      <div class="flex items-center gap-1.5">
        <span>{{ t('preference.shortcut.logout') }}</span>
        <XhKbd :keys="[...keys.logout]" />
        <PrefTip :content="t('preference.shortcut.logout_tip')" />
      </div>
      <XhSwitch v-model:checked="appStore.shortcutLogout" :disabled="!appStore.shortcutEnable" />
    </div>
  </section>
</template>
