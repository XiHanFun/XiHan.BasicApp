// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Files.Domain.Entities;

/// <summary>
/// 导入历史：一次导入结束后由前端记下的结果
/// </summary>
/// <remarks>
/// 导入没有后端的批量接口：前端逐行调资源的新建操作，每行照常经过权限与校验；这张表只在导入结束后由前端记一条汇总，后端不经手导入的数据
/// 按侧、本人与页面码归类，导入弹窗列出本人在这一页最近几次，索引就按这一种读法建；错误摘要是前端给的 JSON，截到 8000 个字符
/// 租户库只追加的记录（平台一侧租户为 <c>platform</c>），写入后不改；没有删除，也没有清理作业
/// </remarks>
[SugarTable(TableName = "sys_import_history", TableDescription = "导入历史：一次导入结束后由前端记下的结果")]
[SugarIndex("ix_sys_import_history_tenant_id_user_id_page_code_created_at", nameof(TenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc, nameof(PageCode), OrderByType.Asc, nameof(CreatedAt), OrderByType.Asc)]
public sealed class SysImportHistory : TenantScopedRecord
{
    /// <summary>
    /// 导入的用户
    /// </summary>
    [SugarColumn(ColumnName = "user_id", IsNullable = false, ColumnDescription = "导入的用户")]
    public Guid UserId { get; set; }

    /// <summary>
    /// 页面码
    /// </summary>
    [SugarColumn(ColumnName = "page_code", Length = 100, IsNullable = false, ColumnDescription = "页面码")]
    public string PageCode { get; set; } = string.Empty;

    /// <summary>
    /// 资源码（导入调用的操作所属资源）
    /// </summary>
    [SugarColumn(ColumnName = "resource_code", Length = 100, IsNullable = true, ColumnDescription = "资源码（导入调用的操作所属资源）")]
    public string? ResourceCode { get; set; }

    /// <summary>
    /// 导入的文件名
    /// </summary>
    [SugarColumn(ColumnName = "file_name", Length = 256, IsNullable = false, ColumnDescription = "导入的文件名")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 总行数
    /// </summary>
    [SugarColumn(ColumnName = "total_count", IsNullable = false, ColumnDescription = "总行数")]
    public int TotalCount { get; set; }

    /// <summary>
    /// 成功行数
    /// </summary>
    [SugarColumn(ColumnName = "success_count", IsNullable = false, ColumnDescription = "成功行数")]
    public int SuccessCount { get; set; }

    /// <summary>
    /// 失败行数
    /// </summary>
    [SugarColumn(ColumnName = "fail_count", IsNullable = false, ColumnDescription = "失败行数")]
    public int FailCount { get; set; }

    /// <summary>
    /// 错误摘要（前端给的 JSON，至多 8000 个字符）
    /// </summary>
    [SugarColumn(ColumnName = "error_summary", IsNullable = true, ColumnDescription = "错误摘要（前端给的 JSON，至多 8000 个字符）")]
    [LongText]
    public string? ErrorSummary { get; set; }
}
