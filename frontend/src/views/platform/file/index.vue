<script setup lang="ts">
import type { DataTableColumns, UploadCustomRequestOptions } from 'naive-ui'
import type {
  ApiId,
  FileDetailDto,
  FileListItemDto,
  FileStorageDetailDto,
  FileStorageListItemDto,
} from '@/api'
import {
  NButton,
  NCard,
  NDataTable,
  NDescriptions,
  NDescriptionsItem,
  NDrawer,
  NDrawerContent,
  NIcon,
  NInput,
  NInputNumber,
  NPagination,
  NPopconfirm,
  NSelect,
  NSpace,
  NSwitch,
  NTag,
  NUpload,
  useMessage,
} from 'naive-ui'
import { computed, h, nextTick, onMounted, reactive, ref } from 'vue'
import {
  createPageRequest,
  fileManagementApi,
  FileStatus,
  FileStorageStatus,
  FileStorageType,
  FileType,
  ResourceAccessLevel,
} from '@/api'
import { Icon } from '~/components'
import { formatDate, formatFileSize, getOptionLabel } from '~/utils'

defineOptions({ name: 'PlatformFilePage' })

type FileTab = 'file' | 'storage'
type DetailKind = 'file' | 'storage'
type TagType = 'default' | 'error' | 'info' | 'success' | 'warning'

const message = useMessage()
const activeTab = ref<FileTab>('file')
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailKind = ref<DetailKind>('file')
const currentFileDetail = ref<FileDetailDto | null>(null)
const currentStorageDetail = ref<FileStorageDetailDto | null>(null)
const uploadVisible = ref(false)
const uploadLoading = ref(false)
const metadataVisible = ref(false)
const metadataLoading = ref(false)
const actionLoading = ref(false)

const fileLoading = ref(false)
const fileList = ref<FileListItemDto[]>([])
const fileTotal = ref(0)
const filePage = ref(1)
const filePageSize = ref(20)

const storageLoading = ref(false)
const storageList = ref<FileStorageListItemDto[]>([])
const storageTotal = ref(0)
const storagePage = ref(1)
const storagePageSize = ref(20)

const fileQuery = reactive({
  accessLevel: null as ResourceAccessLevel | null,
  fileExtension: '',
  fileType: null as FileType | null,
  isEncrypted: undefined as number | undefined,
  isTemporary: undefined as number | undefined,
  keyword: '',
  mimeType: '',
  status: null as FileStatus | null,
})

const storageQuery = reactive({
  fileId: null as ApiId | null,
  isPrimary: undefined as number | undefined,
  isSynced: undefined as number | undefined,
  isVerified: undefined as number | undefined,
  keyword: '',
  status: null as FileStorageStatus | null,
  storageType: null as FileStorageType | null,
})

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

const metadataForm = reactive({
  accessLevel: ResourceAccessLevel.Authorized as ResourceAccessLevel,
  accessPermissions: '',
  isEncrypted: false,
  isTemporary: false,
  remark: '',
  retentionDays: 0,
  tags: '',
})

const tabOptions = [
  { label: '文件列表', value: 'file' },
  { label: '存储副本', value: 'storage' },
]

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

const detailTitle = computed(() => detailKind.value === 'file' ? '文件详情' : '存储副本详情')

function normalizeNullable(value?: string | null) {
  const normalized = value?.trim()
  return normalized || null
}

function normalizeId(value: ApiId | null) {
  return value || null
}

function toOptionalBoolean(value: number | undefined) {
  if (value === undefined) {
    return undefined
  }

  return value === 1
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

  if (status === FileStorageStatus.Uploading || status === FileStorageStatus.Syncing || status === FileStorageStatus.PendingVerification) {
    return 'warning'
  }

  return 'default'
}

async function fetchFileData() {
  fileLoading.value = true
  try {
    const result = await fileManagementApi.page({
      ...createPageRequest({
        page: {
          pageIndex: filePage.value,
          pageSize: filePageSize.value,
        },
      }),
      accessLevel: fileQuery.accessLevel,
      fileExtension: normalizeNullable(fileQuery.fileExtension),
      fileType: fileQuery.fileType,
      isEncrypted: toOptionalBoolean(fileQuery.isEncrypted),
      isTemporary: toOptionalBoolean(fileQuery.isTemporary),
      keyword: normalizeNullable(fileQuery.keyword),
      mimeType: normalizeNullable(fileQuery.mimeType),
      status: fileQuery.status,
    })
    fileList.value = result.items
    fileTotal.value = result.page.totalCount
  }
  catch {
    message.error('查询文件列表失败')
    fileList.value = []
    fileTotal.value = 0
  }
  finally {
    fileLoading.value = false
  }
}

async function fetchStorageData() {
  storageLoading.value = true
  try {
    const result = await fileManagementApi.storagePage({
      ...createPageRequest({
        page: {
          pageIndex: storagePage.value,
          pageSize: storagePageSize.value,
        },
      }),
      fileId: normalizeId(storageQuery.fileId),
      isPrimary: toOptionalBoolean(storageQuery.isPrimary),
      isSynced: toOptionalBoolean(storageQuery.isSynced),
      isVerified: toOptionalBoolean(storageQuery.isVerified),
      keyword: normalizeNullable(storageQuery.keyword),
      status: storageQuery.status,
      storageType: storageQuery.storageType,
    })
    storageList.value = result.items
    storageTotal.value = result.page.totalCount
  }
  catch {
    message.error('查询存储副本失败')
    storageList.value = []
    storageTotal.value = 0
  }
  finally {
    storageLoading.value = false
  }
}

const fileColumns = computed<DataTableColumns<FileListItemDto>>(() => [
  {
    key: 'originalName',
    title: '原始文件名',
    minWidth: 220,
    ellipsis: { tooltip: true },
    fixed: 'left',
  },
  {
    key: 'fileName',
    title: '存储文件名',
    minWidth: 220,
    ellipsis: { tooltip: true },
  },
  {
    key: 'fileType',
    title: '文件类型',
    width: 110,
    render: row =>
      h(NTag, { round: true, size: 'small' }, () => getOptionLabel(fileTypeOptions, row.fileType)),
  },
  {
    key: 'fileSize',
    title: '文件大小',
    minWidth: 110,
    render: row => formatFileSize(Number(row.fileSize || 0)),
  },
  {
    key: 'status',
    title: '状态',
    width: 110,
    render: row =>
      h(NTag, { type: getFileStatusTagType(row.status), round: true, size: 'small' }, () => getOptionLabel(fileStatusOptions, row.status)),
  },
  {
    key: 'accessLevel',
    title: '访问级别',
    minWidth: 110,
    render: row => getOptionLabel(accessLevelOptions, row.accessLevel),
  },
  {
    key: 'fileExtension',
    title: '扩展名',
    minWidth: 90,
  },
  {
    key: 'mimeType',
    title: 'MIME',
    minWidth: 160,
    ellipsis: { tooltip: true },
  },
  {
    key: 'isTemporary',
    title: '临时',
    width: 82,
    render: row =>
      h(NTag, { type: row.isTemporary ? 'warning' : 'default', round: true, size: 'small' }, () => row.isTemporary ? '是' : '否'),
  },
  {
    key: 'isEncrypted',
    title: '加密',
    width: 82,
    render: row =>
      h(NTag, { type: row.isEncrypted ? 'info' : 'default', round: true, size: 'small' }, () => row.isEncrypted ? '是' : '否'),
  },
  {
    key: 'downloadCount',
    title: '下载',
    minWidth: 90,
  },
  {
    key: 'viewCount',
    title: '访问',
    minWidth: 90,
  },
  {
    key: 'createdTime',
    title: '创建时间',
    minWidth: 170,
    sorter: true,
    render: row => formatDateTime(row.createdTime),
  },
  {
    key: 'actions',
    title: '操作',
    width: 200,
    render: row =>
      h(NSpace, { size: 4 }, () => [
        h(NButton, { ariaLabel: '详情', circle: true, quaternary: true, size: 'small', type: 'primary', onClick: () => handleFileDetail(row) }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:eye' })) }),
        h(NButton, { ariaLabel: '元数据', circle: true, quaternary: true, size: 'small', onClick: () => openMetadataDrawer(row) }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:pencil' })) }),
        h(NButton, { ariaLabel: '存储副本', circle: true, quaternary: true, size: 'small', onClick: () => handleFileStorages(row) }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:database' })) }),
        row.status !== FileStatus.Archived
          ? h(NButton, { ariaLabel: '归档', circle: true, quaternary: true, size: 'small', type: 'warning', onClick: () => handleUpdateFileStatus(row, FileStatus.Archived) }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:archive' })) })
          : null,
        row.status !== FileStatus.Deleted
          ? h(NPopconfirm, { onPositiveClick: () => handleDeleteFile(row) }, {
              trigger: () => h(NButton, { ariaLabel: '删除', circle: true, quaternary: true, size: 'small', type: 'error', loading: actionLoading.value }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:trash-2' })) }),
              default: () => '确定删除该文件？',
            })
          : null,
      ]),
  },
])

const storageColumns = computed<DataTableColumns<FileStorageListItemDto>>(() => [
  {
    key: 'fileId',
    title: '文件主键',
    minWidth: 140,
    fixed: 'left',
  },
  {
    key: 'storageProvider',
    title: '提供商',
    minWidth: 130,
    ellipsis: { tooltip: true },
  },
  {
    key: 'storageType',
    title: '存储类型',
    minWidth: 120,
    render: row => getOptionLabel(storageTypeOptions, row.storageType),
  },
  {
    key: 'status',
    title: '状态',
    width: 110,
    render: row =>
      h(NTag, { type: getStorageStatusTagType(row.status), round: true, size: 'small' }, () => getOptionLabel(storageStatusOptions, row.status)),
  },
  {
    key: 'isPrimary',
    title: '主存储',
    width: 92,
    render: row =>
      h(NTag, { type: row.isPrimary ? 'success' : 'default', round: true, size: 'small' }, () => row.isPrimary ? '是' : '否'),
  },
  {
    key: 'isBackup',
    title: '备份',
    width: 82,
    render: row =>
      h(NTag, { type: row.isBackup ? 'info' : 'default', round: true, size: 'small' }, () => row.isBackup ? '是' : '否'),
  },
  {
    key: 'isVerified',
    title: '已验证',
    width: 92,
    render: row =>
      h(NTag, { type: row.isVerified ? 'success' : 'warning', round: true, size: 'small' }, () => row.isVerified ? '是' : '否'),
  },
  {
    key: 'isSynced',
    title: '已同步',
    width: 92,
    render: row =>
      h(NTag, { type: row.isSynced ? 'success' : 'default', round: true, size: 'small' }, () => row.isSynced ? '是' : '否'),
  },
  {
    key: 'accessControl',
    title: '访问控制',
    minWidth: 120,
    ellipsis: { tooltip: true },
  },
  {
    key: 'uploadedAt',
    title: '上传时间',
    minWidth: 170,
    render: row => formatDateTime(row.uploadedAt),
  },
  {
    key: 'createdTime',
    title: '创建时间',
    minWidth: 170,
    sorter: true,
    render: row => formatDateTime(row.createdTime),
  },
  {
    key: 'actions',
    title: '操作',
    width: 132,
    render: row =>
      h(NSpace, { size: 4 }, () => [
        h(NButton, { ariaLabel: '详情', circle: true, quaternary: true, size: 'small', type: 'primary', onClick: () => handleStorageDetail(row) }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:eye' })) }),
        h(NButton, { ariaLabel: '校验', circle: true, quaternary: true, size: 'small', onClick: () => handleVerifyStorage(row) }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:shield-check' })) }),
        h(NButton, { ariaLabel: '设为主存储', circle: true, disabled: row.isPrimary, quaternary: true, size: 'small', type: 'primary', onClick: () => handleSwitchPrimary(row) }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:star' })) }),
      ]),
  },
])

const fileTotalPages = computed(() => Math.max(1, Math.ceil(fileTotal.value / filePageSize.value)))

function handleFilePageChange(page: number) {
  filePage.value = page
  fetchFileData()
}

function handleFilePageSizeChange(pageSize: number) {
  filePageSize.value = pageSize
  filePage.value = 1
  fetchFileData()
}

const storageTotalPages = computed(() => Math.max(1, Math.ceil(storageTotal.value / storagePageSize.value)))

function handleStoragePageChange(page: number) {
  storagePage.value = page
  fetchStorageData()
}

function handleStoragePageSizeChange(pageSize: number) {
  storagePageSize.value = pageSize
  storagePage.value = 1
  fetchStorageData()
}

function reloadActiveData() {
  if (activeTab.value === 'file') {
    filePage.value = 1
    fetchFileData()
    return
  }

  storagePage.value = 1
  fetchStorageData()
}

function handleSearch() {
  reloadActiveData()
}

function handleTabChanged() {
  reloadActiveData()
}

function handleReset() {
  if (activeTab.value === 'file') {
    fileQuery.accessLevel = null
    fileQuery.fileExtension = ''
    fileQuery.fileType = null
    fileQuery.isEncrypted = undefined
    fileQuery.isTemporary = undefined
    fileQuery.keyword = ''
    fileQuery.mimeType = ''
    fileQuery.status = null
    filePage.value = 1
    fetchFileData()
  }
  else {
    storageQuery.fileId = null
    storageQuery.isPrimary = undefined
    storageQuery.isSynced = undefined
    storageQuery.isVerified = undefined
    storageQuery.keyword = ''
    storageQuery.status = null
    storageQuery.storageType = null
    storagePage.value = 1
    fetchStorageData()
  }
}

function openUploadDrawer() {
  uploadVisible.value = true
}

function openMetadataDrawer(row: FileListItemDto) {
  metadataForm.accessLevel = row.accessLevel
  metadataForm.accessPermissions = ''
  metadataForm.isEncrypted = row.isEncrypted
  metadataForm.isTemporary = row.isTemporary
  metadataForm.retentionDays = row.retentionDays
  metadataForm.tags = ''
  metadataForm.remark = ''
  metadataVisible.value = true
  ;(metadataForm as any)._row = row
}

async function handleSaveMetadata() {
  const row = (metadataForm as any)._row as FileListItemDto | undefined
  if (!row) return
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
    reloadActiveData()
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
    await fileManagementApi.updateStatus({
      basicId: row.basicId,
      status,
    })
    message.success('文件状态已更新')
    reloadActiveData()
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
    await fileManagementApi.updateStatus({
      basicId: row.basicId,
      status: FileStatus.Deleted,
    })
    message.success('文件已删除')
    reloadActiveData()
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
    activeTab.value = 'file'
    await nextTick()
    filePage.value = 1
    fetchFileData()
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

async function handleStorageDetail(row: FileStorageListItemDto) {
  detailKind.value = 'storage'
  detailVisible.value = true
  detailLoading.value = true
  currentFileDetail.value = null

  try {
    currentStorageDetail.value = await fileManagementApi.storageDetail(row.basicId)
  }
  catch {
    currentStorageDetail.value = null
    message.error('加载存储副本详情失败')
  }
  finally {
    detailLoading.value = false
  }
}

async function handleFileStorages(row: FileListItemDto) {
  activeTab.value = 'storage'
  storageQuery.fileId = row.basicId
  await nextTick()
  storagePage.value = 1
  fetchStorageData()
}

async function handleVerifyStorage(row: FileStorageListItemDto) {
  try {
    await fileManagementApi.verifyStorage({ basicId: row.basicId })
    fetchStorageData()
    message.success('校验完成')
  }
  catch {
    message.error('校验存储副本失败')
  }
}

async function handleSwitchPrimary(row: FileStorageListItemDto) {
  if (row.isPrimary) {
    return
  }

  try {
    await fileManagementApi.switchPrimaryStorage({
      basicId: row.fileId,
      storageId: row.basicId,
    })
    fetchStorageData()
    message.success('主存储已切换')
  }
  catch {
    message.error('切换主存储失败')
  }
}

onMounted(() => {
  fetchFileData()
})
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <div class="xh-query-panel">
      <div class="xh-query-panel__content">
        <NSelect
          v-model:value="activeTab"
          :options="tabOptions"
          style="width: 120px"
          @update:value="handleTabChanged"
        />

        <template v-if="activeTab === 'file'">
          <NInput
            v-model:value="fileQuery.keyword"
            clearable
            placeholder="搜索文件名/哈希"
            style="width: 220px"
            @keyup.enter="handleSearch"
          />
          <NSelect
            v-model:value="fileQuery.fileType"
            :options="fileTypeOptions"
            clearable
            placeholder="文件类型"
            style="width: 120px"
          />
          <NSelect
            v-model:value="fileQuery.status"
            :options="fileStatusOptions"
            clearable
            placeholder="文件状态"
            style="width: 120px"
          />
          <NSelect
            v-model:value="fileQuery.accessLevel"
            :options="accessLevelOptions"
            clearable
            placeholder="访问级别"
            style="width: 120px"
          />
          <NSelect
            v-model:value="fileQuery.isTemporary"
            :options="booleanOptions"
            clearable
            placeholder="临时"
            style="width: 100px"
          />
          <NSelect
            v-model:value="fileQuery.isEncrypted"
            :options="booleanOptions"
            clearable
            placeholder="加密"
            style="width: 100px"
          />
          <NInput
            v-model:value="fileQuery.fileExtension"
            clearable
            placeholder="扩展名"
            style="width: 100px"
            @keyup.enter="handleSearch"
          />
          <NInput
            v-model:value="fileQuery.mimeType"
            clearable
            placeholder="MIME"
            style="width: 140px"
            @keyup.enter="handleSearch"
          />
        </template>

        <template v-else>
          <NInput
            v-model:value="storageQuery.keyword"
            clearable
            placeholder="搜索提供商/路径"
            style="width: 220px"
            @keyup.enter="handleSearch"
          />
          <NInput
            v-model:value="storageQuery.fileId"
            clearable
            placeholder="文件主键"
            style="width: 140px"
            @keyup.enter="handleSearch"
          />
          <NSelect
            v-model:value="storageQuery.storageType"
            :options="storageTypeOptions"
            clearable
            placeholder="存储类型"
            style="width: 130px"
          />
          <NSelect
            v-model:value="storageQuery.status"
            :options="storageStatusOptions"
            clearable
            placeholder="存储状态"
            style="width: 120px"
          />
          <NSelect
            v-model:value="storageQuery.isPrimary"
            :options="booleanOptions"
            clearable
            placeholder="主存储"
            style="width: 100px"
          />
          <NSelect
            v-model:value="storageQuery.isVerified"
            :options="booleanOptions"
            clearable
            placeholder="验证"
            style="width: 100px"
          />
          <NSelect
            v-model:value="storageQuery.isSynced"
            :options="booleanOptions"
            clearable
            placeholder="同步"
            style="width: 100px"
          />
        </template>

        <NButton size="small" type="primary" @click="handleSearch">
          <template #icon>
            <NIcon><Icon icon="lucide:search" /></NIcon>
          </template>
          查询
        </NButton>
        <NButton size="small" @click="handleReset">
          <template #icon>
            <NIcon><Icon icon="lucide:rotate-ccw" /></NIcon>
          </template>
          重置
        </NButton>
        <NButton v-if="activeTab === 'file'" size="small" type="primary" @click="openUploadDrawer">
          <template #icon>
            <NIcon><Icon icon="lucide:upload" /></NIcon>
          </template>
          上传
        </NButton>
      </div>
    </div>

    <NCard v-show="activeTab === 'file'" class="flex-1" style="height: 0">
      <NDataTable
        :columns="fileColumns"
        :data="fileList"
        :loading="fileLoading"
        :max-height="500"
        :row-key="(row: FileListItemDto) => row.basicId"
        :scroll-x="1800"
        size="small"
        striped
      />
      <div class="flex justify-end p-2">
        <NPagination
          v-model:page="filePage"
          v-model:page-size="filePageSize"
          :item-count="fileTotal"
          :page-count="fileTotalPages"
          :page-sizes="[10, 20, 50, 100]"
          show-size-picker
          @update:page="handleFilePageChange"
          @update:page-size="handleFilePageSizeChange"
        />
      </div>
    </NCard>

    <NCard v-show="activeTab === 'storage'" class="flex-1" style="height: 0">
      <NDataTable
        :columns="storageColumns"
        :data="storageList"
        :loading="storageLoading"
        :max-height="500"
        :row-key="(row: FileStorageListItemDto) => row.basicId"
        :scroll-x="1500"
        size="small"
        striped
      />
      <div class="flex justify-end p-2">
        <NPagination
          v-model:page="storagePage"
          v-model:page-size="storagePageSize"
          :item-count="storageTotal"
          :page-count="storageTotalPages"
          :page-sizes="[10, 20, 50, 100]"
          show-size-picker
          @update:page="handleStoragePageChange"
          @update:page-size="handleStoragePageSizeChange"
        />
      </div>
    </NCard>

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
            {{ formatDateTime(currentFileDetail.expiresAt) }}
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
            {{ formatDateTime(currentStorageDetail.uploadedAt) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="最后验证">
            {{ formatDateTime(currentStorageDetail.lastVerifiedAt) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="同步时间">
            {{ formatDateTime(currentStorageDetail.syncedAt) }}
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
  </div>
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
</style>
