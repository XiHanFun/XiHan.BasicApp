<script lang="ts" setup>
import type { FileUploadRequest } from '@xihan-ui/vue'
import type { FileDetailsForm, FileRow, FileStorageRow, PreviewKind } from './file-library'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance, XDataTableColumn } from '~/components'
import type { SelectOption } from '~/types'
import {
  XhButton,
  XhButtonIndicator,
  XhButtonLabel,
  XhCodeViewCode,
  XhCodeViewPre,
  XhCodeViewRoot,
  XhDescriptionsItem,
  XhDescriptionsLabel,
  XhDescriptionsRoot,
  XhDescriptionsValue,
  XhDialogBody,
  XhDialogCloseTrigger,
  XhDialogContent,
  XhDialogHeader,
  XhDialogRoot,
  XhDialogTitle,
  XhDrawerBody,
  XhDrawerCloseTrigger,
  XhDrawerContent,
  XhDrawerFooter,
  XhDrawerHeader,
  XhDrawerRoot,
  XhDrawerTitle,
  XhFieldControl,
  XhFieldLabel,
  XhFieldRoot,
  XhFileUploadDropzone,
  XhFileUploadHiddenInput,
  XhFileUploadRoot,
  XhSwitch,
} from '@xihan-ui/vue'
import { computed, h, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { actionConfirmText, Icon, renderTag, SchemaPage, XDataTable, XDatePicker, XIconButton, XInput, XSelect } from '~/components'
import { confirmDanger, dialog, islandStart, toast } from '~/composables'
import { files as filesApi } from '~/contracts'
import { usePermission } from '~/hooks'
import { formatDate, formatFileSize } from '~/utils'
import { downloadFile, fetchFileBlob, fileLink, uploadFile } from '../../api'
import { storageTypeLabel } from '../storage-configs/storage-config'
import {
  accessLevelLabel,
  canPreview,
  canSetPrimary,
  defaultExpiry,
  emptyDetailsForm,
  FILE_ACCESS_LEVELS,
  FILE_KINDS,
  FILE_SOURCES,
  FILE_STATUSES,
  fileResource,
  fileStatusLabel,
  fileStatusTone,
  isTextPreview,
  kindIcon,
  kindLabel,
  PREVIEW_TEXT_MAX_BYTES,
  previewKindOf,
  sourceLabel,
  storageReasonLabel,
  storageStatusLabel,
  storageStatusTone,
  storageToggleTarget,
  textLangOf,
  throwOnFailed,
  toCsvTable,
  toDetailsForm,
  toFileDetails,
} from './file-library'

defineOptions({ name: 'FilesLibraryView' })

const { t } = useI18n()
const { hasPermission } = usePermission()
const resource = fileResource(t)

const schemaPageRef = ref<SchemaPageInstance | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

const asRow = (row: object) => row as FileRow
const displayName = (file: FileRow) => file.originalName || file.name
const kindOptions = computed<SelectOption[]>(() => FILE_KINDS.map(kind => ({ label: kindLabel(t, kind), value: kind })))
const statusOptions = computed<SelectOption[]>(() => FILE_STATUSES.map(status => ({ label: fileStatusLabel(t, status), value: status })))
const accessLevelOptions = computed<SelectOption[]>(() => FILE_ACCESS_LEVELS.map(level => ({ label: accessLevelLabel(t, level), value: level })))
const sourceOptions = computed<SelectOption[]>(() => FILE_SOURCES.map(source => ({ label: sourceLabel(t, source), value: source })))
const yesNoOptions = computed<SelectOption[]>(() => [
  { label: t('common.statuses.yes'), value: 'true' },
  { label: t('common.statuses.no'), value: 'false' },
])

function yesNo(value: boolean): string {
  return value ? t('common.statuses.yes') : t('common.statuses.no')
}

// ── 字段单一事实源（列 + 搜索；仅搜索字段 visible:false；可排序、可筛选的只放后端白名单里的） ──
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('files.library.fields.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('files.library.placeholders.keyword'), width: 220, order: 0 },
  { key: 'originalName', title: t('files.library.fields.original_name'), dataType: 'string', sortable: true, minWidth: 220, fixed: 'left', order: 1 },
  { key: 'name', title: t('files.library.fields.name'), dataType: 'string', minWidth: 220, order: 2 },
  { key: 'kindIcon', title: t('files.library.fields.kind_icon'), dataType: 'string', width: 64, order: 2.5, render: row => h('span', { class: 'flex items-center justify-center' }, h(Icon, { icon: kindIcon(asRow(row).kind), width: 16 })) },
  { key: 'kind', title: t('files.library.fields.kind'), dataType: 'enum', searchable: true, searchMultiple: true, sortable: true, options: kindOptions.value, searchPlaceholder: t('files.library.placeholders.kind'), width: 110, order: 3, render: row => renderTag(kindLabel(t, asRow(row).kind), 'neutral') },
  { key: 'size', title: t('files.library.fields.size'), dataType: 'number', sortable: true, minWidth: 110, order: 4, render: row => formatFileSize(Number(asRow(row).size)) },
  { key: 'status', title: t('files.library.fields.status'), dataType: 'enum', searchable: true, searchMultiple: true, options: statusOptions.value, searchPlaceholder: t('files.library.placeholders.status'), width: 110, order: 5, render: row => renderTag(fileStatusLabel(t, asRow(row).status), fileStatusTone(asRow(row).status)) },
  { key: 'accessLevel', title: t('files.library.fields.access_level'), dataType: 'enum', searchable: true, searchMultiple: true, options: accessLevelOptions.value, searchPlaceholder: t('files.library.placeholders.access_level'), minWidth: 110, order: 6, render: row => accessLevelLabel(t, asRow(row).accessLevel) },
  { key: 'isTemporary', title: t('files.library.fields.is_temporary'), dataType: 'boolean', searchable: true, options: yesNoOptions.value, searchPlaceholder: t('files.library.placeholders.is_temporary'), width: 82, order: 7 },
  { key: 'extension', title: t('files.library.fields.extension'), dataType: 'string', advancedSearch: true, searchPlaceholder: t('files.library.placeholders.extension'), minWidth: 90, order: 9 },
  { key: 'contentType', title: t('files.library.fields.content_type'), dataType: 'string', advancedSearch: true, searchPlaceholder: t('files.library.placeholders.content_type'), minWidth: 160, order: 10 },
  { key: 'source', title: t('files.library.fields.source'), dataType: 'enum', visible: false, advancedSearch: true, options: sourceOptions.value, searchPlaceholder: t('files.library.placeholders.source'), order: 10.5 },
  { key: 'downloadCount', title: t('files.library.fields.download_count'), dataType: 'number', sortable: true, minWidth: 90, order: 11 },
  { key: 'expiresAt', title: t('files.library.fields.expires_at'), dataType: 'datetime', visible: false, advancedSearch: true, searchRange: true, order: 12 },
  { key: 'createdAt', title: t('files.library.fields.created_at'), dataType: 'datetime', sortable: true, searchable: true, searchRange: true, minWidth: 170, order: 13 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'files.library',
  pageName: t('files.library.title'),
  batchRemovable: true,
  removePermission: 'files.files.delete',
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'upload', title: t('files.library.actions.upload'), scope: 'page', tone: 'brand', icon: 'lucide:upload', permission: 'files.files.create' },
    { key: 'preview', title: t('files.library.actions.preview'), scope: 'row', icon: 'lucide:scan-eye', visible: row => canPreview(asRow(row)) },
    { key: 'download', title: t('files.library.actions.download'), scope: 'row', icon: 'lucide:download', visible: row => asRow(row).status === 'Normal' },
    { key: 'view', title: t('files.library.actions.view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'metadata', title: t('files.library.actions.metadata'), scope: 'row', icon: 'lucide:tags', permission: 'files.files.update' },
    { key: 'storages', title: t('files.library.actions.storages'), scope: 'row', icon: 'lucide:hard-drive' },
    // 正常的可归档，归档的可恢复（损坏的要经副本修复，不在这里恢复），任意状态可彻底删除
    { key: 'archive', title: t('files.library.actions.archive'), scope: 'row', icon: 'lucide:archive', tone: 'warning', confirm: true, confirmText: row => actionConfirmText(t, t('files.library.actions.archive'), displayName(asRow(row))), visible: row => asRow(row).status === 'Normal', permission: 'files.files.update' },
    { key: 'restore', title: t('files.library.actions.restore'), scope: 'row', icon: 'lucide:archive-restore', visible: row => asRow(row).status === 'Archived', permission: 'files.files.update' },
    { key: 'delete', title: t('files.library.actions.delete'), scope: 'row', icon: 'lucide:trash-2', tone: 'danger', permission: 'files.files.delete' },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row ? asRow(payload.row) : undefined
  switch (payload.key) {
    case 'upload':
      openUpload()
      break
    case 'preview':
      if (row)
        void openPreview(row)
      break
    case 'download':
      if (row)
        void download(row)
      break
    case 'view':
      if (row)
        void openFileDetail(row)
      break
    case 'metadata':
      if (row)
        openMetadata(row)
      break
    case 'storages':
      if (row)
        void openStorages(row)
      break
    case 'archive':
      if (row)
        void setFileStatus(row, 'Archived', t('files.library.messages.archived'))
      break
    case 'restore':
      if (row)
        void setFileStatus(row, 'Normal', t('files.library.messages.restored'))
      break
    case 'delete':
      if (row)
        confirmDelete(row)
      break
  }
}

/** 打开「临时」时没填到期时间即按缺省保留天数补上 */
function changeTemporary(target: FileDetailsForm, value: boolean) {
  target.isTemporary = value
  if (value && target.expiresAt == null)
    target.expiresAt = defaultExpiry()
}

// ── 上传 ────────────────────────────────────────────────────────
const uploadVisible = ref(false)
const uploadForm = reactive<FileDetailsForm>(emptyDetailsForm())

function openUpload() {
  // 每次打开重置为缺省，避免上次的开关与备注残留
  Object.assign(uploadForm, emptyDetailsForm())
  uploadVisible.value = true
}

async function handleUploadRequest(request: FileUploadRequest) {
  const file = request.file
  const details = toFileDetails(uploadForm)
  // 上传作为持续任务进灵动岛（关掉灵动岛时降级为轻提示）；每个文件一条，文件名放副文本，免得长文件名把胶囊撑宽
  const task = islandStart(`files:upload:${file.name}:${file.size}`, t('files.library.upload.uploading'), { detail: file.name, icon: 'lucide:upload', progress: 0 })
  // 弹窗即时关闭，进度交给灵动岛，不挡用户继续操作；所以不把上传器的中止信号接到请求上（弹窗卸载会触发它）
  uploadVisible.value = false
  try {
    await uploadFile(file, {
      ...details,
      onProgress: (percent) => {
        task.setProgress(percent)
        request.onProgress(percent)
      },
    })
    request.onProgress(100)
    task.success(t('files.library.upload.upload_success'), { detail: file.name })
    reload()
  }
  catch (error) {
    task.error(t('files.library.upload.upload_failed'), { detail: `${file.name}: ${(error as Error).message}` })
    // 抛回去让上传器把这一条记成失败态
    throw error
  }
}

// ── 元数据 ──────────────────────────────────────────────────────
const metadataVisible = ref(false)
const metadataSaving = ref(false)
const metadataFile = ref<FileRow | null>(null)
const metadataForm = reactive<FileDetailsForm>(emptyDetailsForm())

function openMetadata(row: FileRow) {
  metadataFile.value = row
  Object.assign(metadataForm, toDetailsForm(row))
  metadataVisible.value = true
}

async function saveMetadata() {
  const file = metadataFile.value
  if (!file)
    return
  metadataSaving.value = true
  try {
    await filesApi.update({ id: file.id, ...toFileDetails(metadataForm) })
    toast.success(t('files.library.metadata.updated'))
    metadataVisible.value = false
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('files.library.metadata.update_failed'))
  }
  finally {
    metadataSaving.value = false
  }
}

// ── 归档 / 恢复 / 彻底删除 / 下载 ────────────────────────────────
async function setFileStatus(row: FileRow, status: 'Archived' | 'Normal', successText: string) {
  try {
    throwOnFailed(t, await filesApi.setStatus({ ids: [row.id], status }))
    toast.success(successText)
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.operation_failed'))
  }
}

/** 彻底删除：记录与副本一并删掉，内容不再被别的文件用到时随之从存储里删除，不可恢复，强二次确认 */
function confirmDelete(row: FileRow) {
  void confirmDanger({
    title: t('files.library.messages.delete_title'),
    content: t('files.library.messages.delete_content', { name: displayName(row) }),
    okText: t('files.library.actions.delete'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        await resource.remove?.(row.id)
        toast.success(t('files.library.messages.deleted'))
        reload()
      }
      catch (error) {
        toast.danger((error as Error).message || t('common.messages.delete_failed'))
      }
    },
  })
}

/** 经签名地址让浏览器自己下载（带原始文件名） */
async function download(row: FileRow) {
  try {
    await downloadFile(row.id)
  }
  catch (error) {
    toast.danger((error as Error).message || t('files.library.messages.download_failed'))
  }
}

// ── 详情（文件与副本共用一个抽屉） ──────────────────────────────
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailKind = ref<'file' | 'storage'>('file')
const fileDetail = ref<FileRow | null>(null)
const storageDetail = ref<FileStorageRow | null>(null)
const detailTitle = computed(() => (detailKind.value === 'file' ? t('files.library.detail.title') : t('files.library.detail.storage_title')))

async function openFileDetail(row: FileRow) {
  detailKind.value = 'file'
  storageDetail.value = null
  detailVisible.value = true
  detailLoading.value = true
  try {
    fileDetail.value = await filesApi.get({ id: row.id })
  }
  catch (error) {
    fileDetail.value = null
    toast.danger((error as Error).message || t('files.library.messages.detail_failed'))
  }
  finally {
    detailLoading.value = false
  }
}

/** 副本详情：副本列表已带全部字段，直接展示 */
function openStorageDetail(storage: FileStorageRow) {
  detailKind.value = 'storage'
  fileDetail.value = null
  storageDetail.value = storage
  detailLoading.value = false
  detailVisible.value = true
}

// ── 存储副本 ────────────────────────────────────────────────────
const storagesVisible = ref(false)
const storagesLoading = ref(false)
const storageRows = ref<FileStorageRow[]>([])
const storageFile = ref<FileRow | null>(null)
/** 副本的写操作（设主副本、校验、启停）都是 files.files.update */
const canUpdateFile = computed(() => hasPermission('files.files.update'))

async function openStorages(row: FileRow) {
  storageFile.value = row
  storageRows.value = []
  storagesVisible.value = true
  await loadStorages()
}

async function loadStorages() {
  const file = storageFile.value
  if (!file)
    return
  storagesLoading.value = true
  try {
    storageRows.value = [...(await filesApi.storages({ id: file.id })).items]
  }
  catch (error) {
    storageRows.value = []
    toast.danger((error as Error).message || t('files.library.storage_list.load_failed'))
  }
  finally {
    storagesLoading.value = false
  }
}

/** 副本操作会改文件状态（损坏的文件换了主副本即恢复、主副本校验不过即损坏），列表一并刷新 */
async function afterStorageChange() {
  await loadStorages()
  reload()
}

async function setPrimary(storage: FileStorageRow) {
  const file = storageFile.value
  if (!file)
    return
  try {
    await filesApi.setPrimaryStorage({ id: file.id, storageId: storage.id })
    toast.success(t('files.library.messages.set_primary'))
    await afterStorageChange()
  }
  catch (error) {
    toast.danger((error as Error).message || t('files.library.messages.set_primary_failed'))
  }
}

/** 校验即时完成（读出内容按 SHA-256 比对）：按校验后的副本状态给结论 */
async function verifyStorage(storage: FileStorageRow) {
  const file = storageFile.value
  if (!file)
    return
  try {
    await filesApi.verifyStorage({ id: file.id, storageId: storage.id })
    await afterStorageChange()
    const verified = storageRows.value.find(row => row.id === storage.id)
    if (verified?.status === 'VerificationFailed')
      toast.warning(t('files.library.messages.verify_mismatch', { reason: storageReasonLabel(t, verified.remark) || '-' }))
    else
      toast.success(t('files.library.messages.verify_passed'))
  }
  catch (error) {
    toast.danger((error as Error).message || t('files.library.messages.verify_failed'))
  }
}

function toggleLabel(target: 'Normal' | 'Unavailable'): string {
  return target === 'Unavailable' ? t('files.library.storage_list.tooltip.disable') : t('files.library.storage_list.tooltip.enable')
}

function toggleStorage(storage: FileStorageRow) {
  const file = storageFile.value
  const target = storageToggleTarget(storage)
  if (!file || !target)
    return
  const action = toggleLabel(target)
  void dialog.confirm({
    badge: 'warning',
    title: action,
    content: t('files.library.storage_list.confirm_toggle', { action }),
    okText: action,
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        await filesApi.setStorageStatus({ id: file.id, storageId: storage.id, status: target })
        toast.success(t('files.library.messages.storage_status_updated'))
        await afterStorageChange()
      }
      catch (error) {
        toast.danger((error as Error).message || t('files.library.messages.storage_status_failed'))
      }
    },
  })
}

function primaryHint(storage: FileStorageRow): string {
  if (storage.isPrimary)
    return t('files.library.storage_list.tooltip.is_primary')
  return canSetPrimary(storage) ? t('files.library.storage_list.tooltip.set_primary') : t('files.library.storage_list.tooltip.not_usable')
}

/** 启停钮：主副本不能停用、校验失败的要先重新校验，这两种钮置灰并说明原因 */
function toggleHint(storage: FileStorageRow): string {
  const target = storageToggleTarget(storage)
  if (target)
    return toggleLabel(target)
  return storage.isPrimary ? t('files.library.storage_list.tooltip.primary_locked') : t('files.library.storage_list.tooltip.verify_first')
}

function renderStorageActions(storage: FileStorageRow) {
  const target = storageToggleTarget(storage)
  return h('div', { class: 'inline-flex items-center gap-1' }, [
    h(XIconButton, { icon: 'lucide:eye', label: t('files.library.storage_list.tooltip.detail'), onClick: () => openStorageDetail(storage) }),
    canUpdateFile.value
      ? [
          h(XIconButton, { icon: 'lucide:star', label: primaryHint(storage), tone: 'brand', disabled: !canSetPrimary(storage), onClick: () => setPrimary(storage) }),
          h(XIconButton, { icon: 'lucide:shield-check', label: t('files.library.storage_list.tooltip.verify'), tone: 'info', onClick: () => verifyStorage(storage) }),
          h(XIconButton, { icon: target === 'Normal' ? 'lucide:circle-check' : 'lucide:ban', label: toggleHint(storage), tone: target === 'Normal' ? 'success' : 'warning', disabled: !target, onClick: () => toggleStorage(storage) }),
        ]
      : null,
  ])
}

/** 状态列：校验失败的在标签后跟上原因 */
function renderStorageStatus(storage: FileStorageRow) {
  const reason = storage.status === 'VerificationFailed' ? storageReasonLabel(t, storage.remark) : ''
  return h('span', { class: 'file-storage-status' }, [
    renderTag(storageStatusLabel(t, storage.status), storageStatusTone(storage.status)),
    reason ? h('span', { class: 'file-storage-status__reason' }, reason) : null,
  ])
}

const storageColumns = computed<XDataTableColumn<FileStorageRow>[]>(() => [
  { key: 'storageType', title: t('files.library.storage_list.columns.storage_type'), minWidth: 110, render: storage => storageTypeLabel(t, storage.storageType) },
  { key: 'storageConfig', title: t('files.library.storage_list.columns.storage_config'), minWidth: 150, ellipsis: true, render: storage => storage.storageConfigName || storage.storageConfigCode || '-' },
  { key: 'isPrimary', title: t('files.library.storage_list.columns.is_primary'), width: 80, render: storage => (storage.isPrimary ? renderTag(t('files.library.storage_list.primary_tag'), 'info') : '-') },
  { key: 'verifiedAt', title: t('files.library.storage_list.columns.verified_at'), minWidth: 160, render: storage => formatDate(storage.verifiedAt) },
  { key: 'status', title: t('files.library.storage_list.columns.status'), minWidth: 120, render: renderStorageStatus },
  { key: 'actions', title: t('component.schema_page.actions_column'), width: 150, fixed: 'right', render: renderStorageActions },
])

// ── 预览 ────────────────────────────────────────────────────────
const previewVisible = ref(false)
const previewLoading = ref(false)
const previewKind = ref<PreviewKind | null>(null)
const previewName = ref('')
const previewUrl = ref('')
const previewText = ref('')
const previewLang = ref<string | undefined>(undefined)
const previewError = ref('')
const csvColumns = ref<XDataTableColumn<Record<string, string>>[]>([])
const csvRows = ref<Record<string, string>[]>([])
/** 连点两个文件时只认最后一次的结果 */
let previewSeq = 0

async function openPreview(row: FileRow) {
  const seq = ++previewSeq
  const kind = previewKindOf(row)
  previewKind.value = kind
  previewName.value = displayName(row)
  previewLang.value = textLangOf(row)
  previewUrl.value = ''
  previewText.value = ''
  previewError.value = ''
  csvColumns.value = []
  csvRows.value = []
  previewVisible.value = true
  previewLoading.value = true
  try {
    if (isTextPreview(kind)) {
      if (Number(row.size) > PREVIEW_TEXT_MAX_BYTES) {
        previewError.value = t('files.library.preview.text_too_large')
        return
      }
      const text = await (await fetchFileBlob(row.id)).text()
      if (seq !== previewSeq)
        return
      if (kind === 'csv')
        fillCsv(text)
      else
        previewText.value = text
    }
    else {
      // 图片与音视频直接用签名地址（按内容类型内联显示），不把内容读进内存
      const url = await fileLink(row.id)
      if (seq === previewSeq)
        previewUrl.value = url
    }
  }
  catch (error) {
    if (seq !== previewSeq)
      return
    const reason = (error as Error).message || t('files.library.preview.load_failed')
    previewError.value = reason
    toast.danger(reason)
  }
  finally {
    if (seq === previewSeq)
      previewLoading.value = false
  }
}

function fillCsv(text: string) {
  const table = toCsvTable(text)
  csvColumns.value = table.header.map((title, index) => ({
    key: String(index),
    title: title || t('files.library.preview.csv_column', { index: index + 1 }),
    minWidth: 100,
    ellipsis: true,
  }))
  csvRows.value = table.rows
}

async function copyPreviewText() {
  try {
    await navigator.clipboard.writeText(previewText.value)
    toast.success(t('files.library.preview.copied'))
  }
  catch (error) {
    toast.danger((error as Error).message || t('files.library.preview.copy_failed'))
  }
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <!-- 上传：拖拽区点选或拖入即上传（落在当前一侧的缺省存储配置上） -->
    <XhDialogRoot v-model:open="uploadVisible">
      <XhDialogContent style="--xh-dialog-max-w: 520px">
        <XhDialogHeader>
          <XhDialogTitle>{{ t('files.library.upload.title') }}</XhDialogTitle>
        </XhDialogHeader>
        <XhDialogCloseTrigger />
        <XhDialogBody class="xh-overlay-body">
          <XhFileUploadRoot :upload="handleUploadRequest" :max-files="Infinity">
            <XhFileUploadHiddenInput />
            <XhFileUploadDropzone>
              <div class="file-upload-dragger">
                <Icon width="38" height="38" icon="lucide:cloud-upload" />
                <div class="file-upload-dragger__text">
                  {{ t('files.library.upload.dragger_text') }}
                </div>
                <div class="file-upload-dragger__hint">
                  {{ t('files.library.upload.dragger_hint') }}
                </div>
              </div>
            </XhFileUploadDropzone>
          </XhFileUploadRoot>

          <!-- 上传参数：与新增 / 编辑弹窗同一套表单网格，字段名在上、控件在下 -->
          <div class="xh-edit-form-grid" data-testid="file-upload-form">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('files.library.form.access_level') }}</XhFieldLabel>
              <XhFieldControl>
                <XSelect v-model:value="uploadForm.accessLevel" name="accessLevel" :options="accessLevelOptions" />
              </XhFieldControl>
            </XhFieldRoot>
            <XhFieldRoot>
              <XhFieldLabel>{{ t('files.library.form.temporary') }}</XhFieldLabel>
              <XhFieldControl>
                <XhSwitch :checked="uploadForm.isTemporary" name="isTemporary" @update:checked="value => changeTemporary(uploadForm, value)" />
              </XhFieldControl>
            </XhFieldRoot>
            <!-- 到期时间：只对临时文件生效 -->
            <XhFieldRoot v-if="uploadForm.isTemporary" class="xh-span-2">
              <XhFieldLabel>{{ t('files.library.form.expires_at') }}</XhFieldLabel>
              <XhFieldControl>
                <XDatePicker v-model:value="uploadForm.expiresAt" clearable show-time :placeholder="t('files.library.form.expires_placeholder')" />
              </XhFieldControl>
            </XhFieldRoot>
            <XhFieldRoot class="xh-span-2">
              <XhFieldLabel>{{ t('files.library.form.tags') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="uploadForm.tags" name="tags" clearable :max-length="500" :placeholder="t('files.library.form.tags_placeholder')" />
              </XhFieldControl>
            </XhFieldRoot>
            <XhFieldRoot class="xh-span-2">
              <XhFieldLabel>{{ t('files.library.form.remark') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="uploadForm.remark" name="remark" clearable type="textarea" :rows="2" :max-length="500" :placeholder="t('files.library.form.remark_placeholder')" />
              </XhFieldControl>
            </XhFieldRoot>
          </div>
        </XhDialogBody>
      </XhDialogContent>
    </XhDialogRoot>

    <!-- 编辑元数据：访问级别、临时与到期、标签与备注 -->
    <XhDrawerRoot v-model:open="metadataVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 460px">
        <XhDrawerHeader>
          <XhDrawerTitle>{{ t('files.library.metadata.title') }}</XhDrawerTitle>
        </XhDrawerHeader>
        <XhDrawerCloseTrigger />
        <XhDrawerBody class="xh-overlay-body">
          <div class="xh-edit-form-grid" data-testid="file-metadata-form">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('files.library.form.access_level') }}</XhFieldLabel>
              <XhFieldControl>
                <XSelect v-model:value="metadataForm.accessLevel" name="accessLevel" :options="accessLevelOptions" />
              </XhFieldControl>
            </XhFieldRoot>
            <XhFieldRoot>
              <XhFieldLabel>{{ t('files.library.form.temporary') }}</XhFieldLabel>
              <XhFieldControl>
                <XhSwitch :checked="metadataForm.isTemporary" name="isTemporary" @update:checked="value => changeTemporary(metadataForm, value)" />
              </XhFieldControl>
            </XhFieldRoot>
            <XhFieldRoot v-if="metadataForm.isTemporary" class="xh-span-2">
              <XhFieldLabel>{{ t('files.library.form.expires_at') }}</XhFieldLabel>
              <XhFieldControl>
                <XDatePicker v-model:value="metadataForm.expiresAt" clearable show-time :placeholder="t('files.library.form.expires_placeholder')" />
              </XhFieldControl>
            </XhFieldRoot>
            <XhFieldRoot class="xh-span-2">
              <XhFieldLabel>{{ t('files.library.form.tags') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="metadataForm.tags" name="tags" clearable :max-length="500" :placeholder="t('files.library.form.tags_placeholder')" />
              </XhFieldControl>
            </XhFieldRoot>
            <XhFieldRoot class="xh-span-2">
              <XhFieldLabel>{{ t('files.library.form.remark') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="metadataForm.remark" name="remark" clearable type="textarea" :rows="2" :max-length="500" :placeholder="t('files.library.form.remark_placeholder')" />
              </XhFieldControl>
            </XhFieldRoot>
          </div>
        </XhDrawerBody>
        <XhDrawerFooter>
          <XhButton variant="subtle" @click="metadataVisible = false">
            {{ t('common.actions.cancel') }}
          </XhButton>
          <XhButton variant="subtle" tone="brand" :loading="metadataSaving" @click="saveMetadata">
            <XhButtonIndicator />
            <XhButtonLabel>{{ t('files.library.metadata.save') }}</XhButtonLabel>
          </XhButton>
        </XhDrawerFooter>
      </XhDrawerContent>
    </XhDrawerRoot>

    <!-- 详情：文件与副本共用 -->
    <XhDrawerRoot v-model:open="detailVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 680px">
        <XhDrawerHeader>
          <XhDrawerTitle>{{ detailTitle }}</XhDrawerTitle>
        </XhDrawerHeader>
        <XhDrawerCloseTrigger />
        <XhDrawerBody class="xh-overlay-body">
          <p v-if="detailLoading" class="file-muted">
            {{ t('common.statuses.loading') }}
          </p>

          <XhDescriptionsRoot v-else-if="detailKind === 'file' && fileDetail" :columns="2" variant="outline" size="sm">
            <XhDescriptionsItem class="file-span-2">
              <XhDescriptionsLabel>{{ t('files.library.detail.original_name') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ fileDetail.originalName }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('files.library.detail.kind') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ kindLabel(t, fileDetail.kind) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('files.library.detail.size') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatFileSize(Number(fileDetail.size)) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('files.library.detail.content_type') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ fileDetail.contentType || '-' }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('files.library.detail.access_level') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ accessLevelLabel(t, fileDetail.accessLevel) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('files.library.detail.status') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ fileStatusLabel(t, fileDetail.status) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('files.library.detail.temporary') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ yesNo(fileDetail.isTemporary) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('files.library.detail.download_count') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ fileDetail.downloadCount }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('files.library.detail.last_downloaded_at') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatDate(fileDetail.lastDownloadedAt) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('files.library.detail.created_at') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatDate(fileDetail.createdAt) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('files.library.detail.modified_at') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatDate(fileDetail.modifiedAt) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem v-if="fileDetail.isTemporary" class="file-span-2">
              <XhDescriptionsLabel>{{ t('files.library.detail.expires_at') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatDate(fileDetail.expiresAt) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem v-if="fileDetail.tags" class="file-span-2">
              <XhDescriptionsLabel>{{ t('files.library.detail.tags') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ fileDetail.tags }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem v-if="fileDetail.remark" class="file-span-2">
              <XhDescriptionsLabel>{{ t('files.library.detail.remark') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                <div class="file-detail-content">
                  {{ fileDetail.remark }}
                </div>
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem class="file-span-2">
              <XhDescriptionsLabel>{{ t('files.library.detail.source') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ sourceLabel(t, fileDetail.source) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem class="file-span-2">
              <XhDescriptionsLabel>{{ t('files.library.detail.name') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ fileDetail.name }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem class="file-span-2">
              <XhDescriptionsLabel>{{ t('files.library.detail.hash') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                <code class="file-detail-content">{{ fileDetail.hash || '-' }}</code>
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem class="file-span-2">
              <XhDescriptionsLabel>{{ t('files.library.detail.id') }}</XhDescriptionsLabel>
              <XhDescriptionsValue><code>{{ fileDetail.id }}</code></XhDescriptionsValue>
            </XhDescriptionsItem>
          </XhDescriptionsRoot>

          <XhDescriptionsRoot v-else-if="detailKind === 'storage' && storageDetail" :columns="1" variant="outline" size="sm">
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('files.library.detail.storage_id') }}</XhDescriptionsLabel>
              <XhDescriptionsValue><code>{{ storageDetail.id }}</code></XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem v-if="storageFile">
              <XhDescriptionsLabel>{{ t('files.library.detail.file_id') }}</XhDescriptionsLabel>
              <XhDescriptionsValue><code>{{ storageFile.id }}</code></XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('files.library.detail.storage_type') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ storageTypeLabel(t, storageDetail.storageType) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('files.library.detail.storage_config') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ storageDetail.storageConfigName || '-' }} / {{ storageDetail.storageConfigCode || '-' }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('files.library.detail.bucket') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ storageDetail.bucket || '-' }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('files.library.detail.key') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                <div class="file-detail-content">
                  {{ storageDetail.key || '-' }}
                </div>
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('files.library.detail.is_primary') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ yesNo(storageDetail.isPrimary) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('files.library.detail.status') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ storageStatusLabel(t, storageDetail.status) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('files.library.detail.failure_reason') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ storageReasonLabel(t, storageDetail.remark) || '-' }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('files.library.detail.verified_at') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatDate(storageDetail.verifiedAt) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('files.library.detail.created_at') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatDate(storageDetail.createdAt) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
          </XhDescriptionsRoot>

          <p v-else class="file-muted">
            {{ t('files.library.detail.empty') }}
          </p>
        </XhDrawerBody>
      </XhDrawerContent>
    </XhDrawerRoot>

    <!-- 存储副本：列出这个文件的全部副本（主副本在前），可看详情、设主副本、校验、启停 -->
    <XhDrawerRoot v-model:open="storagesVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 760px">
        <XhDrawerHeader>
          <XhDrawerTitle>{{ t('files.library.storage_list.title', { name: storageFile ? displayName(storageFile) : '' }) }}</XhDrawerTitle>
        </XhDrawerHeader>
        <XhDrawerCloseTrigger />
        <XhDrawerBody class="xh-overlay-body">
          <div class="file-storages__head">
            <span class="file-muted">{{ t('files.library.storage_list.total', { count: storageRows.length }) }}</span>
            <XhButton variant="subtle" size="sm" :loading="storagesLoading" @click="loadStorages">
              <XhButtonIndicator>
                <Icon icon="lucide:refresh-cw" width="14" />
              </XhButtonIndicator>
              <XhButtonLabel>{{ t('common.actions.refresh') }}</XhButtonLabel>
            </XhButton>
          </div>
          <XDataTable :columns="storageColumns" :data="storageRows" row-key="id" :loading="storagesLoading" />
        </XhDrawerBody>
      </XhDrawerContent>
    </XhDrawerRoot>

    <!-- 预览：浏览器直接渲染得了的几类（图片、音视频、文本与源码（含 Markdown 原文）、CSV） -->
    <XhDialogRoot v-model:open="previewVisible">
      <XhDialogContent style="--xh-dialog-max-w: 80vw">
        <XhDialogHeader>
          <XhDialogTitle>{{ t('files.library.preview.title', { name: previewName }) }}</XhDialogTitle>
        </XhDialogHeader>
        <XhDialogCloseTrigger />
        <XhDialogBody>
          <div class="file-preview" :class="{ 'is-block': isTextPreview(previewKind) && !previewLoading && !previewError }">
            <p v-if="previewLoading" class="file-muted">
              {{ t('common.statuses.loading') }}
            </p>
            <p v-else-if="previewError" class="file-muted">
              {{ previewError }}
            </p>
            <div v-else-if="previewKind === 'text'" class="file-preview__code">
              <XhButton class="file-preview__copy" variant="subtle" size="sm" @click="copyPreviewText">
                <XhButtonIndicator>
                  <Icon icon="lucide:copy" width="14" />
                </XhButtonIndicator>
                <XhButtonLabel>{{ t('files.library.preview.copy_code') }}</XhButtonLabel>
              </XhButton>
              <div class="file-preview__code-scroll">
                <!-- 着色实现 @xihan-ui/code-highlight 不在本应用的依赖里：显式关掉着色，按等宽原文显示 -->
                <XhCodeViewRoot :code="previewText" :lang="previewLang" :highlighter="null" complete>
                  <XhCodeViewPre>
                    <XhCodeViewCode />
                  </XhCodeViewPre>
                </XhCodeViewRoot>
              </div>
            </div>
            <XDataTable v-else-if="previewKind === 'csv'" class="file-preview__csv" :columns="csvColumns" :data="csvRows" row-key="_row" max-height="66vh" />
            <p v-else-if="!previewUrl" class="file-muted">
              {{ t('files.library.preview.cannot_preview') }}
            </p>
            <img v-else-if="previewKind === 'image'" :src="previewUrl" :alt="previewName" class="file-preview__media">
            <video v-else-if="previewKind === 'video'" :src="previewUrl" controls class="file-preview__media" />
            <audio v-else-if="previewKind === 'audio'" :src="previewUrl" controls class="file-preview__audio" />
          </div>
        </XhDialogBody>
      </XhDialogContent>
    </XhDialogRoot>
  </SchemaPage>
</template>

<style scoped>
.file-muted {
  margin: 0;
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}

.file-detail-content {
  white-space: pre-wrap;
  word-break: break-word;
}

.file-span-2 {
  grid-column: span 2;
}

.file-upload-dragger {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
  align-items: center;
  padding-block: var(--xh-space-4);
}

.file-upload-dragger__text {
  color: var(--xh-fg-default);
  font-size: var(--xh-text-body-size);
  font-weight: var(--xh-font-weight-medium);
}

.file-upload-dragger__hint {
  color: var(--xh-fg-subtle);
  font-size: var(--xh-text-caption-size);
}

.file-storages__head {
  display: flex;
  flex-wrap: wrap;
  gap: var(--xh-space-2);
  align-items: center;
  justify-content: space-between;
}

/* 副本表的状态列是列 render 出的节点，不带本页的 scope，样式要穿透 */
:deep(.file-storage-status) {
  display: inline-flex;
  flex-wrap: wrap;
  gap: var(--xh-space-1);
  align-items: center;
}

:deep(.file-storage-status__reason) {
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}

.file-preview {
  display: flex;
  align-items: center;
  justify-content: center;
  min-block-size: 60vh;
  max-block-size: 72vh;
  overflow: auto;
}

/* 文本、代码与 CSV：块级流 + 滚动，免得居中布局把内容压塌 */
.file-preview.is-block {
  display: block;
  text-align: start;
}

.file-preview__media {
  max-inline-size: 100%;
  max-block-size: 70vh;
  object-fit: contain;
}

.file-preview__audio,
.file-preview__csv {
  inline-size: 100%;
}

.file-preview__code {
  position: relative;
  inline-size: 100%;
}

.file-preview__copy {
  position: absolute;
  inset-block-start: var(--xh-space-2);
  inset-inline-end: var(--xh-space-2);
  z-index: 1;
}

.file-preview__code-scroll {
  max-block-size: 70vh;
  overflow: auto;
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-radius-md);
}
</style>
