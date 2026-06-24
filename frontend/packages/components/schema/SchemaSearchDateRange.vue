<script setup lang="ts">
import { NDatePicker } from 'naive-ui'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

/**
 * 搜索区间日期组件（封装：双端日期/时间 + 便捷预设区间）。
 * - 值为 [startTs, endTs]（毫秒时间戳）或 null，受控 v-model:value。
 * - type=date → 日期区间(daterange)；type=datetime → 日期时间区间(datetimerange)。
 * - 内置快捷区间：今天/昨天/近7天/近30天/本月/上月。
 */
defineOptions({ name: 'SchemaSearchDateRange' })

const props = withDefaults(defineProps<{
  /** 区间值 [开始, 结束]（毫秒时间戳） */
  value?: [number, number] | null
  /** 日期粒度 */
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

const pickerType = computed(() => (props.type === 'datetime' ? 'datetimerange' : 'daterange'))

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

/** 便捷预设区间（label → 返回 [start, end]） */
const shortcuts = computed<Record<string, () => [number, number]>>(() => ({
  [t('component.search_date_range.today')]: () => dayRange(0, 0),
  [t('component.search_date_range.yesterday')]: () => dayRange(-1, -1),
  [t('component.search_date_range.last7')]: () => dayRange(-6, 0),
  [t('component.search_date_range.last30')]: () => dayRange(-29, 0),
  [t('component.search_date_range.this_month')]: () => monthRange(0),
  [t('component.search_date_range.last_month')]: () => monthRange(-1),
}))
</script>

<template>
  <NDatePicker
    :value="value ?? null"
    :type="pickerType"
    clearable
    size="small"
    class="w-full"
    :shortcuts="shortcuts"
    :start-placeholder="placeholder ?? t('component.search_date_range.start')"
    :end-placeholder="t('component.search_date_range.end')"
    @update:value="(v) => emit('update:value', (v as [number, number] | null))"
  />
</template>
