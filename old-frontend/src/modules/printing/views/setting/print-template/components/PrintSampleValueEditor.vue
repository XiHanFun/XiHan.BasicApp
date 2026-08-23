<!--
  模拟打印数据单值编辑器。
  职责：根据注册字段类型或当前样例值选择 Naive UI 控件，并保持打印数据的字符串、数值和布尔类型。
-->
<script setup lang="ts">
import type { PrintSampleFormField, PrintSampleInputType } from '~/printing'
import { NDatePicker, NInput, NInputNumber, NSwitch } from 'naive-ui'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { inferPrintSampleInputType } from '~/printing'

defineOptions({ name: 'PrintSampleValueEditor' })

const props = defineProps<{
  field: PrintSampleFormField
  value: unknown
}>()

const emit = defineEmits<{
  'update:value': [value: unknown]
}>()

const { t } = useI18n()
const inputType = computed<PrintSampleInputType>(() => props.field.kind === 'image'
  || props.field.kind === 'barcode'
  || props.field.kind === 'qrcode'
  ? 'text'
  : inferPrintSampleInputType(props.field.inputType, props.value))
const placeholder = computed(() => props.field.placeholder
  || t('setting.print_template.sample_field_placeholder', { label: props.field.label }))
const stringValue = computed(() => props.value === null || props.value === undefined ? '' : String(props.value))
const numberValue = computed(() => typeof props.value === 'number' ? props.value : null)
const booleanValue = computed(() => props.value === true)
const dateValue = computed(() => stringValue.value || null)
const dateFormat = computed(() => inputType.value === 'datetime' ? 'yyyy-MM-dd HH:mm:ss' : 'yyyy-MM-dd')

/**
 * 发布文本值；不自动转换数字，避免条码、二维码和前导零编码被破坏。
 * @param value Naive UI 输入框的新值。
 * @returns 无返回值。
 */
function updateText(value: string): void {
  emit('update:value', value)
}

/**
 * 发布日期格式化字符串，清空时使用空字符串以符合 hiprint 文本字段语义。
 * @param value DatePicker 格式化值。
 * @returns 无返回值。
 */
function updateDate(value: null | string): void {
  emit('update:value', value ?? '')
}
</script>

<template>
  <NSwitch
    v-if="inputType === 'boolean'"
    :value="booleanValue"
    @update:value="emit('update:value', $event)"
  />
  <NInputNumber
    v-else-if="inputType === 'number'"
    :value="numberValue"
    :placeholder="placeholder"
    class="w-full"
    clearable
    @update:value="emit('update:value', $event)"
  />
  <NDatePicker
    v-else-if="inputType === 'date' || inputType === 'datetime'"
    :formatted-value="dateValue"
    :type="inputType === 'datetime' ? 'datetime' : 'date'"
    :value-format="dateFormat"
    :placeholder="placeholder"
    class="w-full"
    clearable
    @update:formatted-value="updateDate"
  />
  <NInput
    v-else
    :value="stringValue"
    :type="inputType === 'textarea' ? 'textarea' : 'text'"
    :autosize="inputType === 'textarea' ? { minRows: 2, maxRows: 5 } : false"
    :placeholder="placeholder"
    clearable
    @update:value="updateText"
  />
</template>
