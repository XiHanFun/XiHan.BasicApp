// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma warning disable CS1591

using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.Core.Dtos;

namespace XiHan.BasicApp.CodeGeneration.Application.Dtos;

/// <summary>
/// 代码生成历史分页查询 DTO
/// </summary>
public sealed class CodeGenHistoryPageQueryDto : BasicAppPRDto
{
    public long? TableId { get; set; }
    public string? TableName { get; set; }
    public string? BatchNumber { get; set; }
    public GenStatus? GenStatus { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
}

/// <summary>
/// 代码生成历史列表项 DTO
/// </summary>
public class CodeGenHistoryListItemDto : BasicAppDto
{
    public long TableId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string? BatchNumber { get; set; }
    public GenStatus GenStatus { get; set; }
    public GenType GenType { get; set; }
    public DateTimeOffset GenTime { get; set; }
    public long Duration { get; set; }
    public int FileCount { get; set; }
    public long TotalSize { get; set; }
    public string? OperatorName { get; set; }
}

/// <summary>
/// 代码生成历史详情 DTO（含产物清单与错误信息快照）
/// </summary>
public sealed class CodeGenHistoryDetailDto : CodeGenHistoryListItemDto
{
    public string? GenPath { get; set; }
    public string? DownloadPath { get; set; }
    public string? GeneratedFiles { get; set; }
    public string? UsedTemplates { get; set; }
    public string? TableSnapshot { get; set; }
    public string? ErrorMessage { get; set; }
    public string? OperatorIp { get; set; }
    public string? Remark { get; set; }
}
