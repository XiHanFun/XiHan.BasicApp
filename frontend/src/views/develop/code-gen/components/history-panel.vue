<script setup lang="ts">
import type {
  CodeGenHistoryDetailDto,
  CodeGenHistoryListItemDto,
  GenStatus,
  PageResult,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import {
  NButton,
  NDescriptions,
  NDescriptionsItem,
  NModal,
  NSpace,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  codeGenHistoryApi,
  createPageRequest,
  GEN_STATUS_OPTIONS,
  GEN_TYPE_OPTIONS,
  GenStatus as GenStatusEnum,
  querySortsFromSchema,
} from '@/api'
import { SchemaPage } from '~/components'
import { getOptionLabel } from '~/utils'

defineOptions({ name: 'CodeGenHistoryPanel' })

const { t } = useI18n()
const message = useMessage()

function formatDuration(value: string) {
  const ms = Number(value)
  if (!Number.isFinite(ms)) {
    return value
  }
  return ms >= 1000 ? `${(ms / 1000).toFixed(2)}s` : `${ms}ms`
}

function formatSize(value: string) {
  const bytes = Number(value)
  if (!Number.isFinite(bytes)) {
    return value
  }
  if (bytes >= 1024 * 1024) {
    return `${(bytes / 1024 / 1024).toFixed(2)} MB`
  }
  if (bytes >= 1024) {
    return `${(bytes / 1024).toFixed(2)} KB`
  }
  return `${bytes} B`
}

function genStatusTagType(status: GenStatus) {
  if (status === GenStatusEnum.Generated) {
    return 'success'
  }
  if (status === GenStatusEnum.Failed) {
    return 'error'
  }
  return 'default'
}

const fields = computed<ListFieldSchema[]>(() => [
  {
    key: 'tableName',
    title: t('develop.code_gen.history.col_table_name'),
    dataType: 'string',
    searchable: true,
    sortable: true,
    searchPlaceholder: t('develop.code_gen.history.search_placeholder'),
    minWidth: 150,
    order: 0,
  },
  {
    key: 'batchNumber',
    title: t('develop.code_gen.history.col_batch_number'),
    dataType: 'string',
    sortable: true,
    minWidth: 160,
    order: 1,
  },
  {
    key: 'genStatus',
    title: t('develop.code_gen.history.col_status'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    options: GEN_STATUS_OPTIONS,
    searchPlaceholder: t('develop.code_gen.history.filter_gen_status'),
    width: 90,
    order: 2,
    render: (row) => {
      const r = row as unknown as CodeGenHistoryListItemDto
      return h(NTag, { size: 'small', round: true, bordered: false, type: genStatusTagType(r.genStatus) }, () => getOptionLabel(GEN_STATUS_OPTIONS, r.genStatus))
    },
  },
  {
    key: 'genType',
    title: t('develop.code_gen.history.col_gen_type'),
    dataType: 'enum',
    sortable: true,
    options: GEN_TYPE_OPTIONS,
    width: 110,
    order: 3,
    render: row => getOptionLabel(GEN_TYPE_OPTIONS, (row as unknown as CodeGenHistoryListItemDto).genType),
  },
  {
    key: 'fileCount',
    title: t('develop.code_gen.history.col_file_count'),
    dataType: 'number',
    sortable: true,
    width: 80,
    order: 4,
  },
  {
    key: 'totalSize',
    title: t('develop.code_gen.history.col_total_size'),
    dataType: 'string',
    sortable: true,
    width: 100,
    order: 5,
    render: row => formatSize((row as unknown as CodeGenHistoryListItemDto).totalSize),
  },
  {
    key: 'duration',
    title: t('develop.code_gen.history.col_duration'),
    dataType: 'string',
    sortable: true,
    width: 90,
    order: 6,
    render: row => formatDuration((row as unknown as CodeGenHistoryListItemDto).duration),
  },
  {
    key: 'operatorName',
    title: t('develop.code_gen.history.col_operator'),
    dataType: 'string',
    sortable: true,
    width: 110,
    order: 7,
  },
  {
    key: 'genTime',
    title: t('develop.code_gen.history.col_gen_time'),
    dataType: 'datetime',
    searchable: true,
    searchRange: true,
    sortable: true,
    minWidth: 170,
    order: 8,
  },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'develop.codegen.history',
  pageName: t('develop.code_gen.tabs.history'),
  rowKey: 'basicId',
  scrollX: 1200,
  fields: fields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return codeGenHistoryApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        tableName: (f.tableName as string | undefined)?.trim() || undefined,
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
  },
  actions: [
    { key: 'view', title: t('common.actions.detail'), scope: 'row', type: 'primary', icon: 'lucide:eye' },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as CodeGenHistoryListItemDto | undefined
  switch (payload.key) {
    case 'view':
      if (row) {
        void handleDetail(row)
      }
      break
  }
}

// ── 详情弹窗 ────────────────────────────────────────────────────
const detailVisible = ref(false)
const detailLoading = ref(false)
const detail = ref<CodeGenHistoryDetailDto | null>(null)

const generatedFiles = computed<string[]>(() => {
  if (!detail.value?.generatedFiles) {
    return []
  }
  try {
    const parsed = JSON.parse(detail.value.generatedFiles)
    if (Array.isArray(parsed)) {
      return parsed.map(item => (typeof item === 'string' ? item : (item?.relativePath ?? item?.fileName ?? JSON.stringify(item))))
    }
    return []
  }
  catch {
    return []
  }
})

async function handleDetail(row: CodeGenHistoryListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  detail.value = null
  try {
    detail.value = await codeGenHistoryApi.detail(row.basicId)
    if (!detail.value) {
      message.error(t('develop.code_gen.history.not_found'))
    }
  }
  catch {
    message.error(t('develop.code_gen.history.load_detail_failed'))
  }
  finally {
    detailLoading.value = false
  }
}
</script>

<template>
  <SchemaPage :schema="schema" @action="onAction">
    <NModal
      v-model:show="detailVisible"
      :auto-focus="false"
      :bordered="false"
      preset="card"
      style="width: 820px; max-width: 94vw"
      :title="t('develop.code_gen.history.detail_title')"
    >
      <NSpace v-if="detailLoading" justify="center">
        {{ t('common.statuses.loading') }}
      </NSpace>
      <template v-else-if="detail">
        <NDescriptions :column="2" label-placement="left" size="small">
          <NDescriptionsItem :label="t('develop.code_gen.history.detail_table_name')">
            {{ detail.tableName }}
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('develop.code_gen.history.detail_batch_number')">
            {{ detail.batchNumber ?? '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('develop.code_gen.history.detail_status')">
            <NTag :bordered="false" round size="small" :type="genStatusTagType(detail.genStatus)">
              {{ getOptionLabel(GEN_STATUS_OPTIONS, detail.genStatus) }}
            </NTag>
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('develop.code_gen.history.detail_gen_type')">
            {{ getOptionLabel(GEN_TYPE_OPTIONS, detail.genType) }}
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('develop.code_gen.history.detail_file_count')">
            {{ detail.fileCount }}
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('develop.code_gen.history.detail_total_size')">
            {{ formatSize(detail.totalSize) }}
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('develop.code_gen.history.detail_duration')">
            {{ formatDuration(detail.duration) }}
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('develop.code_gen.history.detail_operator')">
            {{ detail.operatorName ?? '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('develop.code_gen.history.detail_gen_time')">
            {{ detail.genTime }}
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('develop.code_gen.history.detail_operator_ip')">
            {{ detail.operatorIp ?? '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem v-if="detail.genPath" :label="t('develop.code_gen.history.detail_gen_path')" :span="2">
            {{ detail.genPath }}
          </NDescriptionsItem>
          <NDescriptionsItem v-if="detail.downloadPath" :label="t('develop.code_gen.history.detail_download_path')" :span="2">
            {{ detail.downloadPath }}
          </NDescriptionsItem>
        </NDescriptions>

        <div v-if="detail.errorMessage" class="detail-section detail-section--error">
          <div class="detail-section__title">
            {{ t('develop.code_gen.history.detail_error') }}
          </div>
          <pre class="detail-section__pre">{{ detail.errorMessage }}</pre>
        </div>

        <div v-if="generatedFiles.length" class="detail-section">
          <div class="detail-section__title">
            {{ t('develop.code_gen.history.detail_artifacts', { count: generatedFiles.length }) }}
          </div>
          <ul class="detail-files">
            <li v-for="file in generatedFiles" :key="file" class="detail-files__item">
              {{ file }}
            </li>
          </ul>
        </div>
      </template>
      <NSpace v-else justify="center">
        {{ t('common.statuses.no_data') }}
      </NSpace>

      <template #footer>
        <NSpace justify="end">
          <NButton @click="detailVisible = false">
            {{ t('common.actions.close') }}
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </SchemaPage>
</template>

<style scoped>
.detail-section {
  margin-top: 16px;
}

.detail-section__title {
  margin-bottom: 8px;
  font-size: 13px;
  font-weight: 600;
}

.detail-section__pre {
  margin: 0;
  padding: 10px 12px;
  max-height: 200px;
  overflow: auto;
  background: hsl(var(--muted));
  border-radius: 8px;
  font-size: 12px;
  white-space: pre-wrap;
  word-break: break-all;
}

.detail-section--error .detail-section__pre {
  color: hsl(var(--destructive, 0 84% 60%));
}

.detail-files {
  margin: 0;
  padding: 10px 12px 10px 28px;
  max-height: 240px;
  overflow: auto;
  background: hsl(var(--muted));
  border-radius: 8px;
}

.detail-files__item {
  font-size: 12px;
  line-height: 1.8;
  word-break: break-all;
}
</style>
