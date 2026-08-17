<script setup lang="ts">
import { XhSliderControl, XhSliderRange, XhSliderRoot, XhSliderThumb, XhSliderTrack } from '@xihan-ui/vue'
import { computed } from 'vue'

/**
 * 单值滑块。
 *
 * 组件库的滑块值恒为数组（它同时支持多滑块区间），业务侧一律是一个数；换算与部件摆放收在这里。
 */
defineOptions({ name: 'XSlider' })

const props = withDefaults(defineProps<{
  value?: number | null
  min?: number
  max?: number
  step?: number
  disabled?: boolean
}>(), {
  value: 0,
  min: 0,
  max: 100,
  step: 1,
  disabled: false,
})

const emit = defineEmits<{
  'update:value': [value: number]
}>()

const values = computed(() => [props.value ?? props.min])
</script>

<template>
  <XhSliderRoot
    :value="values"
    :min="min"
    :max="max"
    :step="step"
    :disabled="disabled"
    @update:value="(next: number[]) => next[0] !== undefined && emit('update:value', next[0])"
  >
    <XhSliderControl>
      <XhSliderTrack>
        <XhSliderRange />
      </XhSliderTrack>
      <XhSliderThumb :index="0" />
    </XhSliderControl>
  </XhSliderRoot>
</template>
