<script setup lang="ts">
import {
  XhColorPickerArea,
  XhColorPickerAreaThumb,
  XhColorPickerChannelSlider,
  XhColorPickerChannelSliderThumb,
  XhColorPickerChannelSliderTrack,
  XhColorPickerContent,
  XhColorPickerPositioner,
  XhColorPickerRoot,
  XhColorPickerSwatch,
  XhColorPickerSwatchGroup,
  XhColorPickerSwatchItem,
  XhColorPickerTrigger,
  XhColorPickerValueText,
} from '@xihan-ui/vue'

/**
 * 取色器：值为 hex 串。
 *
 * 十来个部件摆一遍才是一个完整取色面板，收在这里；预设色板由调用方给。
 */
defineOptions({ name: 'XColorPicker' })

withDefaults(defineProps<{
  value?: string | null
  /** 预设色板 */
  swatches?: string[]
  disabled?: boolean
}>(), {
  value: undefined,
  swatches: undefined,
  disabled: false,
})

const emit = defineEmits<{
  'update:value': [value: string]
}>()
</script>

<template>
  <XhColorPickerRoot
    :value="value ?? undefined"
    :swatches="swatches"
    :disabled="disabled"
    @update:value="(next: string) => emit('update:value', next)"
  >
    <XhColorPickerTrigger>
      <XhColorPickerSwatch />
      <XhColorPickerValueText />
    </XhColorPickerTrigger>
    <XhColorPickerPositioner>
      <XhColorPickerContent>
        <XhColorPickerArea>
          <XhColorPickerAreaThumb />
        </XhColorPickerArea>
        <XhColorPickerChannelSlider channel="hue">
          <XhColorPickerChannelSliderTrack />
          <XhColorPickerChannelSliderThumb />
        </XhColorPickerChannelSlider>
        <XhColorPickerSwatchGroup v-if="swatches?.length">
          <XhColorPickerSwatchItem v-for="color in swatches" :key="color" :value="color" />
        </XhColorPickerSwatchGroup>
      </XhColorPickerContent>
    </XhColorPickerPositioner>
  </XhColorPickerRoot>
</template>
