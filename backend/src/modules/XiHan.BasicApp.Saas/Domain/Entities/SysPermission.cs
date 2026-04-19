#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysPermission
// Guid:4c28152c-d6e9-4396-addb-b479254bad0e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 02:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统权限实体
/// 标准 RBAC 权限点：权限 = 资源(ResourceId) + 操作(OperationId)，是授权决策的最小原子单位
/// </summary>
/// <remarks>
/// 关联：
/// - ResourceId → SysResource（被控对象，必填）
/// - OperationId → SysOperation（执行动作，必填）
/// - 反向：SysRolePermission、SysUserPermission、SysTenantEditionPermission、SysPermissionCondition（ABAC 条件）
///
/// 写入：
/// - TenantId + PermissionCode 租户内唯一（UX_TeId_PeCo），建议格式 "{resource}:{operation}"
/// - TenantId + ResourceId + OperationId 租户内唯一（UX_TeId_ReId_OpId），防止重复授权
/// - IsGlobal=true 时 TenantId 必须为 0（平台租户占位），作为平台模板供订阅版本引用（SysTenantEditionPermission）
/// - IsRequireAudit=true 时，该权限的操作应强制写 SysAuditLog
///
/// 查询：
/// - 全局权限 + 租户私有权限合并查询：WHERE TenantId = ? OR IsGlobal = 1
/// - 按资源反查权限：走 IX_ReId
///
/// 删除：
/// - 仅软删；删除前须校验：无角色/用户仍授权（SysRolePermission/SysUserPermission）、无菜单引用（SysMenu.PermissionId）、无 Edition 引用（SysTenantEditionPermission）
///
/// 状态与优先级：
/// - Status: Yes=启用 / No=停用
/// - Priority: 数字越大越高；冲突合并时用于决策覆盖（参考 RBAC 决策算法 deny-overrides + priority）
///
/// 场景：
/// - 权限定义：user:read / order:approve 等
/// - 菜单按钮鉴权入口：SysMenu.PermissionId 反向绑定
/// - 版本门控：SysTenantEditionPermission 限定租户可用权限集
/// </remarks>
[SugarTable("SysPermission", "系统权限表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_PeCo", nameof(TenantId), OrderByType.Asc, nameof(PermissionCode), OrderByType.Asc, true)]
[SugarIndex("UX_{table}_TeId_ReId_OpId", nameof(TenantId), OrderByType.Asc, nameof(ResourceId), OrderByType.Asc, nameof(OperationId), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_ReId", nameof(ResourceId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_{table}_IsGl", nameof(IsGlobal), OrderByType.Asc)]
public partial class SysPermission : BasicAppFullAuditedEntity
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
    [SugarColumn(ColumnDescription = "权限编码", Length = 200, IsNullable = false)]
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
    public virtual bool IsRequireAudit { get; set; } = false;

    /// <summary>
    /// 是否平台级全局权限（全局权限所有租户共享，TenantId = 0）
    /// </summary>
    [SugarColumn(ColumnDescription = "是否全局权限")]
    public virtual bool IsGlobal { get; set; } = false;

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
