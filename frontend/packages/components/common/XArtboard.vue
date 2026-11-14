<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'

/**
 * 画板：里面的内容按固定的设计尺寸排版，再整体缩放到铺满外框
 * 用在示意图这类「一小块界面的缩影」上——内容照常用间距、字号与圆角令牌写，
 * 外框多大它就缩放成多大，比例不走样
 * 画板内容只是示意，整块对读屏隐藏、不进 tab 序列；要说的话写在画板外面
 */
defineOptions({ name: 'XArtboard' })

const props = withDefaults(defineProps<{
  /** 设计宽度，以 4 号间距（--xh-space-4）为单位 */
  columns?: number
  /** 设计高度，以 4 号间距为单位 */
  rows?: number
}>(), {
  columns: 35,
  rows: 26,
})

const frame = ref<HTMLElement | null>(null)
const board = ref<HTMLElement | null>(null)
const scale = ref(1)
let observer: null | ResizeObserver = null

const boardStyle = computed(() => ({
  '--x-artboard-columns': String(props.columns),
  '--x-artboard-rows': String(props.rows),
  'transform': `scale(${scale.value})`,
}))

/** 设计尺寸是布局量出来的（不受缩放影响），外框的尺寸除以它就是缩放比 */
function fit(): void {
  const outer = frame.value
  const inner = board.value
  if (!outer || !inner || inner.offsetWidth === 0 || inner.offsetHeight === 0)
    return
  scale.value = Math.min(outer.clientWidth / inner.offsetWidth, outer.clientHeight / inner.offsetHeight)
}

onMounted(() => {
  fit()
  if (frame.value && typeof ResizeObserver === 'function') {
    observer = new ResizeObserver(fit)
    observer.observe(frame.value)
  }
})

onBeforeUnmount(() => observer?.disconnect())
</script>

<template>
  <div ref="frame" class="x-artboard" aria-hidden="true" inert>
    <div ref="board" class="x-artboard__board" :style="boardStyle">
      <slot />
    </div>
  </div>
</template>

<style scoped>
.x-artboard {
  position: relative;
  inline-size: 100%;
  block-size: 100%;
  overflow: hidden;
}

/* 从外框中心往外缩放：先摆到正中，再按比例缩 */
.x-artboard__board {
  position: absolute;
  inset-block-start: 50%;
  inset-inline-start: 50%;
  inline-size: calc(var(--xh-space-4) * var(--x-artboard-columns));
  block-size: calc(var(--xh-space-4) * var(--x-artboard-rows));
  margin-block-start: calc(var(--xh-space-4) * var(--x-artboard-rows) / -2);
  margin-inline-start: calc(var(--xh-space-4) * var(--x-artboard-columns) / -2);
  transform-origin: center;
}
</style>
