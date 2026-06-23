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
  { label: t('file.exportCenter.status.pending'), value: ExportTaskStatus.Pending },
  { label: t('file.exportCenter.status.processing'), value: ExportTaskStatus.Processing },
  { label: t('file.exportCenter.status.success'), value: ExportTaskStatus.Success },
  { label: t('file.exportCenter.status.failed'), value: ExportTaskStatus.Failed },
])

const scopeOptions = computed(() => [
  { label: t('file.exportCenter.scope.currentPage'), value: ExportScope.CurrentPage },
  { label: t('file.exportCenter.scope.searchResult'), value: ExportScope.SearchResult },
  { label: t('file.exportCenter.scope.all'), value: ExportScope.All },
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
  { key: 'taskName', title: t('file.exportCenter.columns.taskName'), dataType: 'string', minWidth: 220, order: 10 },
  { key: 'businessType', title: t('file.exportCenter.columns.businessType'), dataType: 'string', width: 150, order: 11 },
  {
    key: 'status',
    title: t('file.exportCenter.columns.status'),
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
    title: t('file.exportCenter.columns.progress'),
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
        return h('span', t('file.exportCenter.rows', { count: r.totalCount }))
      }
      return h('span', { style: 'opacity:.5' }, '–')
    },
  },
  { key: 'scope', title: t('file.exportCenter.columns.scope'), dataType: 'enum', options: scopeOptions.value, width: 100, order: 14, render: row => getOptionLabel(scopeOptions.value, (row as unknown as ExportTaskDto).scope) },
  { key: 'format', title: t('file.exportCenter.columns.format'), dataType: 'enum', options: formatOptions, width: 90, order: 15, render: row => getOptionLabel(formatOptions, (row as unknown as ExportTaskDto).format) },
  { key: 'fileSize', title: t('file.exportCenter.columns.fileSize'), dataType: 'string', width: 110, order: 16, render: row => formatBytes((row as unknown as ExportTaskDto).fileSize) },
  { key: 'createdTime', title: t('file.exportCenter.columns.createdTime'), dataType: 'datetime', minWidth: 170, order: 17 },
  { key: 'finishedTime', title: t('file.exportCenter.columns.finishedTime'), dataType: 'datetime', minWidth: 170, order: 18 },
  { key: 'errorMessage', title: t('file.exportCenter.columns.errorMessage'), dataType: 'text', visible: false, order: 30 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'file.export-center',
  pageName: t('file.exportCenter.pageName'),
  rowKey: 'basicId',
  scrollX: 1400,
  fields: fields.value,
  resource: {
    page: params => exportTaskApi.mine(params.page, params.pageSize) as unknown as Promise<PageResult<Record<string, unknown>>>,
  },
  actions: [
    { key: 'download', title: t('file.exportCenter.actions.download'), scope: 'row', icon: 'lucide:download', type: 'primary', visible: row => (row as unknown as ExportTaskDto).status === ExportTaskStatus.Success && !!(row as unknown as ExportTaskDto).fileId },
    { key: 'cancel', title: t('file.exportCenter.actions.cancel'), scope: 'row', icon: 'lucide:circle-x', type: 'warning', visible: row => (row as unknown as ExportTaskDto).status === ExportTaskStatus.Pending },
    { key: 'delete', title: t('file.exportCenter.actions.delete'), scope: 'row', icon: 'lucide:trash-2', type: 'error' },
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
    message.warning(t('file.exportCenter.productNotExist'))
    return
  }
  try {
    const blob = await fileApi.download(row.fileId)
    downloadBlob(blob, row.fileName || `${row.taskName}.csv`)
  }
  catch (e) {
    message.error((e as Error).message || t('file.exportCenter.downloadFailed'))
  }
}

async function handleCancel(row: ExportTaskDto) {
  try {
    await exportTaskApi.cancel(row.basicId)
    message.success(t('file.exportCenter.canceled'))
    reload()
  }
  catch (e) {
    message.error((e as Error).message || t('file.exportCenter.cancelFailed'))
  }
}

function handleDelete(row: ExportTaskDto) {
  dialog.warning({
    title: t('file.exportCenter.deleteTitle'),
    content: t('file.exportCenter.deleteContent', { name: row.taskName }),
    positiveText: t('file.exportCenter.actions.delete'),
    negativeText: t('file.exportCenter.actions.cancel'),
    onPositiveClick: async () => {
      try {
        await exportTaskApi.remove(row.basicId)
        message.success(t('file.exportCenter.deleted'))
        reload()
      }
      catch (e) {
        message.error((e as Error).message || t('file.exportCenter.deleteFailed'))
      }
    },
  })
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction" />
</template>
