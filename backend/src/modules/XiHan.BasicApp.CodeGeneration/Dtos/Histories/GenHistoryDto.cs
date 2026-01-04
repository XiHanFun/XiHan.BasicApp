#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:GenHistoryDto
// Guid:a1b2c3d4-e5f6-7890-abcd-ef1234567025
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/23 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.CodeGeneration.Dtos.Base;
using XiHan.BasicApp.CodeGeneration.Enums;
using XiHan.BasicApp.Core;

namespace XiHan.BasicApp.CodeGeneration.Dtos.Histories;

/// <summary>
/// 代码生成历史记录 DTO
/// </summary>
public class GenHistoryDto : CodeGenFullAuditedDtoBase
{
    /// <summary>
    /// 所属表ID
    /// </summary>
    public long TableId { get; set; }

    /// <summary>
    /// 表名称
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// 生成批次号
    /// </summary>
    public string? BatchNumber { get; set; }

    /// <summary>
    /// 生成状态
    /// </summary>
    public GenStatus GenStatus { get; set; }

    /// <summary>
    /// 生成方式
    /// </summary>
    public GenType GenType { get; set; }

    /// <summary>
    /// 生成时间
    /// </summary>
    public DateTimeOffset GenTime { get; set; }

    /// <summary>
    /// 生成耗时（毫秒）
    /// </summary>
    public long Duration { get; set; }

    /// <summary>
    /// 生成文件数量
    /// </summary>
    public int FileCount { get; set; }

    /// <summary>
    /// 生成文件总大小（字节）
    /// </summary>
    public long TotalSize { get; set; }

    /// <summary>
    /// 生成路径
    /// </summary>
    public string? GenPath { get; set; }

    /// <summary>
    /// 下载路径
    /// </summary>
    public string? DownloadPath { get; set; }

    /// <summary>
    /// 生成文件列表
    /// </summary>
    public string? GeneratedFiles { get; set; }

    /// <summary>
    /// 使用的模板列表
    /// </summary>
    public string? UsedTemplates { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 操作用户ID
    /// </summary>
    public long? OperatorId { get; set; }

    /// <summary>
    /// 操作用户名
    /// </summary>
    public string? OperatorName { get; set; }

    /// <summary>
    /// 操作IP
    /// </summary>
    public string? OperatorIp { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 代码生成历史详情 DTO
/// </summary>
public class GenHistoryDetailDto : GenHistoryDto
{
    /// <summary>
    /// 表配置快照
    /// </summary>
    public string? TableSnapshot { get; set; }

    /// <summary>
    /// 生成文件详情列表
    /// </summary>
    public List<GeneratedFileDto>? GeneratedFileDetails { get; set; }

    /// <summary>
    /// 使用的模板详情列表
    /// </summary>
    public List<UsedTemplateDto>? UsedTemplateDetails { get; set; }
}

/// <summary>
/// 生成文件详情 DTO
/// </summary>
public class GeneratedFileDto
{
    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 模板名称
    /// </summary>
    public string? TemplateName { get; set; }
}

/// <summary>
/// 使用的模板 DTO
/// </summary>
public class UsedTemplateDto
{
    /// <summary>
    /// 模板ID
    /// </summary>
    public long TemplateId { get; set; }

    /// <summary>
    /// 模板编码
    /// </summary>
    public string TemplateCode { get; set; } = string.Empty;

    /// <summary>
    /// 模板名称
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;
}
