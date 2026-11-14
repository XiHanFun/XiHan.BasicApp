// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Auditing;

namespace XiHan.BasicApp.Auditing.Domain.Entities;

/// <summary>
/// 登录日志：登录、刷新、退出、切换租户、模仿与本人改密
/// </summary>
/// <remarks>
/// 身份事件（<see cref="LoginEvent"/>：登录、刷新、退出、切换租户、模仿、改密、双因素、激活角色）单独成表，成败都记：账号安全审计与个人中心的登录记录按账号看经历，访问日志按调用记，登录时调用方还是匿名，拼不出这条线
/// 主体是账号本身而不是调用方：用户、登录名、会话与租户取登记的值，没有再取调用方；认不出账号的失败登录只有填的登录名，所以登录名另建索引
/// 写入方 Identity 与 Access 在 Tier 1 不引用本包，经 Contracts 的 <see cref="LoginLogEntry"/> 在调用里登记；调用结束后才按最终结果定成败，中途失败返回之前已填的照样记下
/// <see cref="AccountTenantId"/> 存登录时填的租户原值：目录里不存在或停用的租户，日志落在平台一侧，原值留着供排查
/// 模仿期间的事件记发起人：被模仿的人在自己的登录记录里能分出哪些是别人模仿时留下的
/// 不受访问日志的开关与排除名单影响；分表、异步批量写入与只追加同其他分表日志（见 <see cref="AuditLogRecord"/>）
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
