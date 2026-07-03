import type { ApiId, BasicDto } from '../../types'
import type { EnableStatus } from '../shared'
import type { DictSelectorType, HtmlType, QueryType } from './codegen.enums'

export { EnableStatus } from '../shared'

/**
 * 代码生成列配置列表项 DTO（镜像后端 CodeGenTableColumnListItemDto : BasicAppDto）。
 */
export interface CodeGenTableColumnListItemDto extends BasicDto {
  tableId: ApiId
  columnName: string
  columnComment?: string | null
  columnType?: string | null
  cSharpType?: string | null
  cSharpProperty?: string | null
  tsType?: string | null
  columnLength?: number | null
  decimalDigits?: number | null
  isPrimaryKey: boolean
  isIdentity: boolean
  isNullable: boolean
  isRequired: boolean
  isList: boolean
  isInsert: boolean
  isEdit: boolean
  isQuery: boolean
  queryType: QueryType
  htmlType: HtmlType
  dictSelectorType?: DictSelectorType | null
  dictCode?: string | null
  enumTypeName?: string | null
  constValues?: string | null
  sort: number
  status: EnableStatus
}

/**
 * 代码生成列配置更新 DTO（单列编辑，镜像后端 CodeGenTableColumnUpdateDto : BasicAppUDto）。
 */
export interface CodeGenTableColumnUpdateDto extends BasicDto {
  columnComment?: string | null
  cSharpType?: string | null
  cSharpProperty?: string | null
  tsType?: string | null
  isRequired: boolean
  isList: boolean
  isInsert: boolean
  isEdit: boolean
  isQuery: boolean
  queryType: QueryType
  htmlType: HtmlType
  dictSelectorType?: DictSelectorType | null
  dictCode?: string | null
  enumTypeName?: string | null
  constValues?: string | null
  defaultValue?: string | null
  regexPattern?: string | null
  validationMessage?: string | null
  sort: number
  status: EnableStatus
}

/**
 * 代码生成列配置批量保存 DTO（按表整体提交，镜像后端 CodeGenTableColumnBatchSaveDto）。
 */
export interface CodeGenTableColumnBatchSaveDto {
  tableId: ApiId
  columns: CodeGenTableColumnUpdateDto[]
}
