<script setup lang="ts">
import { NButton, NInput, NInputGroup, NInputNumber, NModal, NRadio, NRadioGroup, NSelect, NSwitch, NTabPane, NTabs, NTag } from 'naive-ui'
import { computed, nextTick, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'

/**
 * Cron 表达式编辑组件（公共组件）。
 * - 表现为「输入框 + 构建器按钮」，点击按钮弹出可视化构建器（不直接占用表单空间）。
 * - 支持 5 段（分 时 日 月 周）与 6 段（秒 分 时 日 月 周），与后端 CronHelper 方言一致。
 * - 字段语法：* ? , - /（不含 L/W/#）；周 0-6（0=周日）。
 * - v-model:value 双向绑定表达式字符串；构建器与手动输入实时同步，并预览最近执行时间。
 */
defineOptions({ name: 'CronExpression' })

const props = withDefaults(defineProps<{
  /** 表达式（v-model） */
  value?: string | null
  /** 新建（空值）时默认是否包含秒（6 段） */
  secondsDefault?: boolean
  /** 预览的执行次数 */
  previewCount?: number
  /** 输入框占位符 */
  placeholder?: string
}>(), {
  secondsDefault: true,
  previewCount: 5,
})

const emit = defineEmits<{ 'update:value': [string] }>()

const { t } = useI18n()

type FieldKey = 'second' | 'minute' | 'hour' | 'day' | 'month' | 'week'
type FieldMode = 'every' | 'interval' | 'range' | 'specific' | 'unspecified'

interface FieldState {
  mode: FieldMode
  start: number
  step: number
  from: number
  to: number
  values: number[]
}

interface FieldDef {
  key: FieldKey
  label: string
  min: number
  max: number
  unspecified: boolean
}

const builderVisible = ref(false)
const hasSeconds = ref(props.secondsDefault)
const rawText = ref(props.value ?? '')

const weekNames = computed(() => t('component.cron.week_names').split(','))

const fieldDefs = computed<FieldDef[]>(() => [
  { key: 'second', label: t('component.cron.field_second'), min: 0, max: 59, unspecified: false },
  { key: 'minute', label: t('component.cron.field_minute'), min: 0, max: 59, unspecified: false },
  { key: 'hour', label: t('component.cron.field_hour'), min: 0, max: 23, unspecified: false },
  { key: 'day', label: t('component.cron.field_day'), min: 1, max: 31, unspecified: true },
  { key: 'month', label: t('component.cron.field_month'), min: 1, max: 12, unspecified: false },
  { key: 'week', label: t('component.cron.field_week'), min: 0, max: 6, unspecified: true },
])

const activeFieldDefs = computed(() =>
  hasSeconds.value ? fieldDefs.value : fieldDefs.value.filter(d => d.key !== 'second'))

function defOf(key: FieldKey): FieldDef {
  return fieldDefs.value.find(d => d.key === key) ?? fieldDefs.value[1]!
}

function createField(min: number): FieldState {
  return { mode: 'every', start: min, step: 1, from: min, to: min, values: [] }
}

const fields = reactive<Record<FieldKey, FieldState>>({
  second: createField(0),
  minute: createField(0),
  hour: createField(0),
  day: createField(1),
  month: createField(1),
  week: createField(0),
})
const activeTab = ref<FieldKey>('minute')

// 防止「解析回填」触发「再次 emit」造成回环
let syncingFromValue = false

// ── 工具 ───────────────────────────────────────────────────────
const MONTH_NAMES = ['JAN', 'FEB', 'MAR', 'APR', 'MAY', 'JUN', 'JUL', 'AUG', 'SEP', 'OCT', 'NOV', 'DEC']
const DAY_NAMES = ['SUN', 'MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT']

function clamp(v: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, Number.isFinite(v) ? v : min))
}

function toNum(v: string, def: FieldDef, fallback: number): number {
  const n = Number.parseInt(v, 10)
  if (!Number.isNaN(n)) {
    return n
  }
  const u = v.trim().toUpperCase()
  if (def.max === 12) {
    const mi = MONTH_NAMES.indexOf(u)
    if (mi >= 0) {
      return mi + 1
    }
  }
  if (def.max === 6) {
    const di = DAY_NAMES.indexOf(u)
    if (di >= 0) {
      return di
    }
  }
  return fallback
}

// ── 字段 ↔ 字符串 ──────────────────────────────────────────────
function fieldToStr(f: FieldState, def: FieldDef): string {
  switch (f.mode) {
    case 'every':
      return '*'
    case 'unspecified':
      return '?'
    case 'interval':
      return `${clamp(f.start, def.min, def.max)}/${Math.max(1, f.step)}`
    case 'range':
      return `${clamp(f.from, def.min, def.max)}-${clamp(f.to, def.min, def.max)}`
    case 'specific':
      return f.values.length ? [...f.values].sort((a, b) => a - b).join(',') : '*'
    default:
      return '*'
  }
}

function parseFieldStr(raw: string, def: FieldDef): FieldState {
  const f = createField(def.min)
  const s = (raw ?? '').trim()
  if (s === '' || s === '*') {
    f.mode = 'every'
    return f
  }
  if (s === '?') {
    f.mode = def.unspecified ? 'unspecified' : 'every'
    return f
  }
  if (s.includes('/')) {
    const segs = s.split('/')
    const head = segs[0] ?? '*'
    f.mode = 'interval'
    f.start = head === '*' || head === '' ? def.min : toNum(head, def, def.min)
    f.step = Math.max(1, Number.parseInt(segs[1] ?? '1', 10) || 1)
    return f
  }
  if (/^\d+-\d+$/.test(s)) {
    const segs = s.split('-')
    f.mode = 'range'
    f.from = toNum(segs[0] ?? '', def, def.min)
    f.to = toNum(segs[1] ?? '', def, def.max)
    return f
  }
  // 列表（含名称、内嵌区间）
  const values: number[] = []
  for (const part of s.split(',')) {
    const p = part.trim()
    const rng = p.match(/^(\d+)-(\d+)$/)
    if (rng) {
      const a = toNum(rng[1] ?? '', def, def.min)
      const b = toNum(rng[2] ?? '', def, def.max)
      for (let i = a; i <= b; i++) {
        values.push(i)
      }
    }
    else if (p) {
      values.push(toNum(p, def, def.min))
    }
  }
  f.mode = 'specific'
  f.values = [...new Set(values)].filter(v => v >= def.min && v <= def.max).sort((a, b) => a - b)
  return f
}

// ── 组装 / 解析整表达式 ────────────────────────────────────────
const fieldOrder = computed<FieldKey[]>(() =>
  hasSeconds.value
    ? ['second', 'minute', 'hour', 'day', 'month', 'week']
    : ['minute', 'hour', 'day', 'month', 'week'])

const expression = computed(() => fieldOrder.value.map(k => fieldToStr(fields[k], defOf(k))).join(' '))

function applyExpression(raw: string | null | undefined): void {
  const parts = (raw ?? '').trim().split(/\s+/).filter(Boolean)
  if (parts.length !== 5 && parts.length !== 6) {
    return
  }
  syncingFromValue = true
  hasSeconds.value = parts.length === 6
  fieldOrder.value.forEach((k, i) => {
    Object.assign(fields[k], parseFieldStr(parts[i] ?? '*', defOf(k)))
  })
  void nextTick(() => {
    syncingFromValue = false
  })
}

// 构建器结构变化 → 向上 emit（同步回填期间不发）
watch([() => hasSeconds.value, fields], () => {
  if (syncingFromValue) {
    return
  }
  rawText.value = expression.value
  emit('update:value', expression.value)
}, { deep: true })

// 外部值变化 → 同步输入框与构建器
watch(() => props.value, (v) => {
  rawText.value = v ?? ''
  if ((v ?? '').trim() && (v ?? '') !== expression.value) {
    applyExpression(v)
  }
}, { immediate: true })

watch(hasSeconds, () => {
  if (activeTab.value === 'second' && !hasSeconds.value) {
    activeTab.value = 'minute'
  }
})

// ── 输入框（手动编辑）────────────────────────────────────────────
function applyRaw(): void {
  const v = rawText.value.trim()
  applyExpression(v)
  emit('update:value', v)
}

const isValid = computed(() => {
  const len = rawText.value.trim().split(/\s+/).filter(Boolean).length
  return len === 0 || len === 5 || len === 6
})

// ── 指定值选项 ─────────────────────────────────────────────────
function specificOptions(def: FieldDef): { label: string, value: number }[] {
  if (def.key === 'week') {
    return weekNames.value.map((n, i) => ({ label: n, value: i }))
  }
  const out: { label: string, value: number }[] = []
  for (let i = def.min; i <= def.max; i++) {
    out.push({ label: String(i), value: i })
  }
  return out
}

// ── 预览：最近 N 次执行（镜像后端 AND 语义；周 0-6=周日..周六）──
function fieldAllowed(f: FieldState, def: FieldDef): Set<number> | null {
  switch (f.mode) {
    case 'every':
    case 'unspecified':
      return null // 任意
    case 'interval': {
      const set = new Set<number>()
      for (let i = clamp(f.start, def.min, def.max); i <= def.max; i += Math.max(1, f.step)) {
        set.add(i)
      }
      return set
    }
    case 'range': {
      const set = new Set<number>()
      for (let i = clamp(f.from, def.min, def.max); i <= clamp(f.to, def.min, def.max); i++) {
        set.add(i)
      }
      return set
    }
    case 'specific':
      return new Set(f.values)
    default:
      return null
  }
}

const nextRuns = computed<Date[]>(() => {
  if (!builderVisible.value) {
    return []
  }
  const sec = hasSeconds.value ? fieldAllowed(fields.second, defOf('second')) : new Set([0])
  const min = fieldAllowed(fields.minute, defOf('minute'))
  const hour = fieldAllowed(fields.hour, defOf('hour'))
  const day = fieldAllowed(fields.day, defOf('day'))
  const month = fieldAllowed(fields.month, defOf('month'))
  const week = fieldAllowed(fields.week, defOf('week'))
  const hit = (set: Set<number> | null, v: number): boolean => set === null || set.has(v)

  const results: Date[] = []
  const cursor = new Date()
  cursor.setMilliseconds(0)
  cursor.setSeconds(cursor.getSeconds() + 1)
  // 上限：约 1 年的秒级 / 分级步进，覆盖常见表达式且防止死循环
  const maxIter = hasSeconds.value ? 366 * 24 * 60 * 60 : 366 * 24 * 60
  for (let i = 0; i < maxIter && results.length < props.previewCount; i++) {
    if (
      hit(month, cursor.getMonth() + 1)
      && hit(day, cursor.getDate())
      && hit(week, cursor.getDay())
      && hit(hour, cursor.getHours())
      && hit(min, cursor.getMinutes())
      && hit(sec, cursor.getSeconds())
    ) {
      results.push(new Date(cursor))
    }
    cursor.setSeconds(cursor.getSeconds() + (hasSeconds.value ? 1 : 60))
  }
  return results
})

function fmt(d: Date): string {
  const p = (n: number): string => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())} ${p(d.getHours())}:${p(d.getMinutes())}:${p(d.getSeconds())}`
}

// ── 预设 ───────────────────────────────────────────────────────
const presets = computed(() => [
  { label: t('component.cron.preset_per_minute'), value: '0 * * * * ?' },
  { label: t('component.cron.preset_per_5min'), value: '0 0/5 * * * ?' },
  { label: t('component.cron.preset_hourly'), value: '0 0 * * * ?' },
  { label: t('component.cron.preset_daily'), value: '0 0 0 * * ?' },
  { label: t('component.cron.preset_weekly'), value: '0 0 0 ? * 1' },
  { label: t('component.cron.preset_monthly'), value: '0 0 0 1 * ?' },
])
function applyPreset(v: string): void {
  applyExpression(v)
  emit('update:value', v)
}
</script>

<template>
  <div class="cron-field">
    <NInputGroup>
      <NInput
        v-model:value="rawText"
        :status="isValid ? undefined : 'error'"
        :placeholder="placeholder ?? '* * * * * ?'"
        @blur="applyRaw"
        @keyup.enter="applyRaw"
      />
      <NButton ghost type="primary" :focusable="false" @click="builderVisible = true">
        <template #icon>
          <Icon icon="lucide:wand-2" />
        </template>
        {{ t('component.cron.build') }}
      </NButton>
    </NInputGroup>

    <NModal
      v-model:show="builderVisible"
      preset="card"
      :auto-focus="false"
      :bordered="false"
      :title="t('component.cron.title')"
      style="width: 600px; max-width: 94vw"
    >
      <div class="cron-editor">
        <!-- 预设 + 秒开关 -->
        <div class="cron-toolbar">
          <div class="cron-presets">
            <NTag
              v-for="p in presets"
              :key="p.value"
              class="cron-preset"
              :bordered="false"
              size="small"
              round
              @click="applyPreset(p.value)"
            >
              {{ p.label }}
            </NTag>
          </div>
          <label class="cron-seconds">
            <span>{{ t('component.cron.with_seconds') }}</span>
            <NSwitch v-model:value="hasSeconds" size="small" />
          </label>
        </div>

        <!-- 字段编辑 -->
        <NTabs v-model:value="activeTab" type="line" size="small" animated>
          <NTabPane v-for="def in activeFieldDefs" :key="def.key" :name="def.key" :tab="def.label">
            <NRadioGroup v-model:value="fields[def.key].mode" class="cron-modes">
              <div class="cron-mode-row">
                <NRadio value="every">
                  {{ t('component.cron.mode_every', { unit: def.label }) }}
                </NRadio>
              </div>
              <div class="cron-mode-row">
                <NRadio value="interval">
                  {{ t('component.cron.mode_interval') }}
                </NRadio>
                <span class="cron-inline" :class="{ 'is-disabled': fields[def.key].mode !== 'interval' }">
                  {{ t('component.cron.from') }}
                  <NInputNumber
                    v-model:value="fields[def.key].start" size="tiny" :min="def.min" :max="def.max"
                    :disabled="fields[def.key].mode !== 'interval'" style="width: 76px"
                  />
                  {{ t('component.cron.every_step') }}
                  <NInputNumber
                    v-model:value="fields[def.key].step" size="tiny" :min="1" :max="def.max"
                    :disabled="fields[def.key].mode !== 'interval'" style="width: 76px"
                  />
                  {{ def.label }}
                </span>
              </div>
              <div class="cron-mode-row">
                <NRadio value="range">
                  {{ t('component.cron.mode_range') }}
                </NRadio>
                <span class="cron-inline" :class="{ 'is-disabled': fields[def.key].mode !== 'range' }">
                  {{ t('component.cron.from') }}
                  <NInputNumber
                    v-model:value="fields[def.key].from" size="tiny" :min="def.min" :max="def.max"
                    :disabled="fields[def.key].mode !== 'range'" style="width: 76px"
                  />
                  {{ t('component.cron.to') }}
                  <NInputNumber
                    v-model:value="fields[def.key].to" size="tiny" :min="def.min" :max="def.max"
                    :disabled="fields[def.key].mode !== 'range'" style="width: 76px"
                  />
                </span>
              </div>
              <div class="cron-mode-row">
                <NRadio value="specific">
                  {{ t('component.cron.mode_specific') }}
                </NRadio>
                <NSelect
                  v-model:value="fields[def.key].values"
                  class="cron-specific"
                  multiple
                  size="tiny"
                  :options="specificOptions(def)"
                  :max-tag-count="6"
                  :placeholder="t('component.cron.specific_placeholder', { unit: def.label })"
                  :disabled="fields[def.key].mode !== 'specific'"
                />
              </div>
              <div v-if="def.unspecified" class="cron-mode-row">
                <NRadio value="unspecified">
                  {{ t('component.cron.mode_unspecified') }}
                </NRadio>
              </div>
            </NRadioGroup>
          </NTabPane>
        </NTabs>

        <!-- 预览 -->
        <div class="cron-preview">
          <div class="cron-preview-title">
            {{ t('component.cron.preview', { count: previewCount }) }}
          </div>
          <ul v-if="nextRuns.length" class="cron-preview-list">
            <li v-for="(d, i) in nextRuns" :key="i">
              {{ fmt(d) }}
            </li>
          </ul>
          <div v-else class="cron-preview-empty">
            {{ t('component.cron.preview_none') }}
          </div>
        </div>
      </div>

      <template #footer>
        <div class="cron-footer">
          <code class="cron-footer-expr">{{ expression }}</code>
          <NButton type="primary" @click="builderVisible = false">
            {{ t('component.cron.done') }}
          </NButton>
        </div>
      </template>
    </NModal>
  </div>
</template>

<style scoped>
.cron-field {
  width: 100%;
}

.cron-editor {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.cron-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.cron-presets {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.cron-preset {
  cursor: pointer;
  background: hsl(var(--accent));
}

.cron-preset:hover {
  color: hsl(var(--primary));
}

.cron-seconds {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  flex-shrink: 0;
  font-size: 13px;
  color: hsl(var(--muted-foreground));
}

.cron-modes {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 4px 2px;
}

.cron-mode-row {
  display: flex;
  align-items: center;
  gap: 8px;
  min-height: 28px;
  flex-wrap: wrap;
}

.cron-inline {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  color: hsl(var(--foreground));
  transition: opacity 0.15s ease;
}

.cron-inline.is-disabled {
  opacity: 0.45;
}

.cron-specific {
  flex: 1;
  min-width: 220px;
  max-width: 420px;
}

.cron-preview {
  border-radius: var(--radius);
  background: hsl(var(--muted) / 0.4);
  padding: 8px 12px;
}

.cron-preview-title {
  font-size: 12px;
  font-weight: 600;
  color: hsl(var(--muted-foreground));
  margin-bottom: 4px;
}

.cron-preview-list {
  margin: 0;
  padding: 0;
  list-style: none;
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(170px, 1fr));
  gap: 2px 16px;
}

.cron-preview-list li {
  font-size: 12px;
  font-family: ui-monospace, SFMono-Regular, monospace;
  color: hsl(var(--foreground));
}

.cron-preview-empty {
  font-size: 12px;
  color: hsl(var(--muted-foreground));
}

.cron-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.cron-footer-expr {
  font-family: ui-monospace, SFMono-Regular, monospace;
  font-size: 13px;
  color: hsl(var(--primary));
}
</style>
