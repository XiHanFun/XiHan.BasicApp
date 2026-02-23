<script setup lang="ts">
defineOptions({ name: 'LayoutPreviewSvg' })
defineProps<{ type: string }>()
</script>

<template>
  <!-- 垂直：左侧边栏 + 顶部标题栏 + 内容 (key: side) -->
  <svg v-if="type === 'side'" viewBox="0 0 130 88" fill="none" xmlns="http://www.w3.org/2000/svg">
    <rect width="130" height="88" rx="4" class="svg-bg" />
    <rect x="2" y="2" width="28" height="84" rx="3" class="svg-sidebar" />
    <rect x="34" y="2" width="94" height="16" rx="3" class="svg-header" />
    <rect x="34" y="22" width="94" height="8" rx="2" class="svg-row" />
    <rect x="34" y="34" width="94" height="8" rx="2" class="svg-row" />
    <rect x="34" y="46" width="94" height="8" rx="2" class="svg-row" />
    <rect x="34" y="58" width="94" height="28" rx="2" class="svg-block" />
  </svg>

  <!-- 双列菜单：窄侧边栏 + 宽侧边栏 + 内容 (key: side-mixed) -->
  <svg v-else-if="type === 'side-mixed'" viewBox="0 0 130 88" fill="none" xmlns="http://www.w3.org/2000/svg">
    <rect width="130" height="88" rx="4" class="svg-bg" />
    <rect x="2" y="2" width="16" height="84" rx="3" class="svg-sidebar-dark" />
    <rect x="21" y="2" width="24" height="84" rx="3" class="svg-sidebar" />
    <rect x="49" y="2" width="79" height="16" rx="3" class="svg-header" />
    <rect x="49" y="22" width="79" height="8" rx="2" class="svg-row" />
    <rect x="49" y="34" width="79" height="8" rx="2" class="svg-row" />
    <rect x="49" y="46" width="79" height="40" rx="2" class="svg-block" />
  </svg>

  <!-- 水平（顶部导航）：全宽头部 + 内容 -->
  <svg v-else-if="type === 'top'" viewBox="0 0 130 88" fill="none" xmlns="http://www.w3.org/2000/svg">
    <rect width="130" height="88" rx="4" class="svg-bg" />
    <rect x="2" y="2" width="126" height="16" rx="3" class="svg-header" />
    <rect x="2" y="22" width="126" height="8" rx="2" class="svg-row" />
    <rect x="2" y="34" width="126" height="8" rx="2" class="svg-row" />
    <rect x="2" y="46" width="126" height="8" rx="2" class="svg-row" />
    <rect x="2" y="58" width="126" height="28" rx="2" class="svg-block" />
  </svg>

  <!-- 侧边导航：顶部品牌栏 + 侧边 + 顶部菜单 + 内容 -->
  <svg v-else-if="type === 'header-sidebar'" viewBox="0 0 130 88" fill="none" xmlns="http://www.w3.org/2000/svg">
    <rect width="130" height="88" rx="4" class="svg-bg" />
    <rect x="2" y="2" width="126" height="13" rx="3" class="svg-header" />
    <rect x="2" y="18" width="28" height="68" rx="3" class="svg-sidebar" />
    <rect x="34" y="18" width="94" height="11" rx="2" class="svg-sub-header" />
    <rect x="34" y="33" width="94" height="7" rx="2" class="svg-row" />
    <rect x="34" y="44" width="94" height="7" rx="2" class="svg-row" />
    <rect x="34" y="55" width="94" height="31" rx="2" class="svg-block" />
  </svg>

  <!-- 混合垂直：顶部导航 + 侧边栏 + 内容 (key: mix) -->
  <svg v-else-if="type === 'mix'" viewBox="0 0 130 88" fill="none" xmlns="http://www.w3.org/2000/svg">
    <rect width="130" height="88" rx="4" class="svg-bg" />
    <rect x="2" y="2" width="126" height="14" rx="3" class="svg-header" />
    <rect x="2" y="19" width="24" height="67" rx="3" class="svg-sidebar" />
    <rect x="30" y="19" width="98" height="8" rx="2" class="svg-row" />
    <rect x="30" y="31" width="98" height="8" rx="2" class="svg-row" />
    <rect x="30" y="43" width="98" height="8" rx="2" class="svg-row" />
    <rect x="30" y="55" width="98" height="31" rx="2" class="svg-block" />
  </svg>

  <!-- 混合双列：顶部导航 + 两侧边栏 + 内容 (key: header-mix) -->
  <svg v-else-if="type === 'header-mix'" viewBox="0 0 130 88" fill="none" xmlns="http://www.w3.org/2000/svg">
    <rect width="130" height="88" rx="4" class="svg-bg" />
    <rect x="2" y="2" width="126" height="14" rx="3" class="svg-header" />
    <rect x="2" y="19" width="16" height="67" rx="3" class="svg-sidebar-dark" />
    <rect x="21" y="19" width="22" height="67" rx="3" class="svg-sidebar" />
    <rect x="47" y="19" width="81" height="8" rx="2" class="svg-row" />
    <rect x="47" y="31" width="81" height="8" rx="2" class="svg-row" />
    <rect x="47" y="43" width="81" height="43" rx="2" class="svg-block" />
  </svg>

  <!-- 内容全屏：纯内容，无侧边和头部导航 -->
  <svg v-else-if="type === 'full'" viewBox="0 0 130 88" fill="none" xmlns="http://www.w3.org/2000/svg">
    <rect width="130" height="88" rx="4" class="svg-bg" />
    <rect x="2" y="2" width="126" height="10" rx="2" class="svg-sub-header" />
    <rect x="2" y="16" width="126" height="8" rx="2" class="svg-row" />
    <rect x="2" y="28" width="126" height="8" rx="2" class="svg-row" />
    <rect x="2" y="40" width="126" height="8" rx="2" class="svg-row" />
    <rect x="2" y="52" width="126" height="34" rx="2" class="svg-block" />
  </svg>

  <!-- 内容流式：等宽内容充满容器 -->
  <svg v-else-if="type === 'content-fluid'" viewBox="0 0 130 88" fill="none" xmlns="http://www.w3.org/2000/svg">
    <rect width="130" height="88" rx="4" class="svg-bg" />
    <rect x="2" y="2" width="126" height="13" rx="3" class="svg-header" />
    <rect x="2" y="19" width="126" height="8" rx="2" class="svg-row" />
    <rect x="2" y="31" width="126" height="8" rx="2" class="svg-row" />
    <rect x="2" y="43" width="126" height="8" rx="2" class="svg-row" />
    <rect x="2" y="55" width="126" height="31" rx="2" class="svg-block" />
  </svg>

  <!-- 内容定宽：内容居中，两侧留白 -->
  <svg v-else-if="type === 'content-fixed'" viewBox="0 0 130 88" fill="none" xmlns="http://www.w3.org/2000/svg">
    <rect width="130" height="88" rx="4" class="svg-bg" />
    <rect x="2" y="2" width="126" height="13" rx="3" class="svg-header" />
    <!-- 两侧留白 -->
    <rect x="2" y="19" width="126" height="69" rx="2" class="svg-block-faint" />
    <!-- 居中内容 -->
    <rect x="18" y="19" width="94" height="8" rx="2" class="svg-row" />
    <rect x="18" y="31" width="94" height="8" rx="2" class="svg-row" />
    <rect x="18" y="43" width="94" height="8" rx="2" class="svg-row" />
    <rect x="18" y="55" width="94" height="31" rx="2" class="svg-block" />
  </svg>

  <!-- 兜底 -->
  <svg v-else viewBox="0 0 130 88" fill="none" xmlns="http://www.w3.org/2000/svg">
    <rect width="130" height="88" rx="4" class="svg-bg" />
    <rect x="2" y="2" width="126" height="84" rx="4" class="svg-block" />
  </svg>
</template>

<style scoped>
svg {
  width: 100%;
  height: 100%;
  display: block;
}

.svg-bg {
  fill: hsl(var(--muted) / 0.4);
  stroke: hsl(var(--border));
  stroke-width: 0.5;
}

/* 主侧边栏（深色，品牌色） */
.svg-sidebar {
  fill: hsl(var(--primary) / 0.65);
}

/* 次级侧边栏（更深） */
.svg-sidebar-dark {
  fill: hsl(var(--primary) / 0.85);
}

/* 顶部主导航 */
.svg-header {
  fill: hsl(var(--primary) / 0.55);
}

/* 次级标题栏 / 标签栏 */
.svg-sub-header {
  fill: hsl(var(--primary) / 0.3);
}

/* 内容行 */
.svg-row {
  fill: hsl(var(--primary) / 0.15);
}

/* 内容大块 */
.svg-block {
  fill: hsl(var(--primary) / 0.08);
}

/* 极淡背景（定宽留白区域） */
.svg-block-faint {
  fill: hsl(var(--primary) / 0.04);
}
</style>
