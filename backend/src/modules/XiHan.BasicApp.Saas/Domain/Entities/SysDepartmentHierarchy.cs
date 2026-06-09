#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysDepartmentHierarchy
// Guid:5889aba2-71e8-48e2-8c6c-2cc2f403ed0a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统部门层级闭包表实体
/// 闭包表模式预计算所有祖先-后代对（含自环 Depth=0），支持 O(1) 祖先/后代/深度查询
/// </summary>
/// <remarks>
/// 职责边界：
/// - 本表是 SysDepartment 的"查询加速镜像"，不是独立业务数据；所有写入必须由 SysDepartment 变更触发
///
/// 关联：
/// - AncestorId / DescendantId → SysDepartment
///
/// 写入（由服务层在部门增删移时统一维护）：
/// - AncestorId + DescendantId 唯一（UX_AnId_DeId）
/// - 新增部门 N 父为 P：INSERT SELECT AncestorId, N, Depth+1 FROM 本表 WHERE DescendantId=P，再补 (N,N,0)
/// - 移动子树：先删除旧闭包，再按新父重建
/// - 删除部门：DELETE WHERE DescendantId=N OR AncestorId=N
/// - Path/PathName 冗余字段便于面包屑展示，服务层同步维护
///
/// 查询：
/// - 某部门所有后代：WHERE AncestorId=? AND Depth&gt;0（IX_AnId_De）
/// - 某部门所有祖先：WHERE DescendantId=?（IX_DeId）
/// - 租户+祖先定位：IX_TeId_AnId
///
/// 删除：
/// - 硬删；随部门删除级联清理
///
/// 场景：
/// - DataScope=DEPT_AND_SUB 数据权限展开
/// - 组织架构树快速渲染
/// - 部门变更时 Path 刷新
/// </remarks>
[SugarTable("SysDepartmentHierarchy", "系统部门继承关系表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_AnId_DeId", nameof(TenantId), OrderByType.Asc, nameof(AncestorId), OrderByType.Asc, nameof(DescendantId), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_DeId", nameof(DescendantId), OrderByType.Asc)]
[SugarIndex("IX_{table}_De", nameof(Depth), OrderByType.Asc)]
[SugarIndex("IX_{table}_AnId_De", nameof(AncestorId), OrderByType.Asc, nameof(Depth), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_AnId", nameof(TenantId), OrderByType.Asc, nameof(AncestorId), OrderByType.Asc)]
public partial class SysDepartmentHierarchy : BasicAppCreationEntity
{
    /// <summary>
    /// 祖先部门ID
    /// </summary>
    /// <remarks>
    /// 包含所有上级部门，包括自己（Depth=0）
    /// </remarks>
    [SugarColumn(ColumnDescription = "祖先部门ID", IsNullable = false)]
    public virtual long AncestorId { get; set; }

    /// <summary>
    /// 后代部门ID
    /// </summary>
    /// <remarks>
    /// 包含所有下级部门，包括自己（Depth=0）
    /// </remarks>
    [SugarColumn(ColumnDescription = "后代部门ID", IsNullable = false)]
    public virtual long DescendantId { get; set; }

    /// <summary>
    /// 层级深度
    /// </summary>
    /// <remarks>
    /// - 0: 自己（自关联记录）
    /// - 1: 直接子部门
    /// - 2: 孙部门
    /// - n: n级子部门
    /// </remarks>
    [SugarColumn(ColumnDescription = "层级深度")]
    public virtual int Depth { get; set; } = 0;

    /// <summary>
    /// 路径（从祖先到后代的完整路径）
    /// </summary>
    /// <remarks>
    /// 格式：祖先ID/...中间ID.../后代ID
    /// 例如：1/3/5/7 表示从部门1到部门7的路径
    /// 用于快速显示部门层级关系和面包屑导航
    /// </remarks>
    [SugarColumn(ColumnDescription = "路径", Length = 1000, IsNullable = true)]
    public virtual string? Path { get; set; }

    /// <summary>
    /// 路径名称（用于显示）
    /// </summary>
    /// <remarks>
    /// 格式：祖先名称/...中间名称.../后代名称
    /// 例如：集团总部/华东大区/上海分公司/技术部
    /// 冗余字段，便于直接显示完整路径
    /// </remarks>
    [SugarColumn(ColumnDescription = "路径名称", Length = 1000, IsNullable = true)]
    public virtual string? PathName { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
