<!--
  业务编号规则预览面板。
  职责：按已保存规则的格式执行不消耗流水的单个或连续批量预览，批量数量限制为 1～50，并展示周期、日期段和示例流水区间。
-->
<script setup lang="ts">
import type { RuleRow } from '../use-numbering-rules'
import type { XDataTableColumn } from '~/components'
import { XhButton, XhButtonIndicator, XhButtonLabel, XhCardContent, XhCardRoot, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDialogBody, XhDialogCloseTrigger, XhDialogContent, XhDialogHeader, XhDialogRoot, XhDialogTitle, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhFieldControl, XhFieldLabel, XhFieldRoot, XhFlex, XhFormRoot } from '@xihan-ui/vue'
import { computed, onUnmounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon, XDataTable, XInput, XNumberInput, XSegmented } from '~/components'
import { toast } from '~/composables'
import { PREVIEW_MAX_COUNT, useNumberingPreview } from '../use-numbering-rules'

defineOptions({ name: 'NumberingPreviewModal' })

const props = defineProps<{
  /** 是否显示预览面板 */
  show: boolean
  /** 当前待预览规则；关闭面板时允许为空 */
  rule: null | RuleRow
}>()

const emit = defineEmits<{
  'update:show': [value: boolean]
}>()

const { t } = useI18n()
const preview = useNumberingPreview()

interface PreviewTableRow {
  sequence: number
  number: string
}

const title = computed(() => t('settings.numbering_rules.preview_title', { code: props.rule?.code ?? '' }))
const modeOptions = computed(() => [
  { value: 'single' as const, label: t('settings.numbering_rules.preview_single') },
  { value: 'batch' as const, label: t('settings.numbering_rules.preview_batch') },
])
const columns = computed<XDataTableColumn<PreviewTableRow>[]>(() => [
  { key: 'sequence', title: t('settings.numbering_rules.preview_sequence'), width: 90, align: 'center' },
  { key: 'number', title: t('settings.numbering_rules.preview_number'), minWidth: 260, align: 'center', ellipsis: true },
])

watch(
  () => [props.show, props.rule?.id] as const,
  ([show]) => {
    if (show)
      preview.load(props.rule)
  },
)

onUnmounted(() => {
  // 请求封装未暴露 AbortSignal；作废版本可阻止组件卸载后的旧响应回写状态
  preview.dispose()
})

async function executePreview(): Promise<void> {
  try {
    const outcome = await preview.execute()
    if (outcome === 'invalid_start')
      toast.warning(t('settings.numbering_rules.preview_invalid_start'))
    else if (outcome === 'invalid_count')
      toast.warning(t('settings.numbering_rules.preview_invalid_count', { max: PREVIEW_MAX_COUNT }))
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.operation_failed'))
  }
}
</script>

<template>
  <XhDialogRoot
    :open="show"
    @update:open="(open: boolean) => emit('update:show', open)"
  >
    <XhDialogContent style="--xh-dialog-max-w: 1040px">
      <XhDialogHeader>
        <XhDialogTitle>{{ title }}</XhDialogTitle>
      </XhDialogHeader>
      <XhDialogCloseTrigger />
      <XhDialogBody class="xh-overlay-body">
        <div class="grid min-h-[480px] grid-cols-1 gap-4 lg:grid-cols-[340px_minmax(0,1fr)]">
          <XhCardRoot variant="ghost">
            <XhCardContent>
              <XhFlex orientation="vertical" gap="lg">
                <XhDescriptionsRoot v-if="rule" :columns="1" variant="outline" size="sm">
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('settings.numbering_rules.fields.code') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ rule.code }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('settings.numbering_rules.fields.template') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ rule.template }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('settings.numbering_rules.fields.time_zone') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ rule.timeZoneId }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                </XhDescriptionsRoot>

                <XhFormRoot @submit="executePreview">
                  <XhFieldRoot>
                    <XhFieldLabel>{{ t('settings.numbering_rules.preview_mode') }}</XhFieldLabel>
                    <XhFieldControl>
                      <XSegmented v-model:value="preview.mode.value" :options="modeOptions" />
                    </XhFieldControl>
                  </XhFieldRoot>
                  <XhFieldRoot>
                    <XhFieldLabel>{{ t('settings.numbering_rules.preview_start_value') }}</XhFieldLabel>
                    <XhFieldControl>
                      <XInput v-model:value="preview.startValue.value" name="startValue" :max-length="18" inputmode="numeric" autocomplete="off" />
                    </XhFieldControl>
                  </XhFieldRoot>
                  <XhFieldRoot v-if="preview.mode.value === 'batch'">
                    <XhFieldLabel>{{ t('settings.numbering_rules.preview_count') }}</XhFieldLabel>
                    <XhFieldControl>
                      <XNumberInput v-model:value="preview.count.value" name="count" class="w-full" :min="1" :max="PREVIEW_MAX_COUNT" :precision="0" />
                    </XhFieldControl>
                  </XhFieldRoot>
                </XhFormRoot>

                <XhButton full-width variant="solid" tone="brand" :loading="preview.loading.value" :disabled="!rule" @click="executePreview">
                  <XhButtonIndicator />
                  <XhButtonLabel>{{ t('settings.numbering_rules.preview_execute') }}</XhButtonLabel>
                </XhButton>
              </XhFlex>
            </XhCardContent>
          </XhCardRoot>

          <XhCardRoot variant="ghost">
            <XhCardContent>
              <XhFlex v-if="preview.metadata.value" orientation="vertical" gap="lg">
                <XhDescriptionsRoot :columns="3" variant="outline" size="sm">
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('settings.numbering_rules.preview_period') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ preview.metadata.value.periodKey }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('settings.numbering_rules.preview_date_text') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ preview.metadata.value.dateText || '-' }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('settings.numbering_rules.preview_serial_range') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ preview.metadata.value.startValue }} - {{ preview.metadata.value.endValue }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                </XhDescriptionsRoot>

                <XDataTable
                  :columns="columns"
                  :data="preview.rows.value"
                  :row-key="(row: PreviewTableRow) => String(row.sequence)"
                />
              </XhFlex>
              <div v-else class="flex min-h-[400px] items-center justify-center">
                <XhEmptyStateRoot>
                  <XhEmptyStateIndicator>
                    <Icon icon="lucide:inbox" width="28" />
                  </XhEmptyStateIndicator>
                  <XhEmptyStateTitle>{{ t('common.statuses.no_data') }}</XhEmptyStateTitle>
                  <XhEmptyStateDescription>{{ t('settings.numbering_rules.preview_empty') }}</XhEmptyStateDescription>
                </XhEmptyStateRoot>
              </div>
            </XhCardContent>
          </XhCardRoot>
        </div>
      </XhDialogBody>
    </XhDialogContent>
  </XhDialogRoot>
</template>
