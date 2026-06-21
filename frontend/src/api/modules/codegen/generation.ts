import type { DynamicApiParams } from '../../base'
import type {
  CodeGenDbTableQueryDto,
  CodeGenGenerateRequestDto,
  CodeGenImportTableDto,
  CodeGenPreviewRequestDto,
  CodeGenResultDto,
} from './generation.types'
import type { CodeGenTableDetailDto } from './table.types'
import { appendDynamicApiParam, createDynamicApiClient } from '../../base'

const orchestration = createDynamicApiClient('CodeGeneration')

/**
 * 代码生成编排 API（CodeGeneration 控制器）。
 * 动作名直调（非 createReadApi）：DatabaseTables(GET) / ImportTable / Preview / Generate(POST)。
 */
export const codeGenerationApi = {
  /** 列出可导入的数据库表（逆向工程）。后端返回表名字符串数组 */
  listDatabaseTables(input: CodeGenDbTableQueryDto) {
    const params: DynamicApiParams = {}
    appendDynamicApiParam(params, 'ConnectionConfigId', input.connectionConfigId)
    appendDynamicApiParam(params, 'Keyword', input.keyword)
    return orchestration.get<string[]>('DatabaseTables', params)
  },
  /** 从数据库表导入并创建一条表配置（含列配置），返回表详情 */
  importTable(input: CodeGenImportTableDto) {
    return orchestration.post<CodeGenTableDetailDto, CodeGenImportTableDto>('ImportTable', input)
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
