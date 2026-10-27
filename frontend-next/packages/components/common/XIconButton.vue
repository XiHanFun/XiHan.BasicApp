<script setup lang="ts">
import type { ActionVariant } from '@xihan-ui/core'
import { XhButton, XhSpinner, XhTooltipArrow, XhTooltipContent, XhTooltipPositioner, XhTooltipRoot, XhTooltipTrigger } from '@xihan-ui/vue'
import { Icon } from '~/iconify'

defineOptions({ name: 'XIconButton', inheritAttrs: false })

withDefaults(defineProps<{
  /** iconify 图标名 */
  icon: string
  /** 提示文案，同时作为读屏名 */
  label: string
  /** 形态：缺省 ghost（透明底）；一排里的主操作写 solid（品牌实心） */
  variant?: ActionVariant
  loading?: boolean
  disabled?: boolean
}>(), {
  variant: 'ghost',
  loading: false,
  disabled: false,
})

const emit = defineEmits<{ click: [event: MouseEvent] }>()
</script>

<template>
  <!-- 提示触发器合到组件库的纯图标按钮上：尺寸随 sm 档与密度走，悬停、按下与禁用都由按钮给，
       与同排的菜单 / 浮层触发钮（同样是 sm 纯图标按钮）齐高。
       根是 tooltip 的 fragment，落在标签上的属性接不住，转交给按钮 -->
  <XhTooltipRoot>
    <XhTooltipTrigger as-child>
      <XhButton
        v-bind="$attrs"
        class="x-icon-btn"
        :variant="variant"
        size="sm"
        icon-only
        :aria-label="label"
        :disabled="disabled || loading"
        @click="(event: MouseEvent) => emit('click', event)"
      >
        <XhSpinner v-if="loading" :label="label" />
        <Icon v-else :icon="icon" />
      </XhButton>
    </XhTooltipTrigger>
    <XhTooltipPositioner>
      <XhTooltipContent>
        {{ label }}
        <XhTooltipArrow />
      </XhTooltipContent>
    </XhTooltipPositioner>
  </XhTooltipRoot>
</template>
