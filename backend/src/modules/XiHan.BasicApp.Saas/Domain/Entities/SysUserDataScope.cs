#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserDataScope
// Guid:b8c9d0e1-f2a3-4567-3456-890123456789
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 用户自定义数据权限范围实体（用户级 Custom 数据范围的部门明细）
/// 对齐角色模型：范围档位（All/Self/Dept/Custom 等）由 SysUser.DataScopeOverride 单值承载，
/// 本表仅在 DataScopeOverride=Custom 时枚举用户可见的部门集合（纯部门明细，不再携带范围档位枚举）
/// </summary>
/// <remarks>
/// 职责边界：
/// - 与 SysRoleDataScope 完全对称：SysRoleDataScope 服务 SysRole.DataScope=Custom；本表服务 SysUser.DataScopeOverride=Custom
/// - 用户级覆盖优先级高于角色级：当 SysUser.DataScopeOverride 非空时，忽略角色的 DataScope
///
/// 关联：
/// - UserId → SysUser；DepartmentId → SysDepartment
///
/// 写入：
/// - TenantId + UserId + DepartmentId 唯一（UX_TeId_UsId_DeId），避免重复配置
/// - 同租户约束：UserId 与 DepartmentId 必须同 TenantId
/// - 仅当 SysUser.DataScopeOverride=Custom 时写入本表；其它档位本表应无记录
/// - IncludeChildren=true 时服务层需配合 SysDepartmentHierarchy 展开所有后代部门
///
/// 查询：
/// - 权限决策数据过滤：按 UserId 加载部门集合 → 生成 WHERE dept_id IN (...)
/// - 部门反查：IX_DeId
///
/// 删除：
/// - 硬删；撤销用户级覆盖或删除部门时直接删除相关记录
/// </remarks>
[SugarTable(TableName = "Sys_User_Data_Scope", TableDescription = "用户自定义数据权限范围表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_UsId_DeId", nameof(TenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc, nameof(DepartmentId), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_TeId_UsId", nameof(TenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_{table}_DeId", nameof(DepartmentId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public partial class SysUserDataScope : BasicAppCreationEntity
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnName = "User_Id", ColumnDescription = "用户ID", IsNullable = false)]
    public virtual long UserId { get; set; }

    /// <summary>
    /// 部门ID（用户级 Custom 范围可访问的部门）
    /// </summary>
    [SugarColumn(ColumnName = "Department_Id", ColumnDescription = "部门ID", IsNullable = false)]
    public virtual long DepartmentId { get; set; }

    /// <summary>
    /// 是否包含子部门
    /// </summary>
    [SugarColumn(ColumnName = "Include_Children", ColumnDescription = "是否包含子部门")]
    public virtual bool IncludeChildren { get; set; } = false;

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
