<script setup lang="ts">
import type { DataTableColumns, UploadCustomRequestOptions } from 'naive-ui'
import type {
  FileDetailDto,
  FileListItemDto,
  FileStorageDetailDto,
  FileStorageListItemDto,
  PageResult,
} from '@/api'

import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import hljs from 'highlight.js/lib/common'
import {
  NButton,
  NCode,
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
  NUploadDragger,
  useDialog,
  useMessage,
} from 'naive-ui'
import Papa from 'papaparse'
import { computed, h, nextTick, reactive, ref, watch } from 'vue'
import {
  createPageRequest,
  fileManagementApi,
  FileStatus,
  FileStorageStatus,
  FileStorageType,
  FileType,
  ResourceAccessLevel,
} from '@/api'
import { Icon, SchemaPage, XMdEditor } from '~/components'
import { islandStart } from '~/composables/useDynamicIsland'
import { downloadBlob, formatDate, formatFileSize, getOptionLabel } from '~/utils'

defineOptions({ name: 'PlatformFilePage' })

type DetailKind = 'file' | 'storage'
type TagType = 'default' | 'error' | 'info' | 'success' | 'warning'

const message = useMessage()
const dialog = useDialog()

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

// ── 文件预览类型判定（仅零依赖、浏览器可直接渲染的类型）─────────
type PreviewKind = 'image' | 'video' | 'audio' | 'markdown' | 'text' | 'csv'

const PREVIEW_IMAGE_EXTENSIONS = new Set(['jpg', 'jpeg', 'png', 'gif', 'webp', 'svg', 'bmp', 'ico'])
const PREVIEW_VIDEO_EXTENSIONS = new Set(['mp4', 'webm', 'ogv'])
const PREVIEW_AUDIO_EXTENSIONS = new Set(['mp3', 'wav', 'ogg'])
const PREVIEW_MARKDOWN_EXTENSIONS = new Set(['md', 'markdown'])

/** 文本/源码/配置/数据交换扩展名 → highlight.js 语言名（空串表示自动识别） */
const PREVIEW_TEXT_LANG: Record<string, string> = {
  txt: '',
  log: '',
  json: 'json',
  xml: 'xml',
  yaml: 'yaml',
  yml: 'yaml',
  toml: 'ini',
  ini: 'ini',
  conf: 'ini',
  env: 'ini',
  html: 'html',
  htm: 'html',
  css: 'css',
  js: 'javascript',
  mjs: 'javascript',
  cjs: 'javascript',
  jsx: 'javascript',
  ts: 'typescript',
  tsx: 'typescript',
  vue: 'html',
  cs: 'csharp',
  java: 'java',
  go: 'go',
  py: 'python',
  php: 'php',
  sql: 'sql',
}

function getFileExt(row: FileListItemDto): string {
  return (row.fileExtension ?? '').replace(/^\./, '').toLowerCase()
}

/** 仅当文件为本轮零依赖可渲染类型时返回其预览种类，否则返回 null */
function getPreviewKind(row: FileListItemDto): PreviewKind | null {
  const mime = (row.mimeType ?? '').toLowerCase()
  const ext = getFileExt(row)
  if (mime.startsWith('image/') || PREVIEW_IMAGE_EXTENSIONS.has(ext) || row.fileType === FileType.Image) {
    return 'image'
  }
  if (mime.startsWith('video/') || PREVIEW_VIDEO_EXTENSIONS.has(ext) || row.fileType === FileType.Video) {
    return 'video'
  }
  if (mime.startsWith('audio/') || PREVIEW_AUDIO_EXTENSIONS.has(ext) || row.fileType === FileType.Audio) {
    return 'audio'
  }
  if (PREVIEW_MARKDOWN_EXTENSIONS.has(ext)) {
    return 'markdown'
  }
  if (ext === 'csv') {
    return 'csv'
  }
  if (mime.startsWith('text/') || PREVIEW_TEXT_LANG[ext] !== undefined) {
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
    { key: 'download', title: '下载', scope: 'row', visible: row => (row as unknown as FileListItemDto).status === FileStatus.Normal },
    { key: 'view', title: '查看详情', scope: 'row' },
    { key: 'metadata', title: '编辑元数据', scope: 'row' },
    { key: 'storages', title: '存储副本', scope: 'row' },
    // 回收站语义：正常文件可「归档」（软删，可恢复）；非正常文件可「恢复」；任意状态可「彻底删除」（物理删，不可恢复）
    { key: 'archive', title: '归档', scope: 'row', visible: row => (row as unknown as FileListItemDto).status === FileStatus.Normal },
    { key: 'restore', title: '恢复', scope: 'row', visible: row => (row as unknown as FileListItemDto).status !== FileStatus.Normal },
    { key: 'destroy', title: '彻底删除', scope: 'row', type: 'error' },
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
    case 'download':
      if (row) {
        void handleDownload(row)
      }
      break
    case 'view':
      if (row) {
        void handleFileDetail(row)
      }
      break
    case 'metadata':
      if (row) {
        void openMetadataDrawer(row)
      }
      break
    case 'storages':
      if (row) {
        void openStorageList(row)
      }
      break
    case 'archive':
      if (row) {
        void handleUpdateFileStatus(row, FileStatus.Archived, '已归档')
      }
      break
    case 'restore':
      if (row) {
        void handleUpdateFileStatus(row, FileStatus.Normal, '已恢复')
      }
      break
    case 'destroy':
      if (row) {
        handleDestroyFile(row)
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

/**
 * 尺寸/时长智能文案：图片显「宽 × 高」、音视频显时长；文档等无此信息时返回空（详情中整行隐藏，不再显示「- x - / -」）。
 */
const dimensionText = computed(() => {
  const detail = currentFileDetail.value
  if (!detail) {
    return ''
  }
  const parts: string[] = []
  if (detail.width || detail.height) {
    parts.push(`${detail.width ?? '?'} × ${detail.height ?? '?'}`)
  }
  if (detail.duration) {
    parts.push(formatDuration(detail.duration))
  }
  return parts.join(' / ')
})

// ── 上传抽屉 ────────────────────────────────────────────────────
const uploadVisible = ref(false)
const uploadLoading = ref(false)
// 上传表单仅保留与用户相关的字段；存储供应商/桶/目录/访问控制等由后端默认存储配置决定，不再让用户填写
const uploadForm = reactive({
  accessLevel: ResourceAccessLevel.Authorized,
  isEncrypted: false,
  isTemporary: false,
  overwrite: false,
  remark: '',
  // 临时文件保留天数（开启「临时」时生效；后端要求临时文件必须带过期/保留天数，否则校验失败）
  retentionDays: 7,
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
  // 每次打开重置为默认，避免上次的开关/备注残留
  uploadForm.accessLevel = ResourceAccessLevel.Authorized
  uploadForm.isEncrypted = false
  uploadForm.isTemporary = false
  uploadForm.overwrite = false
  uploadForm.remark = ''
  uploadForm.retentionDays = 7
  uploadForm.tags = ''
  uploadVisible.value = true
}

async function openMetadataDrawer(row: FileListItemDto) {
  metadataRow.value = row
  // 先用列表值快速回填（列表 DTO 无 tags/remark/访问权限）
  metadataForm.accessLevel = row.accessLevel
  metadataForm.accessPermissions = ''
  metadataForm.isEncrypted = row.isEncrypted
  metadataForm.isTemporary = row.isTemporary
  metadataForm.retentionDays = row.retentionDays || 7
  metadataForm.tags = ''
  metadataForm.remark = ''
  metadataVisible.value = true
  // 取详情回填标签/备注/访问权限，避免编辑时看不到原值、保存被清空
  try {
    const detail = await fileManagementApi.detail(row.basicId)
    if (detail && metadataRow.value?.basicId === row.basicId) {
      metadataForm.accessPermissions = detail.accessPermissions ?? ''
      metadataForm.tags = detail.tags ?? ''
      metadataForm.remark = detail.remark ?? ''
      if (detail.retentionDays) {
        metadataForm.retentionDays = detail.retentionDays
      }
    }
  }
  catch {
    // 取详情失败：保持列表值回填，不阻断编辑
  }
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
      // 临时文件保留天数兜底 ≥1，满足后端校验；非临时按原值
      retentionDays: metadataForm.isTemporary ? Math.max(1, metadataForm.retentionDays) : metadataForm.retentionDays,
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

async function handleUpdateFileStatus(row: FileListItemDto, status: FileStatus, successText = '文件状态已更新') {
  actionLoading.value = true
  try {
    await fileManagementApi.updateStatus({ basicId: row.basicId, status })
    message.success(successText)
    reload()
  }
  catch {
    message.error('操作失败')
  }
  finally {
    actionLoading.value = false
  }
}

/** 彻底删除：物理删除记录 + 物理文件，不可恢复，强二次确认 */
function handleDestroyFile(row: FileListItemDto) {
  dialog.error({
    title: '彻底删除',
    content: `将永久删除「${row.originalName}」的记录、所有存储副本及物理文件，此操作不可恢复。确定继续？`,
    positiveText: '彻底删除',
    negativeText: '取消',
    onPositiveClick: async () => {
      actionLoading.value = true
      try {
        await fileManagementApi.destroy({ basicId: row.basicId, deletePhysical: true })
        message.success('已彻底删除')
        reload()
      }
      catch {
        message.error('彻底删除失败')
      }
      finally {
        actionLoading.value = false
      }
    },
  })
}

async function handleUploadRequest(options: UploadCustomRequestOptions) {
  const rawFile = options.file.file
  if (!rawFile) {
    options.onError()
    return
  }
  uploadLoading.value = true
  // 接入灵动岛：上传作为持续任务呈现进度，完成/失败进终态（灵动岛关闭时自动降级为消息提示）
  // 折叠态文案精简为「正在上传」，文件名放 detail（展开/悬停可见），避免长文件名把胶囊撑宽
  const task = islandStart('file:upload', '正在上传', { detail: rawFile.name, icon: 'lucide:upload', progress: 0 })
  // 弹窗即时关闭，上传进度交由灵动岛跟踪，不阻塞用户继续操作
  uploadVisible.value = false
  try {
    await fileManagementApi.upload(
      {
        accessLevel: uploadForm.accessLevel,
        file: rawFile,
        isEncrypted: uploadForm.isEncrypted,
        isTemporary: uploadForm.isTemporary,
        overwrite: uploadForm.overwrite,
        remark: normalizeNullable(uploadForm.remark),
        // 仅临时文件传保留天数（≥1），普通文件传 0；满足后端临时文件必须带保留期的校验
        retentionDays: uploadForm.isTemporary ? Math.max(1, uploadForm.retentionDays) : 0,
        tags: normalizeNullable(uploadForm.tags),
      },
      percent => task.setProgress(percent),
    )
    options.onProgress({ percent: 100 })
    options.onFinish()
    task.success('上传成功', { detail: rawFile.name })
    await nextTick()
    reload()
  }
  catch {
    options.onError()
    task.error('上传失败', { detail: rawFile.name })
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
/** 文本预览上限 2MB，超出仅提示下载，避免大文件拖垮浏览器 */
const PREVIEW_TEXT_MAX_BYTES = 2 * 1024 * 1024

const previewVisible = ref(false)
const previewLoading = ref(false)
const previewUrl = ref<string>('')
const previewKind = ref<PreviewKind | null>(null)
const previewName = ref<string>('')
const previewText = ref<string>('')
const previewLang = ref<string>('')
const previewTextError = ref<string>('')
const csvColumns = ref<DataTableColumns>([])
const csvData = ref<Record<string, string>[]>([])

/** 媒体 blob 的对象 URL，需在切换/关闭时手动释放，避免内存泄漏 */
let previewObjectUrl = ''

/** 非媒体类型（文本/代码/CSV）用块级流布局，媒体类型用居中 */
const isBlockPreview = computed(() =>
  !!previewKind.value && !['image', 'video', 'audio'].includes(previewKind.value),
)

/** 复制当前预览的原始文本到剪贴板 */
async function copyPreviewText() {
  try {
    await navigator.clipboard.writeText(previewText.value)
    message.success('已复制到剪贴板')
  }
  catch {
    message.error('复制失败')
  }
}

function revokePreviewObjectUrl() {
  if (previewObjectUrl) {
    URL.revokeObjectURL(previewObjectUrl)
    previewObjectUrl = ''
  }
}

/** 经鉴权下载接口取文件内容（axios 自动带 token），按种类在弹窗中预览 */
async function handlePreview(row: FileListItemDto) {
  const kind = getPreviewKind(row)
  previewKind.value = kind
  previewName.value = row.originalName
  previewLang.value = PREVIEW_TEXT_LANG[getFileExt(row)] ?? ''
  revokePreviewObjectUrl()
  previewUrl.value = ''
  previewText.value = ''
  previewTextError.value = ''
  csvColumns.value = []
  csvData.value = []
  previewVisible.value = true
  previewLoading.value = true
  try {
    if ((kind === 'markdown' || kind === 'text') && row.fileSize > PREVIEW_TEXT_MAX_BYTES) {
      previewTextError.value = '文件较大，暂不支持在线文本预览，请下载查看'
      return
    }
    const blob = await fileManagementApi.download(row.basicId)
    if (kind === 'markdown' || kind === 'text') {
      previewText.value = await blob.text()
    }
    else if (kind === 'csv') {
      parseCsv(await blob.text())
    }
    else {
      // 媒体（图片/音视频）：后端下载流为 application/octet-stream，用文件真实 MIME 重建
      // blob 后生成对象 URL（媒体亦按内容解码，重建仅为标注正确类型）。
      const typedBlob = row.mimeType ? new Blob([blob], { type: row.mimeType }) : blob
      previewObjectUrl = URL.createObjectURL(typedBlob)
      previewUrl.value = previewObjectUrl
    }
  }
  catch (error) {
    const reason = (error as Error).message || '加载预览失败'
    if (kind === 'markdown' || kind === 'text') {
      previewTextError.value = reason
    }
    message.error(reason)
  }
  finally {
    previewLoading.value = false
  }
}

/** 解析 CSV 文本为 NDataTable 的列与行（首行作表头） */
function parseCsv(text: string) {
  const parsed = Papa.parse<string[]>(text, { skipEmptyLines: true })
  const rows = parsed.data
  if (!rows.length) {
    return
  }
  const header = rows[0] ?? []
  csvColumns.value = header.map((title, index) => ({
    title: title || `列${index + 1}`,
    key: String(index),
    resizable: true,
    ellipsis: { tooltip: true },
    minWidth: 100,
  }))
  csvData.value = rows.slice(1).map(row =>
    Object.fromEntries(header.map((_, index) => [String(index), row[index] ?? ''])),
  )
}

watch(previewVisible, (visible) => {
  if (!visible) {
    revokePreviewObjectUrl()
  }
})

/** 经鉴权下载接口取文件流，交由通用下载器触发浏览器下载 */
async function handleDownload(row: FileListItemDto) {
  try {
    const blob = await fileManagementApi.download(row.basicId)
    downloadBlob(blob, row.originalName)
  }
  catch (error) {
    message.error((error as Error).message || '下载失败')
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
    <NModal
      v-model:show="uploadVisible"
      preset="card"
      title="上传文件"
      :bordered="false"
      :mask-closable="!uploadLoading"
      :closable="!uploadLoading"
      style="width: 520px; max-width: calc(100vw - 32px)"
    >
      <NSpace vertical :size="16">
        <!-- 拖拽区：点击或拖入文件即上传（按当前默认存储配置保存） -->
        <NUpload
          :custom-request="handleUploadRequest"
          :disabled="uploadLoading"
          :show-file-list="false"
          :multiple="false"
        >
          <NUploadDragger>
            <div class="file-upload-dragger">
              <NIcon :size="38" :depth="3">
                <Icon icon="lucide:cloud-upload" />
              </NIcon>
              <div class="file-upload-dragger__text">
                {{ uploadLoading ? '上传中…' : '点击或拖拽文件到此处上传' }}
              </div>
              <div class="file-upload-dragger__hint">
                文件将按当前默认存储配置保存
              </div>
            </div>
          </NUploadDragger>
        </NUpload>

        <!-- 访问级别 -->
        <div class="file-upload-field">
          <span class="file-upload-field__label">访问级别</span>
          <NSelect
            v-model:value="uploadForm.accessLevel"
            :options="accessLevelOptions"
            :disabled="uploadLoading"
            placeholder="访问级别"
          />
        </div>

        <!-- 开关：覆盖 / 加密 / 临时 -->
        <div class="file-upload-switches">
          <div class="file-upload-switch">
            <span>覆盖同名</span>
            <NSwitch v-model:value="uploadForm.overwrite" :disabled="uploadLoading" />
          </div>
          <div class="file-upload-switch">
            <span>加密</span>
            <NSwitch v-model:value="uploadForm.isEncrypted" :disabled="uploadLoading" />
          </div>
          <div class="file-upload-switch">
            <span>临时</span>
            <NSwitch v-model:value="uploadForm.isTemporary" :disabled="uploadLoading" />
          </div>
        </div>

        <!-- 保留天数：仅临时文件需要 -->
        <div v-if="uploadForm.isTemporary" class="file-upload-field">
          <span class="file-upload-field__label">保留天数</span>
          <NInputNumber
            v-model:value="uploadForm.retentionDays"
            :min="1"
            :disabled="uploadLoading"
            placeholder="临时文件到期后自动清理"
            style="width: 100%"
          />
        </div>

        <NInput v-model:value="uploadForm.tags" clearable :disabled="uploadLoading" placeholder="标签（可选）" />
        <NInput v-model:value="uploadForm.remark" clearable :disabled="uploadLoading" placeholder="备注（可选）" type="textarea" :rows="2" />
      </NSpace>
    </NModal>

    <NDrawer v-model:show="metadataVisible" :width="460">
      <NDrawerContent closable title="编辑文件元数据">
        <NSpace vertical :size="16">
          <div class="file-upload-field">
            <span class="file-upload-field__label">访问级别</span>
            <NSelect
              v-model:value="metadataForm.accessLevel"
              :options="accessLevelOptions"
              placeholder="访问级别"
            />
          </div>

          <div class="file-upload-switches">
            <div class="file-upload-switch">
              <span>加密</span>
              <NSwitch v-model:value="metadataForm.isEncrypted" />
            </div>
            <div class="file-upload-switch">
              <span>临时</span>
              <NSwitch v-model:value="metadataForm.isTemporary" />
            </div>
          </div>

          <div v-if="metadataForm.isTemporary" class="file-upload-field">
            <span class="file-upload-field__label">保留天数</span>
            <NInputNumber
              v-model:value="metadataForm.retentionDays"
              :min="1"
              placeholder="临时文件到期后自动清理"
              style="width: 100%"
            />
          </div>

          <NInput v-model:value="metadataForm.tags" clearable placeholder="标签（可选）" />
          <NInput v-model:value="metadataForm.remark" clearable placeholder="备注（可选）" type="textarea" :rows="2" />

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
          :column="2"
          label-placement="left"
          bordered
          size="small"
        >
          <NDescriptionsItem label="原始文件名" :span="2">
            {{ currentFileDetail.originalName }}
          </NDescriptionsItem>
          <NDescriptionsItem label="文件类型">
            {{ getOptionLabel(fileTypeOptions, currentFileDetail.fileType) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="文件大小">
            {{ formatFileSize(Number(currentFileDetail.fileSize || 0)) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="MIME">
            {{ currentFileDetail.mimeType || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem v-if="dimensionText" label="尺寸/时长">
            {{ dimensionText }}
          </NDescriptionsItem>
          <NDescriptionsItem label="访问级别">
            {{ getOptionLabel(accessLevelOptions, currentFileDetail.accessLevel) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="状态">
            {{ getOptionLabel(fileStatusOptions, currentFileDetail.status) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="加密">
            {{ formatFlag(currentFileDetail.isEncrypted) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="临时">
            {{ formatFlag(currentFileDetail.isTemporary) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="下载次数">
            {{ currentFileDetail.downloadCount }}
          </NDescriptionsItem>
          <NDescriptionsItem label="访问次数">
            {{ currentFileDetail.viewCount }}
          </NDescriptionsItem>
          <NDescriptionsItem label="创建时间">
            {{ formatDateTime(currentFileDetail.createdTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="修改时间">
            {{ formatDateTime(currentFileDetail.modifiedTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem v-if="currentFileDetail.isTemporary" label="过期时间" :span="2">
            {{ formatDateTime(currentFileDetail.expirationTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem v-if="currentFileDetail.tags" label="标签" :span="2">
            {{ currentFileDetail.tags }}
          </NDescriptionsItem>
          <NDescriptionsItem v-if="currentFileDetail.remark" label="备注" :span="2">
            <div class="file-detail-content">
              {{ currentFileDetail.remark }}
            </div>
          </NDescriptionsItem>
          <NDescriptionsItem v-if="currentFileDetail.accessPermissions" label="访问权限" :span="2">
            <div class="file-detail-content">
              {{ currentFileDetail.accessPermissions }}
            </div>
          </NDescriptionsItem>
          <NDescriptionsItem label="上传来源" :span="2">
            {{ currentFileDetail.uploadIp || '-' }} / {{ currentFileDetail.uploadSource || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="存储文件名" :span="2">
            {{ currentFileDetail.fileName }}
          </NDescriptionsItem>
          <NDescriptionsItem label="文件哈希" :span="2">
            <div class="file-detail-content">
              {{ currentFileDetail.fileHash || '-' }}
            </div>
          </NDescriptionsItem>
          <NDescriptionsItem label="文件主键" :span="2">
            {{ currentFileDetail.basicId }}
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

    <!-- 文件预览弹窗：仅零依赖、浏览器可直接渲染的类型（图片/音视频/Markdown/文本与源码高亮/CSV） -->
    <NModal
      v-model:show="previewVisible"
      preset="card"
      :title="`预览 - ${previewName}`"
      :bordered="false"
      style="width: 80vw;"
    >
      <div
        class="file-preview-body"
        :class="{ 'is-text': isBlockPreview }"
      >
        <div v-if="previewLoading" class="text-gray-400">
          加载中...
        </div>
        <div v-else-if="previewTextError" class="text-gray-400">
          {{ previewTextError }}
        </div>
        <XMdEditor
          v-else-if="previewKind === 'markdown'"
          preview-only
          :model-value="previewText"
          class="file-preview-md"
        />
        <div v-else-if="previewKind === 'text'" class="file-preview-code-wrap">
          <button type="button" class="file-preview-copy" @click="copyPreviewText">
            复制代码
          </button>
          <div class="file-preview-code-scroll">
            <NCode
              :code="previewText"
              :language="previewLang || undefined"
              :hljs="hljs"
              show-line-numbers
            />
          </div>
        </div>
        <NDataTable
          v-else-if="previewKind === 'csv'"
          :columns="csvColumns"
          :data="csvData"
          :scroll-x="Math.max(csvColumns.length * 120, 600)"
          max-height="66vh"
          size="small"
          class="file-preview-csv"
        />
        <div v-else-if="!previewUrl" class="text-gray-400">
          无法获取预览内容
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
  gap: 20px;
}

.file-upload-switch {
  display: flex;
  gap: 8px;
  align-items: center;
  font-size: 13px;
  color: var(--n-text-color, inherit);
}

.file-upload-dragger {
  display: flex;
  flex-direction: column;
  gap: 6px;
  align-items: center;
  padding: 16px 0;
}

.file-upload-dragger__text {
  font-size: 14px;
  font-weight: 500;
  color: var(--n-text-color, inherit);
}

.file-upload-dragger__hint {
  font-size: 12px;
  color: var(--text-secondary, rgb(140 145 150));
}

.file-upload-field {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.file-upload-field__label {
  font-size: 13px;
  color: var(--text-secondary, rgb(118 124 130));
}

.file-preview-body {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 60vh;
  max-height: 72vh;
  overflow: auto;
}

/* 文本/Markdown/代码：用块级流 + 滚动，避免 flex 居中布局压塌内容 */
.file-preview-body.is-text {
  display: block;
  text-align: left;
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

.file-preview-md {
  width: 100%;
}

.file-preview-csv {
  width: 100%;
}

.file-preview-code-wrap {
  position: relative;
  width: 100%;
}

.file-preview-copy {
  position: absolute;
  top: 8px;
  right: 8px;
  z-index: 1;
  padding: 2px 10px;
  font-size: 12px;
  color: #374151;
  cursor: pointer;
  background: #fff;
  border: 1px solid #d1d5db;
  border-radius: 4px;
}

.file-preview-copy:hover {
  background: #f3f4f6;
}

.file-preview-code-scroll {
  max-height: 70vh;
  padding: 12px 16px;
  overflow: auto;
  font-size: 13px;
  border: 1px solid rgba(128, 128, 128, 0.2);
  border-radius: 6px;
}
</style>
