#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysPermission
// Guid:4c28152c-d6e9-4396-addb-b479254bad0e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 2:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统权限实体
/// 权限 = 资源 + 操作（标准 RBAC 模型）
/// </summary>
[SugarTable("Sys_Permission", "系统权限表")]
[SugarIndex("IX_SysPermission_PermissionCode", nameof(PermissionCode), OrderByType.Asc, true)]
[SugarIndex("IX_SysPermission_Resource_Operation", nameof(ResourceId), OrderByType.Asc, nameof(OperationId), OrderByType.Asc, true)]
[SugarIndex("IX_SysPermission_ResourceId", nameof(ResourceId), OrderByType.Asc)]
public partial class SysPermission : RbacFullAuditedEntity<long>
{
    /// <summary>
    /// 资源ID（关联 SysResource 表，必填）
    /// </summary>
    [SugarColumn(ColumnDescription = "资源ID", IsNullable = false)]
    public virtual long ResourceId { get; set; }

    /// <summary>
    /// 操作ID（关联 SysOperation 表，必填）
    /// </summary>
    [SugarColumn(ColumnDescription = "操作ID", IsNullable = false)]
    public virtual long OperationId { get; set; }

    /// <summary>
    /// 权限编码（唯一标识，格式：资源编码:操作编码，如：user:create, order:view）
    /// </summary>
    [SugarColumn(ColumnDescription = "权限编码", Length = 150, IsNullable = false)]
    public virtual string PermissionCode { get; set; } = string.Empty;

    /// <summary>
    /// 权限名称
    /// </summary>
    [SugarColumn(ColumnDescription = "权限名称", Length = 200, IsNullable = false)]
    public virtual string PermissionName { get; set; } = string.Empty;

    /// <summary>
    /// 权限描述
    /// </summary>
    [SugarColumn(ColumnDescription = "权限描述", Length = 500, IsNullable = true)]
    public virtual string? PermissionDescription { get; set; }

    /// <summary>
    /// 权限标签（多个标签用逗号分隔，如：admin,sensitive,audit）
    /// 用于权限分类和快速筛选
    /// </summary>
    [SugarColumn(ColumnDescription = "权限标签", Length = 200, IsNullable = true)]
    public virtual string? Tags { get; set; }

    /// <summary>
    /// 是否需要审计（操作此权限是否需要记录审计日志）
    /// </summary>
    [SugarColumn(ColumnDescription = "是否需要审计")]
    public virtual bool RequireAudit { get; set; } = false;

    /// <summary>
    /// 优先级（数字越大优先级越高，用于权限冲突时的决策）
    /// </summary>
    [SugarColumn(ColumnDescription = "优先级")]
    public virtual int Priority { get; set; } = 0;

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public virtual YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnDescription = "排序")]
    public virtual int Sort { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
