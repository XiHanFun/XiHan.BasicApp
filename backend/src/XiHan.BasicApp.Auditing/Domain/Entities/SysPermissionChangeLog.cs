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
/// 授权面的变更单独成表，按语义（<see cref="PermissionChangeType"/>）记角色、用户、套餐与租户几个维度：权限是安全边界，要回答「谁在何时把什么权限给了谁或收回」，数据变更日志只看得到行的增删，看不出给了哪个角色哪些权限码
/// 只在调用成功时落库：失败的调用已回滚，什么也没变；警告或只记录的约束规则违反也记在这里，留下放行的证据
/// 写入方 Access 与 Tenancy 在 Tier 1 不引用本包，经 Contracts 的 <see cref="PermissionChangeLogEntry"/> 在调用里登记；系统自动做的指派与解除（新租户建内建管理员、成员移出租户）同样登记，不只记手工操作
/// 角色名、目标用户的登录名与套餐编码是写入时的快照，对象删除或改名后日志仍看得懂；操作人就是公共列里的用户，不另设列
/// 另按目标用户建索引：查一个人的权限被谁改过；分表、异步批量写入与只追加同其他分表日志（见 <see cref="AuditLogRecord"/>）
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
