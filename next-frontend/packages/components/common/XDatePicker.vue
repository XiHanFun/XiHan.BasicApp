<script setup lang="ts">
import type { Size } from '@xihan-ui/kernel'
import {
  XhButton,
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
  XhDatePickerInput,
  XhDatePickerLabel,
  XhDatePickerNextTrigger,
  XhDatePickerPositioner,
  XhDatePickerPrevTrigger,
  XhDatePickerRoot,
  XhDatePickerSegment,
  XhDatePickerTimePanel,
  XhDatePickerTrigger,
  XhDatePickerWeekDay,
  XhDatePickerWeekRow,
} from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import { useControlAttrs } from './control-attrs'

/**
 * 日期选择（单选 / 区间 / 日期时间）。
 *
 * 组件库那侧是二十来个部件的完整日历，摆一遍要六十行；这里摆一次，全站复用。
 * 另一件必须收口的事是值类型：组件库收发 ISO 串（`YYYY-MM-DD`，带时间时是
 * `YYYY-MM-DDTHH:mm`），而本应用上下游一律用时间戳（毫秒），换算在这里做。
 */
defineOptions({ name: 'XDatePicker', inheritAttrs: false })

const props = withDefaults(defineProps<{
  /** 单选传时间戳，区间传 [起, 止] */
  value?: number | [number, number] | null
  /** 区间模式 */
  range?: boolean
  /** 粒度。datetime 在浮层里多出时分两列，收口交给确认钮（组件库只对单选支持） */
  type?: 'date' | 'datetime'
  placeholder?: string
  clearable?: boolean
  disabled?: boolean
  size?: Size
  /** 浮层内的快捷区间；resolve 给时间戳（单选一个，区间两个） */
  shortcuts?: Array<{ label: string, resolve: () => number | [number, number] }>
}>(), {
  value: null,
  range: false,
  type: 'date',
  placeholder: undefined,
  clearable: true,
  disabled: false,
  size: 'sm',
  shortcuts: undefined,
})

const emit = defineEmits<{
  'update:value': [value: number | [number, number] | null]
}>()

// 字段挂来的 id 与 aria-* 转交给输入区，见 control-attrs.ts
const { attrs, controlAttrs } = useControlAttrs()

const { t, locale } = useI18n()

/** 时分两列只在单选下开：区间那一路组件库直接吞掉 showTime */
const withTime = computed(() => !props.range && props.type === 'datetime')

function pad(value: number): string {
  return String(value).padStart(2, '0')
}

/** 时间戳 → ISO 串。用本地分量拼，避免 toISOString 的 UTC 偏移把日期挪一天 */
function toIso(timestamp: number): string {
  const date = new Date(timestamp)
  const day = `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
  return withTime.value ? `${day}T${pad(date.getHours())}:${pad(date.getMinutes())}` : day
}

/** ISO 串 → 时间戳（本地时区）。空串与残缺串给 null：'' 被 Number 读成 0，会落成 1900 年 */
function toTimestamp(iso: string): number | null {
  const [datePart, timePart] = iso.split('T')
  const [year, month, day] = (datePart ?? '').split('-').map(Number)
  if (!Number.isFinite(year) || !Number.isFinite(month) || !Number.isFinite(day)) {
    return null
  }
  const [hour = 0, minute = 0, second = 0] = (timePart ?? '').split(':').map(Number)
  const date = new Date(0)
  // 不用 new Date(y, …)：0..99 的年份会被当成 1900+y
  date.setFullYear(year!, month! - 1, day!)
  date.setHours(hour, minute, second, 0)
  return date.getTime()
}

const modelIso = computed<string[]>(() => {
  if (props.value == null) {
    return []
  }
  return Array.isArray(props.value) ? props.value.map(toIso) : [toIso(props.value)]
})

/**
 * 区间只挑了一端时的中间态。留在组件内部当受控值，上抛的区间仍然两端齐备。
 * 存的是组件库交出来的原始形状：['', 终点] 这种前导空位要原样留着，抹掉会让终点落回起点那一格。
 */
const draft = ref<string[] | null>(null)
const isoValue = computed<string[]>(() => draft.value ?? modelIso.value)

watch(() => props.value, () => {
  draft.value = null
})

function onValueChange(next: string[]): void {
  if (!props.range) {
    draft.value = null
    emit('update:value', next.length > 0 ? toTimestamp(next[0]!) : null)
    return
  }
  const start = next[0] ?? ''
  const end = next[1] ?? ''
  if (!start && !end) {
    draft.value = null
    emit('update:value', null)
    return
  }
  const from = start ? toTimestamp(start) : null
  const to = end ? toTimestamp(end) : null
  // 只落定一端（或某端解析不出来）：留成草稿，不上抛
  if (from == null || to == null) {
    draft.value = next
    return
  }
  draft.value = null
  emit('update:value', [from, to])
}

/** 收起浮层即放弃没挑完的那一段：草稿不能比浮层活得久 */
function onOpenChange(details: { open: boolean }): void {
  if (!details.open) {
    draft.value = null
  }
}

function shortcutIso(value: number | [number, number]): string[] {
  return Array.isArray(value) ? value.map(toIso) : [toIso(value)]
}
</script>

<template>
  <XhDatePickerRoot
    v-slot="{ panels, weekDays, segments, endSegments, setValue, setOpen, canClear }"
    :class="attrs.class"
    :style="attrs.style"
    :value="isoValue"
    :locale="locale"
    :selection-mode="range ? 'range' : 'single'"
    :show-time="withTime"
    :disabled="disabled"
    :size="size"
    @update:value="onValueChange"
    @open-change="onOpenChange"
  >
    <!-- 浮层、触发钮与分段容器三个可及名字都指向它，不渲染就都指向一个不存在的 id -->
    <XhDatePickerLabel class="sr-only">
      {{ placeholder ?? t('component.date_picker.label') }}
    </XhDatePickerLabel>
    <XhDatePickerControl class="x-date-picker__control">
      <!-- 区间两组段位各认领一端：组号 0 起点、1 终点。名字由组件库按起止报，别再写 aria-label 盖掉 -->
      <template v-if="range">
        <XhDatePickerInput :index="0" v-bind="controlAttrs">
          <template v-for="(seg, i) in segments" :key="seg.type">
            <span v-if="i > 0">-</span>
            <XhDatePickerSegment :index="i" />
          </template>
        </XhDatePickerInput>
        <span class="x-date-picker__sep" aria-hidden="true">~</span>
        <XhDatePickerInput :index="1">
          <template v-for="(seg, i) in endSegments" :key="seg.type">
            <span v-if="i > 0">-</span>
            <XhDatePickerSegment :index="i" />
          </template>
        </XhDatePickerInput>
      </template>
      <XhDatePickerInput v-else v-bind="controlAttrs">
        <template v-for="(seg, i) in segments" :key="seg.type">
          <span v-if="i > 0">-</span>
          <XhDatePickerSegment :index="i" />
        </template>
      </XhDatePickerInput>
      <XhDatePickerClearTrigger v-if="clearable && canClear">
        ✕
      </XhDatePickerClearTrigger>
      <XhDatePickerTrigger>
        <Icon icon="lucide:calendar" width="14" height="14" />
      </XhDatePickerTrigger>
    </XhDatePickerControl>
    <XhDatePickerPositioner>
      <XhDatePickerContent>
        <!-- 面板横排由这层承担：区间是两张月历并排，datetime 是月历 + 时间列 -->
        <div class="x-date-picker__panels">
          <XhDatePickerCalendar v-for="panel in panels" :key="panel.index">
            <XhDatePickerHeader>
              <!-- 往前只在最左那张、往后只在最右那张：整窗一起走 -->
              <XhDatePickerPrevTrigger v-if="panel.index === 0">
                ‹
              </XhDatePickerPrevTrigger>
              <XhDatePickerHeading :index="panel.index" />
              <XhDatePickerNextTrigger v-if="panel.index === panels.length - 1">
                ›
              </XhDatePickerNextTrigger>
            </XhDatePickerHeader>
            <XhDatePickerGrid :index="panel.index">
              <XhDatePickerGridHead>
                <XhDatePickerWeekRow>
                  <XhDatePickerWeekDay v-for="d in weekDays" :key="d.value" :value="d.value" />
                </XhDatePickerWeekRow>
              </XhDatePickerGridHead>
              <XhDatePickerGridBody>
                <!-- v-for 必带 key：就地复用会让承载焦点的那一格换了身份 -->
                <XhDatePickerWeekRow v-for="week in panel.weeks" :key="week[0]!.value">
                  <!-- index 必须给：同一天会同时出现在两个面板里 -->
                  <XhDatePickerCell
                    v-for="day in week"
                    :key="day.value"
                    :value="day.value"
                    :index="panel.index"
                  >
                    <XhDatePickerCellTrigger>{{ day.day }}</XhDatePickerCellTrigger>
                  </XhDatePickerCell>
                </XhDatePickerWeekRow>
              </XhDatePickerGridBody>
            </XhDatePickerGrid>
          </XhDatePickerCalendar>
          <XhDatePickerTimePanel v-if="withTime" />
        </div>
        <div v-if="shortcuts?.length" class="x-date-picker__shortcuts">
          <XhButton
            v-for="item in shortcuts"
            :key="item.label"
            size="sm"
            variant="outline"
            @click="setValue(shortcutIso(item.resolve())); setOpen(false)"
          >
            {{ item.label }}
          </XhButton>
        </div>
        <div v-if="withTime" class="x-date-picker__confirm">
          <XhDatePickerConfirmTrigger>{{ t('common.actions.confirm') }}</XhDatePickerConfirmTrigger>
        </div>
      </XhDatePickerContent>
    </XhDatePickerPositioner>
  </XhDatePickerRoot>
</template>

<style scoped>
/* 区间两组段位撑不进搜索栅格的单元格：跟着外层宽度走并允许收缩 */
:deep([data-scope='date-picker'][data-part='control']) {
  inline-size: 100%;
  min-inline-size: 0;
}

.x-date-picker__sep {
  flex: none;
  padding-inline: 4px;
  color: var(--xh-fg-muted);
}

.x-date-picker__panels {
  display: flex;
  align-items: flex-start;
}

.x-date-picker__shortcuts {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-block-start: 12px;
  max-inline-size: 100%;
}

.x-date-picker__confirm {
  display: flex;
  justify-content: flex-end;
  margin-block-start: 8px;
}
</style>
