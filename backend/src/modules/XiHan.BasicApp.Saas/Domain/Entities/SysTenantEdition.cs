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
/// B2B SaaS 产品订阅单元：定义可售卖版本、计费周期、资源配额；与 SysTenantEditionPermission 组合实现功能门控
/// </summary>
/// <remarks>
/// 职责边界：
/// - 平台级实体（TenantId 为空或 0），由平台运营管理
/// - 本实体描述"版本能做什么"和"版本卖多少"；"某租户订阅哪个版本"由 SysTenant.EditionId 承载
///
/// 关联：
/// - 反向：SysTenant.EditionId、SysTenantEditionPermission.EditionId
///
/// 写入：
/// - EditionCode 全局唯一（UX_EdCo），如 free/basic/pro/enterprise
/// - 同一时刻仅允许一个 IsDefault=true；服务层写入时必须互斥
/// - UserLimit/StorageLimit 为空表示不限；租户级 SysTenant.UserLimit 可按需覆盖
///
/// 查询：
/// - 启用中的版本列表：按 Status=Yes + IsDefault 排序
/// - 默认版本：IX_IsDf + WHERE IsDefault=1
///
/// 删除：
/// - 仅软删；删除前必须校验：无租户仍引用（SysTenant.EditionId）、非默认版本
///
/// 状态与生命周期：
/// - Status: Yes=可售/在架 / No=下架（已订阅租户不受影响，仅停止新订阅）
/// - IsFree=true 通常与 Price=null 一致
///
/// 场景：
/// - 新租户注册自动分配 IsDefault=true 的免费版
/// - 版本升级：修改 SysTenant.EditionId 并刷新权限缓存
/// - 价格调整：维护 Price + BillingPeriodMonths
/// </remarks>
[SugarTable("SysTenantEdition", "租户版本套餐表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
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
