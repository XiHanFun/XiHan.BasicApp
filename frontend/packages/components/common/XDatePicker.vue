<script setup lang="ts">
import type { Size } from '@xihan-ui/kernel'
import {
  XhDatePickerCalendar,
  XhDatePickerCell,
  XhDatePickerCellTrigger,
  XhDatePickerClearTrigger,
  XhDatePickerContent,
  XhDatePickerControl,
  XhDatePickerGrid,
  XhDatePickerGridBody,
  XhDatePickerGridHead,
  XhDatePickerHeader,
  XhDatePickerHeading,
  XhDatePickerNextTrigger,
  XhDatePickerNextYearTrigger,
  XhDatePickerPositioner,
  XhDatePickerPresets,
  XhDatePickerPrevTrigger,
  XhDatePickerPrevYearTrigger,
  XhDatePickerRoot,
  XhDatePickerSegment,
  XhDatePickerSegmentGroup,
  XhDatePickerWeekDay,
  XhDatePickerWeekRow,
} from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import { useControlAttrs } from './control-attrs'

/**
 * 日期选择（单选 / 区间）。
 *
 * 组件库那侧是二十来个部件的完整日历，摆一遍要六十行；这里摆一次，全站复用。
 * 另一件必须收口的事是值类型：组件库收发 ISO 日期串（`YYYY-MM-DD`），
 * 而本应用上下游一律用时间戳（毫秒），换算在这里做。
 */
defineOptions({ name: 'XDatePicker', inheritAttrs: false })

const props = withDefaults(defineProps<{
  /** 单选传时间戳，区间传 [起, 止] */
  value?: number | [number, number] | null
  /** 区间模式 */
  range?: boolean
  placeholder?: string
  clearable?: boolean
  disabled?: boolean
  size?: Size
  /** 快捷选项：值取 datePickerPreset* 系列算出的串 */
  presets?: Array<{ label: string, value: string }>
}>(), {
  value: null,
  range: false,
  placeholder: undefined,
  clearable: true,
  disabled: false,
  size: 'sm',
  presets: undefined,
})

const emit = defineEmits<{
  'update:value': [value: number | [number, number] | null]
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

const isoValue = computed<string[]>(() => {
  if (props.value == null) {
    return []
  }
  return Array.isArray(props.value) ? props.value.map(toIso) : [toIso(props.value)]
})

function onValueChange(next: string[]): void {
  if (next.length === 0) {
    emit('update:value', null)
    return
  }
  if (!props.range) {
    const single = toTimestamp(next[0] ?? '')
    emit('update:value', single)
    return
  }
  // 区间只选了起点时不上抛：调用方拿到的区间要么两端齐备、要么为空
  const start = toTimestamp(next[0] ?? '')
  const end = toTimestamp(next[1] ?? '')
  if (start == null || end == null) {
    return
  }
  emit('update:value', [start, end])
}
</script>

<template>
  <XhDatePickerRoot
    v-slot="{ panels, weeks, weekDays, segments, endSegments }"
    :class="attrs.class"
    :style="attrs.style"
    :value="isoValue"
    :locale="locale"
    :selection-mode="range ? 'range' : 'single'"
    :disabled="disabled"
    :size="size"
    :presets="presets"
    @update:value="onValueChange"
  >
    <XhDatePickerControl v-bind="controlAttrs" :aria-label="placeholder">
      <!-- 区间要两组段位：组号定这组认领哪一端，0 起点、1 终点 -->
      <XhDatePickerSegmentGroup
        v-for="end in (range ? 2 : 1)"
        :key="end"
        :index="end - 1"
      >
        <template v-for="(seg, i) in (end === 1 ? segments : endSegments)" :key="seg.type">
          <span v-if="i > 0">-</span>
          <!-- 段位不写内容：显示什么由组件按当前值填 -->
          <XhDatePickerSegment :index="i" />
        </template>
      </XhDatePickerSegmentGroup>
      <!-- 不写内容：字形由组件库出；无值时它自己收起 -->
      <XhDatePickerClearTrigger v-if="clearable" />
    </XhDatePickerControl>
    <XhDatePickerPositioner>
      <XhDatePickerContent>
        <!-- 不写默认插槽就按 presets 数据自动铺 -->
        <XhDatePickerPresets v-if="presets?.length" />
        <!-- 面板号写在日历上，面板内的标题、网格与格子跟着它走 -->
        <XhDatePickerCalendar v-for="panel in panels" :key="panel.index" :index="panel.index">
          <XhDatePickerHeader>
            <!-- 翻页整窗一起走：往前只画在最左那张，往后只画在最右那张。
                 年钮在外、月钮在内：一大步在外圈，一小步在内圈 -->
            <XhDatePickerPrevYearTrigger
              v-if="panel.index === 0"
              :aria-label="t('component.date_picker.prev_year')"
            >
              <Icon icon="lucide:chevrons-left" width="14" height="14" />
            </XhDatePickerPrevYearTrigger>
            <XhDatePickerPrevTrigger
              v-if="panel.index === 0"
              :aria-label="t('component.date_picker.prev_month')"
            >
              <Icon icon="lucide:chevron-left" width="14" height="14" />
            </XhDatePickerPrevTrigger>
            <XhDatePickerHeading />
            <XhDatePickerNextTrigger
              v-if="panel.index === panels.length - 1"
              :aria-label="t('component.date_picker.next_month')"
            >
              <Icon icon="lucide:chevron-right" width="14" height="14" />
            </XhDatePickerNextTrigger>
            <XhDatePickerNextYearTrigger
              v-if="panel.index === panels.length - 1"
              :aria-label="t('component.date_picker.next_year')"
            >
              <Icon icon="lucide:chevrons-right" width="14" height="14" />
            </XhDatePickerNextYearTrigger>
          </XhDatePickerHeader>
          <XhDatePickerGrid>
            <XhDatePickerGridHead>
              <XhDatePickerWeekRow>
                <XhDatePickerWeekDay v-for="d in weekDays" :key="d.value" :value="d.value" />
              </XhDatePickerWeekRow>
            </XhDatePickerGridHead>
            <XhDatePickerGridBody>
              <!-- v-for 必带 key：就地复用会让承载焦点的那一格换了身份 -->
              <XhDatePickerWeekRow v-for="week in (panel.weeks ?? weeks)" :key="week[0]!.value">
                <XhDatePickerCell v-for="day in week" :key="day.value" :value="day.value">
                  <XhDatePickerCellTrigger>{{ day.day }}</XhDatePickerCellTrigger>
                </XhDatePickerCell>
              </XhDatePickerWeekRow>
            </XhDatePickerGridBody>
          </XhDatePickerGrid>
        </XhDatePickerCalendar>
      </XhDatePickerContent>
    </XhDatePickerPositioner>
  </XhDatePickerRoot>
</template>
