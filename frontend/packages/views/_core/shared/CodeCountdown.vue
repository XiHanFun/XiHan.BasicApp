<script lang="ts" setup>
import type { VNode } from 'vue'
import { XhTimerDisplay, XhTimerRoot } from '@xihan-ui/vue'

// 验证码重发倒计时：秒数大于 0 时逐拍倒数，走到 0 发 finish。

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
  <!-- 倒计时多半嵌在按钮里：字色与字号都跟着宿主走，不带 Timer 自己的展示档 -->
  <XhTimerRoot
    v-if="seconds > 0"
    v-slot="{ value }"
    :value="seconds * 1000"
    :precision="3"
    :style="{
      '--xh-timer-fg': 'currentColor',
      '--xh-timer-display-fg': 'currentColor',
      '--xh-timer-completed-fg': 'currentColor',
      '--xh-timer-digit-font-size': 'inherit',
    }"
    @complete="emit('finish')"
  >
    <XhTimerDisplay>
      <slot :seconds="toSeconds(value)">
        {{ toSeconds(value) }}s
      </slot>
    </XhTimerDisplay>
  </XhTimerRoot>
</template>
