<script lang="ts" setup generic="V extends string">
import { XhSegmentedRoot } from '@xihan-ui/vue'
import { computed } from 'vue'
import { Icon } from '~/iconify'
import { useControlAttrs } from './control-attrs'

/** 分段选择器：一组互斥选项排成按钮条，用于视图切换、格式切换这类小集合 */
defineOptions({ name: 'XSegmented', inheritAttrs: false })

const props = withDefaults(defineProps<{
  /** icon 为 iconify 名称（如 lucide:user），给了才渲染图标 */
  options: ReadonlyArray<{ label: string, value: V, disabled?: boolean, icon?: string }>
  size?: 'sm' | 'md' | 'lg'
  disabled?: boolean
  /** 铺满容器宽度，各段等分 */
  block?: boolean
  /** 只显示图标：放不下文字时用；文字退成 sr-only，每段仍有可读名称 */
  iconOnly?: boolean
}>(), {
  size: 'sm',
  disabled: false,
  block: false,
  iconOnly: false,
})

// 组本身就是控件，字段挂来的 id 与 aria-* 落在根上，见 control-attrs.ts
const { attrs, controlAttrs } = useControlAttrs()

const value = defineModel<V>('value', { required: true })

const collection = computed(() => props.options.map(option => ({
  value: String(option.value),
  label: option.label,
  ...(option.disabled ? { disabled: true } : {}),
})))

/** 组件库的 item 插槽只回传 value / label，图标按 value 回查 */
const iconByValue = computed(() => new Map(props.options.map(option => [String(option.value), option.icon])))

/** 选不空，机器给回 null 时保持原值 */
function onValueChange(details: { value: string | null }) {
  if (details.value != null) {
    value.value = details.value as V
  }
}
</script>

<template>
  <!-- 条目由 collection 代铺，滑动指示器与连体皮肤归组件库 -->
  <XhSegmentedRoot
    v-bind="controlAttrs"
    :class="attrs.class"
    :style="attrs.style"
    :collection="collection"
    :value="String(value)"
    :disabled="disabled"
    :size="size"
    :block="block"
    @value-change="onValueChange"
  >
    <template #item="node">
      <span class="x-segmented__item">
        <span v-if="iconByValue.get(node.value)" data-segmented-icon class="x-segmented__icon" aria-hidden="true">
          <Icon :icon="iconByValue.get(node.value)!" width="16" height="16" />
        </span>
        <span data-segmented-label :class="{ 'sr-only': iconOnly && iconByValue.get(node.value) }">{{ node.label }}</span>
      </span>
    </template>
  </XhSegmentedRoot>
</template>

<style scoped>
.x-segmented__item {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: var(--xh-space-1);
}

.x-segmented__icon {
  display: inline-flex;
}
</style>
