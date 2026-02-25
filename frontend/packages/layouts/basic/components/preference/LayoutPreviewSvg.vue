<script setup lang="ts">
defineOptions({ name: 'LayoutPreviewSvg' })
defineProps<{ type: string }>()
</script>

<template>
  <!-- 垂直：左侧边栏 + 头部 + 内容 -->
  <svg v-if="type === 'side'" viewBox="0 0 130 88" fill="none" xmlns="http://www.w3.org/2000/svg">
    <rect width="130" height="88" rx="4" class="svg-bg" />
    <!-- 侧边栏 -->
    <rect x="2" y="2" width="26" height="84" rx="3" class="svg-sidebar-strong" />
    <rect x="8" y="5" width="14" height="6" rx="1.5" class="svg-brand" />
    <rect x="7" y="18" width="16" height="2.5" rx="1.2" class="svg-on-dark" />
    <rect x="7" y="26" width="16" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="7" y="34" width="16" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="7" y="42" width="16" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <!-- 头部 -->
    <rect x="31" y="2" width="97" height="10" rx="2" class="svg-header" />
    <rect x="34" y="5" width="14" height="2.5" rx="1.2" class="svg-chip" />
    <rect x="51" y="5" width="8" height="2.5" rx="1.2" class="svg-chip" />
    <rect x="100" y="4.2" width="3.5" height="3.5" rx="1" class="svg-chip" />
    <rect x="107" y="4.2" width="3.5" height="3.5" rx="1" class="svg-chip" />
    <rect x="114" y="4.2" width="3.5" height="3.5" rx="1" class="svg-chip" />
    <!-- 内容 -->
    <rect x="31" y="15" width="46" height="20" rx="2" class="svg-block" />
    <rect x="80" y="15" width="48" height="20" rx="2" class="svg-block" />
    <rect x="31" y="38" width="97" height="48" rx="2" class="svg-block-faint" />
    <rect x="35" y="43" width="89" height="2.5" rx="1.2" class="svg-row" />
    <rect x="35" y="50" width="89" height="2.5" rx="1.2" class="svg-row" />
    <rect x="35" y="57" width="89" height="2.5" rx="1.2" class="svg-row" />
    <rect x="35" y="64" width="65" height="2.5" rx="1.2" class="svg-row" />
  </svg>

  <!-- 双列菜单：固定双栏 + 内容 -->
  <svg v-else-if="type === 'side-mixed'" viewBox="0 0 130 88" fill="none" xmlns="http://www.w3.org/2000/svg">
    <rect width="130" height="88" rx="4" class="svg-bg" />
    <!-- 双列侧边栏 -->
    <rect x="2" y="2" width="12" height="84" rx="3" class="svg-sidebar-strong" />
    <rect x="17" y="2" width="20" height="84" rx="3" class="svg-sidebar" />
    <rect x="5" y="5" width="6" height="6" rx="1.5" class="svg-brand" />
    <rect x="5" y="18" width="6" height="2.5" rx="1.2" class="svg-on-dark" />
    <rect x="5" y="26" width="6" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="5" y="34" width="6" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="5" y="42" width="6" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="20" y="8" width="14" height="2.5" rx="1.2" class="svg-row" />
    <rect x="20" y="18" width="14" height="2.5" rx="1.2" class="svg-row" />
    <rect x="20" y="28" width="14" height="2.5" rx="1.2" class="svg-row" />
    <rect x="20" y="38" width="14" height="2.5" rx="1.2" class="svg-row" />
    <!-- 头部 -->
    <rect x="40" y="2" width="88" height="10" rx="2" class="svg-header" />
    <rect x="43" y="5" width="14" height="2.5" rx="1.2" class="svg-chip" />
    <rect x="60" y="5" width="8" height="2.5" rx="1.2" class="svg-chip" />
    <rect x="103" y="4.2" width="3.5" height="3.5" rx="1" class="svg-chip" />
    <rect x="110" y="4.2" width="3.5" height="3.5" rx="1" class="svg-chip" />
    <rect x="117" y="4.2" width="3.5" height="3.5" rx="1" class="svg-chip" />
    <!-- 内容 -->
    <rect x="40" y="15" width="40" height="20" rx="2" class="svg-block" />
    <rect x="83" y="15" width="45" height="20" rx="2" class="svg-block" />
    <rect x="40" y="38" width="88" height="48" rx="2" class="svg-block-faint" />
    <rect x="44" y="43" width="80" height="2.5" rx="1.2" class="svg-row" />
    <rect x="44" y="50" width="80" height="2.5" rx="1.2" class="svg-row" />
    <rect x="44" y="57" width="80" height="2.5" rx="1.2" class="svg-row" />
    <rect x="44" y="64" width="58" height="2.5" rx="1.2" class="svg-row" />
  </svg>

  <!-- 水平：顶部导航 + 内容 -->
  <svg v-else-if="type === 'top'" viewBox="0 0 130 88" fill="none" xmlns="http://www.w3.org/2000/svg">
    <rect width="130" height="88" rx="4" class="svg-bg" />
    <!-- 头部（强调） -->
    <rect x="2" y="2" width="126" height="10" rx="2" class="svg-header-strong" />
    <rect x="4" y="4" width="7" height="6" rx="1.5" class="svg-brand" />
    <rect x="16" y="5" width="9" height="2.5" rx="1.2" class="svg-on-dark" />
    <rect x="28" y="5" width="9" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="40" y="5" width="9" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="52" y="5" width="9" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="114" y="4.2" width="3.5" height="3.5" rx="1" class="svg-on-dark-soft" />
    <rect x="120" y="4.2" width="3.5" height="3.5" rx="1" class="svg-on-dark-soft" />
    <!-- 内容 -->
    <rect x="2" y="15" width="60" height="20" rx="2" class="svg-block" />
    <rect x="65" y="15" width="63" height="20" rx="2" class="svg-block" />
    <rect x="2" y="38" width="126" height="48" rx="2" class="svg-block-faint" />
    <rect x="6" y="43" width="118" height="2.5" rx="1.2" class="svg-row" />
    <rect x="6" y="50" width="118" height="2.5" rx="1.2" class="svg-row" />
    <rect x="6" y="57" width="118" height="2.5" rx="1.2" class="svg-row" />
    <rect x="6" y="64" width="90" height="2.5" rx="1.2" class="svg-row" />
  </svg>

  <!-- 侧边导航：全宽头部（仅 logo + 工具栏） + 左侧完整菜单 -->
  <svg v-else-if="type === 'header-sidebar'" viewBox="0 0 130 88" fill="none" xmlns="http://www.w3.org/2000/svg">
    <rect width="130" height="88" rx="4" class="svg-bg" />
    <!-- 头部（强调，全宽，无导航菜单） -->
    <rect x="2" y="2" width="126" height="10" rx="2" class="svg-header-strong" />
    <rect x="4" y="4" width="7" height="6" rx="1.5" class="svg-brand" />
    <rect x="100" y="4.2" width="3.5" height="3.5" rx="1" class="svg-on-dark-soft" />
    <rect x="107" y="4.2" width="3.5" height="3.5" rx="1" class="svg-on-dark-soft" />
    <rect x="114" y="4.2" width="3.5" height="3.5" rx="1" class="svg-on-dark-soft" />
    <rect x="120" y="4.2" width="3.5" height="3.5" rx="1" class="svg-on-dark-soft" />
    <!-- 侧边栏（头部下方，完整菜单） -->
    <rect x="2" y="15" width="24" height="71" rx="2.5" class="svg-sidebar-strong" />
    <rect x="6" y="20" width="16" height="2.5" rx="1.2" class="svg-on-dark" />
    <rect x="6" y="28" width="16" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="6" y="36" width="16" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="6" y="44" width="16" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <!-- 内容 -->
    <rect x="29" y="15" width="47" height="20" rx="2" class="svg-block" />
    <rect x="79" y="15" width="49" height="20" rx="2" class="svg-block" />
    <rect x="29" y="38" width="99" height="48" rx="2" class="svg-block-faint" />
    <rect x="33" y="43" width="91" height="2.5" rx="1.2" class="svg-row" />
    <rect x="33" y="50" width="91" height="2.5" rx="1.2" class="svg-row" />
    <rect x="33" y="57" width="91" height="2.5" rx="1.2" class="svg-row" />
    <rect x="33" y="64" width="65" height="2.5" rx="1.2" class="svg-row" />
  </svg>

  <!-- 混合垂直：顶部导航 + 左侧边栏 -->
  <svg v-else-if="type === 'mix'" viewBox="0 0 130 88" fill="none" xmlns="http://www.w3.org/2000/svg">
    <rect width="130" height="88" rx="4" class="svg-bg" />
    <!-- 头部（强调） -->
    <rect x="2" y="2" width="126" height="10" rx="2" class="svg-header-strong" />
    <rect x="4" y="4" width="7" height="6" rx="1.5" class="svg-brand" />
    <rect x="16" y="5" width="9" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="28" y="5" width="9" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="40" y="5" width="9" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="114" y="4.2" width="3.5" height="3.5" rx="1" class="svg-on-dark-soft" />
    <rect x="120" y="4.2" width="3.5" height="3.5" rx="1" class="svg-on-dark-soft" />
    <!-- 侧边栏 -->
    <rect x="2" y="15" width="18" height="71" rx="2.5" class="svg-sidebar-strong" />
    <rect x="5.5" y="20" width="11" height="2.5" rx="1.2" class="svg-on-dark" />
    <rect x="5.5" y="28" width="11" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="5.5" y="36" width="11" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="5.5" y="44" width="11" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <!-- 内容 -->
    <rect x="23" y="15" width="50" height="20" rx="2" class="svg-block" />
    <rect x="76" y="15" width="52" height="20" rx="2" class="svg-block" />
    <rect x="23" y="38" width="105" height="48" rx="2" class="svg-block-faint" />
    <rect x="27" y="43" width="97" height="2.5" rx="1.2" class="svg-row" />
    <rect x="27" y="50" width="97" height="2.5" rx="1.2" class="svg-row" />
    <rect x="27" y="57" width="97" height="2.5" rx="1.2" class="svg-row" />
    <rect x="27" y="64" width="72" height="2.5" rx="1.2" class="svg-row" />
  </svg>

  <!-- 混合双列：顶部导航 + 固定双列（侧边栏贯穿全高） -->
  <svg v-else-if="type === 'header-mix'" viewBox="0 0 130 88" fill="none" xmlns="http://www.w3.org/2000/svg">
    <rect width="130" height="88" rx="4" class="svg-bg" />
    <!-- 双列侧边栏（贯穿全高，与双列菜单一致） -->
    <rect x="2" y="2" width="12" height="84" rx="3" class="svg-sidebar-strong" />
    <rect x="17" y="2" width="18" height="84" rx="3" class="svg-sidebar" />
    <rect x="5" y="5" width="6" height="6" rx="1.5" class="svg-brand" />
    <rect x="5" y="18" width="6" height="2.5" rx="1.2" class="svg-on-dark" />
    <rect x="5" y="26" width="6" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="5" y="34" width="6" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="5" y="42" width="6" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="20" y="8" width="12" height="2.5" rx="1.2" class="svg-row" />
    <rect x="20" y="18" width="12" height="2.5" rx="1.2" class="svg-row" />
    <rect x="20" y="28" width="12" height="2.5" rx="1.2" class="svg-row" />
    <rect x="20" y="38" width="12" height="2.5" rx="1.2" class="svg-row" />
    <!-- 头部（强调，仅覆盖内容区宽度） -->
    <rect x="38" y="2" width="90" height="10" rx="2" class="svg-header-strong" />
    <rect x="41" y="5" width="9" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="53" y="5" width="9" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="65" y="5" width="9" height="2.5" rx="1.2" class="svg-on-dark-soft" />
    <rect x="114" y="4.2" width="3.5" height="3.5" rx="1" class="svg-on-dark-soft" />
    <rect x="120" y="4.2" width="3.5" height="3.5" rx="1" class="svg-on-dark-soft" />
    <!-- 内容 -->
    <rect x="38" y="15" width="42" height="20" rx="2" class="svg-block" />
    <rect x="83" y="15" width="45" height="20" rx="2" class="svg-block" />
    <rect x="38" y="38" width="90" height="48" rx="2" class="svg-block-faint" />
    <rect x="42" y="43" width="82" height="2.5" rx="1.2" class="svg-row" />
    <rect x="42" y="50" width="82" height="2.5" rx="1.2" class="svg-row" />
    <rect x="42" y="57" width="82" height="2.5" rx="1.2" class="svg-row" />
    <rect x="42" y="64" width="60" height="2.5" rx="1.2" class="svg-row" />
  </svg>

  <!-- 内容全屏：纯内容，无侧边、头部、标签栏 -->
  <svg v-else-if="type === 'full'" viewBox="0 0 130 88" fill="none" xmlns="http://www.w3.org/2000/svg">
    <rect width="130" height="88" rx="4" class="svg-bg" />
    <rect x="3" y="3" width="124" height="82" rx="3" class="svg-block-faint" />
    <rect x="8" y="8" width="54" height="20" rx="2" class="svg-block" />
    <rect x="66" y="8" width="56" height="20" rx="2" class="svg-block" />
    <rect x="8" y="32" width="114" height="48" rx="2" class="svg-block" />
    <rect x="12" y="37" width="106" height="2.5" rx="1.2" class="svg-row" />
    <rect x="12" y="44" width="106" height="2.5" rx="1.2" class="svg-row" />
    <rect x="12" y="51" width="106" height="2.5" rx="1.2" class="svg-row" />
    <rect x="12" y="58" width="80" height="2.5" rx="1.2" class="svg-row" />
  </svg>

  <!-- 内容流式：等宽内容充满容器 -->
  <svg v-else-if="type === 'content-fluid'" viewBox="0 0 130 88" fill="none" xmlns="http://www.w3.org/2000/svg">
    <rect width="130" height="88" rx="4" class="svg-bg" />
    <!-- 头部 -->
    <rect x="2" y="2" width="126" height="10" rx="2" class="svg-header" />
    <rect x="5" y="5" width="14" height="2.5" rx="1.2" class="svg-chip" />
    <rect x="22" y="5" width="8" height="2.5" rx="1.2" class="svg-chip" />
    <rect x="33" y="5" width="6" height="2.5" rx="1.2" class="svg-chip" />
    <rect x="114" y="4.2" width="3.5" height="3.5" rx="1" class="svg-chip" />
    <rect x="120" y="4.2" width="3.5" height="3.5" rx="1" class="svg-chip" />
    <!-- 内容 -->
    <rect x="2" y="15" width="60" height="20" rx="2" class="svg-block" />
    <rect x="65" y="15" width="63" height="20" rx="2" class="svg-block" />
    <rect x="2" y="38" width="126" height="48" rx="2" class="svg-block-faint" />
    <rect x="6" y="43" width="118" height="2.5" rx="1.2" class="svg-row" />
    <rect x="6" y="50" width="118" height="2.5" rx="1.2" class="svg-row" />
    <rect x="6" y="57" width="118" height="2.5" rx="1.2" class="svg-row" />
    <rect x="6" y="64" width="90" height="2.5" rx="1.2" class="svg-row" />
  </svg>

  <!-- 内容定宽：内容居中，两侧留白 -->
  <svg v-else-if="type === 'content-fixed'" viewBox="0 0 130 88" fill="none" xmlns="http://www.w3.org/2000/svg">
    <rect width="130" height="88" rx="4" class="svg-bg" />
    <!-- 头部 -->
    <rect x="2" y="2" width="126" height="10" rx="2" class="svg-header" />
    <rect x="5" y="5" width="14" height="2.5" rx="1.2" class="svg-chip" />
    <rect x="22" y="5" width="8" height="2.5" rx="1.2" class="svg-chip" />
    <rect x="114" y="4.2" width="3.5" height="3.5" rx="1" class="svg-chip" />
    <rect x="120" y="4.2" width="3.5" height="3.5" rx="1" class="svg-chip" />
    <!-- 两侧留白 + 居中内容区 -->
    <rect x="2" y="15" width="126" height="71" rx="2" class="svg-block-faint" />
    <rect x="18" y="15" width="94" height="71" rx="2" class="svg-block" />
    <rect x="22" y="20" width="40" height="14" rx="1.5" class="svg-block-faint" />
    <rect x="66" y="20" width="42" height="14" rx="1.5" class="svg-block-faint" />
    <rect x="22" y="38" width="86" height="44" rx="1.5" class="svg-block-faint" />
    <rect x="26" y="43" width="78" height="2.5" rx="1.2" class="svg-row" />
    <rect x="26" y="50" width="78" height="2.5" rx="1.2" class="svg-row" />
    <rect x="26" y="57" width="78" height="2.5" rx="1.2" class="svg-row" />
    <rect x="26" y="64" width="58" height="2.5" rx="1.2" class="svg-row" />
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
