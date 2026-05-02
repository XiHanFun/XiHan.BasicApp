import type { ApiId, BasicDto, BasicUpdateDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus } from '../shared'

export enum HttpMethodType {
  GET = 0,
  POST = 1,
  PUT = 2,
  DELETE = 3,
  PATCH = 4,
  HEAD = 5,
  OPTIONS = 6,
  ALL = 99,
}

export enum OperationCategory {
  Crud = 0,
  Business = 1,
  Admin = 2,
  System = 3,
  Custom = 99,
}

export enum OperationTypeCode {
  Create = 0,
  Read = 1,
  Update = 2,
  Delete = 3,
  View = 4,
  Approve = 10,
  Execute = 11,
  Import = 20,
  Export = 21,
  Upload = 22,
  Download = 23,
  Print = 24,
  Share = 25,
  Grant = 30,
  Revoke = 31,
  Enable = 32,
  Disable = 33,
  Custom = 99,
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
