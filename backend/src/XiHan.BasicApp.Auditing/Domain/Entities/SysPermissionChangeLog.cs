// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Auditing.Domain.Entities;

/// <summary>
/// 权限变更日志
/// </summary>
/// <remarks>
/// 记角色授予与撤销、成员加入与移出、数据范围、删除角色、套餐白名单与租户换套餐
/// 由 Access / Tenancy 在调用里登记（<see cref="PermissionChangeLogEntry"/>），只在调用成功时落库；操作人是公共列里的用户
/// </remarks>
[SugarTable(TableName = "sys_permission_change_log_{year}{month}{day}", TableDescription = "权限变更日志")]
[SplitTable(SplitType.Month)]
[SugarIndex("ix_{split_table}_tenant_id_occurred_at", nameof(TenantId), OrderByType.Asc, nameof(OccurredAt), OrderByType.Asc)]
[SugarIndex("ix_{split_table}_trace_id", nameof(TraceId), OrderByType.Asc)]
[SugarIndex("ix_{split_table}_user_id_occurred_at", nameof(UserId), OrderByType.Asc, nameof(OccurredAt), OrderByType.Asc)]
[SugarIndex("ix_{split_table}_target_user_id", nameof(TargetUserId), OrderByType.Asc)]
public sealed class SysPermissionChangeLog : AuditLogRecord
{
    /// <summary>
    /// 变更类型
    /// </summary>
    [SugarColumn(ColumnName = "change_type", IsNullable = false, ColumnDescription = "变更类型")]
    public PermissionChangeType ChangeType { get; set; }

    /// <summary>
    /// 角色
    /// </summary>
    [SugarColumn(ColumnName = "role_id", IsNullable = true, ColumnDescription = "角色")]
    public Guid? RoleId { get; set; }

    /// <summary>
    /// 角色名称（写入时的快照）
    /// </summary>
    [SugarColumn(ColumnName = "role_name", Length = 256, IsNullable = true, ColumnDescription = "角色名称（写入时的快照）")]
    public string? RoleName { get; set; }

    /// <summary>
    /// 目标用户
    /// </summary>
    [SugarColumn(ColumnName = "target_user_id", IsNullable = true, ColumnDescription = "目标用户")]
    public Guid? TargetUserId { get; set; }

    /// <summary>
    /// 目标用户的登录名（写入时的快照）
    /// </summary>
    [SugarColumn(ColumnName = "target_user_name", Length = 256, IsNullable = true, ColumnDescription = "目标用户的登录名（写入时的快照）")]
    public string? TargetUserName { get; set; }

    /// <summary>
    /// 套餐
    /// </summary>
    [SugarColumn(ColumnName = "edition_id", IsNullable = true, ColumnDescription = "套餐")]
    public Guid? EditionId { get; set; }

    /// <summary>
    /// 套餐编码（写入时的快照）
    /// </summary>
    [SugarColumn(ColumnName = "edition_code", Length = 64, IsNullable = true, ColumnDescription = "套餐编码（写入时的快照）")]
    public string? EditionCode { get; set; }

    /// <summary>
    /// 换套餐或转移所有权的租户
    /// </summary>
    [SugarColumn(ColumnName = "target_tenant_id", Length = 64, IsNullable = true, ColumnDescription = "换套餐或转移所有权的租户")]
    public string? TargetTenantId { get; set; }

    /// <summary>
    /// 涉及的权限码，逗号分隔
    /// </summary>
    [SugarColumn(ColumnName = "permission_codes", IsNullable = true, ColumnDescription = "涉及的权限码，逗号分隔")]
    [LongText]
    public string? PermissionCodes { get; set; }

    /// <summary>
    /// 补充说明
    /// </summary>
    [SugarColumn(ColumnName = "detail", Length = 512, IsNullable = true, ColumnDescription = "补充说明")]
    public string? Detail { get; set; }
}
