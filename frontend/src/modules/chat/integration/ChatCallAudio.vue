<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import { ref, watch } from 'vue'

/**
 * 通话里一个人的声音：不显示的音频元素，画面关着、通话窗收拢时照样出声
 * 每个人各一个：多人通话里谁的声音到了就放谁的；选了扬声器就都从它出
 */
defineOptions({ name: 'ChatCallAudio' })

const props = defineProps<{
  stream: MediaStream | null
  /** 输出设备；null 为系统缺省 */
  sinkId: null | string
}>()

const emit = defineEmits<{
  /** 换不到选的扬声器 */
  sinkError: [error: unknown]
}>()

const element = ref<HTMLAudioElement | null>(null)

watch([
  element,
  () => props.stream,
], ([audio, stream]) => {
  if (audio && audio.srcObject !== stream) {
    audio.srcObject = stream
  }
}, { flush: 'post' })

watch([
  element,
  () => props.sinkId,
], ([audio, sinkId]) => {
  if (audio && sinkId && 'setSinkId' in audio) {
    audio.setSinkId(sinkId).catch(error => emit('sinkError', error))
  }
}, { flush: 'post' })
</script>

<template>
  <audio ref="element" autoplay />
</template>
