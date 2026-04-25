#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysRoleDataScope
// Guid:5c28152c-d6e9-4396-addb-b479254bad24
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/01/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统角色自定义数据权限范围实体
/// 当 SysRole.DataScope=Custom 时使用，明确枚举该角色可见的部门范围
/// </summary>
/// <remarks>
/// 职责边界：
/// - 本表仅服务 Custom 范围；其它 DataScope 枚举（All/Self/Dept/DeptAndSub）由服务层直接按语义解析
///
/// 关联：
/// - RoleId → SysRole；DepartmentId → SysDepartment
///
/// 写入：
/// - RoleId + DepartmentId 唯一（UX_RoId_DeId），避免重复配置
/// - IncludeChildren=true 时服务层需配合 SysDepartmentHierarchy 展开所有后代部门
/// - 同租户约束：RoleId 与 DepartmentId 必须同 TenantId
///
/// 查询：
/// - 权限决策数据过滤：按 RoleId 加载所有范围 → 生成 WHERE dept_id IN (...)
/// - 部门反查：IX_DeId
///
/// 删除：
/// - 硬删；删除部门时应级联删除相关数据范围记录
///
/// 状态：
/// - Status: Yes/No
///
/// 场景：
/// - 区域经理：角色 DataScope=Custom + 配置可见多个地区部门（IncludeChildren=true）
/// - 跨部门协作：项目组长角色配置能看几个非下属部门的数据
/// </remarks>
[SugarTable("SysRoleDataScope", "系统角色自定义数据权限范围表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_RoId_DeId", nameof(TenantId), OrderByType.Asc, nameof(RoleId), OrderByType.Asc, nameof(DepartmentId), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_DeId", nameof(DepartmentId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_RoId", nameof(TenantId), OrderByType.Asc, nameof(RoleId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public partial class SysRoleDataScope : BasicAppCreationEntity
{
    /// <summary>
    /// 角色ID
    /// </summary>
    [SugarColumn(ColumnDescription = "角色ID", IsNullable = false)]
    public virtual long RoleId { get; set; }

    /// <summary>
    /// 部门ID（自定义数据权限可访问的部门）
    /// </summary>
    [SugarColumn(ColumnDescription = "部门ID", IsNullable = false)]
    public virtual long DepartmentId { get; set; }

    /// <summary>
    /// 是否包含子部门（true 时自动包含该部门的所有下级，新增子部门自动纳入范围）
    /// </summary>
    [SugarColumn(ColumnDescription = "是否包含子部门")]
    public virtual bool IncludeChildren { get; set; } = false;

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
