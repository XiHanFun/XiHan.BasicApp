import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus } from '../shared'
import { ConfigDataType } from '../authorization'

export { EnableStatus } from '../shared'
export { ConfigDataType } from '../authorization'

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
