<script lang="ts" setup>
import { NWatermark } from 'naive-ui'
import { useAppStore } from '~/stores'

defineOptions({ name: 'AppWatermark' })

const appStore = useAppStore()
</script>

<template>
  <!-- NaiveUI 内置水印组件，自带防篡改能力 -->
  <NWatermark
    v-if="appStore.watermarkEnabled"
    :content="appStore.watermarkText || 'XiHan BasicApp'"
    cross
    fullscreen
    selectable
    :font-size="16"
    :line-height="16"
    :width="480"
    :height="320"
    :x-offset="12"
    :y-offset="60"
    :rotate="-25"
    font-color="rgb(128, 128, 128, 0.18)"
  />
</template>
