<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import XDatePicker from '../common/XDatePicker.vue'

/**
 * 搜索区间日期组件（封装：双端日期 + 便捷预设区间）。
 * - 值为 [startTs, endTs]（毫秒时间戳）或 null，受控 v-model:value。
 * - 内置快捷区间：今天/昨天/近7天/近30天/本月/上月，摆在日历浮层里。
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

function startOfDay(date: Date): number {
  date.setHours(0, 0, 0, 0)
  return date.getTime()
}

function endOfDay(date: Date): number {
  date.setHours(23, 59, 59, 999)
  return date.getTime()
}

function dayRange(offsetStart: number, offsetEnd: number): [number, number] {
  const start = new Date()
  start.setDate(start.getDate() + offsetStart)
  const end = new Date()
  end.setDate(end.getDate() + offsetEnd)
  return [startOfDay(start), endOfDay(end)]
}

function monthRange(monthOffset: number): [number, number] {
  const now = new Date()
  const start = new Date(now.getFullYear(), now.getMonth() + monthOffset, 1)
  const end = new Date(now.getFullYear(), now.getMonth() + monthOffset + 1, 0)
  return [startOfDay(start), endOfDay(end)]
}

/** 便捷预设区间 */
const shortcuts = computed<Array<{ label: string, resolve: () => [number, number] }>>(() => [
  { label: t('component.search_date_range.today'), resolve: () => dayRange(0, 0) },
  { label: t('component.search_date_range.yesterday'), resolve: () => dayRange(-1, -1) },
  { label: t('component.search_date_range.last7'), resolve: () => dayRange(-6, 0) },
  { label: t('component.search_date_range.last30'), resolve: () => dayRange(-29, 0) },
  { label: t('component.search_date_range.this_month'), resolve: () => monthRange(0) },
  { label: t('component.search_date_range.last_month'), resolve: () => monthRange(-1) },
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
    class="schema-date-range"
    :value="value ?? null"
    :placeholder="placeholder ?? t('component.search_date_range.start')"
    :shortcuts="shortcuts"
    @update:value="onRangeChange"
  />
</template>

<style scoped>
.schema-date-range {
  min-width: 0;
}
</style>
