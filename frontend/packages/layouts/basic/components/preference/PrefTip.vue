<script setup lang="ts">
import { Icon } from '@iconify/vue'
import { NIcon, NTooltip } from 'naive-ui'

defineOptions({ name: 'PrefTip' })
defineProps<{ content: string }>()
</script>

<template>
  <NTooltip trigger="hover" :style="{ maxWidth: '240px' }" placement="top">
    <template #trigger>
      <NIcon size="13" class="tip-icon">
        <Icon icon="lucide:circle-help" />
      </NIcon>
    </template>
    <span class="whitespace-pre-line text-xs leading-relaxed">{{ content }}</span>
  </NTooltip>
</template>

<style scoped>
.tip-icon {
  color: hsl(var(--muted-foreground));
  opacity: 0.6;
  flex-shrink: 0;
  cursor: help;
  transition: opacity 0.15s ease;
}

.tip-icon:hover {
  opacity: 1;
}
</style>
