import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum StorageConfigType {
  Local = 'Local',
  S3 = 'S3',
  OSS = 'OSS',
  COS = 'COS',
  MinIO = 'MinIO',
}

export interface StorageConfigPageQueryDto extends PageRequest {
  isDefault?: boolean | null
  isEnabled?: boolean | null
  keyword?: string | null
  storageType?: StorageConfigType | null
}

export interface StorageConfigListItemDto extends BasicDto {
  /** Local 类型时为根路径 */
  bucketName?: string | null
  configCode: string
  configName: string
  createdTime: DateTimeString
  endpoint?: string | null
  isDefault: boolean
  isEnabled: boolean
  modifiedTime?: DateTimeString | null
  region?: string | null
  sort: number
  storageType: StorageConfigType
}

/** SecretAccessKey 为敏感字段不下发，仅以 hasSecretAccessKey 标识是否已配置 */
export interface StorageConfigDetailDto extends StorageConfigListItemDto {
  accessKeyId?: string | null
  createdBy?: string | null
  createdId?: ApiId | null
  hasSecretAccessKey: boolean
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  remark?: string | null
}

export interface StorageConfigCreateDto {
  accessKeyId?: string | null
  /** Local 类型时存放根路径 */
  bucketName?: string | null
  configCode: string
  configName: string
  endpoint?: string | null
  isDefault: boolean
  isEnabled: boolean
  region?: string | null
  remark?: string | null
  secretAccessKey?: string | null
  sort: number
  storageType: StorageConfigType
}

export interface StorageConfigUpdateDto extends BasicDto {
  accessKeyId?: string | null
  /** Local 类型时存放根路径 */
  bucketName?: string | null
  configName: string
  endpoint?: string | null
  region?: string | null
  remark?: string | null
  /** 留空表示保留原密钥 */
  secretAccessKey?: string | null
  sort: number
  storageType: StorageConfigType
}

export interface StorageConfigStatusUpdateDto extends BasicDto {
  isEnabled: boolean
}

export type StorageConfigDefaultUpdateDto = BasicDto
