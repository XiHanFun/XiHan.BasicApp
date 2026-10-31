<script setup lang="ts">
import type { JobCreateForm, JobResponse, RetryForm } from '../job'
import { XhFieldControl, XhFieldDescription, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormRoot, XhSwitch } from '@xihan-ui/vue'
import { computed, reactive, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { XCombobox, XEditModal, XInput, XSelect } from '~/components'
import { toast, useTimezoneOptions } from '~/composables'
import { ApiError } from '~/request'
import { blankRetry, createJob, errorText, loadJobTypeOptions, retryIncomplete } from '../job'
import JobRetryFields from './JobRetryFields.vue'
import JobScheduleEditor from './JobScheduleEditor.vue'

/**
 * 新建作业（照 4.x 的新建任务，替代 4.x 的 DynamicJobWorker）：按已登记的作业类型运行时登记，
 * 填作业名、类型、计划（Cron 构建器或间隔秒数）与时区、参数（JSON 对象）、重试与执行时限、是否允许并发与说明。
 * 保存走 jobs.create；作业名重复、类型不认识、参数不是 JSON 对象等按字段落位，其余弹提示。
 */
defineOptions({ name: 'JobCreateDialog' })

const props = defineProps<{
  show: boolean
}>()

const emit = defineEmits<{
  (e: 'update:show', value: boolean): void
  (e: 'saved', job: JobResponse): void
}>()

/** 参数的示例（JSON 与语言无关，不进文案；花括号也会被 vue-i18n 当成插值） */
const DATA_EXAMPLE = '{ "days": 7 }'

const { t } = useI18n()
const formId = useId()
const timeZones = useTimezoneOptions()

function blank(): Omit<JobCreateForm, keyof RetryForm> {
  return { name: '', jobType: '', expression: '', timeZone: '', data: '', allowConcurrent: false, description: '' }
}

const form = reactive(blank())
const retry = ref<RetryForm>(blankRetry())
const saving = ref(false)
const fieldErrors = ref<Record<string, string[]>>({})
const typeOptions = ref<{ label: string, value: string }[]>([])
const concurrentLabelId = useId()

const timeZoneOptions = computed(() => timeZones.withCurrent(form.timeZone).map(zone => ({ label: zone.label, value: zone.value })))
const errorOf = (field: string) => fieldErrors.value[field]?.[0]
const canSave = computed(() => !!form.name.trim() && !!form.jobType && !!form.expression.trim() && !retryIncomplete(retry.value))

watch(() => props.show, async (show) => {
  if (!show)
    return
  Object.assign(form, blank())
  retry.value = blankRetry()
  fieldErrors.value = {}
  void timeZones.ensureLoaded()
  try {
    typeOptions.value = await loadJobTypeOptions()
  }
  catch (error) {
    toast.danger(errorText(error) || t('maintenance.jobs.create_failed'))
  }
}, { immediate: true })

function onTimeZone(value: unknown) {
  form.timeZone = value === null || value === undefined ? '' : String(value)
}

/** 后端的错误码落到对应字段，认不出的弹提示 */
const CODE_FIELDS: Record<string, keyof JobCreateForm> = {
  MAINTENANCE_CONFLICT_003: 'name',
  MAINTENANCE_VALIDATION_004: 'name',
  MAINTENANCE_CONFLICT_001: 'jobType',
  MAINTENANCE_VALIDATION_005: 'data',
  MAINTENANCE_VALIDATION_002: 'expression',
}

async function submit() {
  if (saving.value || !canSave.value)
    return
  saving.value = true
  fieldErrors.value = {}
  try {
    const saved = await createJob({ ...form, ...retry.value })
    toast.success(t('maintenance.jobs.created'))
    emit('saved', saved)
    emit('update:show', false)
  }
  catch (error) {
    const field = error instanceof ApiError ? CODE_FIELDS[error.code] : undefined
    if (error instanceof ApiError && field)
      fieldErrors.value = { [field]: [error.detail] }
    else if (error instanceof ApiError && error.hasFieldErrors)
      fieldErrors.value = error.errors ?? {}
    else
      toast.danger(errorText(error) || t('maintenance.jobs.create_failed'))
  }
  finally {
    saving.value = false
  }
}
</script>

<template>
  <XEditModal
    :show="show"
    :title="t('maintenance.jobs.create_title')"
    :loading="saving"
    :save-disabled="!canSave"
    :form-id="formId"
    :width="640"
    @update:show="(value: boolean) => emit('update:show', value)"
  >
    <XhFormRoot :id="formId" class="xh-edit-form-grid" data-testid="job-create-form" @submit="submit">
      <XhFieldRoot :invalid="!!errorOf('name')" required>
        <XhFieldLabel>{{ t('maintenance.jobs.task_code') }}</XhFieldLabel>
        <XhFieldControl>
          <XInput v-model:value="form.name" name="name" clearable autocomplete="off" :placeholder="t('maintenance.jobs.name_placeholder')" />
        </XhFieldControl>
        <XhFieldErrorText v-if="errorOf('name')">
          {{ errorOf('name') }}
        </XhFieldErrorText>
        <XhFieldDescription>{{ t('maintenance.jobs.name_hint') }}</XhFieldDescription>
      </XhFieldRoot>

      <XhFieldRoot :invalid="!!errorOf('jobType')" required>
        <XhFieldLabel>{{ t('maintenance.jobs.job_type') }}</XhFieldLabel>
        <XhFieldControl>
          <XSelect v-model:value="form.jobType" name="jobType" filterable :options="typeOptions" :placeholder="t('maintenance.jobs.job_type_placeholder')" />
        </XhFieldControl>
        <XhFieldErrorText v-if="errorOf('jobType')">
          {{ errorOf('jobType') }}
        </XhFieldErrorText>
      </XhFieldRoot>

      <XhFieldRoot class="xh-span-2" :invalid="!!errorOf('expression')" required>
        <XhFieldLabel>{{ t('maintenance.jobs.schedule') }}</XhFieldLabel>
        <XhFieldControl :as-child="false">
          <JobScheduleEditor v-model:expression="form.expression" :invalid="!!errorOf('expression')" />
        </XhFieldControl>
        <XhFieldErrorText v-if="errorOf('expression')">
          {{ errorOf('expression') }}
        </XhFieldErrorText>
      </XhFieldRoot>

      <XhFieldRoot class="xh-span-2" :invalid="!!errorOf('timeZone')">
        <XhFieldLabel>{{ t('maintenance.jobs.time_zone') }}</XhFieldLabel>
        <XhFieldControl>
          <XCombobox
            :options="timeZoneOptions"
            :value="form.timeZone || null"
            :loading="timeZones.loading.value"
            :placeholder="t('maintenance.jobs.time_zone_placeholder')"
            clearable
            allow-custom-value
            @update:value="onTimeZone"
          />
        </XhFieldControl>
        <XhFieldDescription>{{ t('maintenance.jobs.time_zone_hint') }}</XhFieldDescription>
      </XhFieldRoot>

      <XhFieldRoot class="xh-span-2" :invalid="!!errorOf('data')">
        <XhFieldLabel>{{ t('maintenance.jobs.data') }}</XhFieldLabel>
        <XhFieldControl>
          <XInput v-model:value="form.data" name="data" type="textarea" :autosize="{ minRows: 3, maxRows: 10 }" :placeholder="DATA_EXAMPLE" />
        </XhFieldControl>
        <XhFieldErrorText v-if="errorOf('data')">
          {{ errorOf('data') }}
        </XhFieldErrorText>
        <XhFieldDescription>{{ t('maintenance.jobs.data_hint') }}</XhFieldDescription>
      </XhFieldRoot>

      <JobRetryFields v-model="retry" :errors="fieldErrors" />

      <XhFieldRoot>
        <XhFieldLabel :id="concurrentLabelId">
          {{ t('maintenance.jobs.allow_concurrent') }}
        </XhFieldLabel>
        <XhFieldControl>
          <XhSwitch v-model:checked="form.allowConcurrent" :aria-labelledby="concurrentLabelId" />
        </XhFieldControl>
      </XhFieldRoot>

      <XhFieldRoot class="xh-span-2">
        <XhFieldLabel>{{ t('maintenance.jobs.task_description') }}</XhFieldLabel>
        <XhFieldControl>
          <XInput v-model:value="form.description" name="description" type="textarea" :max-length="500" show-count :autosize="{ minRows: 2, maxRows: 6 }" :placeholder="t('maintenance.jobs.description_placeholder')" />
        </XhFieldControl>
      </XhFieldRoot>
    </XhFormRoot>
  </XEditModal>
</template>
