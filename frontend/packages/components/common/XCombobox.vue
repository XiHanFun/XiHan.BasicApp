<script setup lang="ts">
import type { Size } from '@xihan-ui/core'
import {
  XhComboboxClearTrigger,
  XhComboboxContent,
  XhComboboxControl,
  XhComboboxEmpty,
  XhComboboxInput,
  XhComboboxItem,
  XhComboboxItemDeleteTrigger,
  XhComboboxItemDescription,
  XhComboboxItemIndicator,
  XhComboboxItemText,
  XhComboboxLabel,
  XhComboboxLoading,
  XhComboboxOverflowTag,
  XhComboboxPositioner,
  XhComboboxRoot,
  XhComboboxTag,
  XhComboboxTagLabel,
  XhComboboxTagList,
  XhComboboxTrigger,
} from '@xihan-ui/vue'
import { computed, onBeforeUnmount, ref, shallowReactive, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useControlAttrs } from './control-attrs'

/**
 * 可搜索的下拉选择：输入框 + 候选列表，取值方式与 XSelect 一致。
 *
 * 组件库的 combobox 只管显示，筛哪些条目、输入框里写什么都归调用方，这里把全站一样的那套收拢：
 * 1. 值类型。选中值一律是 `string[]`，按 String(value) 建映射，收上来再还原成原类型（同 XSelect）。
 * 2. 输入框文字。由本组件受控：没在打字时显示选中项的文字（选项晚到、换语言后跟着变），
 *    打字时显示打的字；收起或失焦即复位，不留半截筛选串。
 * 3. 筛选。缺省按文字（含副文本）在本地筛；remote 时不筛，候选全由调用方据 search 换。
 * 4. 检索。打字停顿后发出 search，同一串不重复发；清空、选中后回到空串也会发一次，供远程换回缺省清单。
 *
 * 落在这个标签上的 id 与 aria-* 转交给输入框；aria-label 改放进视觉隐藏的 label 部件，
 * 因为输入框的名字由 aria-labelledby 指向 label 部件给出，直接写的 aria-label 会被它盖掉。
 */
defineOptions({ name: 'XCombobox', inheritAttrs: false })

const props = withDefaults(defineProps<{
  /** 选项；description 是第 2 行副文本，同样参与本地筛选 */
  options?: ReadonlyArray<{ label: string, value: string | number, description?: string, disabled?: boolean }>
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
  /** 候选正在取回 */
  loading?: boolean
  /** 远程检索：不在本地筛，候选由调用方据 search 事件换 */
  remote?: boolean
  /** search 的防抖毫秒数 */
  debounce?: number
  /** 无候选时的提示；缺省按有无检索串取「暂无数据 / 没有匹配的结果」 */
  empty?: string
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
  remote: false,
  debounce: 300,
  empty: undefined,
})

const emit = defineEmits<{
  'update:value': [value: string | number | Array<string | number> | null]
  /** 检索串（已去首尾空白），打字停顿后发出 */
  'search': [keyword: string]
}>()

const { t } = useI18n()
const { attrs, controlAttrs } = useControlAttrs()

const ariaLabel = computed(() => {
  const label = attrs['aria-label']
  return typeof label === 'string' && label !== '' ? label : undefined
})
const inputAttrs = computed(() => {
  const rest = { ...controlAttrs.value }
  delete rest['aria-label']
  return rest
})

/**
 * 见过的选项（文字与原始值）。远程检索会把候选整批换掉，已选那条不在新候选里时，
 * 输入框与标签仍要念得出它的名字、删掉别的标签时它也不能被收成字符串。
 */
const known = shallowReactive(new Map<string, { label: string, value: string | number }>())
watch(() => props.options, (options) => {
  for (const option of options) {
    known.set(String(option.value), { label: option.label, value: option.value })
  }
}, { immediate: true })

const currentByKey = computed(() => new Map(props.options.map(option => [String(option.value), option])))

const selected = computed<string[]>(() => {
  if (props.value == null) {
    return []
  }
  return Array.isArray(props.value) ? props.value.map(String) : [String(props.value)]
})

/** 选中项文字：当前选项优先，其次见过的，都没有退回值本身（与 XSelect 的值文本同一口径） */
function labelOf(key: string): string {
  return currentByKey.value.get(key)?.label ?? known.get(key)?.label ?? key
}

/** String(value) → 原始值，收上来时按此还原，数字选项不会变成字符串 */
function restore(key: string): string | number {
  const held = Array.isArray(props.value) ? props.value.find(value => String(value) === key) : props.value
  return currentByKey.value.get(key)?.value ?? known.get(key)?.value ?? (held != null && String(held) === key ? held : key)
}

/** 正在打的字；null 表示没在打字，输入框显示选中项文字 */
const draft = ref<string | null>(null)

const inputValue = computed(() => {
  if (draft.value !== null) {
    return draft.value
  }
  const first = selected.value[0]
  return props.multiple || first === undefined ? '' : labelOf(first)
})

const keyword = computed(() => (draft.value ?? '').trim())

const collection = computed(() => {
  const nodes = props.options.map(option => ({
    value: String(option.value),
    label: option.label,
    ...(option.description ? { description: option.description } : {}),
    ...(option.disabled ? { disabled: true } : {}),
  }))
  const needle = keyword.value.toLowerCase()
  if (props.remote || needle === '') {
    return nodes
  }
  return nodes.filter(node => node.label.toLowerCase().includes(needle) || node.description?.toLowerCase().includes(needle))
})

const emptyText = computed(() => props.empty ?? (keyword.value === '' ? t('common.no_data') : t('common.no_result')))

// 选中值换了（选中、清空、外部改值）就不再算在打字：输入框回到选中项文字
watch(() => selected.value.join('\u0000'), () => {
  draft.value = null
})

let timer: ReturnType<typeof setTimeout> | undefined
let lastSearched = ''

watch(keyword, (next) => {
  clearTimeout(timer)
  timer = setTimeout(() => {
    if (next === lastSearched) {
      return
    }
    lastSearched = next
    emit('search', next)
  }, props.debounce)
})

onBeforeUnmount(() => clearTimeout(timer))

function onValueChange(next: string[]): void {
  const restored = next.map(key => restore(key))
  if (props.multiple) {
    emit('update:value', restored)
    return
  }
  emit('update:value', restored[0] ?? null)
}

/** 打字、选中回填、清空都从这里进来；后两种随后有收起或换值来复位 */
function onInputValueChange(details: { inputValue: string }): void {
  draft.value = details.inputValue
}

function onOpenChange(details: { open: boolean }): void {
  if (!details.open) {
    draft.value = null
  }
}

/** 焦点离开整个控件才复位：收起态失焦不发 open-change，没提交的字也要收回去 */
function onInputBlur(event: FocusEvent): void {
  const root = (event.currentTarget as HTMLElement | null)?.closest('[data-scope="combobox"][data-part="root"]')
  if (root && event.relatedTarget instanceof Node && root.contains(event.relatedTarget)) {
    return
  }
  draft.value = null
}
</script>

<template>
  <XhComboboxRoot
    v-slot="{ tags }"
    :class="attrs.class"
    :style="attrs.style"
    :collection="collection"
    :value="selected"
    :input-value="inputValue"
    :multiple="multiple"
    :disabled="disabled"
    :read-only="readOnly"
    :invalid="invalid"
    :loading="loading"
    :placeholder="placeholder"
    :size="size"
    :max-tag-count="maxTagCount"
    input-behavior="autohighlight"
    open-on-click
    @update:value="onValueChange"
    @input-value-change="onInputValueChange"
    @open-change="onOpenChange"
  >
    <XhComboboxLabel v-if="ariaLabel" class="sr-only">
      {{ ariaLabel }}
    </XhComboboxLabel>
    <XhComboboxControl>
      <XhComboboxTagList v-if="multiple">
        <XhComboboxTag v-for="tag in tags" :key="tag.value" :value="tag.value">
          <XhComboboxTagLabel>{{ labelOf(tag.value) }}</XhComboboxTagLabel>
          <XhComboboxItemDeleteTrigger />
        </XhComboboxTag>
        <XhComboboxOverflowTag />
      </XhComboboxTagList>
      <XhComboboxInput v-bind="inputAttrs" @blur="onInputBlur" />
      <XhComboboxClearTrigger v-if="clearable" />
      <XhComboboxTrigger />
    </XhComboboxControl>
    <XhComboboxPositioner>
      <XhComboboxContent>
        <XhComboboxItem v-for="node in collection" :key="node.value" :value="node.value">
          <XhComboboxItemText>{{ node.label }}</XhComboboxItemText>
          <XhComboboxItemDescription v-if="node.description">
            {{ node.description }}
          </XhComboboxItemDescription>
          <XhComboboxItemIndicator />
        </XhComboboxItem>
      </XhComboboxContent>
      <!-- content 本身就是 listbox，在途与空态是它的兄弟；何时露面由组件库按 loading 与条数收放 -->
      <XhComboboxLoading>{{ t('common.loading') }}</XhComboboxLoading>
      <XhComboboxEmpty>{{ emptyText }}</XhComboboxEmpty>
    </XhComboboxPositioner>
  </XhComboboxRoot>
</template>
