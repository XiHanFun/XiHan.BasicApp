<script setup lang="ts">
/**
 * 整屏页面外壳：头部固定（吸顶）、内容区内部滚动。
 *
 * 依赖布局内容容器为 definite 高度（BasicLayout 已修复：根 h-full → LayoutContentRenderer height:100%）。
 * 页面直接用它即可获得「头部不滚 + 内容内部滚动」，无需任何 JS 定高。
 *
 * 用法：
 *   <XPageShell>
 *     <template #header><SearchBar /></template>
 *     <MyList />
 *   </XPageShell>
 *
 * 规则回顾（若手写不套本组件）：根 h-full + flex 列 + overflow-hidden；
 * 固定区 shrink-0；唯一滚动区 min-h-0 flex-1 overflow-auto。
 */
defineOptions({ name: 'XPageShell' })
</script>

<template>
  <div class="flex h-full min-h-0 flex-col overflow-hidden">
    <div v-if="$slots.header" class="shrink-0">
      <slot name="header" />
    </div>
    <div class="min-h-0 flex-1 overflow-auto">
      <slot />
    </div>
  </div>
</template>
