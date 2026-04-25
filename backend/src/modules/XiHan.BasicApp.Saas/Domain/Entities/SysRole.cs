#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysRole
// Guid:3c28152c-d6e9-4396-addb-b479254bad0d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 02:30:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统角色实体
/// RBAC 权限分配单元：承载一组权限，通过 SysUserRole 赋给用户，通过 SysRoleHierarchy 支持继承
/// </summary>
/// <remarks>
/// 关联：
/// - 反向：SysUserRole（用户赋角色）、SysRolePermission（角色持权限）、SysRoleHierarchy（闭包表继承）、SysRoleDataScope（自定义数据范围）
///
/// 写入：
/// - TenantId + RoleCode 租户内唯一（UX_TeId_RoCo）
/// - RoleType=System 为平台内置角色，禁止普通租户修改/删除
/// - DataScope=Custom 时必须同步写入 SysRoleDataScope，否则视为无可见数据
///
/// 查询：
/// - 必带 TenantId 过滤；权限合并时应同时加载 SysRoleHierarchy 展开继承链
///
/// 删除：
/// - 仅软删；删除前必须校验：无用户仍持有该角色（SysUserRole 无关联记录）、无子角色继承（SysRoleHierarchy）、未被 SysConstraintRuleItem 引用
///
/// 状态：
/// - Status: Yes=启用 / No=停用（停用后用户即使持有该角色也不生效）
///
/// 场景：
/// - 预置角色模板（Owner/Admin/Manager/Member/Viewer/External）
/// - 租户管理员自定义角色 + 权限组合
/// - 角色继承：财务主管 → 财务专员 → 基础查看
/// </remarks>
[SugarTable("SysRole", "系统角色表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_RoCo", nameof(TenantId), OrderByType.Asc, nameof(RoleCode), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_{table}_IsGl", nameof(IsGlobal), OrderByType.Asc)]
public partial class SysRole : BasicAppAggregateRoot
{
    /// <summary>
    /// 角色编码
    /// </summary>
    [SugarColumn(ColumnDescription = "角色编码", Length = 50, IsNullable = false)]
    public virtual string RoleCode { get; set; } = string.Empty;

    /// <summary>
    /// 角色名称
    /// </summary>
    [SugarColumn(ColumnDescription = "角色名称", Length = 100, IsNullable = false)]
    public virtual string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// 角色描述
    /// </summary>
    [SugarColumn(ColumnDescription = "角色描述", Length = 500, IsNullable = true)]
    public virtual string? RoleDescription { get; set; }

    /// <summary>
    /// 角色类型（System=平台预置 / Tenant=租户级 / Custom=租户自定义，仅展示分类）
    /// </summary>
    /// <remarks>
    /// RoleType × IsGlobal 合法组合矩阵：
    /// ┌──────────┬─────────────────┬──────────────────┐
    /// │ RoleType │ IsGlobal=true   │ IsGlobal=false   │
    /// ├──────────┼─────────────────┼──────────────────┤
    /// │ System   │ 合法（平台预置模板，如 SuperAdmin） │ 不合法（平台预置必须全局） │
    /// │ Tenant   │ 合法（租户可见但由平台定义的通用角色） │ 合法（租户自有角色）     │
    /// │ Custom   │ 不合法（租户自定义不应全局化）  │ 合法（租户自建角色）     │
    /// └──────────┴─────────────────┴──────────────────┘
    /// 服务层创建/修改角色时应校验此矩阵，拒绝非法组合。
    /// </remarks>
    [SugarColumn(ColumnDescription = "角色类型")]
    public virtual RoleType RoleType { get; set; } = RoleType.Custom;

    /// <summary>
    /// 是否平台级全局角色（全局角色作为所有租户的模板，TenantId = 0）
    /// </summary>
    [SugarColumn(ColumnDescription = "是否全局角色")]
    public virtual bool IsGlobal { get; set; } = false;

    /// <summary>
    /// 数据权限范围
    /// 禁止依赖枚举数值大小做权限合并，必须按 DataPermissionScope 注释中的显式语义解释
    /// </summary>
    [SugarColumn(ColumnDescription = "数据权限范围")]
    public virtual DataPermissionScope DataScope { get; set; } = DataPermissionScope.SelfOnly;

    /// <summary>
    /// 最大成员数（角色基数约束，0 表示不限；如"总经理"角色最多 1 人）
    /// </summary>
    [SugarColumn(ColumnDescription = "最大成员数")]
    public virtual int MaxMembers { get; set; } = 0;

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public virtual EnableStatus Status { get; set; } = EnableStatus.Enabled;

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
