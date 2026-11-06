<script setup lang="ts">
import type { HistoryDetail, HistoryRow } from './use-histories'
import type { GenerationStatus } from './use-tables'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import type { SelectOption } from '~/types'
import { XhButton, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDialogBody, XhDialogCloseTrigger, XhDialogContent, XhDialogFooter, XhDialogHeader, XhDialogRoot, XhDialogTitle, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { renderTag, SchemaPage } from '~/components'
import { toast } from '~/composables'
import { codegenHistories } from '~/contracts'
import { formatDate } from '~/utils'
import { downloadPackage, formatDuration, formatSize, hasPackage, historiesResource } from './use-histories'
import { DELIVERY_TYPES, enumKey, GENERATION_STATUSES } from './use-tables'

defineOptions({ name: 'CodegenHistoryPanel' })

const { t } = useI18n()
const resource = historiesResource()

const asRow = (row: object) => row as HistoryRow

function generationTone(status: GenerationStatus) {
  if (status === 'Generated')
    return 'success'
  return status === 'Failed'
    ? 'danger'
    : 'neutral'
}

function options(group: string, values: readonly string[]): SelectOption[] {
  return values.map(value => ({
    label: t(enumKey(group, value)),
    value,
  }))
}

const fields = computed<ListFieldSchema[]>(() => [
  {
    key: 'keyword',
    title: t('codegeneration.code_gen.history.col_table_name'),
    dataType: 'string',
    visible: false,
    searchable: true,
    searchPlaceholder: t('codegeneration.code_gen.history.search_placeholder'),
    order: 0,
  },
  {
    key: 'tableName',
    title: t('codegeneration.code_gen.history.col_table_name'),
    dataType: 'string',
    sortable: true,
    minWidth: 150,
    order: 1,
  },
  {
    key: 'batchNumber',
    title: t('codegeneration.code_gen.history.col_batch_number'),
    dataType: 'string',
    sortable: true,
    minWidth: 190,
    order: 2,
  },
  {
    key: 'generationStatus',
    title: t('codegeneration.code_gen.history.col_status'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    options: options('generation_status', GENERATION_STATUSES),
    searchPlaceholder: t('codegeneration.code_gen.history.filter_gen_status'),
    width: 100,
    order: 3,
    render: row => renderTag(t(enumKey('generation_status', asRow(row).generationStatus)), generationTone(asRow(row).generationStatus)),
  },
  {
    key: 'deliveryType',
    title: t('codegeneration.code_gen.history.col_gen_type'),
    dataType: 'enum',
    sortable: true,
    options: options('delivery_type', DELIVERY_TYPES),
    width: 120,
    order: 4,
  },
  {
    key: 'fileCount',
    title: t('codegeneration.code_gen.history.col_file_count'),
    dataType: 'number',
    sortable: true,
    width: 90,
    order: 5,
  },
  {
    key: 'totalSize',
    title: t('codegeneration.code_gen.history.col_total_size'),
    dataType: 'number',
    sortable: true,
    width: 110,
    order: 6,
    render: row => formatSize(asRow(row).totalSize),
  },
  {
    key: 'duration',
    title: t('codegeneration.code_gen.history.col_duration'),
    dataType: 'number',
    sortable: true,
    width: 100,
    order: 7,
    render: row => formatDuration(asRow(row).duration),
  },
  {
    key: 'operatorName',
    title: t('codegeneration.code_gen.history.col_operator'),
    dataType: 'string',
    sortable: true,
    width: 120,
    order: 8,
  },
  {
    key: 'generatedAt',
    title: t('codegeneration.code_gen.history.col_gen_time'),
    dataType: 'datetime',
    searchable: true,
    searchRange: true,
    sortable: true,
    minWidth: 170,
    order: 9,
  },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'codegeneration.code-gen.history',
  pageName: t('codegeneration.code_gen.tabs.history'),
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    {
      key: 'view',
      title: t('common.actions.detail'),
      scope: 'row',
      icon: 'lucide:eye',
    },
    {
      key: 'download',
      title: t('codegeneration.code_gen.history.action_download'),
      scope: 'row',
      tone: 'brand',
      icon: 'lucide:download',
      visible: row => hasPackage(asRow(row)),
      permission: 'codegeneration.codegen-histories.read',
    },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row
    ? asRow(payload.row)
    : undefined
  switch (payload.key) {
    case 'view':
      if (row)
        void openDetail(row)
      break
    case 'download':
      if (row)
        void download(row)
      break
  }
}

async function download(row: Pick<HistoryRow, 'id' | 'tableName' | 'batchNumber'>) {
  try {
    await downloadPackage(row)
  }
  catch (error) {
    toast.danger((error as Error).message || t('codegeneration.code_gen.history.download_failed'))
  }
}

// ── 详情弹窗 ──
const detailOpen = ref(false)
const detailLoading = ref(false)
const detail = ref<HistoryDetail | null>(null)

async function openDetail(row: HistoryRow) {
  detailOpen.value = true
  detailLoading.value = true
  detail.value = null
  try {
    detail.value = await codegenHistories.get({ id: row.id })
  }
  catch (error) {
    toast.danger((error as Error).message || t('codegeneration.code_gen.history.load_detail_failed'))
  }
  finally {
    detailLoading.value = false
  }
}

function orDash(value: null | string | undefined): string {
  return value == null || value === ''
    ? '-'
    : value
}
</script>

<template>
  <SchemaPage :schema="schema" @action="onAction">
    <XhDialogRoot v-model:open="detailOpen">
      <XhDialogContent class="xh-mgmt-detail-modal" style="--xh-dialog-max-w: 820px">
        <XhDialogHeader>
          <XhDialogTitle>{{ t('codegeneration.code_gen.history.detail_title') }}</XhDialogTitle>
        </XhDialogHeader>
        <XhDialogCloseTrigger />
        <XhDialogBody class="xh-overlay-body">
          <div v-if="detailLoading" class="history-detail__empty">
            {{ t('common.statuses.loading') }}
          </div>
          <template v-else-if="detail">
            <XhDescriptionsRoot :columns="2" variant="outline" size="sm">
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('codegeneration.code_gen.history.detail_table_name') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ detail.history.tableName }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('codegeneration.code_gen.history.detail_batch_number') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ orDash(detail.history.batchNumber) }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('codegeneration.code_gen.history.detail_status') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  <XhTagRoot variant="subtle" size="sm" :tone="generationTone(detail.history.generationStatus)">
                    <XhTagLabel>{{ t(enumKey('generation_status', detail.history.generationStatus)) }}</XhTagLabel>
                  </XhTagRoot>
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('codegeneration.code_gen.history.detail_gen_type') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ t(enumKey('delivery_type', detail.history.deliveryType)) }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('codegeneration.code_gen.history.detail_file_count') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ detail.history.fileCount }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('codegeneration.code_gen.history.detail_total_size') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ formatSize(detail.history.totalSize) }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('codegeneration.code_gen.history.detail_duration') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ formatDuration(detail.history.duration) }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('codegeneration.code_gen.history.detail_gen_time') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ formatDate(detail.history.generatedAt) }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('codegeneration.code_gen.history.detail_operator') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ orDash(detail.history.operatorName ?? detail.operatorId) }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('codegeneration.code_gen.history.detail_operator_ip') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ orDash(detail.operatorIp) }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem v-if="detail.history.packageExpiresAt">
                <XhDescriptionsLabel>{{ t('codegeneration.code_gen.history.detail_package_expires') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ formatDate(detail.history.packageExpiresAt) }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem v-if="detail.history.genPath" style="grid-column: span 2">
                <XhDescriptionsLabel>{{ t('codegeneration.code_gen.history.detail_gen_path') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ detail.history.genPath }}</XhDescriptionsValue>
              </XhDescriptionsItem>
            </XhDescriptionsRoot>

            <div v-if="detail.errorMessage" class="history-detail__section history-detail__section--error">
              <div class="history-detail__title">
                {{ t('codegeneration.code_gen.history.detail_error') }}
              </div>
              <pre class="history-detail__pre">{{ detail.errorMessage }}</pre>
            </div>

            <div v-if="detail.generatedFiles.length > 0" class="history-detail__section">
              <div class="history-detail__title">
                {{ t('codegeneration.code_gen.history.detail_artifacts', { count: detail.generatedFiles.length }) }}
              </div>
              <ul class="history-detail__files">
                <li v-for="file in detail.generatedFiles" :key="file">
                  {{ file }}
                </li>
              </ul>
            </div>

            <div v-if="detail.usedTemplates.length > 0" class="history-detail__section">
              <div class="history-detail__title">
                {{ t('codegeneration.code_gen.history.detail_used_templates', { count: detail.usedTemplates.length }) }}
              </div>
              <ul class="history-detail__files">
                <li v-for="code in detail.usedTemplates" :key="code">
                  {{ code }}
                </li>
              </ul>
            </div>
          </template>
          <div v-else class="history-detail__empty">
            {{ t('common.statuses.no_data') }}
          </div>
        </XhDialogBody>
        <XhDialogFooter>
          <XhButton variant="subtle" size="sm" @click="detailOpen = false">
            {{ t('common.actions.close') }}
          </XhButton>
          <XhButton
            v-if="detail && hasPackage(detail.history)"
            variant="subtle"
            size="sm"
            tone="brand"
            @click="download(detail.history)"
          >
            {{ t('codegeneration.code_gen.history.action_download') }}
          </XhButton>
        </XhDialogFooter>
      </XhDialogContent>
    </XhDialogRoot>
  </SchemaPage>
</template>

<style scoped>
.history-detail__empty {
  display: flex;
  justify-content: center;
  padding: var(--xh-space-4) 0;
  color: var(--xh-fg-muted);
}

.history-detail__section {
  margin-top: var(--xh-space-4);
}

.history-detail__title {
  margin-bottom: var(--xh-space-2);
  font-size: var(--xh-text-caption-size);
  font-weight: var(--xh-font-weight-semibold);
}

.history-detail__pre,
.history-detail__files {
  margin: 0;
  max-height: calc(var(--xh-space-8) * 8);
  overflow: auto;
  padding: var(--xh-space-2) var(--xh-space-3);
  border-radius: var(--xh-shape-inset);
  background: var(--xh-bg-subtle);
  font-size: var(--xh-text-caption-size);
  word-break: break-all;
}

.history-detail__pre {
  white-space: pre-wrap;
}

.history-detail__files {
  padding-left: var(--xh-space-6);
  line-height: var(--xh-leading-relaxed);
}

.history-detail__section--error .history-detail__pre {
  color: var(--xh-fg-danger);
}
</style>
