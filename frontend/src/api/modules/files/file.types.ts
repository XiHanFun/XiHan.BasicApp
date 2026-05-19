import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'
import type { ResourceAccessLevel } from '../authorization'

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum FileStatus {
  Normal = 'Normal',
  Uploading = 'Uploading',
  UploadFailed = 'UploadFailed',
  Processing = 'Processing',
  Deleted = 'Deleted',
  Archived = 'Archived',
  Expired = 'Expired',
  Corrupted = 'Corrupted',
  Violation = 'Violation',
}

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum FileType {
  Image = 'Image',
  Document = 'Document',
  Video = 'Video',
  Audio = 'Audio',
  Archive = 'Archive',
  Other = 'Other',
}

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum FileStorageStatus {
  Normal = 'Normal',
  Uploading = 'Uploading',
  UploadFailed = 'UploadFailed',
  Syncing = 'Syncing',
  SyncFailed = 'SyncFailed',
  PendingVerification = 'PendingVerification',
  VerificationFailed = 'VerificationFailed',
  Expired = 'Expired',
  Deleted = 'Deleted',
  Unavailable = 'Unavailable',
}

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum FileStorageType {
  Local = 'Local',
  AliyunOss = 'AliyunOss',
  TencentCos = 'TencentCos',
  Minio = 'Minio',
  Ftp = 'Ftp',
  Sftp = 'Sftp',
  WebDav = 'WebDav',
  Custom = 'Custom',
}

export interface FilePageQueryDto extends PageRequest {
  accessLevel?: ResourceAccessLevel | null
  expiresAtEnd?: DateTimeString | null
  expiresAtStart?: DateTimeString | null
  fileExtension?: string | null
  fileType?: FileType | null
  isEncrypted?: boolean | null
  isTemporary?: boolean | null
  keyword?: string | null
  mimeType?: string | null
  status?: FileStatus | null
}

export interface FileStoragePageQueryDto extends PageRequest {
  enableCdn?: boolean | null
  fileId?: ApiId | null
  isBackup?: boolean | null
  isPrimary?: boolean | null
  isSynced?: boolean | null
  isVerified?: boolean | null
  keyword?: string | null
  status?: FileStorageStatus | null
  storageType?: FileStorageType | null
  uploadedAtEnd?: DateTimeString | null
  uploadedAtStart?: DateTimeString | null
}

export interface FileListItemDto extends BasicDto {
  accessLevel: ResourceAccessLevel
  createdTime: DateTimeString
  downloadCount: number
  duration?: number | null
  expiresAt?: DateTimeString | null
  fileExtension?: string | null
  fileName: string
  fileSize: number
  fileType: FileType
  height?: number | null
  isEncrypted: boolean
  isTemporary: boolean
  lastAccessTime?: DateTimeString | null
  lastDownloadTime?: DateTimeString | null
  mimeType?: string | null
  modifiedTime?: DateTimeString | null
  originalName: string
  retentionDays: number
  status: FileStatus
  thumbnailFileId?: ApiId | null
  viewCount: number
  width?: number | null
}

export interface FileDetailDto extends FileListItemDto {
  accessPermissions?: string | null
  createdBy?: string | null
  createdId?: ApiId | null
  encryptionKeyId?: string | null
  extendData?: string | null
  fileHash?: string | null
  hashAlgorithm?: string | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  remark?: string | null
  tags?: string | null
  uploadIp?: string | null
  uploadSource?: string | null
}

export interface FileStorageListItemDto extends BasicDto {
  accessControl?: string | null
  cacheControl?: string | null
  compressionRatio?: number | null
  createdTime: DateTimeString
  enableCdn: boolean
  fileId: ApiId
  isBackup: boolean
  isCompressed: boolean
  isPrimary: boolean
  isSynced: boolean
  isVerified: boolean
  lastVerifiedAt?: DateTimeString | null
  modifiedTime?: DateTimeString | null
  retryCount: number
  sortOrder: number
  status: FileStorageStatus
  storageClass?: string | null
  storageProvider?: string | null
  storageRegion?: string | null
  storageType: FileStorageType
  syncSourceId?: ApiId | null
  syncedAt?: DateTimeString | null
  uploadDuration?: number | null
  uploadedAt?: DateTimeString | null
}

export interface FileStorageDetailDto extends FileStorageListItemDto {
  bucketName?: string | null
  cdnUrl?: string | null
  createdBy?: string | null
  createdId?: ApiId | null
  extendData?: string | null
  externalUrl?: string | null
  fullPath?: string | null
  internalUrl?: string | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  remark?: string | null
  storageConfigId?: ApiId | null
  storagePath: string
  uploadFailureReason?: string | null
}

export interface FileUploadInput {
  accessControl?: string | null
  accessLevel?: ResourceAccessLevel | null
  accessPermissions?: string | null
  bucketName?: string | null
  cacheControl?: string | null
  directory?: string | null
  duration?: number | null
  encryptionKeyId?: string | null
  expiresAt?: DateTimeString | null
  extendData?: string | null
  file: File
  height?: number | null
  isEncrypted?: boolean | null
  isTemporary?: boolean | null
  overwrite?: boolean | null
  providerName?: string | null
  remark?: string | null
  retentionDays?: number | null
  routeKey?: string | null
  tags?: string | null
  thumbnailFileId?: ApiId | null
  width?: number | null
}

export interface FileFastUploadDto {
  accessLevel?: ResourceAccessLevel | null
  accessPermissions?: string | null
  expiresAt?: DateTimeString | null
  extendData?: string | null
  fileExtension?: string | null
  fileHash: string
  fileSize: number
  isTemporary?: boolean | null
  mimeType?: string | null
  originalName: string
  remark?: string | null
  retentionDays?: number | null
  tags?: string | null
}

export interface FileMetadataUpdateDto extends BasicDto {
  accessLevel: ResourceAccessLevel
  accessPermissions?: string | null
  duration?: number | null
  encryptionKeyId?: string | null
  expiresAt?: DateTimeString | null
  extendData?: string | null
  height?: number | null
  isEncrypted: boolean
  isTemporary: boolean
  remark?: string | null
  retentionDays: number
  tags?: string | null
  thumbnailFileId?: ApiId | null
  width?: number | null
}

export interface FileStatusUpdateDto extends BasicDto {
  remark?: string | null
  status: FileStatus
}

export interface FilePrimaryStorageSwitchDto extends BasicDto {
  remark?: string | null
  storageId: ApiId
}

export interface FileStorageVerifyDto extends BasicDto {
  remark?: string | null
}

export interface FileStorageStatusUpdateDto extends BasicDto {
  remark?: string | null
  status: FileStorageStatus
  uploadFailureReason?: string | null
}
