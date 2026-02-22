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
      全局
    </div>
    <div class="mb-2 flex items-center justify-between">
      <span>快捷键</span>
      <NSwitch v-model:value="appStore.shortcutEnable" />
    </div>
    <div class="mb-2 flex items-center justify-between">
      <span>全局搜索</span>
      <div class="flex items-center gap-2">
        <span class="kbd">Ctrl K</span>
        <NSwitch v-model:value="appStore.shortcutSearch" :disabled="!appStore.shortcutEnable" />
      </div>
    </div>
    <div class="mb-2 flex items-center justify-between">
      <span>退出登录</span>
      <div class="flex items-center gap-2">
        <span class="kbd">Alt Q</span>
        <NSwitch v-model:value="appStore.shortcutLogout" :disabled="!appStore.shortcutEnable" />
      </div>
    </div>
    <div class="flex items-center justify-between">
      <span>锁定屏幕</span>
      <div class="flex items-center gap-2">
        <span class="kbd">Alt L</span>
        <NSwitch v-model:value="appStore.shortcutLock" :disabled="!appStore.shortcutEnable" />
      </div>
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

.kbd {
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
