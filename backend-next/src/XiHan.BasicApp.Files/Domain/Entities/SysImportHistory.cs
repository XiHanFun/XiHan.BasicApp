// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Files.Domain.Entities;

/// <summary>
/// 导入历史：一次导入结束后由前端记下的结果。
/// </summary>
/// <remarks>
/// 租户库只追加的记录，平台一侧租户为 <c>platform</c>；4.x 同。
/// 导入在前端逐行调资源的新建操作完成，结束后由前端记一条结果，导入弹窗列出本人最近几次。
/// </remarks>
[SugarIndex("ix_{table}_tenant_id_user_id_page_code_created_at", nameof(TenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc, nameof(PageCode), OrderByType.Asc, nameof(CreatedAt), OrderByType.Asc)]
public sealed class SysImportHistory : TenantScopedRecord
{
    /// <summary>
    /// 导入的用户。
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 页面码。
    /// </summary>
    [StringLength(100)]
    public string PageCode { get; set; } = string.Empty;

    /// <summary>
    /// 资源码（导入调用的操作所属资源）。
    /// </summary>
    [StringLength(100)]
    public string? ResourceCode { get; set; }

    /// <summary>
    /// 导入的文件名。
    /// </summary>
    [StringLength(256)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 总行数。
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 成功行数。
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 失败行数。
    /// </summary>
    public int FailCount { get; set; }

    /// <summary>
    /// 错误摘要（前端给的 JSON，至多 8000 个字符）。
    /// </summary>
    [LongText]
    public string? ErrorSummary { get; set; }
}
