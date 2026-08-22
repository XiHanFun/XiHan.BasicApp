<script setup lang="ts">
import { datePickerPresetMonth, datePickerPresetRange } from '@xihan-ui/headless'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import XDatePicker from '../common/XDatePicker.vue'

/**
 * 搜索区间日期组件（封装：双端日期 + 便捷预设区间）。
 * - 值为 [startTs, endTs]（毫秒时间戳）或 null，受控 v-model:value。
 * - 快捷区间交给组件库的一等部件，摆在日历浮层里。
 * - 起止两端按整日取：起点当日 00:00:00、终点当日 23:59:59.999，datetime 字段同此口径
 *   （查询侧 queryFiltersFromSchema 也是按整日补齐的）。
 */
defineOptions({ name: 'SchemaSearchDateRange' })

withDefaults(defineProps<{
  /** 区间值 [开始, 结束]（毫秒时间戳） */
  value?: [number, number] | null
  /** 日期粒度。两档都按整日取端点，此处只作调用方语义标注 */
  type?: 'date' | 'datetime'
  /** 占位（用于开始/结束输入框） */
  placeholder?: string
}>(), {
  value: null,
  type: 'datetime',
})

const emit = defineEmits<{
  'update:value': [[number, number] | null]
}>()

const { t } = useI18n()

function endOfDay(date: Date): number {
  date.setHours(23, 59, 59, 999)
  return date.getTime()
}

/**
 * 便捷预设区间。日子在 computed 里算一次：连接层每帧都会跑一遍，
 * 把「今天」放进渲染期会跨零点算出两个答案。
 */
const presets = computed(() => [
  { label: t('component.search_date_range.today'), value: datePickerPresetRange(0, 0) },
  { label: t('component.search_date_range.yesterday'), value: datePickerPresetRange(-1, -1) },
  { label: t('component.search_date_range.last7'), value: datePickerPresetRange(-6, 0) },
  { label: t('component.search_date_range.last30'), value: datePickerPresetRange(-29, 0) },
  { label: t('component.search_date_range.this_month'), value: datePickerPresetMonth(0) },
  { label: t('component.search_date_range.last_month'), value: datePickerPresetMonth(-1) },
])

/**
 * 日历给的是整日零点的两端；终点补到当日 23:59:59.999，
 * 否则「选到今天」会把今天整天排除在外。
 */
function onRangeChange(next: number | [number, number] | null): void {
  if (next == null) {
    emit('update:value', null)
    return
  }
  if (!Array.isArray(next)) {
    return
  }
  emit('update:value', [next[0], endOfDay(new Date(next[1]))])
}
</script>

<template>
  <XDatePicker
    range
    clearable
    size="sm"
    class="w-full"
    :value="value ?? null"
    :presets="presets"
    :placeholder="placeholder ?? t('component.search_date_range.start')"
    @update:value="onRangeChange"
  />
</template>
