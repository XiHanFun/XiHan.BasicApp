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
 * 固定区 shrink-0；唯一滚动区交给组件库滚动区域（root 定高、viewport 溢出滚动）。
 */
import {
  XhScrollAreaContent,
  XhScrollAreaCorner,
  XhScrollAreaRoot,
  XhScrollAreaScrollbar,
  XhScrollAreaThumb,
  XhScrollAreaTrack,
  XhScrollAreaViewport,
} from '@xihan-ui/vue'

defineOptions({ name: 'XPageShell' })
</script>

<template>
  <div class="flex h-full min-h-0 flex-col overflow-hidden">
    <div v-if="$slots.header" class="shrink-0">
      <slot name="header" />
    </div>
    <!-- root 由 flex 定高（皮肤给它 overflow:hidden + 定位上下文），viewport 是真正 overflow:auto 的那层 -->
    <XhScrollAreaRoot class="min-h-0 flex-1">
      <XhScrollAreaViewport>
        <!-- content 撑满视口高度，插槽里按百分比定高的元素照旧解析得到高度 -->
        <XhScrollAreaContent class="h-full">
          <slot />
        </XhScrollAreaContent>
      </XhScrollAreaViewport>
      <!-- 滑块的行程按轨道节点量：少了 Track 这层，拖滑块与点轨道都会变成空操作 -->
      <XhScrollAreaScrollbar orientation="vertical">
        <XhScrollAreaTrack>
          <XhScrollAreaThumb />
        </XhScrollAreaTrack>
        <XhScrollAreaCorner />
      </XhScrollAreaScrollbar>
      <XhScrollAreaScrollbar orientation="horizontal">
        <XhScrollAreaTrack>
          <XhScrollAreaThumb />
        </XhScrollAreaTrack>
      </XhScrollAreaScrollbar>
    </XhScrollAreaRoot>
  </div>
</template>
