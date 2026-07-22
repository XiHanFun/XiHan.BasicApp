// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
/// - TenantId = 0（即派生属性 IsGlobal=true）作为平台模板供订阅版本引用（SysTenantEditionPermission）
/// - IsRequireAudit=true 时，该权限的操作应强制写 SysDiffLog
///
/// 查询：
/// - 全局权限 + 租户私有权限合并查询优先使用：WHERE TenantId IN (0, ?)
///   平台记录以 TenantId=0 唯一约束；IsGlobal 为派生只读属性（= TenantId==0）
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
[SugarTable(TableName = "Sys_Permission", TableDescription = "系统权限表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_PeCo", nameof(TenantId), OrderByType.Asc, nameof(PermissionCode), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_TeId_ReId_OpId", nameof(TenantId), OrderByType.Asc, nameof(ResourceId), OrderByType.Asc, nameof(OperationId), OrderByType.Asc)]
[SugarIndex("IX_{table}_ReId", nameof(ResourceId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_PeTy", nameof(TenantId), OrderByType.Asc, nameof(PermissionType), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public partial class SysPermission : BasicAppAggregateRoot
{
    /// <summary>
    /// 权限类型
    /// </summary>
    [SugarColumn(ColumnName = "Permission_Type", ColumnDescription = "权限类型")]
    public virtual PermissionType PermissionType { get; set; } = PermissionType.ResourceBased;

    /// <summary>
    /// 资源ID（关联 SysResource 表，ResourceBased 类型必填）
    /// </summary>
    [SugarColumn(ColumnName = "Resource_Id", ColumnDescription = "资源ID", IsNullable = true)]
    public virtual long? ResourceId { get; set; }

    /// <summary>
    /// 操作ID（关联 SysOperation 表，ResourceBased 类型必填）
    /// </summary>
    [SugarColumn(ColumnName = "Operation_Id", ColumnDescription = "操作ID", IsNullable = true)]
    public virtual long? OperationId { get; set; }

    /// <summary>
    /// 所属模块编码（支持三段式权限码 module:resource:action，如 saas/crm/billing）
    /// </summary>
    [SugarColumn(ColumnName = "Module_Code", ColumnDescription = "模块编码", Length = 50, IsNullable = true)]
    public virtual string? ModuleCode { get; set; }

    /// <summary>
    /// 权限编码（唯一标识，推荐三段式格式：模块编码:资源编码:操作编码，如：saas:user:create）
    /// </summary>
    [SugarColumn(ColumnName = "Permission_Code", ColumnDescription = "权限编码", Length = 200, IsNullable = false)]
    public virtual string PermissionCode { get; set; } = string.Empty;

    /// <summary>
    /// 权限名称
    /// </summary>
    [SugarColumn(ColumnName = "Permission_Name", ColumnDescription = "权限名称", Length = 200, IsNullable = false)]
    public virtual string PermissionName { get; set; } = string.Empty;

    /// <summary>
    /// 权限描述
    /// </summary>
    [SugarColumn(ColumnName = "Permission_Description", ColumnDescription = "权限描述", Length = 500, IsNullable = true)]
    public virtual string? PermissionDescription { get; set; }

    /// <summary>
    /// 权限标签（JSON 数组格式，如：["admin","sensitive","audit"]）
    /// 用于权限分类和快速筛选
    /// </summary>
    [SugarColumn(ColumnName = "Tags", ColumnDescription = "权限标签", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? Tags { get; set; }

    /// <summary>
    /// 是否需要审计（操作此权限是否需要记录差异日志）
    /// </summary>
    [SugarColumn(ColumnName = "Is_Require_Audit", ColumnDescription = "是否需要审计")]
    public virtual bool IsRequireAudit { get; set; } = false;

    /// <summary>
    /// 优先级（数字越大优先级越高；仅用于同级别 Grant/Deny 之间的排序，不参与 Grant vs Deny 的跨级覆盖决策——Deny 始终优先于 Grant）
    /// </summary>
    [SugarColumn(ColumnName = "Priority", ColumnDescription = "优先级")]
    public virtual int Priority { get; set; } = 0;

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "Status", ColumnDescription = "状态")]
    public virtual EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnName = "Sort", ColumnDescription = "排序")]
    public virtual int Sort { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
