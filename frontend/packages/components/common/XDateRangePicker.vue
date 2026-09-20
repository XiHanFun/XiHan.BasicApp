<script setup lang="ts">
import type { Size } from '@xihan-ui/core'
import {
  XhDateRangePickerCalendar,
  XhDateRangePickerCell,
  XhDateRangePickerCellTrigger,
  XhDateRangePickerClearTrigger,
  XhDateRangePickerContent,
  XhDateRangePickerControl,
  XhDateRangePickerGrid,
  XhDateRangePickerGridBody,
  XhDateRangePickerGridHead,
  XhDateRangePickerHeader,
  XhDateRangePickerHeading,
  XhDateRangePickerNextTrigger,
  XhDateRangePickerNextYearTrigger,
  XhDateRangePickerPositioner,
  XhDateRangePickerPresetGroup,
  XhDateRangePickerPrevTrigger,
  XhDateRangePickerPrevYearTrigger,
  XhDateRangePickerRangeSeparator,
  XhDateRangePickerRoot,
  XhDateRangePickerSegment,
  XhDateRangePickerSegmentGroup,
  XhDateRangePickerWeekDay,
  XhDateRangePickerWeekRow,
} from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import { useControlAttrs } from './control-attrs'

/**
 * 日期区间选择。单日选择是另一件组件，见 XDatePicker。
 *
 * 组件库把区间选择从 DatePicker 拆成了独立的 DateRangePicker：两组段位各认领一端，
 * 值恒为 [起, 止] 两个 ISO 日期串（`YYYY-MM-DD`）。本应用上下游一律用时间戳（毫秒），
 * 换算在这里做；只落下一端时不上抛，调用方拿到的区间要么两端齐备、要么为空。
 */
defineOptions({ name: 'XDateRangePicker', inheritAttrs: false })

const props = withDefaults(defineProps<{
  /** [起, 止] 时间戳（毫秒） */
  value?: [number, number] | null
  placeholder?: string
  clearable?: boolean
  disabled?: boolean
  size?: Size
  /** 快捷选项：值取 dateRangePickerPreset* 系列算出的串 */
  presets?: Array<{ label: string, value: string }>
}>(), {
  value: null,
  placeholder: undefined,
  clearable: true,
  disabled: false,
  size: 'sm',
  presets: undefined,
})

const emit = defineEmits<{
  'update:value': [value: [number, number] | null]
}>()

// 字段挂来的 id 与 aria-* 转交给输入区，见 control-attrs.ts
const { attrs, controlAttrs } = useControlAttrs()

const { locale, t } = useI18n()

/** 时间戳 → 本地日历日的 ISO 串。用本地分量拼，避免 toISOString 的 UTC 偏移把日期挪一天 */
function toIso(timestamp: number): string {
  const date = new Date(timestamp)
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${date.getFullYear()}-${month}-${day}`
}

/** ISO 日历日 → 当日零点的时间戳（本地时区）；空串按无值处理 */
function toTimestamp(iso: string): number | null {
  if (!iso) {
    return null
  }
  const [year, month, day] = iso.split('-').map(Number)
  if (!year || !month || !day) {
    return null
  }
  return new Date(year, month - 1, day).getTime()
}

/** 组件库的受控值按位存放：两端齐备是 [起, 止]，全空是空数组 */
const isoValue = computed<string[]>(() => (props.value == null ? [] : props.value.map(toIso)))

function onValueChange(next: string[]): void {
  if (next.length === 0) {
    emit('update:value', null)
    return
  }
  // 只落下起点或只落下终点时不上抛：调用方拿到的区间要么两端齐备、要么为空
  const start = toTimestamp(next[0] ?? '')
  const end = toTimestamp(next[1] ?? '')
  if (start == null || end == null) {
    return
  }
  emit('update:value', [start, end])
}
</script>

<template>
  <XhDateRangePickerRoot
    v-slot="{ panels, weeks, weekDays, segments, endSegments }"
    :class="attrs.class"
    :style="attrs.style"
    :value="isoValue"
    :locale="locale"
    :disabled="disabled"
    :size="size"
    :presets="presets"
    @update:value="onValueChange"
  >
    <XhDateRangePickerControl v-bind="controlAttrs" :aria-label="placeholder">
      <!-- 两组段位：组号定这组认领哪一端，0 起点、1 终点 -->
      <XhDateRangePickerSegmentGroup :index="0">
        <template v-for="(seg, i) in segments" :key="seg.type">
          <span v-if="i > 0">-</span>
          <!-- 段位不写内容：显示什么由组件按当前值填 -->
          <XhDateRangePickerSegment :index="i" />
        </template>
      </XhDateRangePickerSegmentGroup>
      <XhDateRangePickerRangeSeparator />
      <XhDateRangePickerSegmentGroup :index="1">
        <template v-for="(seg, i) in endSegments" :key="seg.type">
          <span v-if="i > 0">-</span>
          <XhDateRangePickerSegment :index="i" />
        </template>
      </XhDateRangePickerSegmentGroup>
      <!-- 不写内容：字形由组件库出；无值时它自己收起 -->
      <XhDateRangePickerClearTrigger v-if="clearable" />
    </XhDateRangePickerControl>
    <XhDateRangePickerPositioner>
      <XhDateRangePickerContent>
        <!-- 不写默认插槽就按 presets 数据自动铺 -->
        <XhDateRangePickerPresetGroup v-if="presets?.length" />
        <!-- 面板号写在日历上，面板内的标题、网格与格子跟着它走 -->
        <XhDateRangePickerCalendar v-for="panel in panels" :key="panel.index" :index="panel.index">
          <XhDateRangePickerHeader>
            <!-- 翻页整窗一起走：往前只画在最左那张，往后只画在最右那张。
                 年钮在外、月钮在内：一大步在外圈，一小步在内圈 -->
            <XhDateRangePickerPrevYearTrigger
              v-if="panel.index === 0"
              :aria-label="t('component.date_picker.prev_year')"
            >
              <Icon icon="lucide:chevrons-left" width="14" height="14" />
            </XhDateRangePickerPrevYearTrigger>
            <XhDateRangePickerPrevTrigger
              v-if="panel.index === 0"
              :aria-label="t('component.date_picker.prev_month')"
            >
              <Icon icon="lucide:chevron-left" width="14" height="14" />
            </XhDateRangePickerPrevTrigger>
            <XhDateRangePickerHeading />
            <XhDateRangePickerNextTrigger
              v-if="panel.index === panels.length - 1"
              :aria-label="t('component.date_picker.next_month')"
            >
              <Icon icon="lucide:chevron-right" width="14" height="14" />
            </XhDateRangePickerNextTrigger>
            <XhDateRangePickerNextYearTrigger
              v-if="panel.index === panels.length - 1"
              :aria-label="t('component.date_picker.next_year')"
            >
              <Icon icon="lucide:chevrons-right" width="14" height="14" />
            </XhDateRangePickerNextYearTrigger>
          </XhDateRangePickerHeader>
          <XhDateRangePickerGrid>
            <XhDateRangePickerGridHead>
              <XhDateRangePickerWeekRow>
                <XhDateRangePickerWeekDay v-for="d in weekDays" :key="d.value" :value="d.value" />
              </XhDateRangePickerWeekRow>
            </XhDateRangePickerGridHead>
            <XhDateRangePickerGridBody>
              <!-- v-for 必带 key：就地复用会让承载焦点的那一格换了身份 -->
              <XhDateRangePickerWeekRow v-for="week in (panel.weeks ?? weeks)" :key="week[0]!.start">
                <XhDateRangePickerCell v-for="day in week" :key="day.start" :value="day.start">
                  <XhDateRangePickerCellTrigger>{{ day.day }}</XhDateRangePickerCellTrigger>
                </XhDateRangePickerCell>
              </XhDateRangePickerWeekRow>
            </XhDateRangePickerGridBody>
          </XhDateRangePickerGrid>
        </XhDateRangePickerCalendar>
      </XhDateRangePickerContent>
    </XhDateRangePickerPositioner>
  </XhDateRangePickerRoot>
</template>
