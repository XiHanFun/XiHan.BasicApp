<script setup lang="ts">
import type { LogDetailField } from '../_components/log-detail.types.ts'
import type { MigrationHistoryDetailDto, MigrationHistoryListItemDto, PageResult } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaQueryParams } from '~/components'
import { XhBadge } from '@xihan-ui/vue'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { createPageRequest, querySortsFromSchema, versionApi } from '@/api'
import { SchemaPage } from '~/components'
import { toast } from '~/composables'
import { migrationHistoryDetailFields } from '../_components/log-detail-fields'
import LogDetailDrawer from '../_components/LogDetailDrawer.vue'

defineOptions({ name: 'LogMigrationPage' })

const { t } = useI18n()

const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<MigrationHistoryDetailDto | null>(null)

const successOptions = computed(() => [
  { label: t('log.migration.success_yes'), value: 1 },
  { label: t('log.migration.success_no'), value: 0 },
])

// ── 字段单一事实源：列 + 常用搜索 + 高级搜索 ─────────────────────
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('common.fields.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('log.migration.keyword_placeholder'), order: 0 },
  { key: 'version', title: t('log.migration.version'), dataType: 'string', searchable: true, sortable: true, searchPlaceholder: t('log.migration.version_placeholder'), minWidth: 120, order: 10 },
  { key: 'scriptName', title: t('log.migration.script_name'), dataType: 'string', searchable: true, sortable: true, searchPlaceholder: t('log.migration.script_name_placeholder'), minWidth: 200, order: 11 },
  {
    key: 'success',
    title: t('log.migration.success'),
    dataType: 'boolean',
    searchable: true,
    sortable: true,
    options: successOptions.value,
    width: 110,
    order: 12,
    render: row => h(XhBadge, { variant: 'subtle', size: 'sm', tone: (row as unknown as MigrationHistoryListItemDto).success ? 'success' : 'danger' }, () => (row as unknown as MigrationHistoryListItemDto).success ? t('log.migration.success_yes') : t('log.migration.success_no')),
  },
  { key: 'executedTime', title: t('log.migration.executed_time'), dataType: 'datetime', sortable: true, searchRange: true, advancedSearch: true, minWidth: 170, order: 13 },
  { key: 'nodeName', title: t('log.migration.node_name'), dataType: 'string', advancedSearch: true, searchPlaceholder: t('log.migration.node_name_placeholder'), minWidth: 150, order: 14 },
  { key: 'createdTime', title: t('common.fields.created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 15 },
])

function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}
function toBool(v: unknown): boolean | undefined {
  return v == null || v === '' ? undefined : v === 1 || v === true
}

function buildMigrationQuery(params: SchemaQueryParams) {
  const f = params.filters
  return {
    ...createPageRequest({
      page: { pageIndex: params.page, pageSize: params.pageSize },
      conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
    }),
    keyword: toStr(f.keyword),
    version: toStr(f.version),
    scriptName: toStr(f.scriptName),
    nodeName: toStr(f.nodeName),
    success: toBool(f.success),
  }
}

const schema = computed<PageSchema>(() => ({
  pageCode: 'log.migration',
  pageName: t('log.migration.page_name'),
  rowKey: 'basicId',
  fields: fields.value,
  resource: {
    page: params => versionApi.migrationHistoryPage(buildMigrationQuery(params)) as unknown as Promise<PageResult<Record<string, unknown>>>,
  },
  actions: [
    { key: 'view', title: t('common.actions.view_detail'), scope: 'row', icon: 'lucide:eye' },
  ],
}))

const detailFields = computed<LogDetailField[]>(() => migrationHistoryDetailFields(t))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as MigrationHistoryListItemDto | undefined
  if (payload.key === 'view' && row) {
    void handleDetail(row)
  }
}

async function handleDetail(row: MigrationHistoryListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  try {
    detailData.value = await versionApi.migrationHistoryDetail(row.basicId) ?? row
  }
  catch (error) {
    detailData.value = row
    toast.error((error as Error)?.message || t('log.migration.detail_load_failed'))
  }
  finally {
    detailLoading.value = false
  }
}
</script>

<template>
  <SchemaPage :schema="schema" @action="onAction">
    <LogDetailDrawer
      v-model:show="detailVisible"
      :fields="detailFields"
      :loading="detailLoading"
      :record="detailData"
      :title="t('log.migration.detail_title')"
    />
  </SchemaPage>
</template>
