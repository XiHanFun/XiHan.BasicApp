<script setup lang="ts">
import type { UploadCustomRequestOptions } from 'naive-ui'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import type { DataTableColumns } from 'naive-ui'
import type {
  FileDetailDto,
  FileListItemDto,
  FileStorageDetailDto,
  FileStorageListItemDto,
  PageResult,
} from '@/api'
import {
  NButton,
  NDataTable,
  NDescriptions,
  NDescriptionsItem,
  NDrawer,
  NDrawerContent,
  NIcon,
  NInput,
  NInputNumber,
  NModal,
  NPopconfirm,
  NSelect,
  NSpace,
  NSwitch,
  NTag,
  NTooltip,
  NUpload,
  useMessage,
} from 'naive-ui'
import { computed, h, nextTick, reactive, ref } from 'vue'
import {
  createPageRequest,
  fileManagementApi,
  FileStatus,
  FileStorageStatus,
  FileStorageType,
  FileType,
  ResourceAccessLevel,
} from '@/api'
import { Icon, SchemaPage } from '~/components'
import { formatDate, formatFileSize, getOptionLabel } from '~/utils'

defineOptions({ name: 'PlatformFilePage' })

type DetailKind = 'file' | 'storage'
type TagType = 'default' | 'error' | 'info' | 'success' | 'warning'

const message = useMessage()

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

// ── 选项常量（布尔以 1/0 表达，搜索值仅 string|number） ──────────
const booleanOptions = [
  { label: '是', value: 1 },
  { label: '否', value: 0 },
]

const fileTypeOptions = [
  { label: '图片', value: FileType.Image },
  { label: '文档', value: FileType.Document },
  { label: '视频', value: FileType.Video },
  { label: '音频', value: FileType.Audio },
  { label: '压缩包', value: FileType.Archive },
  { label: '其他', value: FileType.Other },
]

const fileStatusOptions = [
  { label: '正常', value: FileStatus.Normal },
  { label: '上传中', value: FileStatus.Uploading },
  { label: '上传失败', value: FileStatus.UploadFailed },
  { label: '处理中', value: FileStatus.Processing },
  { label: '已删除', value: FileStatus.Deleted },
  { label: '已归档', value: FileStatus.Archived },
  { label: '已过期', value: FileStatus.Expired },
  { label: '损坏', value: FileStatus.Corrupted },
  { label: '违规', value: FileStatus.Violation },
]

const accessLevelOptions = [
  { label: '匿名访问', value: ResourceAccessLevel.Public },
  { label: '仅需认证', value: ResourceAccessLevel.Authenticated },
  { label: '需要授权', value: ResourceAccessLevel.Authorized },
]

const storageTypeOptions = [
  { label: '本地存储', value: FileStorageType.Local },
  { label: '阿里云OSS', value: FileStorageType.AliyunOss },
  { label: '腾讯云COS', value: FileStorageType.TencentCos },
  { label: 'MinIO', value: FileStorageType.Minio },
  { label: 'FTP', value: FileStorageType.Ftp },
  { label: 'SFTP', value: FileStorageType.Sftp },
  { label: 'WebDAV', value: FileStorageType.WebDav },
  { label: '自定义存储', value: FileStorageType.Custom },
]

const storageStatusOptions = [
  { label: '正常', value: FileStorageStatus.Normal },
  { label: '上传中', value: FileStorageStatus.Uploading },
  { label: '上传失败', value: FileStorageStatus.UploadFailed },
  { label: '同步中', value: FileStorageStatus.Syncing },
  { label: '同步失败', value: FileStorageStatus.SyncFailed },
  { label: '待验证', value: FileStorageStatus.PendingVerification },
  { label: '验证失败', value: FileStorageStatus.VerificationFailed },
  { label: '已过期', value: FileStorageStatus.Expired },
  { label: '已删除', value: FileStorageStatus.Deleted },
  { label: '不可用', value: FileStorageStatus.Unavailable },
]

// ── 过滤值清洗辅助 ──────────────────────────────────────────────
function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}
function toBool(v: unknown): boolean | undefined {
  return v === undefined || v === null || v === '' ? undefined : Number(v) === 1
}

function formatDateTime(value?: string | null) {
  return value ? formatDate(value) : '-'
}

function formatDuration(value?: number | null) {
  return value === null || value === undefined ? '-' : `${value}s`
}

function formatUploadDuration(value?: number | null) {
  return value === null || value === undefined ? '-' : `${value}ms`
}

function formatFlag(value: boolean) {
  return value ? '是' : '否'
}

function getFileStatusTagType(status: FileStatus): TagType {
  if (status === FileStatus.Normal) {
    return 'success'
  }
  if (status === FileStatus.UploadFailed || status === FileStatus.Corrupted || status === FileStatus.Violation) {
    return 'error'
  }
  if (status === FileStatus.Uploading || status === FileStatus.Processing) {
    return 'warning'
  }
  return 'default'
}

// ── 文件预览类型判定（仅浏览器可直接渲染的简单类型）─────────────
type PreviewKind = 'image' | 'video' | 'audio' | 'pdf' | 'text'

const PREVIEW_TEXT_EXTENSIONS = new Set([
  'txt', 'md', 'markdown', 'json', 'xml', 'yaml', 'yml',
  'csv', 'log', 'ini', 'conf', 'html', 'htm', 'css', 'js', 'ts', 'sql',
])

/** 仅当文件为浏览器可直接渲染的简单类型时返回其预览种类，否则返回 null */
function getPreviewKind(row: FileListItemDto): PreviewKind | null {
  const mime = (row.mimeType ?? '').toLowerCase()
  const ext = (row.fileExtension ?? '').replace(/^\./, '').toLowerCase()
  if (mime.startsWith('image/') || row.fileType === FileType.Image) {
    return 'image'
  }
  if (mime.startsWith('video/') || row.fileType === FileType.Video) {
    return 'video'
  }
  if (mime.startsWith('audio/') || row.fileType === FileType.Audio) {
    return 'audio'
  }
  if (mime === 'application/pdf' || ext === 'pdf') {
    return 'pdf'
  }
  if (mime.startsWith('text/') || PREVIEW_TEXT_EXTENSIONS.has(ext)) {
    return 'text'
  }
  return null
}

/** 文件正常且可预览时才显示预览入口 */
function canPreview(row: FileListItemDto): boolean {
  return row.status === FileStatus.Normal && getPreviewKind(row) !== null
}

// ── 字段单一事实源 ──────────────────────────────────────────────
// 后端 FilePageQueryDto 支持：keyword/fileType/status/accessLevel/isTemporary/isEncrypted/fileExtension/mimeType
const fields: ListFieldSchema[] = [
  { key: 'keyword', title: '关键词', dataType: 'string', visible: false, searchable: true, searchPlaceholder: '搜索文件名/哈希', width: 220, order: 0 },
  {
    key: 'originalName',
    title: '原始文件名',
    dataType: 'string',
    minWidth: 220,
    fixed: 'left',
    order: 1,
  },
  { key: 'fileName', title: '存储文件名', dataType: 'string', minWidth: 220, order: 2 },
  {
    key: 'fileType',
    title: '文件类型',
    dataType: 'enum',
    searchable: true,
    options: fileTypeOptions,
    searchPlaceholder: '文件类型',
    width: 110,
    order: 3,
    render: row => h(NTag, { round: true, size: 'small' }, () => getOptionLabel(fileTypeOptions, (row as unknown as FileListItemDto).fileType)),
  },
  {
    key: 'fileSize',
    title: '文件大小',
    dataType: 'number',
    minWidth: 110,
    order: 4,
    render: row => formatFileSize(Number((row as unknown as FileListItemDto).fileSize || 0)),
  },
  {
    key: 'status',
    title: '状态',
    dataType: 'enum',
    searchable: true,
    options: fileStatusOptions,
    searchPlaceholder: '文件状态',
    width: 110,
    order: 5,
    render: row => h(NTag, { type: getFileStatusTagType((row as unknown as FileListItemDto).status), round: true, size: 'small' }, () => getOptionLabel(fileStatusOptions, (row as unknown as FileListItemDto).status)),
  },
  {
    key: 'accessLevel',
    title: '访问级别',
    dataType: 'enum',
    searchable: true,
    options: accessLevelOptions,
    searchPlaceholder: '访问级别',
    minWidth: 110,
    order: 6,
    render: row => getOptionLabel(accessLevelOptions, (row as unknown as FileListItemDto).accessLevel),
  },
  {
    key: 'isTemporary',
    title: '临时',
    dataType: 'boolean',
    searchable: true,
    options: booleanOptions,
    searchPlaceholder: '临时',
    width: 82,
    order: 7,
  },
  {
    key: 'isEncrypted',
    title: '加密',
    dataType: 'boolean',
    searchable: true,
    options: booleanOptions,
    searchPlaceholder: '加密',
    width: 82,
    order: 8,
  },
  { key: 'fileExtension', title: '扩展名', dataType: 'string', advancedSearch: true, searchPlaceholder: '扩展名', minWidth: 90, order: 9 },
  { key: 'mimeType', title: 'MIME', dataType: 'string', advancedSearch: true, searchPlaceholder: 'MIME', minWidth: 160, order: 10 },
  { key: 'downloadCount', title: '下载', dataType: 'number', minWidth: 90, order: 11 },
  { key: 'viewCount', title: '访问', dataType: 'number', minWidth: 90, order: 12 },
  { key: 'createdTime', title: '创建时间', dataType: 'datetime', sortable: true, minWidth: 170, order: 13 },
]

const schema: PageSchema = {
  pageCode: 'platform.file',
  pageName: '文件管理',
  rowKey: 'basicId',
  scrollX: 1800,
  fields,
  resource: {
    page: (params) => {
      const f = params.filters
      return fileManagementApi.page({
        ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
        keyword: toStr(f.keyword),
        fileType: (f.fileType as FileType | undefined) ?? undefined,
        status: (f.status as FileStatus | undefined) ?? undefined,
        accessLevel: (f.accessLevel as ResourceAccessLevel | undefined) ?? undefined,
        isTemporary: toBool(f.isTemporary),
        isEncrypted: toBool(f.isEncrypted),
        fileExtension: toStr(f.fileExtension),
        mimeType: toStr(f.mimeType),
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
  },
  actions: [
    { key: 'upload', title: '上传文件', scope: 'page', type: 'primary', icon: 'lucide:upload' },
    { key: 'preview', title: '预览', scope: 'row', visible: row => canPreview(row as unknown as FileListItemDto) },
    { key: 'view', title: '查看详情', scope: 'row' },
    { key: 'metadata', title: '编辑元数据', scope: 'row' },
    { key: 'storages', title: '存储副本', scope: 'row' },
    { key: 'archive', title: '归档', scope: 'row', visible: row => (row as unknown as FileListItemDto).status !== FileStatus.Archived },
    { key: 'delete', title: '删除', scope: 'row', visible: row => (row as unknown as FileListItemDto).status !== FileStatus.Deleted },
  ],
}

// ── 行/页面操作分发 ─────────────────────────────────────────────
function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as FileListItemDto | undefined
  switch (payload.key) {
    case 'upload':
      openUploadDrawer()
      break
    case 'preview':
      if (row) {
        void handlePreview(row)
      }
      break
    case 'view':
      if (row) {
        void handleFileDetail(row)
      }
      break
    case 'metadata':
      if (row) {
        openMetadataDrawer(row)
      }
      break
    case 'storages':
      if (row) {
        void openStorageList(row)
      }
      break
    case 'archive':
      if (row) {
        void handleUpdateFileStatus(row, FileStatus.Archived)
      }
      break
    case 'delete':
      if (row) {
        void handleDeleteFile(row)
      }
      break
  }
}

// ── 详情抽屉 ────────────────────────────────────────────────────
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailKind = ref<DetailKind>('file')
const currentFileDetail = ref<FileDetailDto | null>(null)
const currentStorageDetail = ref<FileStorageDetailDto | null>(null)
const detailTitle = computed(() => detailKind.value === 'file' ? '文件详情' : '存储副本详情')

// ── 上传抽屉 ────────────────────────────────────────────────────
const uploadVisible = ref(false)
const uploadLoading = ref(false)
const uploadForm = reactive({
  accessControl: '',
  accessLevel: ResourceAccessLevel.Authorized,
  bucketName: '',
  cacheControl: '',
  directory: '',
  isEncrypted: false,
  isTemporary: false,
  overwrite: false,
  providerName: '',
  remark: '',
  retentionDays: 0,
  routeKey: '',
  tags: '',
})

// ── 元数据抽屉 ──────────────────────────────────────────────────
const metadataVisible = ref(false)
const metadataLoading = ref(false)
const metadataRow = ref<FileListItemDto | null>(null)
const metadataForm = reactive({
  accessLevel: ResourceAccessLevel.Authorized as ResourceAccessLevel,
  accessPermissions: '',
  isEncrypted: false,
  isTemporary: false,
  remark: '',
  retentionDays: 0,
  tags: '',
})

const actionLoading = ref(false)

function normalizeNullable(value?: string | null) {
  const normalized = value?.trim()
  return normalized || null
}

function openUploadDrawer() {
  uploadVisible.value = true
}

function openMetadataDrawer(row: FileListItemDto) {
  metadataRow.value = row
  metadataForm.accessLevel = row.accessLevel
  metadataForm.accessPermissions = ''
  metadataForm.isEncrypted = row.isEncrypted
  metadataForm.isTemporary = row.isTemporary
  metadataForm.retentionDays = row.retentionDays
  metadataForm.tags = ''
  metadataForm.remark = ''
  metadataVisible.value = true
}

async function handleSaveMetadata() {
  const row = metadataRow.value
  if (!row) {
    return
  }
  metadataLoading.value = true
  try {
    await fileManagementApi.updateMetadata({
      basicId: row.basicId,
      accessLevel: metadataForm.accessLevel,
      accessPermissions: normalizeNullable(metadataForm.accessPermissions),
      isEncrypted: metadataForm.isEncrypted,
      isTemporary: metadataForm.isTemporary,
      retentionDays: metadataForm.retentionDays,
      tags: normalizeNullable(metadataForm.tags),
      remark: normalizeNullable(metadataForm.remark),
    })
    message.success('元数据已更新')
    metadataVisible.value = false
    reload()
  }
  catch {
    message.error('更新元数据失败')
  }
  finally {
    metadataLoading.value = false
  }
}

async function handleUpdateFileStatus(row: FileListItemDto, status: FileStatus) {
  actionLoading.value = true
  try {
    await fileManagementApi.updateStatus({ basicId: row.basicId, status })
    message.success('文件状态已更新')
    reload()
  }
  catch {
    message.error('更新文件状态失败')
  }
  finally {
    actionLoading.value = false
  }
}

async function handleDeleteFile(row: FileListItemDto) {
  actionLoading.value = true
  try {
    await fileManagementApi.updateStatus({ basicId: row.basicId, status: FileStatus.Deleted })
    message.success('文件已删除')
    reload()
  }
  catch {
    message.error('删除文件失败')
  }
  finally {
    actionLoading.value = false
  }
}

async function handleUploadRequest(options: UploadCustomRequestOptions) {
  const rawFile = options.file.file
  if (!rawFile) {
    options.onError()
    return
  }
  uploadLoading.value = true
  try {
    await fileManagementApi.upload({
      accessControl: normalizeNullable(uploadForm.accessControl),
      accessLevel: uploadForm.accessLevel,
      bucketName: normalizeNullable(uploadForm.bucketName),
      cacheControl: normalizeNullable(uploadForm.cacheControl),
      directory: normalizeNullable(uploadForm.directory),
      file: rawFile,
      isEncrypted: uploadForm.isEncrypted,
      isTemporary: uploadForm.isTemporary,
      overwrite: uploadForm.overwrite,
      providerName: normalizeNullable(uploadForm.providerName),
      remark: normalizeNullable(uploadForm.remark),
      retentionDays: uploadForm.retentionDays,
      routeKey: normalizeNullable(uploadForm.routeKey),
      tags: normalizeNullable(uploadForm.tags),
    })
    options.onProgress({ percent: 100 })
    options.onFinish()
    uploadVisible.value = false
    await nextTick()
    reload()
    message.success('上传成功')
  }
  catch {
    options.onError()
    message.error('上传失败')
  }
  finally {
    uploadLoading.value = false
  }
}

async function handleFileDetail(row: FileListItemDto) {
  detailKind.value = 'file'
  detailVisible.value = true
  detailLoading.value = true
  currentStorageDetail.value = null
  try {
    currentFileDetail.value = await fileManagementApi.detail(row.basicId)
  }
  catch {
    currentFileDetail.value = null
    message.error('加载文件详情失败')
  }
  finally {
    detailLoading.value = false
  }
}

// ── 文件预览 ────────────────────────────────────────────────────
const previewVisible = ref(false)
const previewLoading = ref(false)
const previewUrl = ref<string>('')
const previewKind = ref<PreviewKind | null>(null)
const previewName = ref<string>('')

/** 生成签名 URL 并按文件种类在弹窗中预览 */
async function handlePreview(row: FileListItemDto) {
  previewKind.value = getPreviewKind(row)
  previewName.value = row.originalName
  previewUrl.value = ''
  previewVisible.value = true
  previewLoading.value = true
  try {
    previewUrl.value = await fileManagementApi.generatePresignedUrl(row.basicId)
  }
  catch {
    previewUrl.value = ''
    message.error('获取预览地址失败')
  }
  finally {
    previewLoading.value = false
  }
}

// ── 存储副本列表抽屉 ────────────────────────────────────────────
const storageListVisible = ref(false)
const storageListLoading = ref(false)
const storageRows = ref<FileStorageListItemDto[]>([])
const storageFile = ref<FileListItemDto | null>(null)

function getStorageStatusTagType(status: FileStorageStatus): TagType {
  if (status === FileStorageStatus.Normal) {
    return 'success'
  }
  if (
    status === FileStorageStatus.UploadFailed
    || status === FileStorageStatus.SyncFailed
    || status === FileStorageStatus.VerificationFailed
    || status === FileStorageStatus.Unavailable
  ) {
    return 'error'
  }
  if (
    status === FileStorageStatus.Uploading
    || status === FileStorageStatus.Syncing
    || status === FileStorageStatus.PendingVerification
  ) {
    return 'warning'
  }
  return 'default'
}

/** 打开副本列表抽屉并加载该文件的全部存储副本 */
async function openStorageList(row: FileListItemDto) {
  storageFile.value = row
  storageListVisible.value = true
  await loadStorageRows()
}

async function loadStorageRows() {
  const file = storageFile.value
  if (!file) {
    return
  }
  storageListLoading.value = true
  try {
    const result = await fileManagementApi.storagePage({
      ...createPageRequest({ page: { pageIndex: 1, pageSize: 100 } }),
      fileId: file.basicId,
    })
    storageRows.value = result.items
  }
  catch {
    storageRows.value = []
    message.error('加载存储副本失败')
  }
  finally {
    storageListLoading.value = false
  }
}

/** 查看单个副本详情（复用文件/副本详情抽屉） */
async function viewStorageDetail(storageId: string) {
  detailKind.value = 'storage'
  detailVisible.value = true
  detailLoading.value = true
  currentFileDetail.value = null
  currentStorageDetail.value = null
  try {
    currentStorageDetail.value = await fileManagementApi.storageDetail(storageId)
  }
  catch {
    currentStorageDetail.value = null
    message.error('加载副本详情失败')
  }
  finally {
    detailLoading.value = false
  }
}

/** 设为主副本 */
async function handleSwitchPrimary(storage: FileStorageListItemDto) {
  const file = storageFile.value
  if (!file) {
    return
  }
  actionLoading.value = true
  try {
    await fileManagementApi.switchPrimaryStorage({ basicId: file.basicId, storageId: storage.basicId })
    message.success('已设为主副本')
    await loadStorageRows()
  }
  catch {
    message.error('设置主副本失败')
  }
  finally {
    actionLoading.value = false
  }
}

/** 校验副本 */
async function handleVerifyStorage(storage: FileStorageListItemDto) {
  actionLoading.value = true
  try {
    await fileManagementApi.verifyStorage({ basicId: storage.basicId })
    message.success('副本校验已触发')
    await loadStorageRows()
  }
  catch {
    message.error('副本校验失败')
  }
  finally {
    actionLoading.value = false
  }
}

/** 启用 / 停用副本（Normal ↔ Unavailable 互切） */
async function handleToggleStorageStatus(storage: FileStorageListItemDto) {
  const nextStatus = storage.status === FileStorageStatus.Normal
    ? FileStorageStatus.Unavailable
    : FileStorageStatus.Normal
  actionLoading.value = true
  try {
    await fileManagementApi.updateStorageStatus({ basicId: storage.basicId, status: nextStatus })
    message.success('副本状态已更新')
    await loadStorageRows()
  }
  catch {
    message.error('更新副本状态失败')
  }
  finally {
    actionLoading.value = false
  }
}

const storageColumns = computed<DataTableColumns<FileStorageListItemDto>>(() => [
  {
    key: 'storageType',
    title: '存储类型',
    minWidth: 110,
    render: row => getOptionLabel(storageTypeOptions, row.storageType),
  },
  {
    key: 'storageProvider',
    title: '提供商/区域',
    minWidth: 140,
    ellipsis: { tooltip: true },
    render: row => `${row.storageProvider || '-'} / ${row.storageRegion || '-'}`,
  },
  {
    key: 'isPrimary',
    title: '主副本',
    width: 80,
    render: row => row.isPrimary
      ? h(NTag, { size: 'small', type: 'info', round: true, bordered: false }, () => '主')
      : h('span', { class: 'text-foreground/40' }, '-'),
  },
  {
    key: 'flags',
    title: '校验/同步',
    width: 110,
    render: row => `${formatFlag(row.isVerified)} / ${formatFlag(row.isSynced)}`,
  },
  {
    key: 'status',
    title: '状态',
    width: 100,
    render: row => h(NTag, { size: 'small', round: true, type: getStorageStatusTagType(row.status) }, () => getOptionLabel(storageStatusOptions, row.status)),
  },
  {
    key: 'actions',
    title: '操作',
    width: 150,
    fixed: 'right',
    render: row => h('div', { style: 'display:flex;align-items:center;gap:2px;' }, [
      h(NTooltip, null, {
        trigger: () => h(NButton, { size: 'small', quaternary: true, circle: true, onClick: () => viewStorageDetail(row.basicId) }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:eye' })) }),
        default: () => '详情',
      }),
      h(NTooltip, null, {
        trigger: () => h(NButton, { size: 'small', quaternary: true, circle: true, type: 'primary', disabled: row.isPrimary, onClick: () => handleSwitchPrimary(row) }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:star' })) }),
        default: () => row.isPrimary ? '已是主副本' : '设为主副本',
      }),
      h(NTooltip, null, {
        trigger: () => h(NButton, { size: 'small', quaternary: true, circle: true, type: 'info', onClick: () => handleVerifyStorage(row) }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:shield-check' })) }),
        default: () => '校验',
      }),
      h(NPopconfirm, { onPositiveClick: () => handleToggleStorageStatus(row) }, {
        trigger: () => h(NTooltip, null, {
          trigger: () => h(NButton, { size: 'small', quaternary: true, circle: true, type: row.status === FileStorageStatus.Normal ? 'warning' : 'success' }, { icon: () => h(NIcon, null, () => h(Icon, { icon: row.status === FileStorageStatus.Normal ? 'lucide:ban' : 'lucide:circle-check' })) }),
          default: () => row.status === FileStorageStatus.Normal ? '停用' : '启用',
        }),
        default: () => `确认${row.status === FileStorageStatus.Normal ? '停用' : '启用'}该副本？`,
      }),
    ]),
  },
])
</script>

<template>
  <SchemaPage
    ref="schemaPageRef"
    :schema="schema"
    @action="onAction"
  >
    <NDrawer v-model:show="uploadVisible" :width="520">
      <NDrawerContent closable title="上传文件">
        <NSpace vertical>
          <NSelect
            v-model:value="uploadForm.accessLevel"
            :options="accessLevelOptions"
            placeholder="访问级别"
          />
          <NInput v-model:value="uploadForm.routeKey" clearable placeholder="路由键" />
          <NInput v-model:value="uploadForm.providerName" clearable placeholder="存储提供商" />
          <NInput v-model:value="uploadForm.bucketName" clearable placeholder="存储桶" />
          <NInput v-model:value="uploadForm.directory" clearable placeholder="目录" />
          <NInput v-model:value="uploadForm.accessControl" clearable placeholder="访问控制" />
          <NInput v-model:value="uploadForm.cacheControl" clearable placeholder="缓存控制" />
          <NInput v-model:value="uploadForm.tags" clearable placeholder="标签" />
          <NInput v-model:value="uploadForm.remark" clearable placeholder="备注" type="textarea" />
          <NInputNumber v-model:value="uploadForm.retentionDays" :min="0" placeholder="保留天数" style="width: 100%" />
          <div class="file-upload-switches">
            <NSpace align="center">
              <span>覆盖</span>
              <NSwitch v-model:value="uploadForm.overwrite" />
            </NSpace>
            <NSpace align="center">
              <span>加密</span>
              <NSwitch v-model:value="uploadForm.isEncrypted" />
            </NSpace>
            <NSpace align="center">
              <span>临时</span>
              <NSwitch v-model:value="uploadForm.isTemporary" />
            </NSpace>
          </div>
          <NUpload :custom-request="handleUploadRequest" :disabled="uploadLoading" :show-file-list="false">
            <NButton block :loading="uploadLoading" type="primary">
              <template #icon>
                <NIcon><Icon icon="lucide:upload" /></NIcon>
              </template>
              选择文件
            </NButton>
          </NUpload>
        </NSpace>
      </NDrawerContent>
    </NDrawer>

    <NDrawer v-model:show="metadataVisible" :width="460">
      <NDrawerContent closable title="编辑文件元数据">
        <NSpace vertical>
          <NSelect
            v-model:value="metadataForm.accessLevel"
            :options="accessLevelOptions"
            placeholder="访问级别"
          />
          <NInput v-model:value="metadataForm.accessPermissions" clearable placeholder="访问权限" />
          <NInput v-model:value="metadataForm.tags" clearable placeholder="标签" />
          <NInput v-model:value="metadataForm.remark" clearable placeholder="备注" type="textarea" />
          <NInputNumber v-model:value="metadataForm.retentionDays" :min="0" placeholder="保留天数" style="width: 100%" />
          <div class="file-upload-switches">
            <NSpace align="center">
              <span>加密</span>
              <NSwitch v-model:value="metadataForm.isEncrypted" />
            </NSpace>
            <NSpace align="center">
              <span>临时</span>
              <NSwitch v-model:value="metadataForm.isTemporary" />
            </NSpace>
          </div>
          <NButton block :loading="metadataLoading" type="primary" @click="handleSaveMetadata">
            <template #icon>
              <NIcon><Icon icon="lucide:save" /></NIcon>
            </template>
            保存元数据
          </NButton>
        </NSpace>
      </NDrawerContent>
    </NDrawer>

    <NDrawer v-model:show="detailVisible" :width="680">
      <NDrawerContent closable :title="detailTitle">
        <NSpace v-if="detailLoading" justify="center">
          加载中...
        </NSpace>

        <NDescriptions
          v-else-if="detailKind === 'file' && currentFileDetail"
          :column="1"
          bordered
          size="small"
        >
          <NDescriptionsItem label="文件主键">
            {{ currentFileDetail.basicId }}
          </NDescriptionsItem>
          <NDescriptionsItem label="原始文件名">
            {{ currentFileDetail.originalName }}
          </NDescriptionsItem>
          <NDescriptionsItem label="存储文件名">
            {{ currentFileDetail.fileName }}
          </NDescriptionsItem>
          <NDescriptionsItem label="文件哈希">
            <div class="file-detail-content">
              {{ currentFileDetail.fileHash || '-' }}
            </div>
          </NDescriptionsItem>
          <NDescriptionsItem label="文件类型">
            {{ getOptionLabel(fileTypeOptions, currentFileDetail.fileType) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="MIME">
            {{ currentFileDetail.mimeType || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="文件大小">
            {{ formatFileSize(Number(currentFileDetail.fileSize || 0)) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="尺寸/时长">
            {{ currentFileDetail.width || '-' }} x {{ currentFileDetail.height || '-' }} / {{ formatDuration(currentFileDetail.duration) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="访问级别">
            {{ getOptionLabel(accessLevelOptions, currentFileDetail.accessLevel) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="访问权限">
            <div class="file-detail-content">
              {{ currentFileDetail.accessPermissions || '-' }}
            </div>
          </NDescriptionsItem>
          <NDescriptionsItem label="状态">
            {{ getOptionLabel(fileStatusOptions, currentFileDetail.status) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="标记">
            加密 {{ formatFlag(currentFileDetail.isEncrypted) }}，临时 {{ formatFlag(currentFileDetail.isTemporary) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="上传来源">
            {{ currentFileDetail.uploadIp || '-' }} / {{ currentFileDetail.uploadSource || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="统计">
            下载 {{ currentFileDetail.downloadCount }}，访问 {{ currentFileDetail.viewCount }}
          </NDescriptionsItem>
          <NDescriptionsItem label="标签">
            <div class="file-detail-content">
              {{ currentFileDetail.tags || '-' }}
            </div>
          </NDescriptionsItem>
          <NDescriptionsItem label="备注">
            <div class="file-detail-content">
              {{ currentFileDetail.remark || '-' }}
            </div>
          </NDescriptionsItem>
          <NDescriptionsItem label="扩展数据">
            <div class="file-detail-content">
              {{ currentFileDetail.extendData || '-' }}
            </div>
          </NDescriptionsItem>
          <NDescriptionsItem label="过期时间">
            {{ formatDateTime(currentFileDetail.expirationTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="创建时间">
            {{ formatDateTime(currentFileDetail.createdTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="修改时间">
            {{ formatDateTime(currentFileDetail.modifiedTime) }}
          </NDescriptionsItem>
        </NDescriptions>

        <NDescriptions
          v-else-if="detailKind === 'storage' && currentStorageDetail"
          :column="1"
          bordered
          size="small"
        >
          <NDescriptionsItem label="副本主键">
            {{ currentStorageDetail.basicId }}
          </NDescriptionsItem>
          <NDescriptionsItem label="文件主键">
            {{ currentStorageDetail.fileId }}
          </NDescriptionsItem>
          <NDescriptionsItem label="存储类型">
            {{ getOptionLabel(storageTypeOptions, currentStorageDetail.storageType) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="提供商/区域">
            {{ currentStorageDetail.storageProvider || '-' }} / {{ currentStorageDetail.storageRegion || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="存储桶">
            {{ currentStorageDetail.bucketName || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="存储路径">
            <div class="file-detail-content">
              {{ currentStorageDetail.storagePath || '-' }}
            </div>
          </NDescriptionsItem>
          <NDescriptionsItem label="完整路径">
            <div class="file-detail-content">
              {{ currentStorageDetail.fullPath || '-' }}
            </div>
          </NDescriptionsItem>
          <NDescriptionsItem label="访问地址">
            <div class="file-detail-content">
              {{ currentStorageDetail.externalUrl || currentStorageDetail.internalUrl || '-' }}
            </div>
          </NDescriptionsItem>
          <NDescriptionsItem label="CDN 地址">
            <div class="file-detail-content">
              {{ currentStorageDetail.cdnUrl || '-' }}
            </div>
          </NDescriptionsItem>
          <NDescriptionsItem label="状态">
            {{ getOptionLabel(storageStatusOptions, currentStorageDetail.status) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="标记">
            主存储 {{ formatFlag(currentStorageDetail.isPrimary) }}，备份 {{ formatFlag(currentStorageDetail.isBackup) }}，CDN {{ formatFlag(currentStorageDetail.enableCdn) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="验证/同步">
            验证 {{ formatFlag(currentStorageDetail.isVerified) }}，同步 {{ formatFlag(currentStorageDetail.isSynced) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="访问控制">
            {{ currentStorageDetail.accessControl || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="存储类别">
            {{ currentStorageDetail.storageClass || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="缓存控制">
            {{ currentStorageDetail.cacheControl || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="上传耗时">
            {{ formatUploadDuration(currentStorageDetail.uploadDuration) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="失败原因">
            <div class="file-detail-content">
              {{ currentStorageDetail.uploadFailureReason || '-' }}
            </div>
          </NDescriptionsItem>
          <NDescriptionsItem label="备注">
            <div class="file-detail-content">
              {{ currentStorageDetail.remark || '-' }}
            </div>
          </NDescriptionsItem>
          <NDescriptionsItem label="上传时间">
            {{ formatDateTime(currentStorageDetail.uploadedTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="最后验证">
            {{ formatDateTime(currentStorageDetail.lastVerifiedTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="同步时间">
            {{ formatDateTime(currentStorageDetail.syncedTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="创建时间">
            {{ formatDateTime(currentStorageDetail.createdTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="修改时间">
            {{ formatDateTime(currentStorageDetail.modifiedTime) }}
          </NDescriptionsItem>
        </NDescriptions>

        <div v-else class="py-8 text-center text-gray-400">
          暂无详情数据
        </div>
      </NDrawerContent>
    </NDrawer>

    <!-- 存储副本列表抽屉：列出该文件全部副本，支持 详情/设主副本/校验/启停 -->
    <NDrawer v-model:show="storageListVisible" :width="760">
      <NDrawerContent closable :title="`存储副本 - ${storageFile?.originalName ?? ''}`">
        <NSpace vertical :size="12">
          <div class="flex items-center justify-between">
            <span class="text-sm text-foreground/60">共 {{ storageRows.length }} 个副本</span>
            <NButton size="small" :loading="storageListLoading" @click="loadStorageRows">
              <template #icon>
                <NIcon><Icon icon="lucide:refresh-cw" /></NIcon>
              </template>
              刷新
            </NButton>
          </div>
          <NDataTable
            :columns="storageColumns"
            :data="storageRows"
            :loading="storageListLoading"
            :row-key="(row: FileStorageListItemDto) => row.basicId"
            :scroll-x="720"
            size="small"
          />
        </NSpace>
      </NDrawerContent>
    </NDrawer>

    <!-- 文件预览弹窗：仅对浏览器可直接渲染的简单类型开放（图片/音视频/PDF/文本）-->
    <NModal
      v-model:show="previewVisible"
      preset="card"
      :title="`预览 - ${previewName}`"
      :bordered="false"
      style="width: 80vw; max-width: 960px;"
    >
      <div class="file-preview-body">
        <div v-if="previewLoading" class="text-gray-400">
          加载中...
        </div>
        <div v-else-if="!previewUrl" class="text-gray-400">
          无法获取预览地址
        </div>
        <img
          v-else-if="previewKind === 'image'"
          :src="previewUrl"
          :alt="previewName"
          class="file-preview-image"
        >
        <video
          v-else-if="previewKind === 'video'"
          :src="previewUrl"
          controls
          class="file-preview-media"
        />
        <audio
          v-else-if="previewKind === 'audio'"
          :src="previewUrl"
          controls
          class="file-preview-audio"
        />
        <iframe
          v-else-if="previewKind === 'pdf' || previewKind === 'text'"
          :src="previewUrl"
          class="file-preview-frame"
          title="文件预览"
        />
      </div>
    </NModal>
  </SchemaPage>
</template>

<style scoped>
.file-detail-content {
  white-space: pre-wrap;
  word-break: break-word;
}

.file-upload-switches {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
}

.file-preview-body {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 60vh;
  max-height: 72vh;
  overflow: auto;
}

.file-preview-image,
.file-preview-media {
  max-width: 100%;
  max-height: 70vh;
  object-fit: contain;
}

.file-preview-audio {
  width: 100%;
}

.file-preview-frame {
  width: 100%;
  height: 70vh;
  border: none;
}
</style>
