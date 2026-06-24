<script setup lang="ts">
import type { DropdownOption, DropdownProps } from 'naive-ui'
import { NDropdown, NIcon } from 'naive-ui'
import { computed, h } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'

/**
 * 布局位置切换组件（登录页等用：居左/居中/居右）。
 * - 触发器经默认插槽传入；受控值 v-model:value。
 * - 下拉项含图标 + i18n 文案，当前选中项高亮（主色 + 加粗）。
 */
export type LayoutAlign = 'left' | 'center' | 'right'

defineOptions({ name: 'LayoutAlignSwitcher' })

const props = withDefaults(defineProps<{
  /** 当前位置（v-model） */
  value?: LayoutAlign
  /** NDropdown 弹出位置 */
  placement?: DropdownProps['placement']
}>(), {
  value: 'right',
  placement: 'bottom-end',
})

const emit = defineEmits<{
  'update:value': [LayoutAlign]
  'change': [LayoutAlign]
}>()

const { t } = useI18n()

const ALIGNS = [
  { value: 'left', labelKey: 'component.layout_align.left', icon: 'lucide:panel-left' },
  { value: 'center', labelKey: 'component.layout_align.center', icon: 'lucide:layout-panel-top' },
  { value: 'right', labelKey: 'component.layout_align.right', icon: 'lucide:panel-right' },
] as const

const options = computed<DropdownOption[]>(() =>
  ALIGNS.map((a) => {
    const active = a.value === props.value
    return {
      key: a.value,
      icon: () => h(NIcon, { size: 14 }, { default: () => h(Icon, { icon: a.icon }) }),
      // 当前选中项高亮：主色 + 加粗（内联样式，确保 teleport 弹层生效）
      label: () => h('span', {
        style: active ? { color: 'hsl(var(--primary))', fontWeight: 600 } : undefined,
      }, t(a.labelKey)),
    }
  }))

function choose(key: string) {
  emit('update:value', key as LayoutAlign)
  emit('change', key as LayoutAlign)
}
</script>

<template>
  <NDropdown :options="options" :placement="placement" @select="(k) => choose(String(k))">
    <slot />
  </NDropdown>
</template>
