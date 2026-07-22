// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 导入历史 DTO
/// </summary>
public sealed class ImportHistoryDto
{
    /// <summary>
    /// 主键
    /// </summary>
    public long BasicId { get; set; }

    /// <summary>
    /// 页面码
    /// </summary>
    public string PageCode { get; set; } = string.Empty;

    /// <summary>
    /// 资源码
    /// </summary>
    public string? ResourceCode { get; set; }

    /// <summary>
    /// 导入文件名
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 数据行总数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 成功行数
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 失败行数
    /// </summary>
    public int FailCount { get; set; }

    /// <summary>
    /// 错误摘要（JSON）
    /// </summary>
    public string? ErrorSummary { get; set; }

    /// <summary>
    /// 导入时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }
}

/// <summary>
/// 新增导入历史入参（导入执行完毕后由前端上报）
/// </summary>
public sealed class ImportHistoryCreateDto
{
    /// <summary>
    /// 页面码
    /// </summary>
    public string PageCode { get; set; } = string.Empty;

    /// <summary>
    /// 资源码
    /// </summary>
    public string? ResourceCode { get; set; }

    /// <summary>
    /// 导入文件名
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 数据行总数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 成功行数
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 失败行数
    /// </summary>
    public int FailCount { get; set; }

    /// <summary>
    /// 错误摘要（JSON，服务端截断保护）
    /// </summary>
    public string? ErrorSummary { get; set; }
}
