<script setup lang="ts">
import { computed } from 'vue'

/**
 * 语言国旗图标。
 * 离线 iconify 仅预载 lucide/tabler/mdi（无国旗图标集），故内置 SVG 直绘，跨平台稳定显示。
 */
defineOptions({ name: 'LocaleFlag' })

const props = withDefaults(defineProps<{ locale: string, size?: number }>(), { size: 18 })

const width = computed(() => props.size)
const height = computed(() => Math.round((props.size * 2) / 3))
</script>

<template>
  <span
    class="locale-flag"
    :style="{ width: `${width}px`, height: `${height}px` }"
    aria-hidden="true"
  >
    <!-- 简体中文：中国国旗 -->
    <svg v-if="locale === 'zh-CN'" viewBox="0 0 30 20" xmlns="http://www.w3.org/2000/svg">
      <rect width="30" height="20" fill="#DE2910" />
      <g fill="#FFDE00">
        <polygon points="0,-1 0.2245,-0.309 0.951,-0.309 0.3633,0.118 0.5878,0.809 0,0.382 -0.5878,0.809 -0.3633,0.118 -0.951,-0.309 -0.2245,-0.309" transform="translate(5,5) scale(3)" />
        <polygon points="0,-1 0.2245,-0.309 0.951,-0.309 0.3633,0.118 0.5878,0.809 0,0.382 -0.5878,0.809 -0.3633,0.118 -0.951,-0.309 -0.2245,-0.309" transform="translate(10,2) scale(1)" />
        <polygon points="0,-1 0.2245,-0.309 0.951,-0.309 0.3633,0.118 0.5878,0.809 0,0.382 -0.5878,0.809 -0.3633,0.118 -0.951,-0.309 -0.2245,-0.309" transform="translate(12,4) scale(1)" />
        <polygon points="0,-1 0.2245,-0.309 0.951,-0.309 0.3633,0.118 0.5878,0.809 0,0.382 -0.5878,0.809 -0.3633,0.118 -0.951,-0.309 -0.2245,-0.309" transform="translate(12,7) scale(1)" />
        <polygon points="0,-1 0.2245,-0.309 0.951,-0.309 0.3633,0.118 0.5878,0.809 0,0.382 -0.5878,0.809 -0.3633,0.118 -0.951,-0.309 -0.2245,-0.309" transform="translate(10,9) scale(1)" />
      </g>
    </svg>

    <!-- English：美国国旗 -->
    <svg v-else-if="locale === 'en-US'" viewBox="0 0 30 20" xmlns="http://www.w3.org/2000/svg">
      <rect width="30" height="20" fill="#FFFFFF" />
      <g fill="#B22234">
        <rect y="0" width="30" height="1.54" />
        <rect y="3.08" width="30" height="1.54" />
        <rect y="6.15" width="30" height="1.54" />
        <rect y="9.23" width="30" height="1.54" />
        <rect y="12.31" width="30" height="1.54" />
        <rect y="15.38" width="30" height="1.54" />
        <rect y="18.46" width="30" height="1.54" />
      </g>
      <rect width="12" height="10.77" fill="#3C3B6E" />
      <g fill="#FFFFFF">
        <circle cx="2.2" cy="2.1" r="0.7" />
        <circle cx="4.6" cy="2.1" r="0.7" />
        <circle cx="7" cy="2.1" r="0.7" />
        <circle cx="9.4" cy="2.1" r="0.7" />
        <circle cx="3.4" cy="4.1" r="0.7" />
        <circle cx="5.8" cy="4.1" r="0.7" />
        <circle cx="8.2" cy="4.1" r="0.7" />
        <circle cx="2.2" cy="6.1" r="0.7" />
        <circle cx="4.6" cy="6.1" r="0.7" />
        <circle cx="7" cy="6.1" r="0.7" />
        <circle cx="9.4" cy="6.1" r="0.7" />
        <circle cx="3.4" cy="8.1" r="0.7" />
        <circle cx="5.8" cy="8.1" r="0.7" />
        <circle cx="8.2" cy="8.1" r="0.7" />
      </g>
    </svg>
  </span>
</template>

<style scoped>
.locale-flag {
  display: inline-block;
  overflow: hidden;
  line-height: 0;
  border-radius: 2px;
  box-shadow: 0 0 0 1px hsl(var(--border));
  vertical-align: middle;
}

.locale-flag svg {
  display: block;
  width: 100%;
  height: 100%;
}
</style>
