<script lang="ts" setup>
import type { TreeSelectOption } from '~/types'
import { XhTreeSelectClearTrigger, XhTreeSelectContent, XhTreeSelectControl, XhTreeSelectIndicator, XhTreeSelectPositioner, XhTreeSelectRoot, XhTreeSelectTree, XhTreeSelectTrigger, XhTreeSelectValueText } from '@xihan-ui/vue'
import { computed } from 'vue'
import { Icon } from '~/iconify'
import { useControlAttrs } from './control-attrs'
import XTreeSelectNodes from './XTreeSelectNodes.vue'

defineOptions({ name: 'XTreeSelect', inheritAttrs: false })

const props = withDefaults(defineProps<{
  /** 选中值（v-model:value），单选 */
  value?: string | number | null | undefined
  options?: TreeSelectOption[]
  placeholder?: string
  disabled?: boolean
  clearable?: boolean
  loading?: boolean
}>(), {
  value: null,
  options: () => [],
  placeholder: undefined,
  disabled: false,
  clearable: false,
  loading: false,
})

const emit = defineEmits<{
  (e: 'update:value', value: string | number | null): void
}>()

// 字段挂来的 id 与 aria-* 转交给触发器，见 control-attrs.ts
const { attrs, controlAttrs } = useControlAttrs()

/** 根只收字符串数组，单选就是长度 0 或 1 */
const selected = computed<string[]>(() => (props.value == null || props.value === '' ? [] : [String(props.value)]))

/** 树的值恒为字符串，数字 id 在喂进去前统一转一道 */
function toNodes(options: TreeSelectOption[]): { value: string, label: string, disabled?: boolean, children?: ReturnType<typeof toNodes> }[] {
  return options.map(o => ({
    value: String(o.value),
    label: o.label,
    disabled: o.disabled,
    ...(o.children?.length ? { children: toNodes(o.children) } : {}),
  }))
}

/** 分支节点也要能被值解析到，整棵树都喂过去 */
const collection = computed(() => toNodes(props.options))

function handleChange(next: string[]) {
  emit('update:value', next[0] ?? null)
}
</script>

<template>
  <XhTreeSelectRoot
    :class="attrs.class"
    :style="attrs.style"
    :value="selected"
    :collection="collection"
    :placeholder="placeholder"
    :disabled="disabled || loading"
    @update:value="handleChange"
  >
    <!-- 视觉盒在 Control 上；清空钮是 Trigger 的兄弟，塞进去会变成按钮套按钮、且点它会冒泡把浮层打开 -->
    <XhTreeSelectControl>
      <XhTreeSelectTrigger v-bind="controlAttrs">
        <XhTreeSelectValueText />
        <XhTreeSelectIndicator>
          <Icon icon="lucide:chevron-down" width="14" height="14" />
        </XhTreeSelectIndicator>
      </XhTreeSelectTrigger>
      <XhTreeSelectClearTrigger v-if="clearable && selected.length" />
    </XhTreeSelectControl>
    <XhTreeSelectPositioner>
      <XhTreeSelectContent>
        <XhTreeSelectTree>
          <XTreeSelectNodes :nodes="options" />
        </XhTreeSelectTree>
      </XhTreeSelectContent>
    </XhTreeSelectPositioner>
  </XhTreeSelectRoot>
</template>
