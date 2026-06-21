#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenerationDtos
// Guid:c0de9e00-0406-4a00-9000-000000000406
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

#pragma warning disable CS1591

using XiHan.BasicApp.CodeGeneration.Domain.Enums;

namespace XiHan.BasicApp.CodeGeneration.Application.Dtos;

/// <summary>
/// 数据库表列表查询 DTO（逆向工程：列出可导入的库表）
/// </summary>
public sealed class CodeGenDbTableQueryDto
{
    /// <summary>连接配置标识（对应 SysCodeGenDataSource；为空表示主库）</summary>
    public string? ConnectionConfigId { get; set; }

    /// <summary>表名关键字过滤</summary>
    public string? Keyword { get; set; }
}

/// <summary>
/// 导入数据库表 DTO（从库表生成一条 SysCodeGenTable 配置 + 列配置）
/// </summary>
public sealed class CodeGenImportTableDto
{
    public string TableName { get; set; } = string.Empty;
    public string? ConnectionConfigId { get; set; }
    public string? ClassName { get; set; }
    public string? Namespace { get; set; }
    public string? ModuleName { get; set; }
    public string? BusinessName { get; set; }
    public string? FunctionName { get; set; }
    public string? Author { get; set; }
    public DatabaseType DatabaseType { get; set; } = DatabaseType.MySql;
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

    /// <summary>Zip 包体（Base64）；GenType.Zip 时填充。TODO(S3)：改为下载令牌 + 流式下载端点。</summary>
    public string? PackageBase64 { get; set; }
}
