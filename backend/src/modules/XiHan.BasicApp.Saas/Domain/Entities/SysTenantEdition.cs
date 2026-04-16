#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTenantEdition
// Guid:c3d4e5f6-a7b8-9012-cdef-123456789003
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
/// 租户版本/套餐实体
/// 定义 B2B SaaS 的产品版本，控制租户可用的功能权限范围
/// 平台级实体，TenantId 为空或 0
/// </summary>
[SugarTable("SysTenantEdition", "租户版本套餐表")]
[SugarIndex("IX_{table}_TeId", nameof(TenantId), OrderByType.Asc)]
[SugarIndex("IX_{table}_CrTi", nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_MoTi", nameof(ModifiedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_MoId", nameof(ModifiedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_IsDe", nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_EdCo", nameof(EditionCode), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_St", nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_{table}_IsDf", nameof(IsDefault), OrderByType.Asc)]
public partial class SysTenantEdition : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 版本编码（唯一标识，如：free, basic, pro, enterprise）
    /// </summary>
    [SugarColumn(ColumnDescription = "版本编码", Length = 50, IsNullable = false)]
    public virtual string EditionCode { get; set; } = string.Empty;

    /// <summary>
    /// 版本名称
    /// </summary>
    [SugarColumn(ColumnDescription = "版本名称", Length = 100, IsNullable = false)]
    public virtual string EditionName { get; set; } = string.Empty;

    /// <summary>
    /// 版本描述
    /// </summary>
    [SugarColumn(ColumnDescription = "版本描述", Length = 500, IsNullable = true)]
    public virtual string? Description { get; set; }

    /// <summary>
    /// 用户数限制（为空表示不限）
    /// </summary>
    [SugarColumn(ColumnDescription = "用户数限制", IsNullable = true)]
    public virtual int? UserLimit { get; set; }

    /// <summary>
    /// 存储空间限制(MB)（为空表示不限）
    /// </summary>
    [SugarColumn(ColumnDescription = "存储空间限制(MB)", IsNullable = true)]
    public virtual long? StorageLimit { get; set; }

    /// <summary>
    /// 价格（元/周期）
    /// </summary>
    [SugarColumn(ColumnDescription = "价格", DecimalDigits = 2, IsNullable = true)]
    public virtual decimal? Price { get; set; }

    /// <summary>
    /// 计费周期（月）
    /// </summary>
    [SugarColumn(ColumnDescription = "计费周期(月)", IsNullable = true)]
    public virtual int? BillingPeriodMonths { get; set; }

    /// <summary>
    /// 是否免费版本
    /// </summary>
    [SugarColumn(ColumnDescription = "是否免费")]
    public virtual bool IsFree { get; set; } = false;

    /// <summary>
    /// 是否默认版本（新租户注册时自动分配）
    /// </summary>
    [SugarColumn(ColumnDescription = "是否默认版本")]
    public virtual bool IsDefault { get; set; } = false;

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
