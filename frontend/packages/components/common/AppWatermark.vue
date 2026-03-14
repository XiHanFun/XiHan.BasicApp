<script lang="ts" setup>
import { computed } from 'vue'
import { useAppStore } from '~/stores'

defineOptions({ name: 'AppWatermark' })

const appStore = useAppStore()

const watermarkStyle = computed(() => {
  const text = appStore.watermarkText || 'XiHan BasicApp'
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="320" height="220">
    <text transform="rotate(-25, 160, 110)" x="40" y="120"
      fill="#808080" fill-opacity="0.18" font-size="16" font-family="sans-serif">
      ${text}
    </text>
  </svg>`
  const encoded = `url("data:image/svg+xml,${encodeURIComponent(svg)}")`
  return { backgroundImage: encoded, backgroundSize: '320px 220px' }
})
</script>

<template>
  <div v-if="appStore.watermarkEnabled" class="app-watermark" :style="watermarkStyle" />
</template>

<style>
.app-watermark {
  pointer-events: none;
  position: fixed;
  inset: 0;
  z-index: 1999;
  background-repeat: repeat;
  user-select: none;
}
</style>
