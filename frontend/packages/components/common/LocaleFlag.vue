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

    <!-- 繁體中文：中华民国国旗 -->
    <svg v-else-if="locale === 'zh-TW'" viewBox="0 0 30 20" xmlns="http://www.w3.org/2000/svg">
      <rect width="30" height="20" fill="#FE0000" />
      <rect width="15" height="10" fill="#000095" />
      <g transform="translate(7.5,5)">
        <circle r="3.6" fill="#FFFFFF" />
        <g fill="#FFFFFF">
          <polygon points="0,-3.9 0.78,-1.3 -0.78,-1.3" />
          <polygon points="0,3.9 0.78,1.3 -0.78,1.3" />
          <polygon points="-3.9,0 -1.3,0.78 -1.3,-0.78" />
          <polygon points="3.9,0 1.3,0.78 1.3,-0.78" />
          <polygon points="-2.76,-2.76 -0.37,-1.47 -1.47,-0.37" />
          <polygon points="2.76,2.76 0.37,1.47 1.47,0.37" />
          <polygon points="2.76,-2.76 1.47,-0.37 0.37,-1.47" />
          <polygon points="-2.76,2.76 -1.47,0.37 -0.37,1.47" />
        </g>
        <circle r="2.4" fill="#000095" />
        <circle r="2.1" fill="#FFFFFF" />
      </g>
    </svg>

    <!-- 日本語：日本国旗 -->
    <svg v-else-if="locale === 'ja-JP'" viewBox="0 0 30 20" xmlns="http://www.w3.org/2000/svg">
      <rect width="30" height="20" fill="#FFFFFF" />
      <circle cx="15" cy="10" r="6" fill="#BC002D" />
    </svg>

    <!-- 한국어：韩国国旗 -->
    <svg v-else-if="locale === 'ko-KR'" viewBox="0 0 30 20" xmlns="http://www.w3.org/2000/svg">
      <rect width="30" height="20" fill="#FFFFFF" />
      <g transform="translate(15,10)">
        <path d="M -4 0 A 4 4 0 0 1 4 0 A 2 2 0 0 0 0 0 A 2 2 0 0 1 -4 0 Z" fill="#CD2E3A" />
        <path d="M -4 0 A 2 2 0 0 1 0 0 A 2 2 0 0 0 4 0 A 4 4 0 0 1 -4 0 Z" fill="#0047A0" />
      </g>
      <g fill="#000000" transform="translate(15,10)">
        <g transform="rotate(-56.31) translate(-8.5,0)">
          <rect x="-1.6" y="-1.5" width="3.2" height="0.8" />
          <rect x="-1.6" y="-0.4" width="3.2" height="0.8" />
          <rect x="-1.6" y="0.7" width="3.2" height="0.8" />
        </g>
        <g transform="rotate(56.31) translate(8.5,0)">
          <rect x="-1.6" y="-1.5" width="3.2" height="0.8" />
          <rect x="-1.6" y="-0.4" width="3.2" height="0.8" />
          <rect x="-1.6" y="0.7" width="3.2" height="0.8" />
        </g>
        <g transform="rotate(56.31) translate(-8.5,0)">
          <rect x="-1.6" y="-1.5" width="3.2" height="0.8" />
          <rect x="-1.6" y="-0.4" width="1.3" height="0.8" />
          <rect x="0.3" y="-0.4" width="1.3" height="0.8" />
          <rect x="-1.6" y="0.7" width="3.2" height="0.8" />
        </g>
        <g transform="rotate(-56.31) translate(8.5,0)">
          <rect x="-1.6" y="-1.5" width="1.3" height="0.8" />
          <rect x="0.3" y="-1.5" width="1.3" height="0.8" />
          <rect x="-1.6" y="-0.4" width="3.2" height="0.8" />
          <rect x="-1.6" y="0.7" width="1.3" height="0.8" />
          <rect x="0.3" y="0.7" width="1.3" height="0.8" />
        </g>
      </g>
    </svg>

    <!-- हिन्दी：印度国旗 -->
    <svg v-else-if="locale === 'hi-IN'" viewBox="0 0 30 20" xmlns="http://www.w3.org/2000/svg">
      <rect width="30" height="6.67" fill="#FF9933" />
      <rect y="6.67" width="30" height="6.67" fill="#FFFFFF" />
      <rect y="13.33" width="30" height="6.67" fill="#138808" />
      <g transform="translate(15,10)">
        <circle r="2.8" fill="none" stroke="#000080" stroke-width="0.5" />
        <circle r="0.5" fill="#000080" />
        <g stroke="#000080" stroke-width="0.18">
          <line x1="0" y1="-2.8" x2="0" y2="2.8" />
          <line x1="-2.8" y1="0" x2="2.8" y2="0" />
          <line x1="-1.98" y1="-1.98" x2="1.98" y2="1.98" />
          <line x1="-1.98" y1="1.98" x2="1.98" y2="-1.98" />
          <line x1="-1.07" y1="-2.59" x2="1.07" y2="2.59" />
          <line x1="1.07" y1="-2.59" x2="-1.07" y2="2.59" />
          <line x1="-2.59" y1="-1.07" x2="2.59" y2="1.07" />
          <line x1="-2.59" y1="1.07" x2="2.59" y2="-1.07" />
        </g>
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
