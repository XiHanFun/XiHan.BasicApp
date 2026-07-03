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
  createDynamicApiClient,
  formatDynamicApiRouteValue,
} from '../../base'

const fileQueryApi = createDynamicApiClient('FileQuery')
const fileCommandApi = createDynamicApiClient('File')

export const fileApi = {
  detail(id: ApiId) {
    return fileQueryApi.get<FileDetailDto | null>(`FileDetail/${formatDynamicApiRouteValue(id)}`)
  },
  /**
   * 彻底删除：物理删除文件记录与副本；deletePhysical=true 时连物理文件一并删除（不可恢复）。
   * 后端 DeleteFileAsync 的 Delete 前缀被 DynamicApi 当 HTTP 谓词剥离，实际路由为 DELETE /api/File/File；
   * 且框架约定 DELETE 不收 body，FileDeleteDto 经 query 绑定。
   */
  destroy(input: { basicId: ApiId, deletePhysical: boolean, reason?: string }) {
    return fileCommandApi.delete<void>('File', {
      params: { basicId: input.basicId, deletePhysical: input.deletePhysical, reason: input.reason },
    })
  },
  download(fileId: ApiId) {
    // 后端 DownloadFileAsync：Download 前缀不在动词约定表，默认 POST；fileId（简单类型）在 POST 下绑定 Query。
    // 走鉴权下载接口（File.Read），axios 自动带 token，规避静态 URL 的匿名/未知 MIME 访问限制。
    return fileCommandApi.post<Blob>('DownloadFile', undefined, {
      params: { fileId },
      responseType: 'blob',
    })
  },
  /** 秒传探测：按哈希命中返回文件详情，未命中返回 null（调用方回退普通上传） */
  fastUpload(input: FileFastUploadDto) {
    return fileCommandApi.post<FileDetailDto | null, FileFastUploadDto>('FastUploadFile', input)
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
    return fileQueryApi.post<PageResult<FileListItemDto>>('FilePage', input)
  },
  storageDetail(id: ApiId) {
    return fileQueryApi.get<FileStorageDetailDto | null>(`FileStorageDetail/${formatDynamicApiRouteValue(id)}`)
  },
  storagePage(input: FileStoragePageQueryDto) {
    return fileQueryApi.post<PageResult<FileStorageListItemDto>>('FileStoragePage', input)
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
  upload(input: FileUploadInput, onProgress?: (percent: number) => void) {
    return fileCommandApi.post<FileDetailDto, FormData>('UploadFile', toFileUploadFormData(input), {
      onUploadProgress: onProgress
        ? e => onProgress(e.total ? Math.round((e.loaded / e.total) * 100) : 0)
        : undefined,
    })
  },
  verifyStorage(input: FileStorageVerifyDto) {
    return fileCommandApi.post<FileStorageDetailDto, FileStorageVerifyDto>('VerifyFileStorage', input)
  },
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
  appendFormData(formData, 'ExpirationTime', input.expirationTime)
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
