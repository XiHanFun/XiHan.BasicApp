<!--
  业务编号规则编辑器。
  职责：创建/更新规则、展示格式冻结提示并调用无副作用预览接口；真实发号不在本组件暴露。
-->
<script setup lang="ts">
import type {
  NumberingRuleCreateDto,
  NumberingRuleDetailDto,
  NumberingRuleUpdateDto,
  NumberingScope,
} from '@/api'
import {
  NAlert,
  NButton,
  NForm,
  NFormItem,
  NInput,
  NInputNumber,
  NSelect,
  NSpace,
  NSwitch,
  NTag,
  NTooltip,
  useMessage,
} from 'naive-ui'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  EnableStatus,
  numberingApi,
  NumberingDateFormat,
  NumberingResetCycle,
} from '@/api'
import { XEditModal } from '~/components'
import { useTimezoneOptions } from '~/composables'
import { useEnumOptions } from '~/hooks'

defineOptions({ name: 'NumberingRuleEditor' })

const props = defineProps<{
  show: boolean
  detail: NumberingRuleDetailDto | null
  scope: NumberingScope
  globalMode: boolean
}>()

const emit = defineEmits<{
  'saved': []
  'update:show': [value: boolean]
}>()

interface RuleFormModel {
  ruleCode: string
  ruleName: string
  prefix: string | null
  separator: string
  dateFormat: NumberingDateFormat
  serialLength: number
  resetCycle: NumberingResetCycle
  timeZoneId: string
  allowTenantUse: boolean
  status: EnableStatus
  sort: number
  remark: string | null
}

const { t } = useI18n()
const message = useMessage()
const submitLoading = ref(false)
const previewLoading = ref(false)
const previewNumber = ref('')
const { loading: timeZoneLoading, ensureLoaded, withCurrent } = useTimezoneOptions()
const form = ref<RuleFormModel>(createDefaultForm())

const title = computed(() => props.detail ? t('setting.numbering.edit_title') : t('setting.numbering.add_title'))
const formatFrozen = computed(() => Boolean(props.detail?.hasAllocated))

// 枚举标签的单一事实源是后端枚举元数据，切换语言时由 useEnumService 整库重取一次并响应式刷新；
// 这里的兜底数组只在元数据尚未到达时短暂生效，不参与日常展示。
const dateFormatOptions = useEnumOptions('NumberingDateFormat', [
  { label: 'yyyyMMdd', value: NumberingDateFormat.YyyyMMdd },
])

const resetCycleOptions = useEnumOptions('NumberingResetCycle', [
  { label: 'Daily', value: NumberingResetCycle.Daily },
])

// 时区目录与顶栏 / 个人中心共用同一来源，此处不再单独维护
const timeZoneOptions = computed(() =>
  withCurrent(form.value.timeZoneId).map(zone => ({ label: zone.label, value: zone.value })))

watch(
  () => [props.show, props.detail] as const,
  ([show]) => {
    if (!show)
      return
    form.value = props.detail ? toForm(props.detail) : createDefaultForm()
    previewNumber.value = ''
    void ensureLoaded()
  },
  { immediate: true },
)

/** 创建安全默认表单。 */
function createDefaultForm(): RuleFormModel {
  return {
    ruleCode: '',
    ruleName: '',
    prefix: '',
    separator: '-',
    dateFormat: NumberingDateFormat.YyyyMMdd,
    serialLength: 4,
    resetCycle: NumberingResetCycle.Daily,
    timeZoneId: 'UTC',
    allowTenantUse: false,
    status: EnableStatus.Enabled,
    sort: 100,
    remark: '',
  }
}

/**
 * 将规则详情复制到可编辑表单。
 * @param detail 后端规则详情。
 * @returns 独立表单对象。
 */
function toForm(detail: NumberingRuleDetailDto): RuleFormModel {
  return {
    ruleCode: detail.ruleCode,
    ruleName: detail.ruleName,
    prefix: detail.prefix ?? '',
    separator: detail.separator,
    dateFormat: detail.dateFormat,
    serialLength: detail.serialLength,
    resetCycle: detail.resetCycle,
    timeZoneId: detail.timeZoneId,
    allowTenantUse: detail.allowTenantUse,
    status: detail.status,
    sort: detail.sort,
    remark: detail.remark ?? '',
  }
}

/** 归一可选字符串，避免把空白保存为业务值。 */
function optional(value: string | null): string | null {
  return value?.trim() || null
}

/**
 * 调用无副作用预览接口。
 * @returns 完成信号。
 * @throws 请求错误由统一拦截器包装后展示。
 */
async function preview(): Promise<void> {
  if (previewLoading.value)
    return
  previewLoading.value = true
  try {
    const result = await numberingApi.preview({
      prefix: optional(form.value.prefix),
      separator: form.value.separator,
      dateFormat: form.value.dateFormat,
      serialLength: form.value.serialLength,
      resetCycle: form.value.resetCycle,
      timeZoneId: form.value.timeZoneId.trim(),
      sampleValue: '1',
    })
    previewNumber.value = result.number
  }
  catch (error) {
    message.error((error as Error).message || t('setting.numbering.preview_failed'))
  }
  finally {
    previewLoading.value = false
  }
}

/**
 * 校验并提交创建或更新请求；loading 锁阻止重复提交。
 * @returns 完成信号。
 * @throws 请求错误由统一拦截器包装后展示。
 */
async function submit(): Promise<void> {
  if (submitLoading.value)
    return
  if (!form.value.ruleCode.trim() || !form.value.ruleName.trim() || !form.value.timeZoneId.trim()) {
    message.warning(t('setting.numbering.required_fields'))
    return
  }

  submitLoading.value = true
  try {
    const common = {
      scope: props.scope,
      ruleName: form.value.ruleName.trim(),
      prefix: optional(form.value.prefix),
      separator: form.value.separator,
      dateFormat: form.value.dateFormat,
      serialLength: form.value.serialLength,
      resetCycle: form.value.resetCycle,
      timeZoneId: form.value.timeZoneId.trim(),
      allowTenantUse: props.globalMode && form.value.allowTenantUse,
      sort: form.value.sort,
      remark: optional(form.value.remark),
    }
    if (props.detail) {
      await numberingApi.update({ ...common, basicId: props.detail.basicId } satisfies NumberingRuleUpdateDto)
    }
    else {
      await numberingApi.create({
        ...common,
        ruleCode: form.value.ruleCode.trim(),
        status: form.value.status,
      } satisfies NumberingRuleCreateDto)
    }
    message.success(t('setting.numbering.save_success'))
    emit('update:show', false)
    emit('saved')
  }
  catch (error) {
    message.error((error as Error).message || t('setting.numbering.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}
</script>

<template>
  <XEditModal
    :show="show"
    :title="title"
    :loading="submitLoading"
    @update:show="emit('update:show', $event)"
    @save="submit"
  >
    <NAlert v-if="formatFrozen" class="mb-3" type="warning" :show-icon="true">
      {{ t('setting.numbering.format_frozen_tip') }}
    </NAlert>
    <NForm :model="form" class="xh-edit-form-grid" label-placement="top">
      <NFormItem :label="t('setting.numbering.rule_code')">
        <NInput v-model:value="form.ruleCode" :disabled="Boolean(detail)" maxlength="100" />
      </NFormItem>
      <NFormItem :label="t('setting.numbering.rule_name')">
        <NInput v-model:value="form.ruleName" maxlength="100" />
      </NFormItem>
      <NFormItem :label="t('setting.numbering.prefix')">
        <NInput v-model:value="form.prefix" :disabled="formatFrozen" maxlength="50" />
      </NFormItem>
      <NFormItem :label="t('setting.numbering.separator')">
        <NInput v-model:value="form.separator" :disabled="formatFrozen" maxlength="10" />
      </NFormItem>
      <NFormItem :label="t('setting.numbering.date_format')">
        <NSelect v-model:value="form.dateFormat" :disabled="formatFrozen" :options="dateFormatOptions" />
      </NFormItem>
      <NFormItem :label="t('setting.numbering.serial_length')">
        <NInputNumber v-model:value="form.serialLength" :disabled="formatFrozen" :min="1" :max="18" />
      </NFormItem>
      <NFormItem :label="t('setting.numbering.reset_cycle')">
        <NSelect v-model:value="form.resetCycle" :disabled="formatFrozen" :options="resetCycleOptions" />
      </NFormItem>
      <NFormItem :label="t('setting.numbering.time_zone')">
        <NSelect
          v-model:value="form.timeZoneId"
          :disabled="formatFrozen"
          :loading="timeZoneLoading"
          :options="timeZoneOptions"
          filterable
        />
      </NFormItem>
      <NFormItem v-if="globalMode">
        <template #label>
          <span class="inline-flex items-center gap-1">
            <span>{{ t('setting.numbering.allow_tenant_use') }}</span>
            <NTooltip trigger="hover" :style="{ maxWidth: '360px' }">
              <template #trigger>
                <button
                  type="button"
                  class="inline-flex size-4 cursor-help items-center justify-center rounded-full border border-current text-[11px] leading-none text-gray-400"
                  :aria-label="t('setting.numbering.allow_tenant_use_tip')"
                >
                  ?
                </button>
              </template>
              {{ t('setting.numbering.allow_tenant_use_tip') }}
            </NTooltip>
          </span>
        </template>
        <NSwitch v-model:value="form.allowTenantUse" />
      </NFormItem>
      <NFormItem v-if="!detail" :label="t('setting.numbering.status')">
        <NSwitch
          :value="form.status === EnableStatus.Enabled"
          @update:value="form.status = $event ? EnableStatus.Enabled : EnableStatus.Disabled"
        />
      </NFormItem>
      <NFormItem :label="t('setting.numbering.sort')">
        <NInputNumber v-model:value="form.sort" :min="0" />
      </NFormItem>
      <NFormItem :label="t('setting.numbering.remark')" class="xh-span-2">
        <NInput v-model:value="form.remark" type="textarea" maxlength="500" />
      </NFormItem>
      <NFormItem :label="t('setting.numbering.preview')" class="xh-span-2">
        <NSpace align="center">
          <NButton :loading="previewLoading" @click="preview">
            {{ t('setting.numbering.preview_action') }}
          </NButton>
          <NTag v-if="previewNumber" type="info" size="large">
            {{ previewNumber }}
          </NTag>
        </NSpace>
      </NFormItem>
    </NForm>
  </XEditModal>
</template>
