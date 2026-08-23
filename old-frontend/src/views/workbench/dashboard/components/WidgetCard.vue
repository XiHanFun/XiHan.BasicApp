<script setup lang="ts">
import { Icon } from '~/iconify'

defineOptions({ name: 'WidgetCard' })

defineProps<{
  /** 小组件图标（iconify，lucide/tabler） */
  icon?: string
  /** 小组件标题 */
  title?: string
}>()
</script>

<template>
  <section class="flex h-full flex-col overflow-hidden rounded-xl border border-border bg-card">
    <header class="flex items-center gap-2 border-b border-border/70 px-4 py-2.5">
      <Icon v-if="icon" :icon="icon" width="16" height="16" class="text-[hsl(var(--primary))]" />
      <span class="text-sm font-medium text-card-foreground">{{ title }}</span>
      <div class="ml-auto flex items-center gap-1">
        <slot name="extra" />
      </div>
    </header>
    <div class="min-h-0 flex-1 overflow-auto p-4">
      <slot />
    </div>
  </section>
</template>
