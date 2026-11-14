// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Auditing;

namespace XiHan.BasicApp.Auditing.Domain.Entities;

/// <summary>
/// 登录日志：登录、刷新、退出、切换租户、模仿与本人改密
/// </summary>
/// <remarks>
/// 成功失败都记。由 Identity 在调用里登记（<see cref="LoginLogEntry"/>），调用结束后按结果落库
/// </remarks>
[SugarTable(TableName = "sys_login_log_{year}{month}{day}", TableDescription = "登录日志：登录、刷新、退出、切换租户、模仿与本人改密")]
[SplitTable(SplitType.Month)]
[SugarIndex("ix_{split_table}_tenant_id_occurred_at", nameof(TenantId), OrderByType.Asc, nameof(OccurredAt), OrderByType.Asc)]
[SugarIndex("ix_{split_table}_trace_id", nameof(TraceId), OrderByType.Asc)]
[SugarIndex("ix_{split_table}_user_id_occurred_at", nameof(UserId), OrderByType.Asc, nameof(OccurredAt), OrderByType.Asc)]
[SugarIndex("ix_{split_table}_user_name", nameof(UserName), OrderByType.Asc)]
public sealed class SysLoginLog : AuditLogRecord
{
    /// <summary>
    /// 事件
    /// </summary>
    [SugarColumn(ColumnName = "event", IsNullable = false, ColumnDescription = "事件")]
    public LoginEvent Event { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    [SugarColumn(ColumnName = "succeeded", IsNullable = false, ColumnDescription = "是否成功")]
    public bool Succeeded { get; set; }

    /// <summary>
    /// 失败的错误码
    /// </summary>
    [SugarColumn(ColumnName = "error_code", Length = 64, IsNullable = true, ColumnDescription = "失败的错误码")]
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 失败的错误消息（按调用方语言渲染）
    /// </summary>
    [SugarColumn(ColumnName = "error_message", Length = 1024, IsNullable = true, ColumnDescription = "失败的错误消息（按调用方语言渲染）")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 账号所在的租户（登录时填的租户，平台账号为 <c>platform</c>）
    /// </summary>
    [SugarColumn(ColumnName = "account_tenant_id", Length = 64, IsNullable = true, ColumnDescription = "账号所在的租户（登录时填的租户，平台账号为 platform）")]
    public string? AccountTenantId { get; set; }

    /// <summary>
    /// 模仿事件的发起人
    /// </summary>
    [SugarColumn(ColumnName = "impersonator_user_id", IsNullable = true, ColumnDescription = "模仿事件的发起人")]
    public Guid? ImpersonatorUserId { get; set; }

    /// <summary>
    /// 补充说明
    /// </summary>
    [SugarColumn(ColumnName = "detail", Length = 512, IsNullable = true, ColumnDescription = "补充说明")]
    public string? Detail { get; set; }

    /// <summary>
    /// 浏览器（由 User-Agent 解析）
    /// </summary>
    [SugarColumn(ColumnName = "browser", Length = 64, IsNullable = true, ColumnDescription = "浏览器（由 User-Agent 解析）")]
    public string? Browser { get; set; }

    /// <summary>
    /// 操作系统（由 User-Agent 解析）
    /// </summary>
    [SugarColumn(ColumnName = "os", Length = 64, IsNullable = true, ColumnDescription = "操作系统（由 User-Agent 解析）")]
    public string? Os { get; set; }

    /// <summary>
    /// 设备类型（由 User-Agent 解析）
    /// </summary>
    [SugarColumn(ColumnName = "device", Length = 32, IsNullable = true, ColumnDescription = "设备类型（由 User-Agent 解析）")]
    public string? Device { get; set; }
}
