<script setup lang="ts">
defineOptions({ name: 'LayoutPreviewSvg' })
defineProps<{ type: string }>()
</script>

<template>
  <!-- 垂直：左侧边栏 + 头部 + 内容 -->
  <svg v-if="type === 'side'" viewBox="0 0 130 88" fill="none" xmlns="http://www.w3.org/2000/svg">
    <rect width="130" height="88" rx="4" class="svg-bg" />
    <rect x="2" y="2" width="26" height="84" rx="3" class="svg-sidebar-strong" />
    <rect x="8" y="6" width="14" height="10" rx="2" class="svg-brand" />
    <rect x="7" y="23" width="16" height="3" rx="1.5" class="svg-on-dark" />
    <rect x="7" y="33" width="16" height="3" rx="1.5" class="svg-on-dark-soft" />
    <rect x="7" y="43" width="16" height="3" rx="1.5" class="svg-on-dark-soft" />
    <rect x="7" y="53" width="16" height="3" rx="1.5" class="svg-on-dark-soft" />
    <rect x="31" y="2" width="97" height="10" rx="2" class="svg-header" />
    <rect x="34" y="5" width="10" height="3" rx="1.5" class="svg-chip" />
    <rect x="80" y="4" width="4" height="4" rx="1" class="svg-chip" />
    <rect x="88" y="4" width="4" height="4" rx="1" class="svg-chip" />
    <rect x="96" y="4" width="4" height="4" rx="1" class="svg-chip" />
    <rect x="31" y="15" width="46" height="22" rx="2" class="svg-block" />
    <rect x="80" y="15" width="48" height="22" rx="2" class="svg-block" />
    <rect x="31" y="40" width="97" height="46" rx="2" class="svg-block-faint" />
    <rect x="35" y="45" width="89" height="4" rx="2" class="svg-row" />
    <rect x="35" y="53" width="89" height="4" rx="2" class="svg-row" />
    <rect x="35" y="61" width="89" height="4" rx="2" class="svg-row" />
  </svg>

  <!-- 双列菜单：固定双栏 + 内容 -->
  <svg v-else-if="type === 'side-mixed'" viewBox="0 0 130 88" fill="none" xmlns="http://www.w3.org/2000/svg">
    <rect width="130" height="88" rx="4" class="svg-bg" />
    <rect x="2" y="2" width="12" height="84" rx="3" class="svg-sidebar-strong" />
    <rect x="17" y="2" width="20" height="84" rx="3" class="svg-sidebar" />
    <rect x="5" y="6" width="6" height="6" rx="1.5" class="svg-brand" />
    <rect x="5" y="20" width="6" height="2.8" rx="1.4" class="svg-on-dark" />
    <rect x="5" y="31" width="6" height="2.8" rx="1.4" class="svg-on-dark-soft" />
    <rect x="5" y="42" width="6" height="2.8" rx="1.4" class="svg-on-dark-soft" />
    <rect x="5" y="53" width="6" height="2.8" rx="1.4" class="svg-on-dark-soft" />
    <rect x="20" y="8" width="14" height="3" rx="1.5" class="svg-row" />
    <rect x="20" y="20" width="14" height="3" rx="1.5" class="svg-row" />
    <rect x="20" y="32" width="14" height="3" rx="1.5" class="svg-row" />
    <rect x="20" y="44" width="14" height="3" rx="1.5" class="svg-row" />
    <rect x="40" y="2" width="88" height="10" rx="2" class="svg-header" />
    <rect x="43" y="5" width="10" height="3" rx="1.5" class="svg-chip" />
    <rect x="91" y="4" width="4" height="4" rx="1" class="svg-chip" />
    <rect x="99" y="4" width="4" height="4" rx="1" class="svg-chip" />
    <rect x="107" y="4" width="4" height="4" rx="1" class="svg-chip" />
    <rect x="40" y="15" width="30" height="22" rx="2" class="svg-block" />
    <rect x="73" y="15" width="55" height="22" rx="2" class="svg-block" />
    <rect x="40" y="40" width="88" height="46" rx="2" class="svg-block-faint" />
  </svg>

  <!-- 水平：顶部导航 + 内容 -->
  <svg v-else-if="type === 'top'" viewBox="0 0 130 88" fill="none" xmlns="http://www.w3.org/2000/svg">
    <rect width="130" height="88" rx="4" class="svg-bg" />
    <rect x="2" y="2" width="126" height="10" rx="2" class="svg-header-strong" />
    <rect x="4" y="4" width="7" height="6" rx="1.5" class="svg-brand" />
    <rect x="18" y="5" width="9" height="2.5" rx="1.2" class="svg-on-dark" />
    <rect x="31" y="5" width="9" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="44" y="5" width="9" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="57" y="5" width="9" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="117" y="4.2" width="4" height="4" rx="1" class="svg-on-dark" />
    <rect x="2" y="15" width="57" height="22" rx="2" class="svg-block" />
    <rect x="62" y="15" width="66" height="22" rx="2" class="svg-block" />
    <rect x="2" y="40" width="126" height="46" rx="2" class="svg-block-faint" />
    <rect x="6" y="45" width="118" height="4" rx="2" class="svg-row" />
    <rect x="6" y="53" width="118" height="4" rx="2" class="svg-row" />
    <rect x="6" y="61" width="118" height="4" rx="2" class="svg-row" />
  </svg>

  <!-- 侧边导航：顶部主栏 + 左侧导航 -->
  <svg v-else-if="type === 'header-sidebar'" viewBox="0 0 130 88" fill="none" xmlns="http://www.w3.org/2000/svg">
    <rect width="130" height="88" rx="4" class="svg-bg" />
    <rect x="2" y="2" width="126" height="10" rx="2" class="svg-header-strong" />
    <rect x="4" y="4" width="7" height="6" rx="1.5" class="svg-brand" />
    <rect x="16" y="5" width="9" height="2.5" rx="1.2" class="svg-on-dark" />
    <rect x="28" y="5" width="9" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="40" y="5" width="9" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="2" y="15" width="24" height="71" rx="2.5" class="svg-sidebar" />
    <rect x="6" y="21" width="16" height="3" rx="1.5" class="svg-row" />
    <rect x="6" y="31" width="16" height="3" rx="1.5" class="svg-row" />
    <rect x="6" y="41" width="16" height="3" rx="1.5" class="svg-row" />
    <rect x="29" y="15" width="99" height="9" rx="2" class="svg-sub-header" />
    <rect x="29" y="27" width="48" height="18" rx="2" class="svg-block" />
    <rect x="80" y="27" width="48" height="18" rx="2" class="svg-block" />
    <rect x="29" y="48" width="99" height="38" rx="2" class="svg-block-faint" />
  </svg>

  <!-- 混合垂直：顶部导航 + 左侧边栏 -->
  <svg v-else-if="type === 'mix'" viewBox="0 0 130 88" fill="none" xmlns="http://www.w3.org/2000/svg">
    <rect width="130" height="88" rx="4" class="svg-bg" />
    <rect x="2" y="2" width="126" height="10" rx="2" class="svg-header-strong" />
    <rect x="4" y="4" width="7" height="6" rx="1.5" class="svg-brand" />
    <rect x="18" y="5" width="9" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="31" y="5" width="9" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="44" y="5" width="9" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="2" y="15" width="18" height="71" rx="2.5" class="svg-sidebar-strong" />
    <rect x="5.5" y="21" width="11" height="3" rx="1.5" class="svg-on-dark" />
    <rect x="5.5" y="31" width="11" height="3" rx="1.5" class="svg-on-dark-soft" />
    <rect x="5.5" y="41" width="11" height="3" rx="1.5" class="svg-on-dark-soft" />
    <rect x="5.5" y="51" width="11" height="3" rx="1.5" class="svg-on-dark-soft" />
    <rect x="23" y="15" width="105" height="20" rx="2" class="svg-block" />
    <rect x="23" y="38" width="105" height="48" rx="2" class="svg-block-faint" />
  </svg>

  <!-- 混合双列：顶部导航 + 固定双列 -->
  <svg v-else-if="type === 'header-mix'" viewBox="0 0 130 88" fill="none" xmlns="http://www.w3.org/2000/svg">
    <rect width="130" height="88" rx="4" class="svg-bg" />
    <rect x="2" y="2" width="126" height="10" rx="2" class="svg-header-strong" />
    <rect x="4" y="4" width="7" height="6" rx="1.5" class="svg-brand" />
    <rect x="18" y="5" width="9" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="31" y="5" width="9" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="44" y="5" width="9" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="2" y="15" width="12" height="71" rx="2.5" class="svg-sidebar-strong" />
    <rect x="17" y="15" width="18" height="71" rx="2.5" class="svg-sidebar" />
    <rect x="5" y="21" width="6" height="2.8" rx="1.4" class="svg-on-dark" />
    <rect x="5" y="32" width="6" height="2.8" rx="1.4" class="svg-on-dark-soft" />
    <rect x="5" y="43" width="6" height="2.8" rx="1.4" class="svg-on-dark-soft" />
    <rect x="20" y="22" width="12" height="3" rx="1.5" class="svg-row" />
    <rect x="20" y="34" width="12" height="3" rx="1.5" class="svg-row" />
    <rect x="20" y="46" width="12" height="3" rx="1.5" class="svg-row" />
    <rect x="38" y="15" width="90" height="20" rx="2" class="svg-block" />
    <rect x="38" y="38" width="90" height="48" rx="2" class="svg-block-faint" />
  </svg>

  <!-- 内容全屏：纯内容，无侧边、头部、标签栏 -->
  <svg v-else-if="type === 'full'" viewBox="0 0 130 88" fill="none" xmlns="http://www.w3.org/2000/svg">
    <rect width="130" height="88" rx="4" class="svg-bg" />
    <rect x="3" y="3" width="124" height="82" rx="3" class="svg-block-faint" />
    <rect x="8" y="10" width="52" height="22" rx="2" class="svg-block" />
    <rect x="64" y="10" width="58" height="22" rx="2" class="svg-block" />
    <rect x="8" y="36" width="114" height="44" rx="2" class="svg-block" />
  </svg>

  <!-- 内容流式：等宽内容充满容器 -->
  <svg v-else-if="type === 'content-fluid'" viewBox="0 0 130 88" fill="none" xmlns="http://www.w3.org/2000/svg">
    <rect width="130" height="88" rx="4" class="svg-bg" />
    <rect x="2" y="2" width="126" height="10" rx="2" class="svg-header" />
    <rect x="6" y="5" width="10" height="3" rx="1.5" class="svg-chip" />
    <rect x="107" y="4" width="4" height="4" rx="1" class="svg-chip" />
    <rect x="115" y="4" width="4" height="4" rx="1" class="svg-chip" />
    <rect x="2" y="15" width="126" height="21" rx="2" class="svg-block" />
    <rect x="2" y="39" width="126" height="47" rx="2" class="svg-block-faint" />
  </svg>

  <!-- 内容定宽：内容居中，两侧留白 -->
  <svg v-else-if="type === 'content-fixed'" viewBox="0 0 130 88" fill="none" xmlns="http://www.w3.org/2000/svg">
    <rect width="130" height="88" rx="4" class="svg-bg" />
    <rect x="2" y="2" width="126" height="10" rx="2" class="svg-header" />
    <rect x="2" y="15" width="126" height="71" rx="2" class="svg-block-faint" />
    <rect x="18" y="15" width="94" height="21" rx="2" class="svg-block" />
    <rect x="18" y="39" width="94" height="47" rx="2" class="svg-block" />
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
  fill: hsl(var(--primary) / 0.42);
}

.svg-sidebar-strong {
  fill: hsl(var(--primary) / 0.82);
}

.svg-header {
  fill: hsl(var(--primary) / 0.18);
}

.svg-header-strong {
  fill: hsl(var(--primary) / 0.7);
}

.svg-sub-header {
  fill: hsl(var(--primary) / 0.24);
}

.svg-row {
  fill: hsl(var(--primary) / 0.15);
}

.svg-block {
  fill: hsl(var(--primary) / 0.11);
}

.svg-block-faint {
  fill: hsl(var(--primary) / 0.06);
}

.svg-brand {
  fill: #fff;
}

.svg-chip {
  fill: hsl(var(--foreground) / 0.3);
}

.svg-on-dark {
  fill: hsl(var(--background) / 0.95);
}

.svg-on-dark-soft {
  fill: hsl(var(--background) / 0.72);
}
</style>
