<script lang="ts" setup>
import type { ExportTaskRow } from './export-center'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance } from '~/components'
import type { SelectOption } from '~/types'
import { XhProgress } from '@xihan-ui/vue'
import { computed, h, onActivated, onBeforeUnmount, onDeactivated, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { actionConfirmText, renderTag, SchemaPage } from '~/components'
import { confirmDanger, toast } from '~/composables'
import { exportTasks as exportTasksApi } from '~/contracts'
import { formatFileSize } from '~/utils'
import { downloadFile } from '../../api'
import {
  businessTypeLabel,
  canCancel,
  canDelete,
  canDownload,
  createAutoRefresh,
  errorMessageText,
  EXPORT_FORMATS,
  EXPORT_TASK_STATUSES,
  exportTaskResource,
  formatLabel,
  KNOWN_BUSINESS_TYPES,
  progressPercent,
  scopeLabel,
  statusLabel,
  statusTone,
} from './export-center'

defineOptions({ name: 'FilesExportCenterView' })

const { t } = useI18n()
const resource = exportTaskResource(t)

const schemaPageRef = ref<SchemaPageInstance | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

// 监视页：每 5 秒刷新一次（标签页可见时），让进度与终态及时呈现；切到别的标签或离开页面即停
const autoRefresh = createAutoRefresh(reload, () => document.visibilityState === 'visible')
let deactivated = false
onMounted(autoRefresh.start)
onActivated(() => {
  autoRefresh.start()
  // 页面缓存在标签里：切回来先刷一次，不等下一个间隔
  if (deactivated) {
    deactivated = false
    reload()
  }
})
onDeactivated(() => {
  deactivated = true
  autoRefresh.stop()
})
onBeforeUnmount(autoRefresh.stop)

const asRow = (row: object) => row as ExportTaskRow
const statusOptions = computed<SelectOption[]>(() => EXPORT_TASK_STATUSES.map(status => ({ label: statusLabel(t, status), value: status })))
const formatOptions = EXPORT_FORMATS.map(format => ({ label: formatLabel(format), value: format }))
const businessTypeOptions = computed<SelectOption[]>(() => KNOWN_BUSINESS_TYPES.map(businessType => ({ label: businessTypeLabel(t, businessType), value: businessType })))

/** 进度列：执行中画进度条（进行中铺流动条纹，减弱动效下静止）并在旁边写百分比；成功写导出行数；其余为空 */
function renderProgress(row: ExportTaskRow) {
  if (row.status === 'Processing') {
    const percent = progressPercent(row)
    return h('div', { class: 'flex items-center gap-2' }, [
      // 可及名写在根上，读屏才知道这是哪一项的进度
      h(XhProgress, { 'class': 'min-w-0 flex-1', 'value': percent, 'size': 'sm', 'striped': true, 'aria-label': t('files.export_center.fields.progress') }),
      h('span', { class: 'tabular-nums' }, `${percent}%`),
    ])
  }
  if (row.status === 'Success')
    return t('files.export_center.rows', { count: Number(row.totalCount) })
  return '-'
}

// ── 字段单一事实源（列 + 搜索；仅搜索字段 visible:false；可排序、可筛选的只放后端白名单里的） ──
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('files.export_center.fields.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('files.export_center.placeholders.keyword'), width: 220, order: 0 },
  { key: 'taskName', title: t('files.export_center.fields.task_name'), dataType: 'string', sortable: true, minWidth: 220, order: 1 },
  // 未登记的业务类型照原样显示列表操作 ID，而不是空
  { key: 'businessType', title: t('files.export_center.fields.business_type'), dataType: 'enum', searchable: true, options: businessTypeOptions.value, searchPlaceholder: t('files.export_center.placeholders.business_type'), minWidth: 140, order: 2, render: row => businessTypeLabel(t, asRow(row).businessType) },
  { key: 'status', title: t('files.export_center.fields.status'), dataType: 'enum', sortable: true, searchable: true, searchMultiple: true, options: statusOptions.value, searchPlaceholder: t('files.export_center.placeholders.status'), width: 100, order: 3, render: row => renderTag(statusLabel(t, asRow(row).status), statusTone(asRow(row).status)) },
  { key: 'progress', title: t('files.export_center.fields.progress'), dataType: 'number', width: 160, order: 4, render: row => renderProgress(asRow(row)) },
  { key: 'scope', title: t('files.export_center.fields.scope'), dataType: 'string', width: 100, order: 5, render: row => scopeLabel(t, asRow(row).scope) },
  { key: 'format', title: t('files.export_center.fields.format'), dataType: 'enum', searchable: true, searchMultiple: true, options: formatOptions, searchPlaceholder: t('files.export_center.placeholders.format'), width: 90, order: 6, render: row => formatLabel(asRow(row).format) },
  { key: 'fileSize', title: t('files.export_center.fields.file_size'), dataType: 'number', width: 110, order: 7, render: row => (asRow(row).fileSize == null ? '-' : formatFileSize(Number(asRow(row).fileSize))) },
  { key: 'createdAt', title: t('files.export_center.fields.created_at'), dataType: 'datetime', sortable: true, minWidth: 170, order: 8 },
  { key: 'finishedAt', title: t('files.export_center.fields.finished_at'), dataType: 'datetime', minWidth: 170, order: 9 },
  { key: 'errorMessage', title: t('files.export_center.fields.error_message'), dataType: 'string', ellipsis: true, minWidth: 200, order: 10, render: row => errorMessageText(t, asRow(row).errorMessage) || '-' },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'files.export-center',
  pageName: t('files.export_center.title'),
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'download', title: t('files.export_center.actions.download'), scope: 'row', icon: 'lucide:download', tone: 'brand', visible: row => canDownload(asRow(row)) },
    { key: 'cancel', title: t('files.export_center.actions.cancel'), scope: 'row', icon: 'lucide:circle-x', tone: 'warning', confirm: true, confirmText: row => actionConfirmText(t, t('files.export_center.actions.cancel'), asRow(row).taskName), visible: row => canCancel(asRow(row)) },
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', icon: 'lucide:trash-2', tone: 'danger', visible: row => canDelete(asRow(row)) },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row ? asRow(payload.row) : undefined
  if (!row)
    return
  switch (payload.key) {
    case 'download':
      void download(row)
      break
    case 'cancel':
      void cancel(row)
      break
    case 'delete':
      confirmDelete(row)
      break
  }
}

/** 结果文件经签名地址让浏览器自己下载（带结果文件名） */
async function download(row: ExportTaskRow) {
  if (!row.fileId)
    return
  try {
    await downloadFile(row.fileId)
  }
  catch (error) {
    toast.danger((error as Error).message || t('files.export_center.messages.download_failed'))
  }
}

async function cancel(row: ExportTaskRow) {
  try {
    await exportTasksApi.cancel({ id: row.id })
    toast.success(t('files.export_center.messages.cancelled'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('files.export_center.messages.cancel_failed'))
  }
}

/** 删除任务连同结果文件，不可恢复，强二次确认 */
function confirmDelete(row: ExportTaskRow) {
  void confirmDanger({
    title: t('files.export_center.messages.delete_title'),
    content: t('files.export_center.messages.delete_content', { name: row.taskName }),
    okText: t('common.actions.delete'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        await resource.remove?.(row.id)
        toast.success(t('files.export_center.messages.deleted'))
        reload()
      }
      catch (error) {
        toast.danger((error as Error).message || t('common.messages.delete_failed'))
      }
    },
  })
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction" />
</template>
