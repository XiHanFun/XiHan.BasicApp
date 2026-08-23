import type { ApiId, BasicDto, BasicUpdateDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus } from '../shared'

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum HttpMethodType {
  GET = 'GET',
  POST = 'POST',
  PUT = 'PUT',
  DELETE = 'DELETE',
  PATCH = 'PATCH',
  HEAD = 'HEAD',
  OPTIONS = 'OPTIONS',
  ALL = 'ALL',
}

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum OperationCategory {
  Crud = 'Crud',
  Business = 'Business',
  Admin = 'Admin',
  System = 'System',
  Custom = 'Custom',
}

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum OperationTypeCode {
  Create = 'Create',
  Read = 'Read',
  Update = 'Update',
  Delete = 'Delete',
  View = 'View',
  Approve = 'Approve',
  Execute = 'Execute',
  Import = 'Import',
  Export = 'Export',
  Upload = 'Upload',
  Download = 'Download',
  Print = 'Print',
  Share = 'Share',
  Grant = 'Grant',
  Revoke = 'Revoke',
  Enable = 'Enable',
  Disable = 'Disable',
  Custom = 'Custom',
}

export interface OperationPageQueryDto extends PageRequest {
  category?: OperationCategory | null
  httpMethod?: HttpMethodType | null
  isDangerous?: boolean | null
  isGlobal?: boolean | null
  isRequireAudit?: boolean | null
  keyword?: string | null
  operationTypeCode?: OperationTypeCode | null
  status?: EnableStatus | null
}

export interface OperationListItemDto extends BasicDto {
  category: OperationCategory
  color?: string | null
  createdTime: DateTimeString
  description?: string | null
  httpMethod?: HttpMethodType | null
  icon?: string | null
  isDangerous: boolean
  isGlobal: boolean
  isRequireAudit: boolean
  modifiedTime?: DateTimeString | null
  operationCode: string
  operationName: string
  operationTypeCode: OperationTypeCode
  sort: number
  status: EnableStatus
}

export interface OperationDetailDto extends OperationListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  remark?: string | null
}

export interface OperationCreateDto {
  category: OperationCategory
  color?: string | null
  description?: string | null
  httpMethod?: HttpMethodType | null
  icon?: string | null
  isDangerous: boolean
  isRequireAudit: boolean
  operationCode: string
  operationName: string
  operationTypeCode: OperationTypeCode
  remark?: string | null
  sort: number
  status: EnableStatus
}

export interface OperationUpdateDto extends BasicUpdateDto {
  category: OperationCategory
  color?: string | null
  description?: string | null
  httpMethod?: HttpMethodType | null
  icon?: string | null
  isDangerous: boolean
  isRequireAudit: boolean
  operationName: string
  operationTypeCode: OperationTypeCode
  remark?: string | null
  sort: number
}

export interface OperationStatusUpdateDto extends BasicUpdateDto {
  remark?: string | null
  status: EnableStatus
}

export interface OperationSelectQueryDto {
  category?: OperationCategory | null
  keyword?: string | null
  limit: number
  operationTypeCode?: OperationTypeCode | null
}

export interface OperationSelectItemDto extends BasicDto {
  category: OperationCategory
  httpMethod?: HttpMethodType | null
  isDangerous: boolean
  isRequireAudit: boolean
  operationCode: string
  operationName: string
  operationTypeCode: OperationTypeCode
}
