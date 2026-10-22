// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Routing;
using XiHan.Framework.Domain.Entities.Abstracts;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 访问策略实体：条件 + 动作，按请求的 IP、地区、时段、用户、接口等属性放行、拒绝或只记录
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><c>TenantId = 0</c> 是平台级策略，对全部请求生效；其余是租户级策略，只作用于该租户已登录的请求与登录。两层严格隔离，租户看不到平台的策略。</item>
/// <item>条件在 <see cref="SysAccessPolicyCondition"/>：组内且、组间或，不允许空条件集。</item>
/// <item>策略生效 = <see cref="Status"/> 启用，且当前时间在 <see cref="EffectiveTime"/>～<see cref="ExpirationTime"/> 内。</item>
/// <item>同一层里放行压过拒绝；平台的拒绝租户放行不了。<see cref="Priority"/> 小的先评估，只决定报告哪一条。</item>
/// </list>
/// </remarks>
[SugarTable(TableName = "Sys_Access_Policy", TableDescription = "访问策略表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_PoCo", nameof(TenantId), OrderByType.Asc, nameof(PolicyCode), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
[PlatformDataSource]
public partial class SysAccessPolicy : BasicAppAggregateRoot, IStrictMultiTenantEntity
{
    /// <summary>
    /// 策略编码（同一层内唯一）
    /// </summary>
    [SugarColumn(ColumnName = "Policy_Code", ColumnDescription = "策略编码", Length = 100, IsNullable = false)]
    public virtual string PolicyCode { get; set; } = string.Empty;

    /// <summary>
    /// 策略名称
    /// </summary>
    [SugarColumn(ColumnName = "Policy_Name", ColumnDescription = "策略名称", Length = 200, IsNullable = false)]
    public virtual string PolicyName { get; set; } = string.Empty;

    /// <summary>
    /// 说明
    /// </summary>
    [SugarColumn(ColumnName = "Description", ColumnDescription = "说明", Length = 1000, IsNullable = true)]
    public virtual string? Description { get; set; }

    /// <summary>
    /// 动作
    /// </summary>
    [SugarColumn(ColumnName = "Action", ColumnDescription = "动作")]
    public virtual AccessPolicyAction Action { get; set; } = AccessPolicyAction.LogOnly;

    /// <summary>
    /// 优先级，越小越先评估
    /// </summary>
    [SugarColumn(ColumnName = "Priority", ColumnDescription = "优先级")]
    public virtual int Priority { get; set; } = 100;

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "Status", ColumnDescription = "状态")]
    public virtual EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>
    /// 生效时间
    /// </summary>
    [SugarColumn(ColumnName = "Effective_Time", ColumnDescription = "生效时间", IsNullable = true)]
    public virtual DateTimeOffset? EffectiveTime { get; set; }

    /// <summary>
    /// 失效时间
    /// </summary>
    [SugarColumn(ColumnName = "Expiration_Time", ColumnDescription = "失效时间", IsNullable = true)]
    public virtual DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// 来源模板编码；从预置模板建出的策略带上它
    /// </summary>
    [SugarColumn(ColumnName = "Template_Code", ColumnDescription = "来源模板编码", Length = 100, IsNullable = true)]
    public virtual string? TemplateCode { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
