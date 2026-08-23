<script setup lang="ts">
import type { ExportTaskDto, PageResult } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { NProgress, NTag, useDialog, useMessage } from 'naive-ui'
import { computed, h, onBeforeUnmount, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ExportFormat, ExportScope, exportTaskApi, ExportTaskStatus, fileApi } from '@/api'
import { SchemaPage } from '~/components'
import { downloadBlob, getOptionLabel } from '~/utils'

defineOptions({ name: 'SettingExportCenterPage' })

const { t } = useI18n()
const message = useMessage()
const dialog = useDialog()

const schemaPageRef = ref<InstanceType<typeof SchemaPage> | null>(null)

// 监视页：每 5s 自动刷新（仅页面可见时），让进度/终态实时呈现；离开页面即停止
const REFRESH_MS = 5_000
let refreshTimer: ReturnType<typeof setInterval> | null = null

function reload() {
  void schemaPageRef.value?.reload()
}

onMounted(() => {
  refreshTimer = setInterval(() => {
    if (document.visibilityState === 'visible') {
      reload()
    }
  }, REFRESH_MS)
})

onBeforeUnmount(() => {
  if (refreshTimer) {
    clearInterval(refreshTimer)
    refreshTimer = null
  }
})

const statusOptions = computed(() => [
  { label: t('file.export_center.status.pending'), value: ExportTaskStatus.Pending },
  { label: t('file.export_center.status.processing'), value: ExportTaskStatus.Processing },
  { label: t('file.export_center.status.success'), value: ExportTaskStatus.Success },
  { label: t('file.export_center.status.failed'), value: ExportTaskStatus.Failed },
])

const scopeOptions = computed(() => [
  { label: t('file.export_center.scope.current_page'), value: ExportScope.CurrentPage },
  { label: t('file.export_center.scope.search_result'), value: ExportScope.SearchResult },
  { label: t('file.export_center.scope.all'), value: ExportScope.All },
])

const formatOptions = [
  { label: 'CSV', value: ExportFormat.Csv },
  { label: 'Excel', value: ExportFormat.Xlsx },
]

function statusTagType(status: ExportTaskStatus): 'default' | 'error' | 'info' | 'success' {
  switch (status) {
    case ExportTaskStatus.Success: return 'success'
    case ExportTaskStatus.Failed: return 'error'
    case ExportTaskStatus.Processing: return 'info'
    default: return 'default'
  }
}

function formatBytes(bytes: number): string {
  if (!bytes || bytes <= 0) {
    return '–'
  }
  const units = ['B', 'KB', 'MB', 'GB']
  let value = bytes
  let unitIndex = 0
  while (value >= 1024 && unitIndex < units.length - 1) {
    value /= 1024
    unitIndex++
  }
  return `${value.toFixed(unitIndex === 0 ? 0 : 1)} ${units[unitIndex]}`
}

const fields = computed<ListFieldSchema[]>(() => [
  { key: 'taskName', title: t('file.export_center.columns.task_name'), dataType: 'string', minWidth: 220, order: 10 },
  { key: 'businessType', title: t('file.export_center.columns.business_type'), dataType: 'string', width: 150, order: 11 },
  {
    key: 'status',
    title: t('file.export_center.columns.status'),
    dataType: 'enum',
    options: statusOptions.value,
    width: 100,
    order: 12,
    render: row => h(
      NTag,
      { size: 'small', round: true, bordered: false, type: statusTagType((row as unknown as ExportTaskDto).status) },
      () => getOptionLabel(statusOptions.value, (row as unknown as ExportTaskDto).status),
    ),
  },
  {
    key: 'progress',
    title: t('file.export_center.columns.progress'),
    dataType: 'number',
    width: 140,
    order: 13,
    render: (row) => {
      const r = row as unknown as ExportTaskDto
      if (r.status === ExportTaskStatus.Processing) {
        return h(NProgress, {
          type: 'line',
          percentage: r.progress,
          height: 8,
          processing: true,
          indicatorPlacement: 'inside',
        })
      }
      if (r.status === ExportTaskStatus.Success) {
        return h('span', t('file.export_center.rows', { count: r.totalCount }))
      }
      return h('span', { style: 'opacity:.5' }, '–')
    },
  },
  { key: 'scope', title: t('file.export_center.columns.scope'), dataType: 'enum', options: scopeOptions.value, width: 100, order: 14, render: row => getOptionLabel(scopeOptions.value, (row as unknown as ExportTaskDto).scope) },
  { key: 'format', title: t('file.export_center.columns.format'), dataType: 'enum', options: formatOptions, width: 90, order: 15, render: row => getOptionLabel(formatOptions, (row as unknown as ExportTaskDto).format) },
  { key: 'fileSize', title: t('file.export_center.columns.file_size'), dataType: 'string', width: 110, order: 16, render: row => formatBytes((row as unknown as ExportTaskDto).fileSize) },
  { key: 'createdTime', title: t('file.export_center.columns.created_time'), dataType: 'datetime', minWidth: 170, order: 17 },
  { key: 'finishedTime', title: t('file.export_center.columns.finished_time'), dataType: 'datetime', minWidth: 170, order: 18 },
  { key: 'errorMessage', title: t('file.export_center.columns.error_message'), dataType: 'text', visible: false, order: 30 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'file.export-center',
  pageName: t('file.export_center.page_name'),
  rowKey: 'basicId',
  scrollX: 1400,
  fields: fields.value,
  resource: {
    page: params => exportTaskApi.mine(params.page, params.pageSize) as unknown as Promise<PageResult<Record<string, unknown>>>,
  },
  actions: [
    { key: 'download', title: t('file.export_center.actions.download'), scope: 'row', icon: 'lucide:download', type: 'primary', visible: row => (row as unknown as ExportTaskDto).status === ExportTaskStatus.Success && !!(row as unknown as ExportTaskDto).fileId },
    { key: 'cancel', title: t('file.export_center.actions.cancel'), scope: 'row', icon: 'lucide:circle-x', type: 'warning', visible: row => (row as unknown as ExportTaskDto).status === ExportTaskStatus.Pending },
    { key: 'delete', title: t('file.export_center.actions.delete'), scope: 'row', icon: 'lucide:trash-2', type: 'error' },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as ExportTaskDto | undefined
  if (!row) {
    return
  }
  if (payload.key === 'download') {
    void handleDownload(row)
  }
  else if (payload.key === 'cancel') {
    void handleCancel(row)
  }
  else if (payload.key === 'delete') {
    handleDelete(row)
  }
}

async function handleDownload(row: ExportTaskDto) {
  if (!row.fileId) {
    message.warning(t('file.export_center.product_not_exist'))
    return
  }
  try {
    const blob = await fileApi.download(row.fileId)
    downloadBlob(blob, row.fileName || `${row.taskName}.csv`)
  }
  catch (e) {
    message.error((e as Error).message || t('file.export_center.download_failed'))
  }
}

async function handleCancel(row: ExportTaskDto) {
  try {
    await exportTaskApi.cancel(row.basicId)
    message.success(t('file.export_center.canceled'))
    reload()
  }
  catch (e) {
    message.error((e as Error).message || t('file.export_center.cancel_failed'))
  }
}

function handleDelete(row: ExportTaskDto) {
  dialog.warning({
    title: t('file.export_center.delete_title'),
    content: t('file.export_center.delete_content', { name: row.taskName }),
    positiveText: t('file.export_center.actions.delete'),
    negativeText: t('file.export_center.actions.cancel'),
    onPositiveClick: async () => {
      try {
        await exportTaskApi.remove(row.basicId)
        message.success(t('file.export_center.deleted'))
        reload()
      }
      catch (e) {
        message.error((e as Error).message || t('file.export_center.delete_failed'))
      }
    },
  })
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction" />
</template>
