#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysPosition
// Guid:2f5b8c1a-7d34-4e09-9c61-1a2b3c4d5e60
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/29 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统岗位实体
/// 扁平岗位（职务/职位）字典：承载租户内可分配的岗位编码与名称，供人员任用与组织管理引用
/// </summary>
/// <remarks>
/// 关联：
/// - 无 FK；按 TenantId + PositionCode 在租户内被业务代码引用
///
/// 写入：
/// - TenantId + PositionCode 租户内唯一（UX_TeId_PoCo）
/// - TenantId 由 ITenantContext 在写入时自动注入
///
/// 查询：
/// - 列表/分页：IX_TeId_CrTi / IX_TeId_St
///
/// 删除：
/// - 仅软删；删除前建议确认无人员任用引用
///
/// 状态：
/// - Status: Enabled/Disabled（停用岗位对分配选择隐藏）
///
/// 场景：
/// - 岗位字典维护、人员任用、按岗位汇总统计
/// </remarks>
[SugarTable(TableName = "Sys_Position", TableDescription = "系统岗位表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_PoCo", nameof(TenantId), OrderByType.Asc, nameof(PositionCode), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public partial class SysPosition : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 岗位编码
    /// </summary>
    [SugarColumn(ColumnName = "Position_Code", ColumnDescription = "岗位编码", Length = 100, IsNullable = false)]
    public virtual string PositionCode { get; set; } = string.Empty;

    /// <summary>
    /// 岗位名称
    /// </summary>
    [SugarColumn(ColumnName = "Position_Name", ColumnDescription = "岗位名称", Length = 100, IsNullable = false)]
    public virtual string PositionName { get; set; } = string.Empty;

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "Status", ColumnDescription = "状态")]
    public virtual EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnName = "Sort", ColumnDescription = "排序")]
    public virtual int Sort { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
