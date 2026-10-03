<script setup lang="ts">
import type { Size } from '@xihan-ui/core'
import {
  XhDateRangePickerCalendar,
  XhDateRangePickerCell,
  XhDateRangePickerCellTrigger,
  XhDateRangePickerClearTrigger,
  XhDateRangePickerConfirmTrigger,
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
  XhDateRangePickerTimePanel,
  XhDateRangePickerTrigger,
  XhDateRangePickerWeekDay,
  XhDateRangePickerWeekRow,
} from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import { useControlAttrs } from './control-attrs'
import { rangeToDraft, resolveRangeChange, segmentLiteralBefore } from './date-picker-value'
import { endSegmentGroupWiring, segmentGroupWiring } from './date-picker-wiring'

/**
 * 日期区间选择。单日选择是另一件组件，见 XDatePicker。
 *
 * 组件库把区间选择从 DatePicker 拆成了独立的 DateRangePicker：两组段位各认领一端，
 * 值按位存放为 [起, 止] 两个 ISO 日期串（`YYYY-MM-DD`），只填了一端时空缺的那端是空串。
 * 本应用上下游一律用时间戳（毫秒），换算在这里做（见 date-picker-value.ts）。
 *
 * 只落下一端时不上抛，调用方拿到的区间要么两端齐备、要么为空；那一半留在本地草稿里
 * 交回组件库——受控值若只认两端齐备，段位里先敲完的那一端会被受控值抹回去，区间永远填不齐。
 */
defineOptions({ name: 'XDateRangePicker', inheritAttrs: false })

const props = withDefaults(defineProps<{
  /** [起, 止] 时间戳（毫秒） */
  value?: [number, number] | null
  clearable?: boolean
  /** 不写时随外层 Field / Form 的 disabled 走；写了以本处为准 */
  disabled?: boolean
  size?: Size
  /** 快捷选项：值取 dateRangePickerPreset* 系列算出的串 */
  presets?: Array<{ label: string, value: string }>
  /** 两端都带上时刻（时:分）：起止各多出一组时间列，选完日期不收起、由确认钮收口 */
  showTime?: boolean
  /** showTime 下只点日期时两端各补的时刻，如 ['00:00', '23:59']；已挑过时刻的一端不动 */
  defaultTime?: [string, string]
  /** 起点那组的整条占位：这一端没填、焦点不在段上时代替各段的 yyyy / mm / dd 显示 */
  startPlaceholder?: string
  /** 终点那组的整条占位 */
  endPlaceholder?: string
}>(), {
  value: null,
  clearable: true,
  disabled: undefined,
  size: 'sm',
  presets: undefined,
  showTime: false,
  defaultTime: undefined,
  startPlaceholder: undefined,
  endPlaceholder: undefined,
})

const emit = defineEmits<{
  'update:value': [value: [number, number] | null]
}>()

// 字段挂来的 id 与 aria-* 转交给两组段位，见 control-attrs.ts 与 date-picker-wiring.ts
const { attrs, controlAttrs } = useControlAttrs()
// 两组段位自带「开始日期」「结束日期」的名字：名字链接在字段标签后面，两样都读
const startGroupAttrs = computed(() => segmentGroupWiring(controlAttrs.value, true))
const endGroupAttrs = computed(() => endSegmentGroupWiring(controlAttrs.value))

const { locale, t } = useI18n()

/** 交给组件库的受控值：两端齐备时跟着 props，填到一半时是本地草稿 */
const draft = ref<string[]>(rangeToDraft(props.value, props.showTime))

// 上游改了值（含表单重置）就以上游为准，丢掉手里那一半
watch(() => props.value, (value) => {
  draft.value = rangeToDraft(value, props.showTime)
})

/** 两组时间列的小标题与列名：按应用的七种语言取。组件库内置的是英文，全局覆盖（locales/xihan-ui.ts）只有中英两份 */
const timeTranslations = computed(() => ({
  startTime: t('component.date_picker.start_time'),
  endTime: t('component.date_picker.end_time'),
  hour: t('component.date_picker.hour'),
  minute: t('component.date_picker.minute'),
}))

function onValueChange(next: string[]): void {
  draft.value = [...next]
  const resolved = resolveRangeChange(next)
  if (resolved !== undefined) {
    emit('update:value', resolved)
  }
}
</script>

<template>
  <XhDateRangePickerRoot
    v-slot="{ panels, weeks, weekDays, segments, endSegments }"
    :class="attrs.class"
    :style="attrs.style"
    :value="draft"
    :locale="locale"
    :disabled="disabled"
    :size="size"
    :presets="presets"
    :show-time="showTime"
    :default-time="defaultTime"
    :start-placeholder="startPlaceholder"
    :end-placeholder="endPlaceholder"
    :translations="showTime ? timeTranslations : undefined"
    @update:value="onValueChange"
  >
    <XhDateRangePickerControl>
      <!-- 两组段位：组号定这组认领哪一端，0 起点、1 终点 -->
      <XhDateRangePickerSegmentGroup :index="0" v-bind="startGroupAttrs">
        <template v-for="(seg, i) in segments" :key="seg.type">
          <span v-if="i > 0">{{ segmentLiteralBefore(seg.type) }}</span>
          <!-- 段位不写内容：显示什么由组件按当前值填 -->
          <XhDateRangePickerSegment :index="i" />
        </template>
      </XhDateRangePickerSegmentGroup>
      <XhDateRangePickerRangeSeparator />
      <XhDateRangePickerSegmentGroup :index="1" v-bind="endGroupAttrs">
        <template v-for="(seg, i) in endSegments" :key="seg.type">
          <span v-if="i > 0">{{ segmentLiteralBefore(seg.type) }}</span>
          <XhDateRangePickerSegment :index="i" />
        </template>
      </XhDateRangePickerSegmentGroup>
      <!-- 两颗都不写内容：字形由组件库出。有值时清空钮原位接替日历钮，无值时只露日历钮 -->
      <XhDateRangePickerClearTrigger v-if="clearable" />
      <!-- 在字段里时组件库把字段标签接成它的名字；不在字段里时读这条 -->
      <XhDateRangePickerTrigger :aria-label="t('component.date_picker.label')" />
    </XhDateRangePickerControl>
    <XhDateRangePickerPositioner>
      <XhDateRangePickerContent>
        <!-- 与 XDatePicker 同一排法：快捷选项、日历与时间列并排，放不下就折行；确认行另起一行 -->
        <div class="x-date-range-picker__panes">
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
          <!-- 起止各一组时、分列，组顶小标题取 translations 的 startTime / endTime -->
          <XhDateRangePickerTimePanel v-if="showTime" />
        </div>
        <div v-if="showTime" class="x-date-range-picker__footer">
          <XhDateRangePickerConfirmTrigger>{{ t('common.actions.confirm') }}</XhDateRangePickerConfirmTrigger>
        </div>
      </XhDateRangePickerContent>
    </XhDateRangePickerPositioner>
  </XhDateRangePickerRoot>
</template>

<style scoped>
/* 同 XDatePicker：横排、放不下折行、同排等高 */
.x-date-range-picker__panes {
  display: flex;
  flex-wrap: wrap;
  align-items: stretch;
}

/* 负外边距吃掉浮层的内边距，分隔线贴满面板两边 */
.x-date-range-picker__footer {
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
