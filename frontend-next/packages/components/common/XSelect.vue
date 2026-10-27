<script setup lang="ts">
import type { Size } from '@xihan-ui/core'
import {
  XhSelectClearTrigger,
  XhSelectContent,
  XhSelectControl,
  XhSelectEmpty,
  XhSelectIndicator,
  XhSelectItem,
  XhSelectItemIndicator,
  XhSelectItemText,
  XhSelectLabel,
  XhSelectList,
  XhSelectLoading,
  XhSelectOverflowTag,
  XhSelectPositioner,
  XhSelectRoot,
  XhSelectTag,
  XhSelectTagList,
  XhSelectTrigger,
  XhSelectValueText,
} from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useControlAttrs } from './control-attrs'

/**
 * 下拉选择。
 *
 * 存在的理由是两件组件库不管、而全站每个下拉都要做的事：
 * 1. 值类型。组件库的选中值一律是 `string[]`（单选也是长度 1 的数组），而业务里的选项值
 *    大量是数字（枚举）。这里按 String(value) 建映射，收上来再还原成原类型。
 * 2. 清除钮。它是一个要自己摆的部件，不是 prop。
 *
 * 其余能力（collection 铺开条目、placeholder、多选标签折叠、加载与空态的收放）都由组件库给。
 * 只能从已知清单里挑；要打字筛选或远程检索的用 XCombobox。
 */
defineOptions({ name: 'XSelect', inheritAttrs: false })

const props = withDefaults(defineProps<{
  /** 选项 */
  options?: ReadonlyArray<{ label: string, value: string | number, disabled?: boolean }>
  /** 选中值：单选传标量，多选传数组 */
  value?: string | number | Array<string | number> | null
  multiple?: boolean
  clearable?: boolean
  /** 三态不写时随外层 Field / Form 走；写了以本处为准 */
  disabled?: boolean
  readOnly?: boolean
  invalid?: boolean
  placeholder?: string
  size?: Size
  /** 多选标签超出几个后折叠成 +N */
  maxTagCount?: number
  /** 选项正在取回 */
  loading?: boolean
}>(), {
  options: () => [],
  value: null,
  multiple: false,
  clearable: false,
  disabled: undefined,
  readOnly: undefined,
  invalid: undefined,
  placeholder: undefined,
  size: 'sm',
  maxTagCount: undefined,
  loading: false,
})

const emit = defineEmits<{
  'update:value': [value: string | number | Array<string | number> | null]
}>()

// 字段挂来的 id 与 aria-* 转交给触发器，见 control-attrs.ts
const { t } = useI18n()
const { attrs, controlAttrs } = useControlAttrs()

/**
 * 触发器的名字由 aria-labelledby（label 部件 + 值文本）给出，写在它身上的 aria-label 会被盖掉。
 * 调用方给了 aria-label 就改放进一个视觉隐藏的 label 部件，名字才念得出来。
 */
const ariaLabel = computed(() => {
  const label = attrs['aria-label']
  return typeof label === 'string' && label !== '' ? label : undefined
})
const triggerAttrs = computed(() => {
  const rest = { ...controlAttrs.value }
  delete rest['aria-label']
  return rest
})

const collection = computed(() => props.options.map(option => ({
  value: String(option.value),
  label: option.label,
  ...(option.disabled ? { disabled: true } : {}),
})))

/** String(value) → 原始值，收上来时按此还原，数字选项不会变成字符串 */
const originalByKey = computed(() => new Map(props.options.map(option => [String(option.value), option.value])))

const selected = computed<string[]>(() => {
  if (props.value == null) {
    return []
  }
  return Array.isArray(props.value) ? props.value.map(String) : [String(props.value)]
})

function onValueChange(next: string[]): void {
  const restored = next.map(key => originalByKey.value.get(key) ?? key)
  if (props.multiple) {
    emit('update:value', restored)
    return
  }
  emit('update:value', restored[0] ?? null)
}
</script>

<template>
  <!-- lazy-mount：条目第一次展开才挂、之后常驻，表单里成片的下拉不再各自先铺一遍列表；
       收起时的选中文字与连打定位按 collection 算，所以 collection 必须传全 -->
  <XhSelectRoot
    v-slot="{ tags }"
    :class="attrs.class"
    :style="attrs.style"
    :collection="collection"
    :value="selected"
    :multiple="multiple"
    :disabled="disabled"
    :read-only="readOnly"
    :invalid="invalid"
    :loading="loading"
    :placeholder="placeholder"
    :size="size"
    :max-tag-count="maxTagCount"
    lazy-mount
    @update:value="onValueChange"
  >
    <XhSelectLabel v-if="ariaLabel" class="sr-only">
      {{ ariaLabel }}
    </XhSelectLabel>
    <XhSelectControl>
      <XhSelectTrigger v-bind="triggerAttrs">
        <!-- 多选时标签行与值文本同写：有选中时标签露面、值文本让位，但仍留在 DOM 里供触发器取名 -->
        <XhSelectValueText />
        <XhSelectTagList v-if="multiple">
          <XhSelectTag v-for="tag in tags" :key="tag.value" :value="tag.value">
            {{ tag.label }}
          </XhSelectTag>
          <XhSelectOverflowTag />
        </XhSelectTagList>
        <XhSelectIndicator />
      </XhSelectTrigger>
      <XhSelectClearTrigger v-if="clearable" />
    </XhSelectControl>
    <XhSelectPositioner>
      <XhSelectContent>
        <!-- 列表不随在途收放：它是触发器 aria-controls 指向的 listbox，后台刷新时也保留上一帧 -->
        <XhSelectList>
          <XhSelectItem v-for="node in collection" :key="node.value" :value="node.value">
            <XhSelectItemText>{{ node.label }}</XhSelectItemText>
            <XhSelectItemIndicator />
          </XhSelectItem>
        </XhSelectList>
        <!-- 在途与空态是 list 的兄弟，何时露面由组件库按 loading 与条数收放 -->
        <XhSelectLoading>{{ t('common.loading') }}</XhSelectLoading>
        <XhSelectEmpty>{{ t('common.no_data') }}</XhSelectEmpty>
      </XhSelectContent>
    </XhSelectPositioner>
  </XhSelectRoot>
</template>
