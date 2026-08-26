<!--
  业务编号规则预览面板。
  职责：根据列表规则执行不消耗流水的单个或连续批量预览，限制批量数量为 1～50，并展示周期、规则本地时间和预览区间。
-->
<script setup lang="ts">
import type {
  NumberingPreviewDto,
  NumberingRuleListItemDto,
} from '@/api'
import type { XDataTableColumn } from '~/components'
import { XhButton, XhCardBody, XhCardRoot, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDialogCloseTrigger, XhDialogContent, XhDialogRoot, XhDialogTitle, XhEmptyStateDescription, XhEmptyStateIcon, XhEmptyStateRoot, XhEmptyStateTitle, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFlex, XhFormRoot } from '@xihan-ui/vue'
import { computed, onUnmounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  NUMBERING_BATCH_PREVIEW_MAX_COUNT,
  numberingApi,
} from '@/api'
import { XDataTable, XInput, XNumberInput, XSegmented } from '~/components'
import { toast } from '~/composables'
import { Icon } from '~/iconify'

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
const mode = ref<PreviewMode>('single')
const sampleValue = ref('1')
const batchCount = ref<number | null>(DefaultBatchPreviewCount)
const loading = ref(false)
const rows = ref<PreviewTableRow[]>([])
const metadata = ref<PreviewMetadata | null>(null)
let requestVersion = 0

const title = computed(() => t('setting.numbering.preview_panel_title', { code: props.rule?.ruleCode ?? '' }))
const columns = computed<XDataTableColumn<PreviewTableRow>[]>(() => [
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
    ellipsis: true,
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
    toast.warning(t('setting.numbering.preview_invalid_start'))
    return
  }

  const count = batchCount.value
  if (mode.value === 'batch'
    && (!Number.isInteger(count)
      || count === null
      || count < 1
      || count > NUMBERING_BATCH_PREVIEW_MAX_COUNT)) {
    toast.warning(t('setting.numbering.preview_invalid_count', { max: NUMBERING_BATCH_PREVIEW_MAX_COUNT }))
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
      toast.error((error as Error).message || t('setting.numbering.preview_failed'))
  }
  finally {
    if (version === requestVersion)
      loading.value = false
  }
}
</script>

<template>
  <XhDialogRoot
    :open="show"
    @update:open="(open: boolean) => emit('update:show', open)"
  >
    <XhDialogContent style="--xh-dialog-max-w: 1040px">
      <XhDialogTitle>{{ title }}</XhDialogTitle>
      <XhDialogCloseTrigger />
      <div class="grid min-h-[480px] grid-cols-1 gap-4 lg:grid-cols-[340px_minmax(0,1fr)]">
        <XhCardRoot variant="ghost">
          <XhCardBody>
            <XhFlex direction="column" gap="lg">
              <XhDescriptionsRoot v-if="rule" :columns="1" bordered placement="left" size="sm">
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('setting.numbering.rule_code') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>
                    {{ rule.ruleCode }}
                  </XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('setting.numbering.time_zone') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>
                    {{ rule.timeZoneId }}
                  </XhDescriptionsValue>
                </XhDescriptionsItem>
              </XhDescriptionsRoot>

              <XhFormRoot
                validate-on="blur"
              >
                <XhFieldRoot>
                  <XhFieldLabel>{{ t('setting.numbering.preview_mode') }}</XhFieldLabel>
                  <XhFieldControl>
                    <XSegmented v-model:value="mode" :options="[{ value: 'single', label: t('setting.numbering.preview_single') }, { value: 'batch', label: t('setting.numbering.preview_batch') }]" />
                  </XhFieldControl>
                  <XhFieldErrorText />
                </XhFieldRoot>

                <XhFieldRoot>
                  <XhFieldLabel>{{ t('setting.numbering.preview_start_value') }}</XhFieldLabel>
                  <XhFieldControl>
                    <XInput v-model:value="sampleValue" :max-length="18" inputmode="numeric" />
                  </XhFieldControl>
                  <XhFieldErrorText />
                </XhFieldRoot>

                <XhFieldRoot v-if="mode === 'batch'">
                  <XhFieldLabel>{{ t('setting.numbering.preview_count') }}</XhFieldLabel>
                  <XhFieldControl>
                    <XNumberInput
                      v-model:value="batchCount"
                      class="w-full"
                      :min="1"
                      :max="NUMBERING_BATCH_PREVIEW_MAX_COUNT"
                      :precision="0"
                    />
                  </XhFieldControl>
                  <XhFieldErrorText />
                </XhFieldRoot>
              </XhFormRoot>

              <XhButton full-width variant="solid" tone="brand" :loading="loading" :disabled="!rule" @click="executePreview">
                {{ t('setting.numbering.preview_execute') }}
              </XhButton>
            </XhFlex>
          </XhCardBody>
        </XhCardRoot>

        <XhCardRoot variant="ghost">
          <XhCardBody>
            <XhFlex v-if="metadata" direction="column" gap="lg">
              <XhDescriptionsRoot :columns="3" bordered placement="top" size="sm">
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('setting.numbering.preview_period') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>
                    {{ metadata.periodKey }}
                  </XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('setting.numbering.preview_rule_local_time') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>
                    {{ metadata.ruleLocalTime }}
                  </XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('setting.numbering.preview_serial_range') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>
                    {{ metadata.startValue }} - {{ metadata.endValue }}
                  </XhDescriptionsValue>
                </XhDescriptionsItem>
              </XhDescriptionsRoot>

              <XDataTable
                :columns="columns"
                :data="rows"
                :row-key="(row: PreviewTableRow) => String(row.sequence)"
              />
            </XhFlex>
            <div v-else class="flex min-h-[400px] items-center justify-center">
              <XhEmptyStateRoot>
                <XhEmptyStateIcon>
                  <Icon icon="lucide:inbox" width="28" />
                </XhEmptyStateIcon>
                <XhEmptyStateTitle>{{ t('common.empty') }}</XhEmptyStateTitle>
                <XhEmptyStateDescription>{{ t('setting.numbering.preview_empty') }}</XhEmptyStateDescription>
              </XhEmptyStateRoot>
            </div>
          </XhCardBody>
        </XhCardRoot>
      </div>
    </XhDialogContent>
  </XhDialogRoot>
</template>
