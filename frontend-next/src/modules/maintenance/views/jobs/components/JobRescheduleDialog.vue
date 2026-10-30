<script setup lang="ts">
import type { JobRow, ScheduleForm, SchedulePreview } from '../job'
import { XhButton, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhFieldControl, XhFieldDescription, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormRoot, XhSpinner, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, onBeforeUnmount, reactive, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { XCombobox, XEditModal, XInput } from '~/components'
import { toast, useTimezoneOptions } from '~/composables'
import { ApiError } from '~/request'
import { formatDate } from '~/utils'
import { errorText, kindLabel, previewSchedule, rescheduleJob, SCHEDULE_PREVIEW_COUNT, scheduleFormOf, schedulePresets } from '../job'

/**
 * 修改计划：换作业唯一触发器的计划（Cron 表达式或计划写法），其余登记与暂停状态不变。
 * 计划与时区改动后停顿片刻即经 jobs.validate-schedule 校验并预览接下来的执行时间（4.x Cron 构建器的预览位置）；
 * 保存走 jobs.reschedule，代码登记的作业改过即标为「已改计划」。
 */
defineOptions({ name: 'JobRescheduleDialog' })

const props = defineProps<{
  show: boolean
  job: JobRow | null
}>()

const emit = defineEmits<{
  (e: 'update:show', value: boolean): void
  (e: 'saved', job: JobRow): void
}>()

/** 停顿多久再校验：连续输入时不逐字请求 */
const PREVIEW_DEBOUNCE_MS = 400

const { t } = useI18n()
const formId = useId()
const timeZones = useTimezoneOptions()

const form = reactive<ScheduleForm>({ expression: '', timeZone: '' })
const saving = ref(false)
const fieldErrors = ref<Record<string, string[]>>({})

const preview = ref<SchedulePreview | null>(null)
const previewError = ref<string | null>(null)
const previewLoading = ref(false)
/** 只认最后一次校验的结果：先发后到的旧结果丢掉 */
let previewSeq = 0
/** 最近一次校验的计划与时区：没变就不再请求（打开时的填充不重复校验） */
let previewKey: null | string = null
let previewTimer: ReturnType<typeof setTimeout> | null = null

function keyOf(value: ScheduleForm): string {
  return `${value.expression.trim()}\n${value.timeZone.trim()}`
}

const title = computed(() => t('maintenance.jobs.reschedule_title', { name: props.job?.title ?? '' }))
const currentTrigger = computed(() => props.job?.triggers[0] ?? null)
const presets = computed(() => schedulePresets(t))
const timeZoneOptions = computed(() => timeZones.withCurrent(form.timeZone).map(zone => ({ label: zone.label, value: zone.value })))
const expressionError = computed(() => fieldErrors.value.expression?.[0])
const timeZoneError = computed(() => fieldErrors.value.timeZone?.[0])

function clearPreviewTimer() {
  if (previewTimer) {
    clearTimeout(previewTimer)
    previewTimer = null
  }
}

async function runPreview() {
  clearPreviewTimer()
  const seq = ++previewSeq
  previewKey = keyOf(form)
  if (!form.expression.trim()) {
    preview.value = null
    previewError.value = null
    previewLoading.value = false
    return
  }
  previewLoading.value = true
  previewError.value = null
  try {
    const result = await previewSchedule(form)
    if (seq === previewSeq) {
      preview.value = result
    }
  }
  catch (error) {
    if (seq === previewSeq) {
      preview.value = null
      previewError.value = errorText(error) || t('maintenance.jobs.preview_failed')
    }
  }
  finally {
    if (seq === previewSeq) {
      previewLoading.value = false
    }
  }
}

function schedulePreviewSoon() {
  clearPreviewTimer()
  previewTimer = setTimeout(() => void runPreview(), PREVIEW_DEBOUNCE_MS)
}

// 打开即按作业现在的计划填好并预览一次；时区目录随后台取回（取不到时仍可手填）
watch(
  () => [props.show, props.job?.name] as const,
  ([show]) => {
    if (!show || !props.job) {
      clearPreviewTimer()
      // 作废还在路上的校验，下次打开重新校验
      previewSeq++
      previewKey = null
      previewLoading.value = false
      return
    }
    Object.assign(form, scheduleFormOf(props.job))
    fieldErrors.value = {}
    preview.value = null
    previewError.value = null
    void timeZones.ensureLoaded()
    void runPreview()
  },
  { immediate: true },
)

watch(() => [form.expression, form.timeZone], () => {
  if (!props.show) {
    return
  }
  fieldErrors.value = {}
  if (keyOf(form) !== previewKey) {
    schedulePreviewSoon()
  }
})

onBeforeUnmount(clearPreviewTimer)

function onTimeZone(value: unknown) {
  form.timeZone = value === null || value === undefined ? '' : String(value)
}

function applyPreset(value: string) {
  form.expression = value
}

function close() {
  emit('update:show', false)
}

async function submit() {
  const job = props.job
  if (!job || saving.value) {
    return
  }
  if (!form.expression.trim()) {
    fieldErrors.value = { expression: [t('maintenance.jobs.expression_required')] }
    return
  }
  saving.value = true
  fieldErrors.value = {}
  try {
    const saved = await rescheduleJob(job.name, form)
    toast.success(t('maintenance.jobs.rescheduled'))
    emit('saved', saved)
    close()
  }
  catch (error) {
    // 计划写错落在计划框下；请求体的字段校验按字段落位；冲突（多个触发器、类型没登记）等其余错误弹提示
    if (error instanceof ApiError && error.code === 'MAINTENANCE_VALIDATION_002') {
      fieldErrors.value = { expression: [error.detail] }
    }
    else if (error instanceof ApiError && error.hasFieldErrors) {
      fieldErrors.value = error.errors ?? {}
    }
    else {
      toast.danger(errorText(error) || t('maintenance.jobs.reschedule_failed'))
    }
  }
  finally {
    saving.value = false
  }
}
</script>

<template>
  <XEditModal
    :show="show"
    :title="title"
    :loading="saving"
    :save-disabled="!form.expression.trim()"
    :form-id="formId"
    :width="640"
    @update:show="(value: boolean) => emit('update:show', value)"
  >
    <p class="job-reschedule__hint">
      {{ t('maintenance.jobs.reschedule_hint') }}
    </p>

    <!-- 现在的计划：只读，改动以下面的表单为准 -->
    <XhDescriptionsRoot class="job-reschedule__current" :columns="2" variant="outline" size="sm">
      <XhDescriptionsItem>
        <XhDescriptionsLabel>{{ t('maintenance.jobs.current_schedule') }}</XhDescriptionsLabel>
        <XhDescriptionsValue>
          <code v-if="currentTrigger">{{ currentTrigger.schedule }}</code>
          <span v-else>-</span>
        </XhDescriptionsValue>
      </XhDescriptionsItem>
      <XhDescriptionsItem>
        <XhDescriptionsLabel>{{ t('maintenance.jobs.trigger_type') }}</XhDescriptionsLabel>
        <XhDescriptionsValue>
          {{ kindLabel(t, currentTrigger?.kind) }}
        </XhDescriptionsValue>
      </XhDescriptionsItem>
    </XhDescriptionsRoot>

    <XhFormRoot :id="formId" class="xh-edit-form-grid" data-testid="job-reschedule-form" @submit="submit">
      <XhFieldRoot class="xh-span-2" :invalid="!!expressionError" required>
        <XhFieldLabel>{{ t('maintenance.jobs.schedule') }}</XhFieldLabel>
        <XhFieldControl>
          <XInput
            v-model:value="form.expression"
            name="expression"
            clearable
            autocomplete="off"
            :placeholder="t('maintenance.jobs.expression_placeholder')"
          />
        </XhFieldControl>
        <XhFieldErrorText v-if="expressionError">
          {{ expressionError }}
        </XhFieldErrorText>
        <XhFieldDescription>{{ t('maintenance.jobs.expression_hint') }}</XhFieldDescription>
      </XhFieldRoot>

      <!-- 常用预设（4.x Cron 构建器的六个）：点一下填进计划框 -->
      <div class="xh-span-2 job-reschedule__presets" role="group" :aria-label="t('maintenance.jobs.presets_label')">
        <XhButton
          v-for="preset in presets"
          :key="preset.value"
          type="button"
          variant="outline"
          size="sm"
          :title="preset.value"
          @click="applyPreset(preset.value)"
        >
          {{ preset.label }}
        </XhButton>
      </div>

      <XhFieldRoot class="xh-span-2" :invalid="!!timeZoneError">
        <XhFieldLabel>{{ t('maintenance.jobs.time_zone') }}</XhFieldLabel>
        <XhFieldControl>
          <XCombobox
            class="job-reschedule__zone"
            :options="timeZoneOptions"
            :value="form.timeZone || null"
            :loading="timeZones.loading.value"
            :placeholder="t('maintenance.jobs.time_zone_placeholder')"
            clearable
            allow-custom-value
            @update:value="onTimeZone"
          />
        </XhFieldControl>
        <XhFieldErrorText v-if="timeZoneError">
          {{ timeZoneError }}
        </XhFieldErrorText>
        <XhFieldDescription>{{ t('maintenance.jobs.time_zone_hint') }}</XhFieldDescription>
      </XhFieldRoot>
    </XhFormRoot>

    <!-- 预览：后端按计划与时区算出的接下来几次执行（显示为本地时间） -->
    <section class="job-reschedule__preview" aria-live="polite">
      <div class="job-reschedule__preview-head">
        <span>{{ t('maintenance.jobs.preview_title', { count: SCHEDULE_PREVIEW_COUNT }) }}</span>
        <XhSpinner v-if="previewLoading" size="sm" />
      </div>
      <p v-if="previewError" class="job-reschedule__preview-text is-error">
        {{ previewError }}
      </p>
      <template v-else-if="preview">
        <p class="job-reschedule__preview-text job-reschedule__preview-schedule">
          <span>{{ t('maintenance.jobs.preview_schedule') }}</span>
          <code>{{ preview.schedule }}</code>
          <XhTagRoot variant="subtle" size="sm" tone="neutral">
            <XhTagLabel>{{ kindLabel(t, preview.kind) }}</XhTagLabel>
          </XhTagRoot>
        </p>
        <ul v-if="preview.nextRuns.length > 0" class="job-reschedule__preview-list">
          <li v-for="run in preview.nextRuns" :key="run">
            {{ formatDate(run) }}
          </li>
        </ul>
        <p v-else class="job-reschedule__preview-text">
          {{ t('maintenance.jobs.preview_none') }}
        </p>
      </template>
      <p v-else-if="!previewLoading" class="job-reschedule__preview-text">
        {{ t('maintenance.jobs.preview_idle') }}
      </p>
    </section>
  </XEditModal>
</template>

<style scoped>
.job-reschedule__hint {
  margin: 0 0 var(--xh-space-3);
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-muted);
}

.job-reschedule__current {
  margin-bottom: var(--xh-space-3);
}

.job-reschedule__current code,
.job-reschedule__preview code {
  font-family: var(--xh-font-family-mono);
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-default);
  word-break: break-all;
}

.job-reschedule__presets {
  display: flex;
  flex-wrap: wrap;
  gap: var(--xh-space-1);
}

.job-reschedule__preview-schedule {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--xh-space-2);
}

/* 组合框默认按内容定宽，这里与字段一样铺满 */
.job-reschedule__zone {
  inline-size: 100%;
}

.job-reschedule__preview {
  margin-top: var(--xh-space-3);
  padding: var(--xh-space-2) var(--xh-space-3);
  border-radius: var(--xh-radius-md);
  background: var(--xh-bg-subtle);
}

.job-reschedule__preview-head {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
  margin-bottom: var(--xh-space-1);
  font-size: var(--xh-font-size-xs);
  font-weight: var(--xh-font-weight-semibold);
  color: var(--xh-fg-muted);
}

.job-reschedule__preview-text {
  margin: 0;
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-muted);
}

.job-reschedule__preview-text.is-error {
  color: var(--xh-fg-danger);
}

.job-reschedule__preview-list {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(calc(var(--xh-space-8) * 5), 1fr));
  gap: var(--xh-space-1) var(--xh-space-4);
  margin: var(--xh-space-1) 0 0;
  padding: 0;
  list-style: none;
}

.job-reschedule__preview-list li {
  font-family: var(--xh-font-family-mono);
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-default);
}
</style>
