<script setup lang="ts">
import type { MenuNode } from '@xihan-ui/headless'
import type { Placement } from '@xihan-ui/kernel'
import { XhMenuRoot } from '@xihan-ui/vue'
import { computed } from 'vue'
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
  /** 浮层弹出位置 */
  placement?: Placement
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

const options = computed<MenuNode[]>(() =>
  ALIGNS.map(a => ({ value: a.value, label: t(a.labelKey) })),
)

/** 条目里的图标与选中态：collection 只带文本，图标与高亮走 item 插槽 */
const iconOf = computed<Record<string, string>>(() =>
  Object.fromEntries(ALIGNS.map(a => [a.value, a.icon])),
)

function choose(key: string) {
  emit('update:value', key as LayoutAlign)
  emit('change', key as LayoutAlign)
}
</script>

<template>
  <XhMenuRoot
    :collection="options"
    :placement="placement"
    @select="(details: { value: string }) => choose(details.value)"
  >
    <template #trigger>
      <slot />
    </template>
    <template #item="node">
      <span class="align-item" :class="{ 'align-item--active': node.value === props.value }">
        <Icon :icon="iconOf[node.value] ?? ''" width="14" height="14" />
        {{ node.label }}
      </span>
    </template>
  </XhMenuRoot>
</template>

<style scoped>
.align-item {
  display: inline-flex;
  gap: 6px;
  align-items: center;
}

/* 当前项高亮：主色 + 加粗 */
.align-item--active {
  color: var(--xh-fg-brand);
  font-weight: 600;
}
</style>
