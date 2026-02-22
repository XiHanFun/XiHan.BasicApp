<script setup lang="ts">
import type { useAppStore } from '~/stores'
import { NCard, NSwitch } from 'naive-ui'

defineOptions({ name: 'PreferenceShortcutTab' })
const props = defineProps<{ appStore: ReturnType<typeof useAppStore> }>()
const appStore = props.appStore
</script>

<template>
  <NCard size="small" :bordered="false">
    <div class="section-title">
      全局快捷键
    </div>
    <div class="mb-2 flex items-center justify-between">
      <span>启用快捷键</span>
      <NSwitch v-model:value="appStore.shortcutEnable" />
    </div>
    <div class="mb-2 flex items-center justify-between">
      <span>全局搜索 (Ctrl+K)</span>
      <NSwitch v-model:value="appStore.shortcutSearch" />
    </div>
    <div class="mb-2 flex items-center justify-between">
      <span>快速登出 (Ctrl+Shift+Q)</span>
      <NSwitch v-model:value="appStore.shortcutLogout" />
    </div>
    <div class="flex items-center justify-between">
      <span>快速锁屏 (Ctrl+L)</span>
      <NSwitch v-model:value="appStore.shortcutLock" />
    </div>
  </NCard>
</template>

<style scoped>
.section-title {
  margin-bottom: 10px;
  font-weight: 600;
  font-size: 13px;
  color: hsl(var(--foreground));
}
</style>
