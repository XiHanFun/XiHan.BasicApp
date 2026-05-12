<script setup lang="ts">
import type { UploadCustomRequestOptions } from 'naive-ui'
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type {
  ApiId,
  FileDetailDto,
  FileListItemDto,
  FileStorageDetailDto,
  FileStorageListItemDto,
} from '@/api'
import {
  NButton,
  NDescriptions,
  NDescriptionsItem,
  NDrawer,
  NDrawerContent,
  NIcon,
  NInput,
  NInputNumber,
  NPopconfirm,
  NSelect,
  NSpace,
  NSwitch,
  NTag,
  NUpload,
  useMessage,
} from 'naive-ui'
import { computed, nextTick, reactive, ref } from 'vue'
import {
  createPageRequest,
  fileManagementApi,
  FileStatus,
  FileStorageStatus,
  FileStorageType,
  FileType,
  ResourceAccessLevel,
} from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { useVxeTable } from '~/hooks'
import { formatDate, formatFileSize, getOptionLabel } from '~/utils'

defineOptions({ name: 'PlatformFilePage' })

type FileTab = 'file' | 'storage'
type DetailKind = 'file' | 'storage'
type TagType = 'default' | 'error' | 'info' | 'success' | 'warning'

interface FileGridResult {
  items: FileListItemDto[]
  total: number
}

interface StorageGridResult {
  items: FileStorageListItemDto[]
  total: number
}

const message = useMessage()
const activeTab = ref<FileTab>('file')
const fileGrid = ref<VxeGridInstance<FileListItemDto>>()
const storageGrid = ref<VxeGridInstance<FileStorageListItemDto>>()
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

function handleFileQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<FileGridResult> {
  return fileManagementApi
    .page({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
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
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询文件列表失败')
      return {
        items: [],
        total: 0,
      }
    })
}

function handleStorageQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<StorageGridResult> {
  return fileManagementApi
    .storagePage({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
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
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询存储副本失败')
      return {
        items: [],
        total: 0,
      }
    })
}

const fileTableOptions = useVxeTable<FileListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'originalName', fixed: 'left', minWidth: 220, showOverflow: 'tooltip', title: '原始文件名' },
      { field: 'fileName', minWidth: 220, showOverflow: 'tooltip', title: '存储文件名' },
      {
        field: 'fileType',
        slots: { default: 'col_file_type' },
        title: '文件类型',
        width: 110,
      },
      {
        field: 'fileSize',
        formatter: ({ cellValue }) => formatFileSize(Number(cellValue || 0)),
        minWidth: 110,
        title: '文件大小',
      },
      {
        field: 'status',
        slots: { default: 'col_file_status' },
        title: '状态',
        width: 110,
      },
      {
        field: 'accessLevel',
        formatter: ({ cellValue }) => getOptionLabel(accessLevelOptions, cellValue),
        minWidth: 110,
        title: '访问级别',
      },
      { field: 'fileExtension', minWidth: 90, title: '扩展名' },
      { field: 'mimeType', minWidth: 160, showOverflow: 'tooltip', title: 'MIME' },
      {
        field: 'isTemporary',
        slots: { default: 'col_file_temporary' },
        title: '临时',
        width: 82,
      },
      {
        field: 'isEncrypted',
        slots: { default: 'col_file_encrypted' },
        title: '加密',
        width: 82,
      },
      { field: 'downloadCount', minWidth: 90, title: '下载' },
      { field: 'viewCount', minWidth: 90, title: '访问' },
      {
        field: 'createdTime',
        formatter: ({ cellValue }) => formatDateTime(cellValue as string | null),
        minWidth: 170,
        sortable: true,
        title: '创建时间',
      },
      {
        field: 'actions',
        fixed: 'right',
        slots: { default: 'col_file_actions' },
        title: '操作',
        width: 200,
      },
    ],
    id: 'sys_file',
    name: '文件管理',
  },
  {
    proxyConfig: {
      autoLoad: true,
      ajax: {
        query: ({ page }) => handleFileQueryApi(page),
      },
    },
  },
)

const storageTableOptions = useVxeTable<FileStorageListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'fileId', fixed: 'left', minWidth: 140, title: '文件主键' },
      { field: 'storageProvider', minWidth: 130, showOverflow: 'tooltip', title: '提供商' },
      {
        field: 'storageType',
        formatter: ({ cellValue }) => getOptionLabel(storageTypeOptions, cellValue),
        minWidth: 120,
        title: '存储类型',
      },
      {
        field: 'status',
        slots: { default: 'col_storage_status' },
        title: '状态',
        width: 110,
      },
      {
        field: 'isPrimary',
        slots: { default: 'col_storage_primary' },
        title: '主存储',
        width: 92,
      },
      {
        field: 'isBackup',
        slots: { default: 'col_storage_backup' },
        title: '备份',
        width: 82,
      },
      {
        field: 'isVerified',
        slots: { default: 'col_storage_verified' },
        title: '已验证',
        width: 92,
      },
      {
        field: 'isSynced',
        slots: { default: 'col_storage_synced' },
        title: '已同步',
        width: 92,
      },
      { field: 'accessControl', minWidth: 120, showOverflow: 'tooltip', title: '访问控制' },
      {
        field: 'uploadedAt',
        formatter: ({ cellValue }) => formatDateTime(cellValue as string | null),
        minWidth: 170,
        title: '上传时间',
      },
      {
        field: 'createdTime',
        formatter: ({ cellValue }) => formatDateTime(cellValue as string | null),
        minWidth: 170,
        sortable: true,
        title: '创建时间',
      },
      {
        field: 'actions',
        fixed: 'right',
        slots: { default: 'col_storage_actions' },
        title: '操作',
        width: 132,
      },
    ],
    id: 'sys_file_storage',
    name: '文件存储副本',
  },
  {
    proxyConfig: {
      autoLoad: true,
      ajax: {
        query: ({ page }) => handleStorageQueryApi(page),
      },
    },
  },
)

function reloadActiveGrid() {
  if (activeTab.value === 'file') {
    fileGrid.value?.commitProxy('reload')
    return
  }

  storageGrid.value?.commitProxy('reload')
}

function handleSearch() {
  reloadActiveGrid()
}

function handleTabChanged() {
  reloadActiveGrid()
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
  }
  else {
    storageQuery.fileId = null
    storageQuery.isPrimary = undefined
    storageQuery.isSynced = undefined
    storageQuery.isVerified = undefined
    storageQuery.keyword = ''
    storageQuery.status = null
    storageQuery.storageType = null
  }

  reloadActiveGrid()
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
  // store the row for later use
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
    reloadActiveGrid()
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
    reloadActiveGrid()
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
    reloadActiveGrid()
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
    fileGrid.value?.commitProxy('reload')
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
  storageGrid.value?.commitProxy('reload')
}

async function handleVerifyStorage(row: FileStorageListItemDto) {
  try {
    await fileManagementApi.verifyStorage({ basicId: row.basicId })
    storageGrid.value?.commitProxy('reload')
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
    storageGrid.value?.commitProxy('reload')
    message.success('主存储已切换')
  }
  catch {
    message.error('切换主存储失败')
  }
}
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <NSelect
          v-model:value="activeTab"
          :options="tabOptions"
          style="width: 120px"
          @update:value="handleTabChanged"
        />

        <template v-if="activeTab === 'file'">
          <vxe-input
            v-model="fileQuery.keyword"
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
          <vxe-input
            v-model="fileQuery.fileExtension"
            clearable
            placeholder="扩展名"
            style="width: 100px"
            @keyup.enter="handleSearch"
          />
          <vxe-input
            v-model="fileQuery.mimeType"
            clearable
            placeholder="MIME"
            style="width: 140px"
            @keyup.enter="handleSearch"
          />
        </template>

        <template v-else>
          <vxe-input
            v-model="storageQuery.keyword"
            clearable
            placeholder="搜索提供商/路径"
            style="width: 220px"
            @keyup.enter="handleSearch"
          />
          <vxe-input
            v-model="storageQuery.fileId"
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
    </XSystemQueryPanel>

    <vxe-card v-show="activeTab === 'file'" class="flex-1" style="height: 0">
      <vxe-grid ref="fileGrid" v-bind="fileTableOptions">
        <template #toolbar_buttons />
        <template #empty>
          <div class="py-12 text-center text-gray-400">
            暂无文件数据
          </div>
        </template>

        <template #col_file_type="{ row }">
          <NTag round size="small">
            {{ getOptionLabel(fileTypeOptions, row.fileType) }}
          </NTag>
        </template>

        <template #col_file_status="{ row }">
          <NTag :type="getFileStatusTagType(row.status)" round size="small">
            {{ getOptionLabel(fileStatusOptions, row.status) }}
          </NTag>
        </template>

        <template #col_file_temporary="{ row }">
          <NTag :type="row.isTemporary ? 'warning' : 'default'" round size="small">
            {{ row.isTemporary ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_file_encrypted="{ row }">
          <NTag :type="row.isEncrypted ? 'info' : 'default'" round size="small">
            {{ row.isEncrypted ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_file_actions="{ row }">
          <NSpace :size="4">
            <NButton aria-label="详情" circle quaternary size="small" type="primary" @click="handleFileDetail(row)">
              <template #icon>
                <NIcon><Icon icon="lucide:eye" /></NIcon>
              </template>
            </NButton>
            <NButton aria-label="元数据" circle quaternary size="small" @click="openMetadataDrawer(row)">
              <template #icon>
                <NIcon><Icon icon="lucide:pencil" /></NIcon>
              </template>
            </NButton>
            <NButton aria-label="存储副本" circle quaternary size="small" @click="handleFileStorages(row)">
              <template #icon>
                <NIcon><Icon icon="lucide:database" /></NIcon>
              </template>
            </NButton>
            <NButton
              v-if="row.status !== FileStatus.Archived"
              aria-label="归档"
              circle
              quaternary
              size="small"
              type="warning"
              @click="handleUpdateFileStatus(row, FileStatus.Archived)"
            >
              <template #icon>
                <NIcon><Icon icon="lucide:archive" /></NIcon>
              </template>
            </NButton>
            <NPopconfirm @positive-click="handleDeleteFile(row)">
              <template #trigger>
                <NButton
                  v-if="row.status !== FileStatus.Deleted"
                  aria-label="删除"
                  circle
                  quaternary
                  size="small"
                  type="error"
                  :loading="actionLoading"
                >
                  <template #icon>
                    <NIcon><Icon icon="lucide:trash-2" /></NIcon>
                  </template>
                </NButton>
              </template>
              确定删除该文件？
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

    <vxe-card v-show="activeTab === 'storage'" class="flex-1" style="height: 0">
      <vxe-grid ref="storageGrid" v-bind="storageTableOptions">
        <template #toolbar_buttons />
        <template #empty>
          <div class="py-12 text-center text-gray-400">
            暂无存储副本数据
          </div>
        </template>

        <template #col_storage_status="{ row }">
          <NTag :type="getStorageStatusTagType(row.status)" round size="small">
            {{ getOptionLabel(storageStatusOptions, row.status) }}
          </NTag>
        </template>

        <template #col_storage_primary="{ row }">
          <NTag :type="row.isPrimary ? 'success' : 'default'" round size="small">
            {{ row.isPrimary ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_storage_backup="{ row }">
          <NTag :type="row.isBackup ? 'info' : 'default'" round size="small">
            {{ row.isBackup ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_storage_verified="{ row }">
          <NTag :type="row.isVerified ? 'success' : 'warning'" round size="small">
            {{ row.isVerified ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_storage_synced="{ row }">
          <NTag :type="row.isSynced ? 'success' : 'default'" round size="small">
            {{ row.isSynced ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_storage_actions="{ row }">
          <NSpace :size="4">
            <NButton aria-label="详情" circle quaternary size="small" type="primary" @click="handleStorageDetail(row)">
              <template #icon>
                <NIcon><Icon icon="lucide:eye" /></NIcon>
              </template>
            </NButton>
            <NButton aria-label="校验" circle quaternary size="small" @click="handleVerifyStorage(row)">
              <template #icon>
                <NIcon><Icon icon="lucide:shield-check" /></NIcon>
              </template>
            </NButton>
            <NButton
              aria-label="设为主存储"
              circle
              :disabled="row.isPrimary"
              quaternary
              size="small"
              type="primary"
              @click="handleSwitchPrimary(row)"
            >
              <template #icon>
                <NIcon><Icon icon="lucide:star" /></NIcon>
              </template>
            </NButton>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

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
