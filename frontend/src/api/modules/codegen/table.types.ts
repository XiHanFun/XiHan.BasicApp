import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus } from '../shared'
import type { CodeGenTableColumnListItemDto } from './codegen-table-column.types'
import type { DatabaseType, GenStatus, GenType, TemplateType } from './codegen.enums'

export { EnableStatus } from '../shared'

/**
 * 代码生成表配置分页查询 DTO（镜像后端 CodeGenTablePageQueryDto : BasicAppPRDto）。
 */
export interface CodeGenTablePageQueryDto extends PageRequest {
  keyword?: string | null
  moduleName?: string | null
  templateType?: TemplateType | null
  genStatus?: GenStatus | null
  status?: EnableStatus | null
}

/**
 * 代码生成表配置列表项 DTO（镜像后端 CodeGenTableListItemDto : BasicAppDto）。
 */
export interface CodeGenTableListItemDto extends BasicDto {
  tableName: string
  tableComment?: string | null
  className: string
  moduleName?: string | null
  businessName?: string | null
  functionName?: string | null
  templateType: TemplateType
  genStatus: GenStatus
  lastGenTime?: DateTimeString | null
  status: EnableStatus
  createdTime: DateTimeString
  modifiedTime?: DateTimeString | null
}

/**
 * 代码生成表配置详情 DTO（镜像后端 CodeGenTableDetailDto : CodeGenTableListItemDto）。
 * 列配置随详情一并返回（后端 IReadOnlyList<CodeGenTableColumnListItemDto>）。
 */
export interface CodeGenTableDetailDto extends CodeGenTableListItemDto {
  namespace?: string | null
  author?: string | null
  genType: GenType
  genPath?: string | null
  parentMenuId?: ApiId | null
  primaryKeyColumn?: string | null
  treeParentColumn?: string | null
  treeNameColumn?: string | null
  masterTableId?: ApiId | null
  masterForeignKey?: string | null
  databaseType: DatabaseType
  /** 数据源标识（对应 SysCodeGenDataSource；为空表示主库） */
  dataSourceId?: ApiId | null
  options?: string | null
  remark?: string | null
  createdId?: ApiId | null
  createdBy?: string | null
  modifiedId?: ApiId | null
  modifiedBy?: string | null
  columns: CodeGenTableColumnListItemDto[]
}

/**
 * 代码生成表配置更新 DTO（表头信息维护，镜像后端 CodeGenTableUpdateDto : BasicAppUDto）。
 * 列配置另走列接口；状态走独立 Status 接口。
 */
export interface CodeGenTableUpdateDto extends BasicDto {
  tableName: string
  tableComment?: string | null
  className: string
  namespace?: string | null
  moduleName?: string | null
  businessName?: string | null
  functionName?: string | null
  author?: string | null
  templateType: TemplateType
  genType: GenType
  genPath?: string | null
  parentMenuId?: ApiId | null
  primaryKeyColumn?: string | null
  treeParentColumn?: string | null
  treeNameColumn?: string | null
  masterTableId?: ApiId | null
  masterForeignKey?: string | null
  databaseType: DatabaseType
  /** 数据源标识（对应 SysCodeGenDataSource；为空表示主库） */
  dataSourceId?: ApiId | null
  options?: string | null
  status: EnableStatus
  remark?: string | null
}

/**
 * 代码生成表配置状态更新 DTO（镜像后端 CodeGenTableStatusUpdateDto : BasicAppDto）。
 */
export interface CodeGenTableStatusUpdateDto extends BasicDto {
  status: EnableStatus
  remark?: string | null
}
