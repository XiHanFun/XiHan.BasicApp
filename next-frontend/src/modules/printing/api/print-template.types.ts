import type { EnableStatus } from '@/api/modules/shared/types'
/**
 * 打印模板 Dynamic API 类型契约。
 * 职责：与后端 PrintTemplate DTO、作用域和字符串行版本保持一一对应。
 */
import type { ApiId, BasicDto, PageRequest } from '@/api/types'

/** 打印模板解析与管理作用域。 */
export enum PrintTemplateScope {
  Auto = 'Auto',
  Tenant = 'Tenant',
  Global = 'Global',
}

/** 打印模板列表项。 */
export interface PrintTemplateListItemDto extends BasicDto {
  allowTenantUse: boolean
  /** null 表示模板不绑定代码数据源。 */
  dataSourceCode: null | string
  engineVersion: string
  isGlobal: boolean
  remark?: null | string
  /** 十进制字符串，避免 JavaScript 整数精度问题。 */
  rowVersion: string
  sort: number
  status: EnableStatus
  templateCode: string
  templateName: string
}

/** 打印模板详情。 */
export interface PrintTemplateDetailDto extends PrintTemplateListItemDto {
  createdTime: string
  modifiedTime?: null | string
  templateJson: string
}

/** 按编码解析后的可打印模板。 */
export interface ResolvedPrintTemplateDto {
  basicId: ApiId
  dataSourceCode: null | string
  engineVersion: string
  requestedScope: PrintTemplateScope
  resolvedScope: PrintTemplateScope
  rowVersion: string
  templateCode: string
  templateJson: string
  templateName: string
}

/** 创建打印模板。 */
export interface PrintTemplateCreateDto {
  allowTenantUse: boolean
  dataSourceCode?: null | string
  engineVersion: string
  remark?: null | string
  scope: PrintTemplateScope
  sort: number
  status: EnableStatus
  templateCode: string
  templateJson: string
  templateName: string
}

/** 更新打印模板；模板编码不可修改，可切换可选数据源。 */
export interface PrintTemplateUpdateDto {
  allowTenantUse: boolean
  basicId: ApiId
  dataSourceCode?: null | string
  engineVersion: string
  remark?: null | string
  rowVersion: string
  scope: PrintTemplateScope
  sort: number
  templateJson: string
  templateName: string
}

/** 启停打印模板。 */
export interface PrintTemplateStatusUpdateDto {
  basicId: ApiId
  remark?: null | string
  rowVersion: string
  scope: PrintTemplateScope
  status: EnableStatus
}

/** 删除已停用模板。 */
export interface PrintTemplateDeleteDto {
  basicId: ApiId
  rowVersion: string
  scope: PrintTemplateScope
}

/** 打印模板分页查询。 */
export interface PrintTemplatePageQueryDto extends PageRequest {
  keyword?: null | string
  scope: PrintTemplateScope
  status?: EnableStatus | null
}
