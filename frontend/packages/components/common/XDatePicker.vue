<script setup lang="ts">
import type { Size } from '@xihan-ui/core'
import {
  XhDatePickerCalendar,
  XhDatePickerCell,
  XhDatePickerCellTrigger,
  XhDatePickerClearTrigger,
  XhDatePickerConfirmTrigger,
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
  XhDatePickerPresetGroup,
  XhDatePickerPrevTrigger,
  XhDatePickerPrevYearTrigger,
  XhDatePickerRoot,
  XhDatePickerSegment,
  XhDatePickerSegmentGroup,
  XhDatePickerTimePanel,
  XhDatePickerTrigger,
  XhDatePickerWeekDay,
  XhDatePickerWeekRow,
} from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import { useControlAttrs } from './control-attrs'
import { isoToTimestamp, segmentLiteralBefore, timestampToIso } from './date-picker-value'
import { segmentGroupWiring } from './date-picker-wiring'

/**
 * 日期选择（单选）。区间选择是另一件组件，见 XDateRangePicker。
 *
 * 组件库那侧是二十来个部件的完整日历，摆一遍要六十行；这里摆一次，全站复用。
 * 另一件必须收口的事是值类型：组件库收发 ISO 串，本应用上下游一律用时间戳（毫秒），
 * 换算在这里做（见 date-picker-value.ts）。
 *
 * placeholder 是整条占位：一段都没填、焦点不在段上时代替各段的 yyyy / mm / dd 显示，只是视觉提示。
 * 不在字段里时，由调用方写 aria-label 给段位组起名。
 */
defineOptions({ name: 'XDatePicker', inheritAttrs: false })

const props = withDefaults(defineProps<{
  /** 时间戳（毫秒） */
  value?: number | null
  /** 带上时刻（时:分）：浮层多出时间列、由确认钮收口；不开时值取当日零点 */
  showTime?: boolean
  /** 可选下界（含当天），时间戳；showTime 时连时刻一起比 */
  min?: number | null
  /** 可选上界（含当天），时间戳；showTime 时连时刻一起比 */
  max?: number | null
  clearable?: boolean
  /** 不写时随外层 Field / Form 的 disabled 走；写了以本处为准 */
  disabled?: boolean
  /** 不写时随外层 Field / Form 走：浮层照常展开翻看，值不可改 */
  readOnly?: boolean
  /** 不写时随外层 Field / Form 走；填齐但越界时组件自己也会标不合法 */
  invalid?: boolean
  size?: Size
  /** 快捷选项：值取 datePickerPreset* 系列算出的串 */
  presets?: Array<{ label: string, value: string }>
  /** 整条占位文字 */
  placeholder?: string
}>(), {
  value: null,
  showTime: false,
  min: null,
  max: null,
  clearable: true,
  disabled: undefined,
  readOnly: undefined,
  invalid: undefined,
  size: 'sm',
  presets: undefined,
  placeholder: undefined,
})

const emit = defineEmits<{
  'update:value': [value: number | null]
}>()

// 字段挂来的 id 与 aria-* 转交给段位组，见 control-attrs.ts 与 date-picker-wiring.ts
const { attrs, controlAttrs } = useControlAttrs()
const groupAttrs = computed(() => segmentGroupWiring(controlAttrs.value))

const { locale, t } = useI18n()

const isoValue = computed<string[]>(() => (props.value == null ? [] : [timestampToIso(props.value, props.showTime)]))
const isoMin = computed(() => (props.min == null ? undefined : timestampToIso(props.min, props.showTime)))
const isoMax = computed(() => (props.max == null ? undefined : timestampToIso(props.max, props.showTime)))

function onValueChange(next: string[]): void {
  emit('update:value', next.length === 0 ? null : isoToTimestamp(next[0] ?? ''))
}
</script>

<template>
  <XhDatePickerRoot
    v-slot="{ panels, weeks, weekDays, segments }"
    :class="attrs.class"
    :style="attrs.style"
    :value="isoValue"
    :min="isoMin"
    :max="isoMax"
    :show-time="showTime"
    :locale="locale"
    :disabled="disabled"
    :read-only="readOnly"
    :invalid="invalid"
    :size="size"
    :presets="presets"
    :placeholder="placeholder"
    @update:value="onValueChange"
  >
    <XhDatePickerControl>
      <XhDatePickerSegmentGroup v-bind="groupAttrs">
        <template v-for="(seg, i) in segments" :key="seg.type">
          <!-- 分隔按段类型出：日期段之间 -，日期与时刻之间空格，时刻段之间 : -->
          <span v-if="i > 0">{{ segmentLiteralBefore(seg.type) }}</span>
          <!-- 段位不写内容：显示什么由组件按当前值填 -->
          <XhDatePickerSegment :index="i" />
        </template>
      </XhDatePickerSegmentGroup>
      <!-- 两颗都不写内容：字形由组件库出。有值时清空钮原位接替日历钮，无值时只露日历钮 -->
      <XhDatePickerClearTrigger v-if="clearable" />
      <!-- 在字段里时组件库把字段标签接成它的名字；不在字段里时读这条 -->
      <XhDatePickerTrigger :aria-label="t('component.date_picker.label')" />
    </XhDatePickerControl>
    <XhDatePickerPositioner>
      <!-- 浮层高度上限在令牌桥里按 14px 根补回（design/xihan-ui.css），这里不再另写 -->
      <XhDatePickerContent>
        <!-- 作者自己的包裹块：快捷选项、日历与时间列并排，放不下就折行；确认行另起一行 -->
        <div class="x-date-picker__panes">
          <!-- 不写默认插槽就按 presets 数据自动铺 -->
          <XhDatePickerPresetGroup v-if="presets?.length" />
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
                <XhDatePickerWeekRow v-for="week in (panel.weeks ?? weeks)" :key="week[0]!.start">
                  <XhDatePickerCell v-for="day in week" :key="day.start" :value="day.start">
                    <XhDatePickerCellTrigger>{{ day.day }}</XhDatePickerCellTrigger>
                  </XhDatePickerCell>
                </XhDatePickerWeekRow>
              </XhDatePickerGridBody>
            </XhDatePickerGrid>
          </XhDatePickerCalendar>
          <!-- 时、分各一列：皮肤按「日历后紧跟时间列」画分隔并对齐网格顶边 -->
          <XhDatePickerTimePanel v-if="showTime" />
        </div>
        <!-- 选完日期不收起，由确认钮收口；底部操作独占一行 -->
        <div v-if="showTime" class="x-date-picker__footer">
          <XhDatePickerConfirmTrigger>{{ t('common.actions.confirm') }}</XhDatePickerConfirmTrigger>
        </div>
      </XhDatePickerContent>
    </XhDatePickerPositioner>
  </XhDatePickerRoot>
</template>

<style scoped>
/* 与组件库「浮层直接摆快捷选项列 / 时间列」那几条同一排法：横排、放不下折行、同排等高 */
.x-date-picker__panes {
  display: flex;
  flex-wrap: wrap;
  align-items: stretch;
}

/* 负外边距吃掉浮层的内边距，分隔线贴满面板两边 */
.x-date-picker__footer {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  margin-block: var(--xh-space-2) calc(-1 * var(--xh-space-2));
  margin-inline: calc(-1 * var(--xh-space-2));
  padding-block: var(--xh-space-1);
  padding-inline: var(--xh-space-2);
  border-block-start: var(--xh-stroke-thin) solid var(--xh-border-subtle);
}
</style>
