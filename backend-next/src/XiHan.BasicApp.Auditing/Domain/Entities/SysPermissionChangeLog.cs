// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using SqlSugar;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Auditing.Domain.Entities;

/// <summary>
/// 权限变更日志：角色授予与撤销、成员加入与移出、数据范围、删除角色、套餐白名单与租户换套餐。
/// 由 Access / Tenancy 在调用里登记（<see cref="PermissionChangeLogEntry"/>），只在调用成功时落库；操作人是公共列里的用户。
/// </summary>
[SugarTable("sys_permission_change_log_{year}{month}{day}")]
[SplitTable(SplitType.Month)]
public sealed class SysPermissionChangeLog : AuditLogRecord
{
    /// <summary>
    /// 变更类型。
    /// </summary>
    public PermissionChangeType ChangeType { get; set; }

    /// <summary>
    /// 角色。
    /// </summary>
    public Guid? RoleId { get; set; }

    /// <summary>
    /// 角色名称（写入时的快照）。
    /// </summary>
    [StringLength(256)]
    public string? RoleName { get; set; }

    /// <summary>
    /// 目标用户。
    /// </summary>
    public Guid? TargetUserId { get; set; }

    /// <summary>
    /// 目标用户的登录名（写入时的快照）。
    /// </summary>
    [StringLength(256)]
    public string? TargetUserName { get; set; }

    /// <summary>
    /// 套餐。
    /// </summary>
    public Guid? EditionId { get; set; }

    /// <summary>
    /// 套餐编码（写入时的快照）。
    /// </summary>
    [StringLength(64)]
    public string? EditionCode { get; set; }

    /// <summary>
    /// 换套餐的租户。
    /// </summary>
    [StringLength(64)]
    public string? TargetTenantId { get; set; }

    /// <summary>
    /// 涉及的权限码，逗号分隔。
    /// </summary>
    [LongText]
    public string? PermissionCodes { get; set; }

    /// <summary>
    /// 补充说明。
    /// </summary>
    [StringLength(512)]
    public string? Detail { get; set; }
}
