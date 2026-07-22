// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma warning disable CS1591

using XiHan.BasicApp.CodeGeneration.Domain.Enums;

namespace XiHan.BasicApp.CodeGeneration.Application.Dtos;

/// <summary>
/// 数据库表列表查询 DTO（逆向工程：列出可导入的库表）
/// </summary>
public sealed class CodeGenDbTableQueryDto
{
    /// <summary>连接配置标识（对应 SysCodeGenDataSource；为空表示主库）</summary>
    public long? DataSourceId { get; set; }

    /// <summary>表名关键字过滤</summary>
    public string? Keyword { get; set; }
}

/// <summary>
/// 导入数据库表 DTO（从库表生成一条 SysCodeGenTable 配置 + 列配置）
/// </summary>
public sealed class CodeGenImportTableDto
{
    public string TableName { get; set; } = string.Empty;
    public long? DataSourceId { get; set; }
    public string? ClassName { get; set; }
    public string? Namespace { get; set; }
    public string? ModuleName { get; set; }
    public string? BusinessName { get; set; }
    public string? FunctionName { get; set; }
    public string? Author { get; set; }
    public DatabaseType DatabaseType { get; set; } = DatabaseType.MySql;
}

/// <summary>
/// 批量导入数据库表 DTO
/// </summary>
public sealed class CodeGenImportTablesDto
{
    /// <summary>数据源标识（对应 SysCodeGenDataSource；为空表示主库）</summary>
    public long? DataSourceId { get; set; }

    /// <summary>数据库类型</summary>
    public DatabaseType DatabaseType { get; set; } = DatabaseType.MySql;

    /// <summary>待导入的表名集合</summary>
    public IReadOnlyList<string> TableNames { get; set; } = [];
}

/// <summary>
/// 批量导入结果 DTO
/// </summary>
public sealed class CodeGenImportTablesResultDto
{
    /// <summary>成功导入的表名</summary>
    public IReadOnlyList<string> Succeeded { get; set; } = [];

    /// <summary>失败的表（表名 → 原因）</summary>
    public IReadOnlyList<CodeGenImportFailureDto> Failed { get; set; } = [];
}

/// <summary>
/// 批量导入失败明细
/// </summary>
public sealed class CodeGenImportFailureDto
{
    public string TableName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// 同步表结构结果 DTO
/// </summary>
public sealed class CodeGenSchemaSyncResultDto
{
    /// <summary>新增列数</summary>
    public int AddedCount { get; set; }

    /// <summary>更新列数（结构性字段或未冻结的推断字段有变化）</summary>
    public int UpdatedCount { get; set; }

    /// <summary>删除列数（库中已不存在）</summary>
    public int RemovedCount { get; set; }

    /// <summary>新增的列名</summary>
    public IReadOnlyList<string> AddedColumns { get; set; } = [];

    /// <summary>删除的列名</summary>
    public IReadOnlyList<string> RemovedColumns { get; set; } = [];
}

/// <summary>
/// 代码生成预览请求 DTO
/// </summary>
public sealed class CodeGenPreviewRequestDto
{
    public long TableId { get; set; }

    /// <summary>指定模板编码（为空表示按表模块取全部启用模板）</summary>
    public IReadOnlyList<string>? TemplateCodes { get; set; }
}

/// <summary>
/// 代码生成执行请求 DTO
/// </summary>
public sealed class CodeGenGenerateRequestDto
{
    public long TableId { get; set; }
    public IReadOnlyList<string>? TemplateCodes { get; set; }

    /// <summary>生成方式（预览/Zip；落盘默认禁用，见安全策略）</summary>
    public GenType GenType { get; set; } = GenType.Zip;
}

/// <summary>
/// 生成产物 DTO（单文件）
/// </summary>
public sealed class CodeGenArtifactDto
{
    public string RelativePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? TemplateCode { get; set; }

    /// <summary>写入策略：机器文件总是覆盖；人类文件仅在目标不存在时创建</summary>
    public ArtifactWriteMode WriteMode { get; set; } = ArtifactWriteMode.AlwaysOverwrite;
}

/// <summary>
/// 代码生成结果 DTO（预览返回内容；Zip 返回 Base64 包体）
/// </summary>
public sealed class CodeGenResultDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int FileCount { get; set; }
    public long DurationMilliseconds { get; set; }

    /// <summary>产物清单（预览 / 文件树展示用）</summary>
    public IReadOnlyList<CodeGenArtifactDto> Artifacts { get; set; } = [];

    /// <summary>实际写入文件数（GenType.CustomPath 时填充）</summary>
    public int WrittenCount { get; set; }

    /// <summary>被跳过的人类文件相对路径（GenType.CustomPath 时填充；目标已存在，未覆盖以保护自定义代码）</summary>
    public IReadOnlyList<string> SkippedPaths { get; set; } = [];

    /// <summary>Zip 包体（Base64）；GenType.Zip 时填充。TODO(S3)：改为下载令牌 + 流式下载端点。</summary>
    public string? PackageBase64 { get; set; }
}
