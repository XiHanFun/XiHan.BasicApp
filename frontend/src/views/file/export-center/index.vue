<script setup lang="ts">
import type { ExportTaskDto, PageResult } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { NProgress, NTag, useDialog, useMessage } from 'naive-ui'
import { h, onBeforeUnmount, onMounted, ref } from 'vue'
import { ExportFormat, ExportScope, exportTaskApi, ExportTaskStatus, fileApi } from '@/api'
import { SchemaPage } from '~/components'
import { downloadBlob, getOptionLabel } from '~/utils'

defineOptions({ name: 'SettingExportCenterPage' })

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

const statusOptions = [
  { label: '待执行', value: ExportTaskStatus.Pending },
  { label: '执行中', value: ExportTaskStatus.Processing },
  { label: '成功', value: ExportTaskStatus.Success },
  { label: '失败', value: ExportTaskStatus.Failed },
]

const scopeOptions = [
  { label: '当前页', value: ExportScope.CurrentPage },
  { label: '查询结果', value: ExportScope.SearchResult },
  { label: '全部', value: ExportScope.All },
]

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

const fields: ListFieldSchema[] = [
  { key: 'taskName', title: '任务名称', dataType: 'string', minWidth: 220, order: 10 },
  { key: 'businessType', title: '业务类型', dataType: 'string', width: 150, order: 11 },
  {
    key: 'status',
    title: '状态',
    dataType: 'enum',
    options: statusOptions,
    width: 100,
    order: 12,
    render: row => h(
      NTag,
      { size: 'small', round: true, bordered: false, type: statusTagType((row as unknown as ExportTaskDto).status) },
      () => getOptionLabel(statusOptions, (row as unknown as ExportTaskDto).status),
    ),
  },
  {
    key: 'progress',
    title: '进度',
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
        return h('span', `${r.totalCount} 行`)
      }
      return h('span', { style: 'opacity:.5' }, '–')
    },
  },
  { key: 'scope', title: '范围', dataType: 'enum', options: scopeOptions, width: 100, order: 14, render: row => getOptionLabel(scopeOptions, (row as unknown as ExportTaskDto).scope) },
  { key: 'format', title: '格式', dataType: 'enum', options: formatOptions, width: 90, order: 15, render: row => getOptionLabel(formatOptions, (row as unknown as ExportTaskDto).format) },
  { key: 'fileSize', title: '文件大小', dataType: 'string', width: 110, order: 16, render: row => formatBytes((row as unknown as ExportTaskDto).fileSize) },
  { key: 'createdTime', title: '创建时间', dataType: 'datetime', minWidth: 170, order: 17 },
  { key: 'finishedTime', title: '完成时间', dataType: 'datetime', minWidth: 170, order: 18 },
  { key: 'errorMessage', title: '错误信息', dataType: 'text', visible: false, order: 30 },
]

const schema: PageSchema = {
  pageCode: 'file.export-center',
  pageName: '导出中心',
  rowKey: 'basicId',
  scrollX: 1400,
  fields,
  resource: {
    page: params => exportTaskApi.mine(params.page, params.pageSize) as unknown as Promise<PageResult<Record<string, unknown>>>,
  },
  actions: [
    { key: 'download', title: '下载', scope: 'row', icon: 'lucide:download', type: 'primary', visible: row => (row as unknown as ExportTaskDto).status === ExportTaskStatus.Success && !!(row as unknown as ExportTaskDto).fileId },
    { key: 'cancel', title: '取消', scope: 'row', icon: 'lucide:circle-x', type: 'warning', visible: row => (row as unknown as ExportTaskDto).status === ExportTaskStatus.Pending },
    { key: 'delete', title: '删除', scope: 'row', icon: 'lucide:trash-2', type: 'error' },
  ],
}

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
    message.warning('产物文件不存在')
    return
  }
  try {
    const blob = await fileApi.download(row.fileId)
    downloadBlob(blob, row.fileName || `${row.taskName}.csv`)
  }
  catch (e) {
    message.error((e as Error).message || '下载失败')
  }
}

async function handleCancel(row: ExportTaskDto) {
  try {
    await exportTaskApi.cancel(row.basicId)
    message.success('已取消')
    reload()
  }
  catch (e) {
    message.error((e as Error).message || '取消失败')
  }
}

function handleDelete(row: ExportTaskDto) {
  dialog.warning({
    title: '删除导出任务',
    content: `确定删除「${row.taskName}」吗？该操作仅删除任务记录。`,
    positiveText: '删除',
    negativeText: '取消',
    onPositiveClick: async () => {
      try {
        await exportTaskApi.remove(row.basicId)
        message.success('已删除')
        reload()
      }
      catch (e) {
        message.error((e as Error).message || '删除失败')
      }
    },
  })
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction" />
</template>
