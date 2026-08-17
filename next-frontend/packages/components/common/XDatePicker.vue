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
  XhDatePickerInput,
  XhDatePickerNextTrigger,
  XhDatePickerPositioner,
  XhDatePickerPrevTrigger,
  XhDatePickerRoot,
  XhDatePickerSegment,
  XhDatePickerWeekDay,
  XhDatePickerWeekRow,
} from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useControlAttrs } from './control-attrs'

/**
 * 日期选择（单选 / 区间）。
 *
 * 组件库那侧是二十来个部件的完整日历，摆一遍要六十行；这里摆一次，全站复用。
 * 另一件必须收口的事是值类型：组件库收发 ISO 日期串（`YYYY-MM-DD`），
 * 而本应用上下游一律用时间戳（毫秒），换算在这里做。
 *
 * 时分秒不在本组件的处置范围：组件库的日期与时间是两个组件。datetime 字段
 * 取当日 00:00:00；需要精确到时分的场景另配时间选择。
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
}>(), {
  value: null,
  range: false,
  placeholder: undefined,
  clearable: true,
  disabled: false,
  size: 'sm',
})

const emit = defineEmits<{
  'update:value': [value: number | [number, number] | null]
}>()

// 字段挂来的 id 与 aria-* 转交给输入区，见 control-attrs.ts
const { attrs, controlAttrs } = useControlAttrs()

const { locale } = useI18n()

/** 时间戳 → 本地日历日的 ISO 串。用本地分量拼，避免 toISOString 的 UTC 偏移把日期挪一天 */
function toIso(timestamp: number): string {
  const date = new Date(timestamp)
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${date.getFullYear()}-${month}-${day}`
}

/** ISO 日历日 → 当日零点的时间戳（本地时区） */
function toTimestamp(iso: string): number {
  const [year, month, day] = iso.split('-').map(Number)
  return new Date(year ?? 1970, (month ?? 1) - 1, day ?? 1).getTime()
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
    emit('update:value', toTimestamp(next[0]!))
    return
  }
  // 区间只选了起点时不上抛：调用方拿到的区间要么两端齐备、要么为空
  if (next.length < 2) {
    return
  }
  emit('update:value', [toTimestamp(next[0]!), toTimestamp(next[1]!)])
}
</script>

<template>
  <XhDatePickerRoot
    v-slot="{ weeks, weekDays }"
    :class="attrs.class"
    :style="attrs.style"
    :value="isoValue"
    :locale="locale"
    :selection-mode="range ? 'range' : 'single'"
    :disabled="disabled"
    :size="size"
    @update:value="onValueChange"
  >
    <XhDatePickerControl>
      <XhDatePickerInput :aria-label="placeholder" v-bind="controlAttrs">
        <!-- 段位不写内容：显示什么由组件按当前值填 -->
        <XhDatePickerSegment :index="0" />
        <span>-</span>
        <XhDatePickerSegment :index="1" />
        <span>-</span>
        <XhDatePickerSegment :index="2" />
      </XhDatePickerInput>
      <XhDatePickerClearTrigger v-if="clearable">
        ✕
      </XhDatePickerClearTrigger>
    </XhDatePickerControl>
    <XhDatePickerPositioner>
      <XhDatePickerContent>
        <XhDatePickerCalendar>
          <XhDatePickerHeader>
            <XhDatePickerPrevTrigger>‹</XhDatePickerPrevTrigger>
            <XhDatePickerHeading />
            <XhDatePickerNextTrigger>›</XhDatePickerNextTrigger>
          </XhDatePickerHeader>
          <XhDatePickerGrid>
            <XhDatePickerGridHead>
              <XhDatePickerWeekRow>
                <XhDatePickerWeekDay v-for="d in weekDays" :key="d.value" :value="d.value" />
              </XhDatePickerWeekRow>
            </XhDatePickerGridHead>
            <XhDatePickerGridBody>
              <!-- v-for 必带 key：就地复用会让承载焦点的那一格换了身份 -->
              <XhDatePickerWeekRow v-for="week in weeks" :key="week[0]!.value">
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
