// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using SqlSugar;
using XiHan.BasicApp.Contracts.Auditing;

namespace XiHan.BasicApp.Auditing.Domain.Entities;

/// <summary>
/// 登录日志：登录、刷新、退出、切换租户、模仿与本人改密。
/// </summary>
/// <remarks>
/// 成功失败都记。由 Identity 在调用里登记（<see cref="LoginLogEntry"/>），调用结束后按结果落库。
/// </remarks>
[SugarTable("sys_login_log_{year}{month}{day}")]
[SplitTable(SplitType.Month)]
public sealed class SysLoginLog : AuditLogRecord
{
    /// <summary>
    /// 事件。
    /// </summary>
    public LoginEvent Event { get; set; }

    /// <summary>
    /// 是否成功。
    /// </summary>
    public bool Succeeded { get; set; }

    /// <summary>
    /// 失败的错误码。
    /// </summary>
    [StringLength(64)]
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 失败的错误消息（按调用方语言渲染）。
    /// </summary>
    [StringLength(1024)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 账号所在的租户（登录时填的租户，平台账号为 <c>platform</c>）。
    /// </summary>
    [StringLength(64)]
    public string? AccountTenantId { get; set; }

    /// <summary>
    /// 模仿事件的发起人。
    /// </summary>
    public Guid? ImpersonatorUserId { get; set; }

    /// <summary>
    /// 补充说明。
    /// </summary>
    [StringLength(512)]
    public string? Detail { get; set; }

    /// <summary>
    /// 浏览器（由 User-Agent 解析）。
    /// </summary>
    [StringLength(64)]
    public string? Browser { get; set; }

    /// <summary>
    /// 操作系统（由 User-Agent 解析）。
    /// </summary>
    [StringLength(64)]
    public string? Os { get; set; }

    /// <summary>
    /// 设备类型（由 User-Agent 解析）。
    /// </summary>
    [StringLength(32)]
    public string? Device { get; set; }
}
