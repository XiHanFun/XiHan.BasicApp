import type { ApiId, BasicDto, DateTimeString, NumericString, PageRequest } from '../../types'
import type { EnableStatus } from '../shared'
import type { DatabaseType } from './codegen.enums'

export { EnableStatus } from '../shared'

/** 代码生成数据源分页查询 DTO */
export interface CodeGenDataSourcePageQueryDto extends PageRequest {
  keyword?: string | null
  databaseType?: DatabaseType | null
  status?: EnableStatus | null
}

/** 代码生成数据源列表项 DTO */
export interface CodeGenDataSourceListItemDto extends BasicDto {
  sourceName: string
  sourceDescription?: string | null
  databaseType: DatabaseType
  host: string
  port: number
  databaseName: string
  isDefault: boolean
  lastTestTime?: DateTimeString | null
  lastTestResult: boolean
  lastTestMessage?: string | null
  status: EnableStatus
  sort: number
  remark?: string | null
  createdTime: DateTimeString
}

/** 代码生成数据源详情 DTO（含连接明细，密码字段输出时须脱敏） */
export interface CodeGenDataSourceDetailDto extends CodeGenDataSourceListItemDto {
  userName: string
  connectionString?: string | null
  extraParams?: string | null
  connectionTimeout: number
  createdId?: ApiId | null
  createdBy?: string | null
  modifiedId?: ApiId | null
  modifiedBy?: string | null
}

/** 代码生成数据源创建 DTO */
export interface CodeGenDataSourceCreateDto {
  sourceName: string
  sourceDescription?: string | null
  databaseType: DatabaseType
  host: string
  port: number
  databaseName: string
  userName: string
  password?: string | null
  connectionString?: string | null
  extraParams?: string | null
  connectionTimeout: number
  isDefault: boolean
  status: EnableStatus
  sort: number
  remark?: string | null
}

/** 代码生成数据源更新 DTO（后端 BasicAppUDto，状态变更走独立 Status 接口） */
export interface CodeGenDataSourceUpdateDto extends BasicDto {
  sourceName: string
  sourceDescription?: string | null
  databaseType: DatabaseType
  host: string
  port: number
  databaseName: string
  userName: string
  password?: string | null
  connectionString?: string | null
  extraParams?: string | null
  connectionTimeout: number
  isDefault: boolean
  sort: number
  remark?: string | null
}

/** 代码生成数据源状态更新 DTO */
export interface CodeGenDataSourceStatusUpdateDto extends BasicDto {
  status: EnableStatus
  remark?: string | null
}

/** 连接测试结果 DTO */
export interface CodeGenConnectionTestResultDto {
  success: boolean
  message?: string | null
  elapsedMilliseconds: NumericString
}
