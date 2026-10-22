// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.Framework.Domain.Entities.Abstracts;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 条件命中记录实体：访问策略的命中（拒绝、放行、只记录）与权限条件的拒绝
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>按月分表，表名 <c>Sys_Access_Policy_Hit_{yyyyMMdd}</c>；查询、清理须带时间范围。</item>
/// <item>落在规则归属的租户：平台策略的命中记在平台，租户策略与权限条件的命中记在该租户。</item>
/// <item>同一（规则、动作、是否拦下、时机、IP、方法、路径、用户）在同一分钟内合并成一行，<see cref="HitCount"/> 记次数，<see cref="TraceId"/> 取这一分钟里的第一次。</item>
/// </list>
/// </remarks>
[SugarTable(TableName = "Sys_Access_Policy_Hit_{year}{month}{day}", TableDescription = "条件命中记录表"), SplitTable(SplitType.Month)]
[SugarIndex("IX_{split_table}_TeId_HiTi", nameof(TenantId), OrderByType.Asc, nameof(HitTime), OrderByType.Desc)]
[SugarIndex("IX_{split_table}_PoId", nameof(PolicyId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_Ip", nameof(Ip), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_UsId", nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_TrId", nameof(TraceId), OrderByType.Asc)]
public partial class SysAccessPolicyHit : BasicAppCreationEntity, ISplitTableEntity, ITraceableEntity, IStrictMultiTenantEntity
{
    /// <summary>
    /// 来源
    /// </summary>
    [SugarColumn(ColumnName = "Source", ColumnDescription = "来源")]
    public virtual AccessPolicyHitSource Source { get; set; } = AccessPolicyHitSource.AccessPolicy;

    /// <summary>
    /// 访问策略主键；权限条件的命中为空
    /// </summary>
    [SugarColumn(ColumnName = "Policy_Id", ColumnDescription = "访问策略主键", IsNullable = true)]
    public virtual long? PolicyId { get; set; }

    /// <summary>
    /// 策略编码；权限条件的命中记权限码
    /// </summary>
    [SugarColumn(ColumnName = "Policy_Code", ColumnDescription = "策略编码", Length = 200, IsNullable = false)]
    public virtual string PolicyCode { get; set; } = string.Empty;

    /// <summary>
    /// 策略名称（命中时的名称）
    /// </summary>
    [SugarColumn(ColumnName = "Policy_Name", ColumnDescription = "策略名称", Length = 200, IsNullable = true)]
    public virtual string? PolicyName { get; set; }

    /// <summary>
    /// 动作
    /// </summary>
    [SugarColumn(ColumnName = "Action", ColumnDescription = "动作")]
    public virtual AccessPolicyAction Action { get; set; }

    /// <summary>
    /// 求值时机
    /// </summary>
    [SugarColumn(ColumnName = "Phase", ColumnDescription = "求值时机")]
    public virtual AccessPolicyPhase Phase { get; set; }

    /// <summary>
    /// 请求是否因此被拦下（拒绝被同层放行豁免时为否）
    /// </summary>
    [SugarColumn(ColumnName = "Is_Blocked", ColumnDescription = "是否拦下")]
    public virtual bool IsBlocked { get; set; }

    /// <summary>
    /// 链路追踪ID（合并行取第一次）
    /// </summary>
    [SugarColumn(ColumnName = "Trace_Id", ColumnDescription = "链路追踪ID", Length = 64, IsNullable = true)]
    public virtual string? TraceId { get; set; }

    /// <summary>
    /// 客户端 IP
    /// </summary>
    [SugarColumn(ColumnName = "Ip", ColumnDescription = "客户端IP", Length = 50, IsNullable = true)]
    public virtual string? Ip { get; set; }

    /// <summary>
    /// IP 归属地
    /// </summary>
    [SugarColumn(ColumnName = "Location", ColumnDescription = "IP归属地", Length = 200, IsNullable = true)]
    public virtual string? Location { get; set; }

    /// <summary>
    /// 用户主键
    /// </summary>
    [SugarColumn(ColumnName = "User_Id", ColumnDescription = "用户ID", IsNullable = true)]
    public virtual long? UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    [SugarColumn(ColumnName = "User_Name", ColumnDescription = "用户名", Length = 50, IsNullable = true)]
    public virtual string? UserName { get; set; }

    /// <summary>
    /// 请求方法
    /// </summary>
    [SugarColumn(ColumnName = "Method", ColumnDescription = "请求方法", Length = 10, IsNullable = true)]
    public virtual string? Method { get; set; }

    /// <summary>
    /// 请求路径（不含查询串）
    /// </summary>
    [SugarColumn(ColumnName = "Path", ColumnDescription = "请求路径", Length = 500, IsNullable = true)]
    public virtual string? Path { get; set; }

    /// <summary>
    /// 端点权限码（多个以逗号分隔）
    /// </summary>
    [SugarColumn(ColumnName = "Permission_Codes", ColumnDescription = "端点权限码", Length = 500, IsNullable = true)]
    public virtual string? PermissionCodes { get; set; }

    /// <summary>
    /// User-Agent
    /// </summary>
    [SugarColumn(ColumnName = "User_Agent", ColumnDescription = "User-Agent", Length = 500, IsNullable = true)]
    public virtual string? UserAgent { get; set; }

    /// <summary>
    /// 客户端类型（web、openapi、oauth、anonymous）
    /// </summary>
    [SugarColumn(ColumnName = "Client_Kind", ColumnDescription = "客户端类型", Length = 20, IsNullable = true)]
    public virtual string? ClientKind { get; set; }

    /// <summary>
    /// 这一分钟里第一次命中的时间
    /// </summary>
    [SugarColumn(ColumnName = "Hit_Time", ColumnDescription = "命中时间")]
    public virtual DateTimeOffset HitTime { get; set; }

    /// <summary>
    /// 这一分钟里的命中次数
    /// </summary>
    [SugarColumn(ColumnName = "Hit_Count", ColumnDescription = "命中次数")]
    public virtual int HitCount { get; set; } = 1;

    /// <summary>
    /// 创建时间（分表字段）
    /// </summary>
    [SugarColumn(ColumnName = "Created_Time", IsNullable = false, ColumnDescription = "创建时间")]
    [SplitField]
    public override DateTimeOffset CreatedTime { get; set; }
}
