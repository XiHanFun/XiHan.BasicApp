<script setup lang="ts">
import { XhSpinner, XhTooltipArrow, XhTooltipContent, XhTooltipPositioner, XhTooltipRoot, XhTooltipTrigger } from '@xihan-ui/vue'
import { Icon } from '~/iconify'

defineOptions({ name: 'XIconButton', inheritAttrs: false })

withDefaults(defineProps<{
  /** iconify 图标名 */
  icon: string
  /** 提示文案，同时作为读屏名 */
  label: string
  loading?: boolean
  disabled?: boolean
}>(), {
  loading: false,
  disabled: false,
})

const emit = defineEmits<{ click: [event: MouseEvent] }>()
</script>

<template>
  <!-- 提示触发器本身就是那颗按钮：皮肤给的是透明底的 button，不必再套一层。
       根是 tooltip 的 fragment，落在标签上的属性接不住，转交给按钮 -->
  <XhTooltipRoot>
    <XhTooltipTrigger
      v-bind="$attrs"
      class="xh-icon-btn"
      :aria-label="label"
      :disabled="disabled || loading"
      @click="(event: MouseEvent) => emit('click', event)"
    >
      <XhSpinner v-if="loading" :label="label" />
      <Icon v-else :icon="icon" />
    </XhTooltipTrigger>
    <XhTooltipPositioner>
      <XhTooltipContent>
        {{ label }}
        <XhTooltipArrow />
      </XhTooltipContent>
    </XhTooltipPositioner>
  </XhTooltipRoot>
</template>
