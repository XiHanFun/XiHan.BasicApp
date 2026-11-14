// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Auditing;

namespace XiHan.BasicApp.Auditing.Domain.Entities;

/// <summary>
/// 条件命中记录
/// </summary>
/// <remarks>
/// 访问策略的每条命中（拒绝、放行、只记录）与权限条件的不满足都记一行，与调用成败无关：只记录与放行的策略不拦请求，效果只能从命中看；排查「为什么被拦」也要看命中本身，不只是失败的调用
/// 命中量随请求放大（一条放行策略每个请求都命中），所以同一批落库里来源、策略、动作、时机、是否拦截、地址、方法、路径、用户与 UTC 分钟都相同的合并成一行并累加 <see cref="HitCount"/>；只在批内合并、不跨批回读改写，表仍是只追加
/// 写入方是 Access 的访问策略中间件、登录守卫与权限条件中间件：Access 在 Tier 1 不引用本包，经 Contracts 的 <see cref="ConditionHitLogEntry"/> 在调用里登记，由本包在 Observe 阶段落库
/// 平台层策略管全部调用，它的命中落在平台一侧；租户层的落在租户一侧，各自只看自己的策略效果
/// <see cref="PolicyId"/> 指向 Access 的 <c>SysAccessPolicy</c>，权限条件没有策略行，<see cref="PolicyCode"/> 改记权限码；策略名称是命中时的快照，策略改名或删除后记录仍看得懂
/// 分表、异步批量写入与只追加同其他分表日志（见 <see cref="AuditLogRecord"/>），另按策略建索引，从一条策略查它的全部命中
/// </remarks>
[SugarTable(TableName = "sys_condition_hit_{year}{month}{day}", TableDescription = "条件命中记录")]
[SplitTable(SplitType.Month)]
[SugarIndex("ix_{split_table}_tenant_id_occurred_at", nameof(TenantId), OrderByType.Asc, nameof(OccurredAt), OrderByType.Asc)]
[SugarIndex("ix_{split_table}_trace_id", nameof(TraceId), OrderByType.Asc)]
[SugarIndex("ix_{split_table}_user_id_occurred_at", nameof(UserId), OrderByType.Asc, nameof(OccurredAt), OrderByType.Asc)]
[SugarIndex("ix_{split_table}_policy_id", nameof(PolicyId), OrderByType.Asc)]
public sealed class SysConditionHit : AuditLogRecord
{
    /// <summary>
    /// 来源
    /// </summary>
    [SugarColumn(ColumnName = "source", IsNullable = false, ColumnDescription = "来源")]
    public ConditionHitSource Source { get; set; }

    /// <summary>
    /// 访问策略
    /// </summary>
    /// <remarks>
    /// 权限条件的命中为空
    /// </remarks>
    [SugarColumn(ColumnName = "policy_id", IsNullable = true, ColumnDescription = "访问策略")]
    public Guid? PolicyId { get; set; }

    /// <summary>
    /// 策略编码
    /// </summary>
    /// <remarks>
    /// 权限条件的命中记权限码
    /// </remarks>
    [SugarColumn(ColumnName = "policy_code", Length = 128, IsNullable = false, ColumnDescription = "策略编码")]
    public string PolicyCode { get; set; } = string.Empty;

    /// <summary>
    /// 策略名称（命中时的快照）
    /// </summary>
    [SugarColumn(ColumnName = "policy_name", Length = 256, IsNullable = true, ColumnDescription = "策略名称（命中时的快照）")]
    public string? PolicyName { get; set; }

    /// <summary>
    /// 规则要求的动作
    /// </summary>
    [SugarColumn(ColumnName = "action", IsNullable = false, ColumnDescription = "规则要求的动作")]
    public ConditionHitAction Action { get; set; }

    /// <summary>
    /// 求值时机
    /// </summary>
    [SugarColumn(ColumnName = "phase", IsNullable = false, ColumnDescription = "求值时机")]
    public ConditionHitPhase Phase { get; set; }

    /// <summary>
    /// 请求是否因此被拦下（拒绝被同层放行豁免时为否）
    /// </summary>
    [SugarColumn(ColumnName = "is_blocked", IsNullable = false, ColumnDescription = "请求是否因此被拦下（拒绝被同层放行豁免时为否）")]
    public bool IsBlocked { get; set; }

    /// <summary>
    /// 操作 ID
    /// </summary>
    [SugarColumn(ColumnName = "operation_id", Length = 128, IsNullable = true, ColumnDescription = "操作 ID")]
    public string? OperationId { get; set; }

    /// <summary>
    /// 请求方法
    /// </summary>
    [SugarColumn(ColumnName = "method", Length = 16, IsNullable = true, ColumnDescription = "请求方法")]
    public string? Method { get; set; }

    /// <summary>
    /// 请求路径（不含查询串）
    /// </summary>
    [SugarColumn(ColumnName = "path", Length = 512, IsNullable = true, ColumnDescription = "请求路径（不含查询串）")]
    public string? Path { get; set; }

    /// <summary>
    /// 端点要求的权限码，逗号分隔
    /// </summary>
    [SugarColumn(ColumnName = "permission_codes", Length = 1024, IsNullable = true, ColumnDescription = "端点要求的权限码，逗号分隔")]
    public string? PermissionCodes { get; set; }

    /// <summary>
    /// 这一分钟里的命中次数
    /// </summary>
    [SugarColumn(ColumnName = "hit_count", IsNullable = false, ColumnDescription = "这一分钟里的命中次数")]
    public int HitCount { get; set; } = 1;
}
