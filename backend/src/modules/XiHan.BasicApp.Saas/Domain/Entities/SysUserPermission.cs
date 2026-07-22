// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统用户权限关联实体（直授）
/// </summary>
/// <remarks>
/// 用户直授权限优先级最高，可覆盖所有角色级别的权限决策：
/// - Deny：最终拒绝该权限，即使用户的所有角色都 Grant 了此权限
/// - Grant：最终授予该权限，即使用户的所有角色都未包含或 Deny 了此权限
/// 适用场景：临时提权、特殊用户例外、紧急权限收回
/// </remarks>
[SugarTable(TableName = "Sys_User_Permission", TableDescription = "系统用户权限关联表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_UsId_PeId", nameof(TenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc, nameof(PermissionId), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_UsId", nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_{table}_PeId", nameof(PermissionId), OrderByType.Asc)]
[SugarIndex("IX_{table}_EfTi", nameof(EffectiveTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_ExTi", nameof(ExpirationTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public partial class SysUserPermission : BasicAppCreationEntity
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnName = "User_Id", ColumnDescription = "用户ID", IsNullable = false)]
    public virtual long UserId { get; set; }

    /// <summary>
    /// 权限ID
    /// </summary>
    [SugarColumn(ColumnName = "Permission_Id", ColumnDescription = "权限ID", IsNullable = false)]
    public virtual long PermissionId { get; set; }

    /// <summary>
    /// 权限操作（授予/禁用）
    /// </summary>
    [SugarColumn(ColumnName = "Permission_Action", ColumnDescription = "权限操作")]
    public virtual PermissionAction PermissionAction { get; set; } = PermissionAction.Grant;

    /// <summary>
    /// 生效时间
    /// </summary>
    [SugarColumn(ColumnName = "Effective_Time", ColumnDescription = "生效时间", IsNullable = true)]
    public virtual DateTimeOffset? EffectiveTime { get; set; }

    /// <summary>
    /// 失效时间
    /// </summary>
    [SugarColumn(ColumnName = "Expiration_Time", ColumnDescription = "失效时间", IsNullable = true)]
    public virtual DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// 授权原因（关联审批单号、工单号等，用于审计追溯）
    /// </summary>
    [SugarColumn(ColumnName = "Grant_Reason", ColumnDescription = "授权原因", Length = 500, IsNullable = true)]
    public virtual string? GrantReason { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "Status", ColumnDescription = "状态")]
    public virtual ValidityStatus Status { get; set; } = ValidityStatus.Valid;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
