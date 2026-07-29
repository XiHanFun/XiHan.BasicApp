<!--
  业务编号规则预览面板。
  职责：根据列表规则执行不消耗流水的单个或连续批量预览，限制批量数量为 1～50，并展示周期、规则本地时间和预览区间。
-->
<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type {
  NumberingPreviewDto,
  NumberingRuleListItemDto,
} from '@/api'
import {
  NButton,
  NCard,
  NDataTable,
  NDescriptions,
  NDescriptionsItem,
  NEmpty,
  NForm,
  NFormItem,
  NInput,
  NInputNumber,
  NModal,
  NRadioButton,
  NRadioGroup,
  NSpace,
  useMessage,
} from 'naive-ui'
import { computed, onUnmounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  NUMBERING_BATCH_PREVIEW_MAX_COUNT,
  numberingApi,
} from '@/api'

defineOptions({ name: 'NumberingPreviewModal' })

const props = defineProps<NumberingPreviewModalProps>()
const emit = defineEmits<{
  'update:show': [value: boolean]
}>()

/** 预览面板输入属性。 */
interface NumberingPreviewModalProps {
  /** 是否显示预览面板。 */
  show: boolean
  /** 当前待预览规则；关闭面板时允许为空。 */
  rule: NumberingRuleListItemDto | null
}

/** 预览结果表格行。 */
interface PreviewTableRow {
  /** 当前结果在预览区间中的一号起始序号。 */
  sequence: number
  /** 后端格式器生成的完整编号。 */
  number: string
}

/** 预览结果公共元数据。 */
interface PreviewMetadata {
  /** 规则时区下的本地时间字符串。 */
  ruleLocalTime: string
  /** 当前预览所属周期键。 */
  periodKey: string
  /** 预览起始流水的十进制字符串。 */
  startValue: string
  /** 预览结束流水的十进制字符串。 */
  endValue: string
}

type PreviewMode = 'single' | 'batch'

/** 批量模式默认展示十个连续编号，用户可在 1～50 内调整。 */
const DefaultBatchPreviewCount = 10
const { t } = useI18n()
const message = useMessage()
const mode = ref<PreviewMode>('single')
const sampleValue = ref('1')
const batchCount = ref<number | null>(DefaultBatchPreviewCount)
const loading = ref(false)
const rows = ref<PreviewTableRow[]>([])
const metadata = ref<PreviewMetadata | null>(null)
let requestVersion = 0

const title = computed(() => t('setting.numbering.preview_panel_title', { code: props.rule?.ruleCode ?? '' }))
const columns = computed<DataTableColumns<PreviewTableRow>>(() => [
  {
    key: 'sequence',
    title: t('setting.numbering.preview_sequence'),
    width: 90,
    align: 'center',
    titleAlign: 'center',
  },
  {
    key: 'number',
    title: t('setting.numbering.preview_number'),
    minWidth: 260,
    align: 'center',
    titleAlign: 'center',
    ellipsis: { tooltip: true },
  },
])

watch(
  () => [props.show, props.rule?.basicId] as const,
  ([show]) => {
    if (!show)
      return
    resetPanel()
  },
)

watch([mode, sampleValue, batchCount], () => {
  // 输入变化后旧结果已不再对应当前条件，立即清空可避免用户误读。
  clearResult()
})

onUnmounted(() => {
  // 请求封装未暴露 AbortSignal；递增版本可阻止组件卸载后的旧响应回写状态。
  requestVersion++
})

/**
 * 重置面板到安全的示例初始值。
 * @returns 无返回值。
 */
function resetPanel(): void {
  requestVersion++
  mode.value = 'single'
  sampleValue.value = '1'
  batchCount.value = DefaultBatchPreviewCount
  loading.value = false
  clearResult()
}

/**
 * 清空预览列表和其对应元数据。
 * @returns 无返回值。
 */
function clearResult(): void {
  rows.value = []
  metadata.value = null
}

/**
 * 使用当前规则组装单个与批量接口共享的纯格式预览参数。
 * @param rule 当前列表规则。
 * @param normalizedSampleValue 已校验的十进制示例流水字符串。
 * @returns 不包含规则主键和租户 ID 的格式预览参数。
 */
function createPreviewInput(
  rule: NumberingRuleListItemDto,
  normalizedSampleValue: string,
): NumberingPreviewDto {
  return {
    prefix: rule.prefix?.trim() || null,
    separator: rule.separator,
    dateFormat: rule.dateFormat,
    serialLength: rule.serialLength,
    resetCycle: rule.resetCycle,
    timeZoneId: rule.timeZoneId,
    sampleValue: normalizedSampleValue,
  }
}

/**
 * 校验示例流水是否为 1 至 18 位正整数。
 * @param value 待校验文本。
 * @returns 校验是否通过。
 */
function isValidSampleValue(value: string): boolean {
  return /^[1-9]\d{0,17}$/.test(value)
}

/**
 * 执行不消耗流水的单个或批量预览；loading 锁阻止重复点击，版本号隔离快速切换规则产生的旧响应。
 * @returns 完成信号。
 * @throws 请求错误由统一请求层包装后在面板中展示。
 */
async function executePreview(): Promise<void> {
  if (!props.rule || loading.value)
    return

  const normalizedSampleValue = sampleValue.value.trim()
  if (!isValidSampleValue(normalizedSampleValue)) {
    message.warning(t('setting.numbering.preview_invalid_start'))
    return
  }

  const count = batchCount.value
  if (mode.value === 'batch'
    && (!Number.isInteger(count)
      || count === null
      || count < 1
      || count > NUMBERING_BATCH_PREVIEW_MAX_COUNT)) {
    message.warning(t('setting.numbering.preview_invalid_count', { max: NUMBERING_BATCH_PREVIEW_MAX_COUNT }))
    return
  }

  const version = ++requestVersion
  const input = createPreviewInput(props.rule, normalizedSampleValue)
  loading.value = true
  clearResult()
  try {
    if (mode.value === 'single') {
      const result = await numberingApi.preview(input)
      if (version !== requestVersion)
        return
      rows.value = [{ sequence: 1, number: result.number }]
      metadata.value = {
        ruleLocalTime: result.ruleLocalTime,
        periodKey: result.periodKey,
        startValue: normalizedSampleValue,
        endValue: normalizedSampleValue,
      }
      return
    }

    const result = await numberingApi.previewBatch({
      ...input,
      count: count!,
    })
    if (version !== requestVersion)
      return
    rows.value = result.numbers.map((number, index) => ({ sequence: index + 1, number }))
    metadata.value = {
      ruleLocalTime: result.ruleLocalTime,
      periodKey: result.periodKey,
      startValue: result.startValue,
      endValue: result.endValue,
    }
  }
  catch (error) {
    if (version === requestVersion)
      message.error((error as Error).message || t('setting.numbering.preview_failed'))
  }
  finally {
    if (version === requestVersion)
      loading.value = false
  }
}
</script>

<template>
  <NModal
    :show="show"
    preset="card"
    :title="title"
    style="width: 1040px; max-width: 94vw"
    @update:show="emit('update:show', $event)"
  >
    <div class="grid min-h-[480px] grid-cols-1 gap-4 lg:grid-cols-[340px_minmax(0,1fr)]">
      <NCard :title="t('setting.numbering.preview_conditions')" size="small" embedded>
        <NSpace vertical :size="16">
          <NDescriptions v-if="rule" :column="1" bordered label-placement="left" size="small">
            <NDescriptionsItem :label="t('setting.numbering.rule_code')">
              {{ rule.ruleCode }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.numbering.time_zone')">
              {{ rule.timeZoneId }}
            </NDescriptionsItem>
          </NDescriptions>

          <NForm label-placement="top">
            <NFormItem :label="t('setting.numbering.preview_mode')">
              <NRadioGroup v-model:value="mode">
                <NRadioButton value="single">
                  {{ t('setting.numbering.preview_single') }}
                </NRadioButton>
                <NRadioButton value="batch">
                  {{ t('setting.numbering.preview_batch') }}
                </NRadioButton>
              </NRadioGroup>
            </NFormItem>

            <NFormItem :label="t('setting.numbering.preview_start_value')">
              <NInput v-model:value="sampleValue" maxlength="18" :input-props="{ inputmode: 'numeric' }" />
            </NFormItem>

            <NFormItem v-if="mode === 'batch'" :label="t('setting.numbering.preview_count')">
              <NInputNumber
                v-model:value="batchCount"
                class="w-full"
                :min="1"
                :max="NUMBERING_BATCH_PREVIEW_MAX_COUNT"
                :precision="0"
              />
            </NFormItem>
          </NForm>

          <NButton block type="primary" :loading="loading" :disabled="!rule" @click="executePreview">
            {{ t('setting.numbering.preview_execute') }}
          </NButton>
        </NSpace>
      </NCard>

      <NCard :title="t('setting.numbering.preview_results')" size="small">
        <NSpace v-if="metadata" vertical :size="16">
          <NDescriptions
            :column="3"
            bordered
            label-placement="top"
            size="small"
            :label-style="{ textAlign: 'center' }"
            :content-style="{ textAlign: 'center' }"
          >
            <NDescriptionsItem :label="t('setting.numbering.preview_period')">
              {{ metadata.periodKey }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.numbering.preview_rule_local_time')">
              {{ metadata.ruleLocalTime }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.numbering.preview_serial_range')">
              {{ metadata.startValue }} - {{ metadata.endValue }}
            </NDescriptionsItem>
          </NDescriptions>

          <NDataTable
            :columns="columns"
            :data="rows"
            :pagination="false"
            :row-key="(row: PreviewTableRow) => row.sequence"
            :max-height="400"
            virtual-scroll
          />
        </NSpace>
        <div v-else class="flex min-h-[400px] items-center justify-center">
          <NEmpty :description="t('setting.numbering.preview_empty')" />
        </div>
      </NCard>
    </div>
  </NModal>
</template>
