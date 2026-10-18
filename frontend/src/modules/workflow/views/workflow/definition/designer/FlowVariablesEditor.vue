<script setup lang="ts">
import { XhCheckbox, XhFieldArrayAddTrigger, XhFieldArrayItem, XhFieldArrayItemAction, XhFieldArrayItemContent, XhFieldArrayItemDeleteTrigger, XhFieldArrayRoot } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { XInput } from '~/components'

/**
 * 启动变量声明的编辑器（设计器属性面板「启动变量」一行）：每行一个变量名与是否必填。
 *
 * 取值是引擎定义的 variables 数组；默认值与说明这里不编辑，原样带着走。
 */
defineOptions({ name: 'FlowVariablesEditor' })

const props = defineProps<{
  value?: unknown
  readonly: boolean
}>()

const emit = defineEmits<{
  'update:value': [value: FlowVariable[]]
  'commit': []
}>()

interface FlowVariable {
  name: string
  required: boolean
  defaultValue?: string | null
  description?: string | null
}

const { t } = useI18n()

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

const variables = computed<FlowVariable[]>(() => (Array.isArray(props.value) ? props.value : [])
  .filter(isRecord)
  .map(item => ({
    ...item,
    name: typeof item.name === 'string' ? item.name : '',
    required: item.required === true,
  })))

function update(index: number, patch: Partial<FlowVariable>) {
  emit('update:value', variables.value.map((item, current) => current === index ? { ...item, ...patch } : item))
}
</script>

<template>
  <!-- 行内控件没有可见标签：输入框给 aria-label，复选框把文字写进自己的插槽；失焦即一步编辑结束 -->
  <XhFieldArrayRoot
    v-slot="{ items }"
    :value="variables"
    :disabled="readonly"
    :create-item="() => ({ name: '', required: false, defaultValue: null, description: null })"
    @update:value="(next: unknown[]) => { emit('update:value', next as FlowVariable[]); emit('commit') }"
    @focusout="emit('commit')"
  >
    <XhFieldArrayItem v-for="item in items" :key="item.key" :index="item.index">
      <XhFieldArrayItemContent>
        <div class="flex w-full items-center gap-2">
          <XInput
            size="sm"
            :value="(item.value as FlowVariable).name"
            :aria-label="t('workflow.designer.variable_name')"
            :placeholder="t('workflow.designer.variable_name')"
            @update:value="(raw: string | number | (string | number)[] | null) => update(item.index, { name: String(raw ?? '') })"
          />
          <XhCheckbox
            size="sm"
            :checked="(item.value as FlowVariable).required"
            @update:checked="(checked: boolean) => { update(item.index, { required: checked }); emit('commit') }"
          >
            {{ t('workflow.designer.variable_required') }}
          </XhCheckbox>
        </div>
      </XhFieldArrayItemContent>
      <XhFieldArrayItemAction>
        <XhFieldArrayItemDeleteTrigger>−</XhFieldArrayItemDeleteTrigger>
      </XhFieldArrayItemAction>
    </XhFieldArrayItem>
    <XhFieldArrayAddTrigger>＋ {{ t('workflow.designer.variable_add') }}</XhFieldArrayAddTrigger>
  </XhFieldArrayRoot>
</template>
