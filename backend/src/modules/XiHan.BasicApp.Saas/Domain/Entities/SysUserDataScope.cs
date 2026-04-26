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
using XiHan.BasicApp.Saas.Domain.Entities.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 用户自定义数据权限范围实体
/// 为单个用户设置独立于角色的数据范围覆盖（如 CEO 虽然角色是部门经理，但需要看全部数据）
/// </summary>
/// <remarks>
/// 职责边界：
/// - 与 SysRoleDataScope 区别：SysRoleDataScope 是角色级数据范围，本表是用户级覆盖
/// - 用户级数据范围优先级高于角色级：若用户有 UserDataScope 记录，则忽略角色的 DataScope
///
/// 关联：
/// - UserId → SysUser；DepartmentId → SysDepartment（DataScope=Custom 时）
///
/// 写入：
/// - TenantId + UserId + DepartmentId 唯一（UX_TeId_UsId_DeId）
/// - 同租户约束：UserId 与 DepartmentId 必须同 TenantId
///
/// 查询：
/// - 权限决策数据过滤：按 UserId 查是否有用户级覆盖
/// - 部门反查：IX_DeId
///
/// 删除：
/// - 硬删；撤销用户级覆盖时直接删除
/// </remarks>
[SugarTable("SysUserDataScope", "用户自定义数据权限范围表")]
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
    [SugarColumn(ColumnDescription = "用户ID", IsNullable = false)]
    public virtual long UserId { get; set; }

    /// <summary>
    /// 数据权限范围（用户级覆盖，优先级高于角色级 DataScope）
    /// </summary>
    [SugarColumn(ColumnDescription = "数据权限范围")]
    public virtual DataPermissionScope DataScope { get; set; } = DataPermissionScope.SelfOnly;

    /// <summary>
    /// 部门ID（DataScope=Custom 时指定可访问的部门）
    /// </summary>
    [SugarColumn(ColumnDescription = "部门ID", IsNullable = false)]
    public virtual long DepartmentId { get; set; }

    /// <summary>
    /// 是否包含子部门
    /// </summary>
    [SugarColumn(ColumnDescription = "是否包含子部门")]
    public virtual bool IncludeChildren { get; set; } = false;

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public virtual ValidityStatus Status { get; set; } = ValidityStatus.Valid;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
