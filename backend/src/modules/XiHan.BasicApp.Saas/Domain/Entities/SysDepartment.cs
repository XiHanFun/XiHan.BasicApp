#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysDepartment
// Guid:6c28152c-d6e9-4396-addb-b479254bad10
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 02:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统部门实体
/// 组织架构树节点：承载层级关系、负责人、联系方式；是数据权限范围 DEPT/DEPT_AND_SUB 的基础
/// </summary>
/// <remarks>
/// 关联：
/// - ParentId → SysDepartment（自关联严格树，单父约束）
/// - LeaderId → SysUser（可空）
/// - 反向：SysUserDepartment、SysRoleDataScope、SysDepartmentHierarchy
///
/// 写入：
/// - TenantId + DepartmentCode 租户内唯一（UX_TeId_DeCo）
/// - 写入/移动节点时服务层必须：(1) 环路检测 (2) 同步重建 SysDepartmentHierarchy 闭包表
/// - DepartmentType 区分 集团/公司/部门/小组/项目组 便于统计维度
///
/// 查询：
/// - 按 ParentId 构建一级树：IX_PaId
/// - 按负责人反查管辖部门：IX_LeId
/// - 祖先/后代展开：走 SysDepartmentHierarchy
///
/// 删除：
/// - 仅软删；删除前必须校验：无子部门、无用户归属（SysUserDepartment）、无数据范围引用（SysRoleDataScope）
/// - 软删后需同步清理 SysDepartmentHierarchy 中相关闭包记录
///
/// 状态：
/// - Status: Yes/No（停用部门对所有数据范围计算隐藏）
///
/// 场景：
/// - 组织架构树渲染、用户归属、数据权限范围计算
/// - 按部门汇总统计（销售额/工时/审批量）
/// </remarks>
[SugarTable("SysDepartment", "系统部门表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_DeCo", nameof(TenantId), OrderByType.Asc, nameof(DepartmentCode), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_PaId", nameof(ParentId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_{table}_LeId", nameof(LeaderId), OrderByType.Asc)]
public partial class SysDepartment : BasicAppAggregateRoot
{
    /// <summary>
    /// 父级部门ID
    /// </summary>
    [SugarColumn(ColumnDescription = "父级部门ID", IsNullable = true)]
    public virtual long? ParentId { get; set; }

    /// <summary>
    /// 部门名称
    /// </summary>
    [SugarColumn(ColumnDescription = "部门名称", Length = 100, IsNullable = false)]
    public virtual string DepartmentName { get; set; } = string.Empty;

    /// <summary>
    /// 部门编码
    /// </summary>
    [SugarColumn(ColumnDescription = "部门编码", Length = 100, IsNullable = false)]
    public virtual string DepartmentCode { get; set; } = string.Empty;

    /// <summary>
    /// 部门类型
    /// </summary>
    [SugarColumn(ColumnDescription = "部门类型")]
    public virtual DepartmentType DepartmentType { get; set; } = DepartmentType.Department;

    /// <summary>
    /// 负责人ID
    /// </summary>
    [SugarColumn(ColumnDescription = "负责人ID", IsNullable = true)]
    public virtual long? LeaderId { get; set; }

    /// <summary>
    /// 联系电话
    /// </summary>
    [SugarColumn(ColumnDescription = "联系电话", Length = 20, IsNullable = true)]
    public virtual string? Phone { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    [SugarColumn(ColumnDescription = "邮箱", Length = 100, IsNullable = true)]
    public virtual string? Email { get; set; }

    /// <summary>
    /// 地址
    /// </summary>
    [SugarColumn(ColumnDescription = "地址", Length = 500, IsNullable = true)]
    public virtual string? Address { get; set; }

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
