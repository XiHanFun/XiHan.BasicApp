import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'
import type { ConfigDataType } from '../authorization'
import type { EnableStatus } from '../shared'

export { EnableStatus } from '../shared'

export enum ConfigType {
  System = 0,
  User = 1,
  Application = 2,
  Business = 3,
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
