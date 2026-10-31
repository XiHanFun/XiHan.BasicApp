<script setup lang="ts">
import type { RetryForm } from '../job'
import { XhFieldControl, XhFieldDescription, XhFieldErrorText, XhFieldLabel, XhFieldRoot } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { XNumberInput, XSelect } from '~/components'
import { backoffOptions, MAX_RETRIES, MAX_RETRY_SECONDS } from '../job'

/**
 * 重试与执行时限（照 4.x 的超时时间与最大重试次数；6.0 另有重试间隔、等待方式与指数退避的上限）。
 * 新建与修改计划共用；时限与重试次数留空即不限时、不重试，设了重试次数才显示间隔与等待方式。
 * 放在父表单的两列网格里，按字段排开。
 */
defineOptions({ name: 'JobRetryFields' })

const props = defineProps<{
  /** 后端按字段名给的校验错误 */
  errors?: Record<string, string[]>
}>()

const model = defineModel<RetryForm>({ required: true })

const { t } = useI18n()
const options = computed(() => backoffOptions(t))
const retrying = computed(() => model.value.maxRetries !== null)
const errorOf = (field: keyof RetryForm) => props.errors?.[field]?.[0]

function set<K extends keyof RetryForm>(field: K, value: RetryForm[K]) {
  model.value = { ...model.value, [field]: value }
}
</script>

<template>
  <XhFieldRoot :invalid="!!errorOf('timeoutSeconds')">
    <XhFieldLabel>{{ t('maintenance.jobs.timeout_label') }}</XhFieldLabel>
    <XhFieldControl>
      <XNumberInput :value="model.timeoutSeconds" name="timeoutSeconds" :min="1" :max="MAX_RETRY_SECONDS" :precision="0" :placeholder="t('maintenance.jobs.no_timeout')" @update:value="(value: number | null) => set('timeoutSeconds', value)" />
    </XhFieldControl>
    <XhFieldErrorText v-if="errorOf('timeoutSeconds')">
      {{ errorOf('timeoutSeconds') }}
    </XhFieldErrorText>
    <XhFieldDescription>{{ t('maintenance.jobs.timeout_hint') }}</XhFieldDescription>
  </XhFieldRoot>

  <XhFieldRoot :invalid="!!errorOf('maxRetries')">
    <XhFieldLabel>{{ t('maintenance.jobs.max_retry_count') }}</XhFieldLabel>
    <XhFieldControl>
      <XNumberInput :value="model.maxRetries" name="maxRetries" :min="1" :max="MAX_RETRIES" :precision="0" :placeholder="t('maintenance.jobs.no_retry')" @update:value="(value: number | null) => set('maxRetries', value)" />
    </XhFieldControl>
    <XhFieldErrorText v-if="errorOf('maxRetries')">
      {{ errorOf('maxRetries') }}
    </XhFieldErrorText>
    <XhFieldDescription>{{ t('maintenance.jobs.retry_hint') }}</XhFieldDescription>
  </XhFieldRoot>

  <template v-if="retrying">
    <XhFieldRoot :invalid="!!errorOf('retryDelaySeconds') || model.retryDelaySeconds === null" required>
      <XhFieldLabel>{{ t('maintenance.jobs.retry_delay_label') }}</XhFieldLabel>
      <XhFieldControl>
        <XNumberInput :value="model.retryDelaySeconds" name="retryDelaySeconds" :min="1" :max="MAX_RETRY_SECONDS" :precision="0" @update:value="(value: number | null) => set('retryDelaySeconds', value)" />
      </XhFieldControl>
      <XhFieldErrorText v-if="errorOf('retryDelaySeconds')">
        {{ errorOf('retryDelaySeconds') }}
      </XhFieldErrorText>
    </XhFieldRoot>

    <XhFieldRoot>
      <XhFieldLabel>{{ t('maintenance.jobs.retry_backoff') }}</XhFieldLabel>
      <XhFieldControl>
        <XSelect :value="model.retryBackoff" name="retryBackoff" :options="options" @update:value="(value: unknown) => set('retryBackoff', value === 'Exponential' ? 'Exponential' : 'Fixed')" />
      </XhFieldControl>
    </XhFieldRoot>

    <XhFieldRoot v-if="model.retryBackoff === 'Exponential'" class="xh-span-2" :invalid="!!errorOf('retryMaxDelaySeconds')">
      <XhFieldLabel>{{ t('maintenance.jobs.retry_max_delay_label') }}</XhFieldLabel>
      <XhFieldControl>
        <XNumberInput :value="model.retryMaxDelaySeconds" name="retryMaxDelaySeconds" :min="1" :max="MAX_RETRY_SECONDS" :precision="0" :placeholder="t('maintenance.jobs.no_timeout')" @update:value="(value: number | null) => set('retryMaxDelaySeconds', value)" />
      </XhFieldControl>
      <XhFieldErrorText v-if="errorOf('retryMaxDelaySeconds')">
        {{ errorOf('retryMaxDelaySeconds') }}
      </XhFieldErrorText>
      <XhFieldDescription>{{ t('maintenance.jobs.retry_max_delay_hint') }}</XhFieldDescription>
    </XhFieldRoot>
  </template>
</template>
