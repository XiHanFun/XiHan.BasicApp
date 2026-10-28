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
    <!-- 窄卡片放不下标题与右侧切换时，切换整组折到下一行靠右，标题与每个切换控件都不在内部折字 -->
    <header class="flex flex-wrap items-center gap-x-2 gap-y-1.5 border-b border-border/70 px-4 py-2.5">
      <Icon v-if="icon" :icon="icon" width="16" height="16" class="shrink-0 text-[hsl(var(--primary))]" />
      <span class="whitespace-nowrap text-sm font-medium text-card-foreground">{{ title }}</span>
      <!-- 标题旁的标签位：小组件自己的身份标记（如欢迎卡片的「官方」） -->
      <slot name="badge" />
      <div class="ml-auto flex flex-wrap items-center justify-end gap-1 *:shrink-0">
        <slot name="extra" />
      </div>
    </header>
    <!-- 内容区是 container：小组件宽度由用户按 12 栅格自定，内部布局按自身宽度（@ 容器查询）而非视口断点自适应 -->
    <div class="@container min-h-0 flex-1 overflow-auto p-4">
      <slot />
    </div>
  </section>
</template>
