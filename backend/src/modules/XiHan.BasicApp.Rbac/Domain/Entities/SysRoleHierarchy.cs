#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysRoleHierarchy
// Guid:4c5d6e7f-8901-2345-def0-234567890123
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/07 10:20:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Rbac.Domain.Enums;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统角色层级关系实体
/// 使用闭包表模式存储角色之间的所有继承关系（包括直接和传递继承）
/// </summary>
/// <remarks>
/// 支持角色多继承，后代角色继承祖先角色的所有权限
/// 闭包表优点：
/// 1. 查询所有子角色：O(1) 单次查询
/// 2. 查询继承链：O(1) 单次查询
/// 3. 避免递归查询的性能问题
/// </remarks>
[SugarTable("Sys_Role_Hierarchy", "系统角色层级关系表")]
[SugarIndex("UX_SysRoleHierarchy_AnId_DeId", nameof(AncestorId), OrderByType.Asc, nameof(DescendantId), OrderByType.Asc, true)]
[SugarIndex("IX_SysRoleHierarchy_DeId", nameof(DescendantId), OrderByType.Asc)]
[SugarIndex("IX_SysRoleHierarchy_St", nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_SysRoleHierarchy_AnId_De", nameof(AncestorId), OrderByType.Asc, nameof(Depth), OrderByType.Asc)]
[SugarIndex("IX_SysRoleHierarchy_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public partial class SysRoleHierarchy : BasicAppCreationEntity
{
    /// <summary>
    /// 祖先角色ID（被继承的角色）
    /// </summary>
    /// <remarks>
    /// 包含所有被继承的角色，包括自己（Depth=0）
    /// </remarks>
    [SugarColumn(ColumnDescription = "祖先角色ID", IsNullable = false)]
    public virtual long AncestorId { get; set; }

    /// <summary>
    /// 后代角色ID（继承者角色）
    /// </summary>
    /// <remarks>
    /// 包含所有继承者角色，包括自己（Depth=0）
    /// </remarks>
    [SugarColumn(ColumnDescription = "后代角色ID", IsNullable = false)]
    public virtual long DescendantId { get; set; }

    /// <summary>
    /// 继承深度
    /// </summary>
    /// <remarks>
    /// - 0: 自己（自关联记录）
    /// - 1: 直接继承
    /// - n: n级间接继承
    /// </remarks>
    [SugarColumn(ColumnDescription = "继承深度")]
    public virtual int Depth { get; set; } = 0;

    /// <summary>
    /// 继承路径（从祖先到后代的完整路径）
    /// </summary>
    /// <remarks>
    /// 格式：祖先ID/...中间ID.../后代ID
    /// 例如：1/3/5 表示角色5继承自角色3，角色3继承自角色1
    /// 用于快速显示角色继承链和权限追溯
    /// </remarks>
    [SugarColumn(ColumnDescription = "继承路径", Length = 1000, IsNullable = true)]
    public virtual string? Path { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public virtual YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
