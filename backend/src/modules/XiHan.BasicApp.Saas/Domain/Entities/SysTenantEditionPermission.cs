#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTenantEditionPermission
// Guid:d4e5f6a7-b8c9-0123-def0-234567890104
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/14 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 租户版本可用权限映射实体
/// 定义每个版本/套餐包含哪些权限，租户管理员只能在此范围内分配权限
/// </summary>
[SugarTable("SysTenantEditionPermission", "租户版本可用权限映射表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("UX_{table}_EdId_PeId", nameof(EditionId), OrderByType.Asc, nameof(PermissionId), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_EdId", nameof(EditionId), OrderByType.Asc)]
[SugarIndex("IX_{table}_PeId", nameof(PermissionId), OrderByType.Asc)]
[SugarIndex("IX_{table}_St", nameof(Status), OrderByType.Asc)]
public partial class SysTenantEditionPermission : BasicAppCreationEntity
{
    /// <summary>
    /// 版本ID
    /// </summary>
    [SugarColumn(ColumnDescription = "版本ID", IsNullable = false)]
    public virtual long EditionId { get; set; }

    /// <summary>
    /// 权限ID（关联平台级全局权限）
    /// </summary>
    [SugarColumn(ColumnDescription = "权限ID", IsNullable = false)]
    public virtual long PermissionId { get; set; }

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
