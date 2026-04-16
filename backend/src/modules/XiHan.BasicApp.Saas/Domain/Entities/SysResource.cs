#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysResource
// Guid:2a3b4c5d-6e7f-8901-bcde-f12345678901
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/07 10:10:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统资源实体
/// 资源是"被控制对象"（API、数据表、文件等），扁平结构，不包含 UI 层级
/// 权限 = 资源 + 操作，菜单通过 PermissionId 外键绑定权限
/// </summary>
[SugarTable("SysResource", "系统资源表")]
[SugarIndex("IX_{table}_TeId", nameof(TenantId), OrderByType.Asc)]
[SugarIndex("IX_{table}_CrTi", nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_MoTi", nameof(ModifiedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_MoId", nameof(ModifiedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_IsDe", nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_ReCo", nameof(TenantId), OrderByType.Asc, nameof(ResourceCode), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_ReCo", nameof(ResourceCode), OrderByType.Asc)]
[SugarIndex("IX_{table}_ReTy", nameof(ResourceType), OrderByType.Asc)]
[SugarIndex("IX_{table}_St", nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_{table}_ReTy_St", nameof(ResourceType), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_ReTy_St", nameof(TenantId), OrderByType.Asc, nameof(ResourceType), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_{table}_IsGl", nameof(IsGlobal), OrderByType.Asc)]
public partial class SysResource : BasicAppAggregateRoot
{
    /// <summary>
    /// 资源编码（唯一标识，如：user, order, department）
    /// </summary>
    [SugarColumn(ColumnDescription = "资源编码", Length = 100, IsNullable = false)]
    public virtual string ResourceCode { get; set; } = string.Empty;

    /// <summary>
    /// 资源名称
    /// </summary>
    [SugarColumn(ColumnDescription = "资源名称", Length = 100, IsNullable = false)]
    public virtual string ResourceName { get; set; } = string.Empty;

    /// <summary>
    /// 资源类型
    /// </summary>
    [SugarColumn(ColumnDescription = "资源类型")]
    public virtual ResourceType ResourceType { get; set; } = ResourceType.Api;

    /// <summary>
    /// 资源路径（URL/文件路径/API路径等）
    /// </summary>
    [SugarColumn(ColumnDescription = "资源路径", Length = 500, IsNullable = true)]
    public virtual string? ResourcePath { get; set; }

    /// <summary>
    /// 资源描述
    /// </summary>
    [SugarColumn(ColumnDescription = "资源描述", Length = 500, IsNullable = true)]
    public virtual string? Description { get; set; }

    /// <summary>
    /// 资源元数据（JSON格式，存储扩展信息）
    /// 例如：{"method": "GET", "controller": "UserController", "action": "GetList"}
    /// </summary>
    [SugarColumn(ColumnDescription = "资源元数据", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? Metadata { get; set; }

    /// <summary>
    /// 资源访问级别（替代原 IsRequireAuth+IsPublic，消除无效布尔组合）
    /// </summary>
    [SugarColumn(ColumnDescription = "访问级别")]
    public virtual ResourceAccessLevel AccessLevel { get; set; } = ResourceAccessLevel.Authorized;

    /// <summary>
    /// 是否平台级全局资源（全局资源所有租户共享，TenantId 为空）
    /// </summary>
    [SugarColumn(ColumnDescription = "是否全局资源")]
    public virtual bool IsGlobal { get; set; } = false;

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
