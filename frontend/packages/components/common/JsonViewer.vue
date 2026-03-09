<script setup lang="ts">
import { NCard, NScrollbar } from 'naive-ui'
import { computed } from 'vue'

const props = withDefaults(defineProps<{
  title?: string
  data?: unknown
  rawText?: string
  maxHeight?: number
  bordered?: boolean
  size?: 'small' | 'medium' | 'large'
}>(), {
  title: '数据预览',
  data: undefined,
  rawText: undefined,
  maxHeight: 360,
  bordered: false,
  size: 'small',
})

const content = computed(() => {
  if (typeof props.rawText === 'string') {
    return props.rawText
  }
  return JSON.stringify(props.data ?? {}, null, 2)
})
</script>

<template>
  <NCard :title="props.title" :bordered="props.bordered" :size="props.size">
    <NScrollbar x-scrollable :style="{ maxHeight: `${props.maxHeight}px` }">
      <pre class="rounded bg-gray-50 p-2 text-xs leading-5">{{ content }}</pre>
    </NScrollbar>
  </NCard>
</template>
