#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserDepartment
// Guid:ac28152c-d6e9-4396-addb-b479254bad14
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 03:05:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Entities.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统用户部门关联实体
/// 支持用户多部门归属 + 主部门标识；是数据权限范围（DataScope=DEPT/DEPT_AND_SUB）的基础
/// </summary>
/// <remarks>
/// 关联：
/// - UserId → SysUser；DepartmentId → SysDepartment
///
/// 写入：
/// - UserId + DepartmentId 唯一（UX_UsId_DeId）
/// - 每个用户至多一个 IsMain=true 的主部门；服务层必须做互斥校验
/// - 部门跨租户归属禁止（DepartmentId 必须与 UserId 同租户）
///
/// 查询：
/// - 数据权限过滤：按 UserId 查所有部门 → 结合 SysDepartmentHierarchy 展开可见范围
/// - 部门反查成员：IX_DeId
/// - 按主部门筛选：IX_IsMa
///
/// 删除：
/// - 硬删；删除主部门关联时应由调用方提示或自动将另一条设为主
///
/// 状态：
/// - Status: Yes/No
///
/// 场景：
/// - 用户入职分配部门
/// - 用户借调/兼职：多部门归属，IsMain 标识主归属用于组织架构展示
/// - 数据范围计算：部门经理查看本部门及下级数据
/// </remarks>
[SugarTable("SysUserDepartment", "系统用户部门关联表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_UsId_DeId", nameof(TenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc, nameof(DepartmentId), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_UsId", nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_{table}_DeId", nameof(DepartmentId), OrderByType.Asc)]
[SugarIndex("IX_{table}_IsMa", nameof(IsMain), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public partial class SysUserDepartment : BasicAppCreationEntity
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID", IsNullable = false)]
    public virtual long UserId { get; set; }

    /// <summary>
    /// 部门ID
    /// </summary>
    [SugarColumn(ColumnDescription = "部门ID", IsNullable = false)]
    public virtual long DepartmentId { get; set; }

    /// <summary>
    /// 是否主部门
    /// </summary>
    [SugarColumn(ColumnDescription = "是否主部门")]
    public virtual bool IsMain { get; set; } = false;

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
