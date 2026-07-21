import type { DynamicApiParams } from '../../base'
import type { ApiId } from '../../types'
import type {
  CodeGenDbTableQueryDto,
  CodeGenGenerateRequestDto,
  CodeGenImportTableDto,
  CodeGenImportTablesDto,
  CodeGenImportTablesResultDto,
  CodeGenPreviewRequestDto,
  CodeGenResultDto,
  CodeGenSchemaSyncResultDto,
} from './generation.types'
import type { CodeGenTableDetailDto } from './table.types'
import { appendDynamicApiParam, createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const orchestration = createDynamicApiClient('CodeGeneration')

/**
 * 代码生成编排 API（CodeGeneration 控制器）。
 * 动作名直调（非 createReadApi）：DatabaseTables(GET) / ImportTable / Preview / Generate(POST)。
 */
export const codeGenerationApi = {
  /** 列出可导入的数据库表（逆向工程）。后端返回表名字符串数组 */
  listDatabaseTables(input: CodeGenDbTableQueryDto) {
    const params: DynamicApiParams = {}
    appendDynamicApiParam(params, 'DataSourceId', input.dataSourceId)
    appendDynamicApiParam(params, 'Keyword', input.keyword)
    return orchestration.get<string[]>('DatabaseTables', params)
  },
  /** 从数据库表导入并创建一条表配置（含列配置），返回表详情 */
  importTable(input: CodeGenImportTableDto) {
    return orchestration.post<CodeGenTableDetailDto, CodeGenImportTableDto>('ImportTable', input)
  },
  /** 批量从数据库表导入（逐表隔离异常，返回成功/失败明细） */
  importTables(input: CodeGenImportTablesDto) {
    return orchestration.post<CodeGenImportTablesResultDto, CodeGenImportTablesDto>('ImportTables', input)
  },
  /** 同步表结构：人工改过的字段冻结，其余跟随最新表结构重新推断 */
  syncSchema(tableId: ApiId) {
    return orchestration.post<CodeGenSchemaSyncResultDto>(`SyncSchema/${formatDynamicApiRouteValue(tableId)}`)
  },
  /** 预览生成（仅返回产物内容） */
  preview(input: CodeGenPreviewRequestDto) {
    return orchestration.post<CodeGenResultDto, CodeGenPreviewRequestDto>('Preview', input)
  },
  /** 执行生成（按 GenType 分流：预览/Zip） */
  generate(input: CodeGenGenerateRequestDto) {
    return orchestration.post<CodeGenResultDto, CodeGenGenerateRequestDto>('Generate', input)
  },
}
