import type { ApiId, NumericString } from '../../types'
import type { ArtifactWriteMode, DatabaseType, GenType } from './codegen.enums'

/**
 * 数据库表列表查询 DTO（逆向工程：列出可导入的库表）
 * 后端 CodeGenDbTableQueryDto
 */
export interface CodeGenDbTableQueryDto {
  /** 连接配置标识（对应数据源；为空表示主库）。普通 string，非雪花 ApiId */
  connectionConfigId?: string | null
  /** 表名关键字过滤 */
  keyword?: string | null
}

/**
 * 导入数据库表 DTO（从库表生成一条表配置 + 列配置）
 * 后端 CodeGenImportTableDto
 */
export interface CodeGenImportTableDto {
  tableName: string
  connectionConfigId?: string | null
  className?: string | null
  namespace?: string | null
  moduleName?: string | null
  businessName?: string | null
  functionName?: string | null
  author?: string | null
  databaseType: DatabaseType
}

/**
 * 代码生成预览请求 DTO
 * 后端 CodeGenPreviewRequestDto
 */
export interface CodeGenPreviewRequestDto {
  /** 表标识（后端 long → ApiId 字符串） */
  tableId: ApiId
  /** 指定模板编码（为空表示按表模块取全部启用模板） */
  templateCodes?: string[] | null
}

/**
 * 代码生成执行请求 DTO
 * 后端 CodeGenGenerateRequestDto
 */
export interface CodeGenGenerateRequestDto {
  tableId: ApiId
  templateCodes?: string[] | null
  /** 生成方式（预览/Zip） */
  genType: GenType
}

/**
 * 生成产物 DTO（单文件）
 * 后端 CodeGenArtifactDto
 */
export interface CodeGenArtifactDto {
  relativePath: string
  fileName: string
  content: string
  templateCode?: string | null
  /** 写入策略：机器文件总是覆盖；人类文件仅在目标不存在时创建 */
  writeMode: ArtifactWriteMode
}

/**
 * 代码生成结果 DTO（预览返回内容；Zip 返回 Base64 包体）
 * 后端 CodeGenResultDto
 */
export interface CodeGenResultDto {
  success: boolean
  message?: string | null
  fileCount: number
  /** 耗时毫秒（后端 long → NumericString，运算需 Number()） */
  durationMilliseconds: NumericString
  /** 产物清单（预览 / 文件树展示用） */
  artifacts: CodeGenArtifactDto[]
  /** 实际写入文件数（GenType.CustomPath 时填充） */
  writtenCount: number
  /** 被跳过的人类文件相对路径（GenType.CustomPath 时填充；目标已存在，未覆盖） */
  skippedPaths: string[]
  /** Zip 包体（Base64）；GenType.Zip 时填充 */
  packageBase64?: string | null
}
