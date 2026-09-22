<script setup lang="ts">
import type { Size } from '@xihan-ui/core'
import {
  XhColorPickerAreaThumb,
  XhColorPickerContent,
  XhColorPickerControl,
  XhColorPickerHueSlider,
  XhColorPickerPositioner,
  XhColorPickerRoot,
  XhColorPickerSaturationArea,
  XhColorPickerSwatch,
  XhColorPickerSwatchPicker,
  XhColorPickerTrigger,
  XhColorPickerValueText,
} from '@xihan-ui/vue'

/**
 * 取色器：值为 hex 串。
 *
 * 部件摆一遍才是一个完整取色面板，收在这里；预设色板由调用方给。
 * 色相滑块与色板都是内嵌的子组件，挂载点不写子节点即自动铺开。
 */
defineOptions({ name: 'XColorPicker' })

withDefaults(defineProps<{
  value?: string | null
  /** 预设色板 */
  swatches?: string[]
  /** 不写时随外层 Field / Form 的 disabled 走；写了以本处为准 */
  disabled?: boolean
  /** 与表单里的其它字段同一档：缺省 sm，与 XInput / XSelect 一致 */
  size?: Size
}>(), {
  value: undefined,
  swatches: undefined,
  disabled: undefined,
  size: 'sm',
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
    :size="size"
    @update:value="(next: string) => emit('update:value', next)"
  >
    <!-- 视觉盒（边框/高度/内边距/聚焦环）在 Control 上，少这层触发钮就退回裸按钮 -->
    <XhColorPickerControl>
      <XhColorPickerTrigger>
        <XhColorPickerSwatch />
        <XhColorPickerValueText />
      </XhColorPickerTrigger>
    </XhColorPickerControl>
    <XhColorPickerPositioner>
      <XhColorPickerContent>
        <XhColorPickerSaturationArea>
          <XhColorPickerAreaThumb />
        </XhColorPickerSaturationArea>
        <XhColorPickerHueSlider />
        <XhColorPickerSwatchPicker v-if="swatches?.length" />
      </XhColorPickerContent>
    </XhColorPickerPositioner>
  </XhColorPickerRoot>
</template>
