<script setup lang="ts">
import { dateRangePickerPresetMonth, dateRangePickerPresetRange } from '@xihan-ui/headless'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import XDateRangePicker from '../common/XDateRangePicker.vue'

/**
 * 搜索区间日期组件（封装：双端日期 + 便捷预设区间）。
 * - 值为 [startTs, endTs]（毫秒时间戳）或 null，受控 v-model:value。
 * - 快捷区间交给组件库的一等部件，摆在日历浮层里。
 * - date 字段两端按整日取：起点当日 00:00、终点当日 23:59:59.999（查询侧 queryFiltersFromSchema 同此口径）。
 * - datetime 字段带上时刻（日志查询要精确到分）：只点日期时两端补 00:00 与 23:59，
 *   终点含这一分钟、补到 59.999 秒；查询侧对 datetime 原样使用端点。
 */
defineOptions({ name: 'SchemaSearchDateRange' })

const props = withDefaults(defineProps<{
  /** 区间值 [开始, 结束]（毫秒时间戳） */
  value?: [number, number] | null
  /** 日期粒度：datetime 带时刻，date 只到日 */
  type?: 'date' | 'datetime'
}>(), {
  value: null,
  type: 'datetime',
})

const emit = defineEmits<{
  'update:value': [[number, number] | null]
}>()

const { t } = useI18n()

const withTime = computed(() => props.type === 'datetime')

/** 只点日期时两端补的时刻：整日起止 */
const DEFAULT_TIME: [string, string] = ['00:00', '23:59']

/** 终点含到哪：带时刻时含这一分钟，只到日时含这一整天 */
function inclusiveEnd(timestamp: number): number {
  const date = new Date(timestamp)
  if (withTime.value) {
    date.setSeconds(59, 999)
  }
  else {
    date.setHours(23, 59, 59, 999)
  }
  return date.getTime()
}

/**
 * 便捷预设区间。日子在 computed 里算一次：连接层每帧都会跑一遍，
 * 把「今天」放进渲染期会跨零点算出两个答案。
 */
const presets = computed(() => [
  { label: t('component.search_date_range.today'), value: dateRangePickerPresetRange(0, 0) },
  { label: t('component.search_date_range.yesterday'), value: dateRangePickerPresetRange(-1, -1) },
  { label: t('component.search_date_range.last7'), value: dateRangePickerPresetRange(-6, 0) },
  { label: t('component.search_date_range.last30'), value: dateRangePickerPresetRange(-29, 0) },
  { label: t('component.search_date_range.this_month'), value: dateRangePickerPresetMonth(0) },
  { label: t('component.search_date_range.last_month'), value: dateRangePickerPresetMonth(-1) },
])

/**
 * 选择器给的终点落在那一刻的开头（只到日时是零点、带时刻时是整分）；补成含到这一天 / 这一分钟，
 * 否则「选到今天」会把今天整天排除在外，「选到 18:00」会漏掉 18:00 那一分钟里的记录。
 */
function onRangeChange(next: [number, number] | null): void {
  if (next == null) {
    emit('update:value', null)
    return
  }
  emit('update:value', [next[0], inclusiveEnd(next[1])])
}
</script>

<template>
  <XDateRangePicker
    clearable
    size="sm"
    class="w-full"
    :value="value ?? null"
    :presets="presets"
    :show-time="withTime"
    :default-time="withTime ? DEFAULT_TIME : undefined"
    :start-placeholder="t('component.search_date_range.start')"
    :end-placeholder="t('component.search_date_range.end')"
    @update:value="onRangeChange"
  />
</template>
