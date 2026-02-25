<script setup lang="ts">
import { NCard, NColorPicker, NSwitch } from 'naive-ui'
import { computed } from 'vue'
import { useTheme } from '~/hooks'
import { useAppStore } from '~/stores'

const appStore = useAppStore()
const { isDark, toggleTheme, setThemeColor } = useTheme()
const color = computed({
  get: () => appStore.themeColor,
  set: (value: string) => setThemeColor(value),
})
</script>

<template>
  <NCard title="主题示例" :bordered="false" class="space-y-4">
    <div class="flex items-center gap-3">
      <span>暗色模式</span>
      <NSwitch :value="isDark" @update:value="toggleTheme" />
    </div>
    <div class="flex items-center gap-3">
      <span>主题色</span>
      <NColorPicker v-model:value="color" :modes="['hex']" />
    </div>
  </NCard>
</template>
