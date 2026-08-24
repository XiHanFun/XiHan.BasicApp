<script lang="ts" setup>
import type { VNode } from 'vue'
import { XhCountdown } from '@xihan-ui/vue'

// 验证码重发倒计时：秒数大于 0 时逐帧倒数，走到 0 发 finish。

defineProps<{
  /** 这一轮的时长（秒），大于 0 才计时 */
  seconds: number
}>()

const emit = defineEmits<{ finish: [] }>()

defineSlots<{
  /** 自行排版剩余秒数；不插内容时显示「60s」 */
  default?: (props: { seconds: number }) => VNode[]
}>()

/** 剩余毫秒向上取整成秒：最后一秒走完才归零 */
function toSeconds(ms: number) {
  return Math.ceil(ms / 1000)
}
</script>

<template>
  <!-- 倒计时多半嵌在按钮里，字色跟着按钮走 -->
  <XhCountdown
    v-if="seconds > 0"
    v-slot="{ value }"
    :value="seconds * 1000"
    :precision="3"
    :style="{ '--xh-countdown-fg': 'currentColor', '--xh-countdown-finished-fg': 'currentColor' }"
    @finish="emit('finish')"
  >
    <slot :seconds="toSeconds(value)">
      {{ toSeconds(value) }}s
    </slot>
  </XhCountdown>
</template>
