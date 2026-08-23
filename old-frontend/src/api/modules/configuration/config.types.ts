import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'
import type { ConfigDataType } from '../authorization'
import type { EnableStatus } from '../shared'

export { EnableStatus } from '../shared'

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum ConfigType {
  Tenant = 'Tenant',
  Organization = 'Organization',
  User = 'User',
  Application = 'Application',
  Environment = 'Environment',
  Feature = 'Feature',
}

export interface ConfigPageQueryDto extends PageRequest {
  configGroup?: string | null
  configType?: ConfigType | null
  dataType?: ConfigDataType | null
  isBuiltIn?: boolean | null
  isEncrypted?: boolean | null
  isGlobal?: boolean | null
  keyword?: string | null
  status?: EnableStatus | null
}

export interface ConfigListItemDto extends BasicDto {
  configDescription?: string | null
  configGroup?: string | null
  configKey: string
  configName: string
  configType: ConfigType
  createdTime: DateTimeString
  dataType: ConfigDataType
  hasCurrentValue: boolean
  hasFallbackValue: boolean
  hasNote: boolean
  isBuiltIn: boolean
  isEncrypted: boolean
  isGlobal: boolean
  modifiedTime?: DateTimeString | null
  sort: number
  status: EnableStatus
}

export interface ConfigDetailDto extends ConfigListItemDto {
  /** 当前配置值（加密项后端返回 null，前端以「已加密」提示替代） */
  configValue?: string | null
  /** 默认值（加密项后端返回 null） */
  defaultValue?: string | null
  remark?: string | null
  createdBy?: string | null
  createdId?: ApiId | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
}

export interface ConfigCreateDto {
  configDescription?: string | null
  configGroup?: string | null
  configKey: string
  configName: string
  configType: ConfigType
  configValue?: string | null
  dataType: ConfigDataType
  defaultValue?: string | null
  isEncrypted: boolean
  isGlobal: boolean
  remark?: string | null
  sort: number
  status: EnableStatus
}

export interface ConfigUpdateDto extends BasicDto {
  configDescription?: string | null
  configGroup?: string | null
  configName: string
  configType: ConfigType
  configValue?: string | null
  dataType: ConfigDataType
  defaultValue?: string | null
  isEncrypted: boolean
  isGlobal: boolean
  remark?: string | null
  sort: number
}

export interface ConfigStatusUpdateDto extends BasicDto {
  remark?: string | null
  status: EnableStatus
}
