import type { ApiId, PageResult } from '../../types'
import type {
  FileDetailDto,
  FileFastUploadDto,
  FileListItemDto,
  FileMetadataUpdateDto,
  FilePageQueryDto,
  FilePrimaryStorageSwitchDto,
  FileStatusUpdateDto,
  FileStorageDetailDto,
  FileStorageListItemDto,
  FileStoragePageQueryDto,
  FileStorageStatusUpdateDto,
  FileStorageVerifyDto,
  FileUploadInput,
} from './file.types'
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
  formatDynamicApiRouteValue,
} from '../../base'

const fileQueryApi = createDynamicApiClient('FileQuery')
const fileCommandApi = createDynamicApiClient('File')

export const fileApi = {
  detail(id: ApiId) {
    return fileQueryApi.get<FileDetailDto | null>(`FileDetail/${formatDynamicApiRouteValue(id)}`)
  },
  fastUpload(input: FileFastUploadDto) {
    return fileCommandApi.post<FileDetailDto, FileFastUploadDto>('FastUploadFile', input)
  },
  generatePresignedUrl(fileId: ApiId) {
    // 后端 GenerateFilePresignedUrlAsync：方法名前缀 Generate 不在动词约定表中，默认 POST；
    // fileId（简单类型）在 POST 下绑定到 Query。返回对象存储签名 URL，<img> 可直接访问且无需 token（会过期）。
    // params 由 axios 负责编码，这里传原始值即可，避免双重编码。
    return fileCommandApi.post<string>(
      'GenerateFilePresignedUrl',
      undefined,
      { params: { fileId } },
    )
  },
  page(input: FilePageQueryDto) {
    return fileQueryApi.get<PageResult<FileListItemDto>>('FilePage', toFilePageParams(input))
  },
  storageDetail(id: ApiId) {
    return fileQueryApi.get<FileStorageDetailDto | null>(`FileStorageDetail/${formatDynamicApiRouteValue(id)}`)
  },
  storagePage(input: FileStoragePageQueryDto) {
    return fileQueryApi.get<PageResult<FileStorageListItemDto>>(
      'FileStoragePage',
      toFileStoragePageParams(input),
    )
  },
  switchPrimaryStorage(input: FilePrimaryStorageSwitchDto) {
    return fileCommandApi.post<FileStorageDetailDto, FilePrimaryStorageSwitchDto>('SwitchPrimaryStorage', input)
  },
  updateMetadata(input: FileMetadataUpdateDto) {
    return fileCommandApi.put<FileDetailDto, FileMetadataUpdateDto>('FileMetadata', input)
  },
  updateStatus(input: FileStatusUpdateDto) {
    return fileCommandApi.put<FileDetailDto, FileStatusUpdateDto>('FileStatus', input)
  },
  updateStorageStatus(input: FileStorageStatusUpdateDto) {
    return fileCommandApi.put<FileStorageDetailDto, FileStorageStatusUpdateDto>('FileStorageStatus', input)
  },
  upload(input: FileUploadInput) {
    return fileCommandApi.post<FileDetailDto, FormData>('UploadFile', toFileUploadFormData(input))
  },
  verifyStorage(input: FileStorageVerifyDto) {
    return fileCommandApi.post<FileStorageDetailDto, FileStorageVerifyDto>('VerifyFileStorage', input)
  },
}

function toFilePageParams(input: FilePageQueryDto) {
  const params = createPageRequestParams(input)
  appendDynamicApiParam(params, 'AccessLevel', input.accessLevel)
  appendDynamicApiParam(params, 'ExpiresAtEnd', input.expiresAtEnd)
  appendDynamicApiParam(params, 'ExpiresAtStart', input.expiresAtStart)
  appendDynamicApiParam(params, 'FileExtension', input.fileExtension)
  appendDynamicApiParam(params, 'FileType', input.fileType)
  appendDynamicApiParam(params, 'IsEncrypted', input.isEncrypted)
  appendDynamicApiParam(params, 'IsTemporary', input.isTemporary)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'MimeType', input.mimeType)
  appendDynamicApiParam(params, 'Status', input.status)
  return params
}

function toFileStoragePageParams(input: FileStoragePageQueryDto) {
  const params = createPageRequestParams(input)
  appendDynamicApiParam(params, 'EnableCdn', input.enableCdn)
  appendDynamicApiParam(params, 'FileId', input.fileId)
  appendDynamicApiParam(params, 'IsBackup', input.isBackup)
  appendDynamicApiParam(params, 'IsPrimary', input.isPrimary)
  appendDynamicApiParam(params, 'IsSynced', input.isSynced)
  appendDynamicApiParam(params, 'IsVerified', input.isVerified)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'Status', input.status)
  appendDynamicApiParam(params, 'StorageType', input.storageType)
  appendDynamicApiParam(params, 'UploadedAtEnd', input.uploadedAtEnd)
  appendDynamicApiParam(params, 'UploadedAtStart', input.uploadedAtStart)
  return params
}

function toFileUploadFormData(input: FileUploadInput) {
  const formData = new FormData()
  formData.append('File', input.file)
  appendFormData(formData, 'RouteKey', input.routeKey)
  appendFormData(formData, 'ProviderName', input.providerName)
  appendFormData(formData, 'Directory', input.directory)
  appendFormData(formData, 'BucketName', input.bucketName)
  appendFormData(formData, 'Overwrite', input.overwrite)
  appendFormData(formData, 'AccessControl', input.accessControl)
  appendFormData(formData, 'CacheControl', input.cacheControl)
  appendFormData(formData, 'AccessLevel', input.accessLevel)
  appendFormData(formData, 'AccessPermissions', input.accessPermissions)
  appendFormData(formData, 'IsEncrypted', input.isEncrypted)
  appendFormData(formData, 'EncryptionKeyId', input.encryptionKeyId)
  appendFormData(formData, 'ExpiresAt', input.expiresAt)
  appendFormData(formData, 'IsTemporary', input.isTemporary)
  appendFormData(formData, 'RetentionDays', input.retentionDays)
  appendFormData(formData, 'Tags', input.tags)
  appendFormData(formData, 'Remark', input.remark)
  appendFormData(formData, 'ExtendData', input.extendData)
  appendFormData(formData, 'Width', input.width)
  appendFormData(formData, 'Height', input.height)
  appendFormData(formData, 'Duration', input.duration)
  appendFormData(formData, 'ThumbnailFileId', input.thumbnailFileId)
  return formData
}

function appendFormData(formData: FormData, key: string, value: boolean | number | string | null | undefined) {
  if (value === undefined || value === null || value === '') {
    return
  }

  formData.append(key, String(value))
}
