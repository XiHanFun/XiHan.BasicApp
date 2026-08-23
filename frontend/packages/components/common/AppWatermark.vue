<script lang="ts" setup>
import { XhWatermarkRoot } from '@xihan-ui/vue'
import { useAppStore } from '~/stores'

defineOptions({ name: 'AppWatermark' })

const appStore = useAppStore()
</script>

<template>
  <!--
    组件库的水印是「盖住一段内容」的容器；全屏水印在这里靠定位实现：
    铺满视口、不吃指针、压在业务内容之上但在浮层之下。
  -->
  <XhWatermarkRoot
    v-if="appStore.watermarkEnabled"
    class="app-watermark"
    :text="appStore.watermarkText || 'XiHan BasicApp'"
    :rotate="-25"
    :font-size="16"
    :gap="160"
    :opacity="0.18"
  />
</template>

<style scoped>
.app-watermark {
  position: fixed;
  inset: 0;
  z-index: 1500;
  pointer-events: none;
}
</style>
