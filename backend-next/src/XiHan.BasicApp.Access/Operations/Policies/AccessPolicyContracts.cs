// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Operations.PermissionConditions;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.Framework.Security.Abstractions.Conditions;

namespace XiHan.BasicApp.Access.Operations.Policies;

/// <summary>
/// 访问策略的一条条件（请求）。
/// </summary>
/// <param name="AttributeName">属性名（见 <c>permission-conditions.attributes</c>）。</param>
/// <param name="Operator">运算符。</param>
/// <param name="GroupNo">组号（同组取且、组间取或）。</param>
/// <param name="IsNegated">是否取反。</param>
/// <param name="Value">条件值（判空类运算符不填）。</param>
/// <param name="Description">说明。</param>
public sealed record AccessPolicyConditionRequest(
    [Required, StringLength(200, MinimumLength = 1)] string AttributeName,
    ConditionOperator Operator,
    [Range(0, 99)] int GroupNo = 0,
    bool IsNegated = false,
    [StringLength(SysPermissionCondition.MaxValueLength)] string? Value = null,
    [StringLength(500)] string? Description = null)
{
    /// <summary>
    /// 转成条件定义。
    /// </summary>
    public PolicyConditionDefinition ToDefinition() => new(GroupNo, AttributeName, Operator, IsNegated, Value, Description);
}

/// <summary>
/// 访问策略的一条条件（响应）。
/// </summary>
/// <param name="GroupNo">组号。</param>
/// <param name="AttributeName">属性名。</param>
/// <param name="Operator">运算符。</param>
/// <param name="IsNegated">是否取反。</param>
/// <param name="Value">条件值。</param>
/// <param name="Description">说明。</param>
public sealed record AccessPolicyConditionResponse(int GroupNo, string AttributeName, ConditionOperator Operator, bool IsNegated, string? Value, string? Description)
{
    /// <summary>
    /// 由实体组装。
    /// </summary>
    public static AccessPolicyConditionResponse From(SysAccessPolicyCondition condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        return new(condition.GroupNo, condition.AttributeName, condition.Operator, condition.IsNegated, condition.Value, condition.Description);
    }

    /// <summary>
    /// 由条件定义组装。
    /// </summary>
    public static AccessPolicyConditionResponse From(PolicyConditionDefinition condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        return new(condition.GroupNo, condition.AttributeName, condition.Operator, condition.IsNegated, condition.Value, condition.Description);
    }
}

/// <summary>
/// 一条访问策略。
/// </summary>
/// <param name="Id">策略 ID。</param>
/// <param name="Code">编码。</param>
/// <param name="Name">名称。</param>
/// <param name="Description">说明。</param>
/// <param name="Action">命中时的动作。</param>
/// <param name="Priority">优先级（越小越先求值）。</param>
/// <param name="Status">状态。</param>
/// <param name="EffectiveAt">生效时间。</param>
/// <param name="ExpiresAt">失效时间。</param>
/// <param name="IsEffective">此刻是否生效（启用且在有效期内）。</param>
/// <param name="Phase">时机：平台策略只引用认证前就取得到的属性即认证前，否则认证后。</param>
/// <param name="TemplateCode">来自哪个模板。</param>
/// <param name="Remark">备注。</param>
/// <param name="Conditions">条件，按写入顺序。</param>
/// <param name="CreatedAt">创建时间。</param>
/// <param name="ModifiedAt">修改时间。</param>
public sealed record AccessPolicyResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    AccessPolicyAction Action,
    int Priority,
    AccessPolicyStatus Status,
    DateTimeOffset? EffectiveAt,
    DateTimeOffset? ExpiresAt,
    bool IsEffective,
    ConditionHitPhase Phase,
    string? TemplateCode,
    string? Remark,
    IReadOnlyList<AccessPolicyConditionResponse> Conditions,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt);

/// <summary>
/// <c>policies.get</c> 的请求。
/// </summary>
/// <param name="Id">策略 ID。</param>
public sealed record GetAccessPolicyRequest([Required] Guid Id);

/// <summary>
/// <c>policies.create</c> 的请求。
/// </summary>
/// <remarks>
/// 新建一律只记录、启用，动作与状态另有操作。
/// </remarks>
/// <param name="Code">编码（同一侧唯一，不含空白）。</param>
/// <param name="Name">名称。</param>
/// <param name="Conditions">条件，1 到 100 条、最多 20 个组。</param>
/// <param name="Description">说明。</param>
/// <param name="Priority">优先级（越小越先求值）。</param>
/// <param name="EffectiveAt">生效时间。</param>
/// <param name="ExpiresAt">失效时间。</param>
/// <param name="TemplateCode">来自哪个模板（见 <c>policies.templates</c>）。</param>
/// <param name="Remark">备注。</param>
public sealed record CreateAccessPolicyRequest(
    [Required, StringLength(100, MinimumLength = 1)] string Code,
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [Required, MinLength(1), MaxLength(SysAccessPolicy.MaxConditions)] IReadOnlyList<AccessPolicyConditionRequest> Conditions,
    [StringLength(1000)] string? Description = null,
    [Range(0, 100000)] int Priority = 100,
    DateTimeOffset? EffectiveAt = null,
    DateTimeOffset? ExpiresAt = null,
    [StringLength(100)] string? TemplateCode = null,
    [StringLength(500)] string? Remark = null)
{
    /// <summary>
    /// 转成策略内容。
    /// </summary>
    public AccessPolicyDefinition ToDefinition() => new(Code, Name, Description, Priority, EffectiveAt, ExpiresAt, TemplateCode, Remark, [.. Conditions.Select(static c => c.ToDefinition())]);
}

/// <summary>
/// <c>policies.update</c> 的请求：整体修改内容与条件，动作与状态不变。
/// </summary>
/// <param name="Id">策略 ID。</param>
/// <param name="Code">编码。</param>
/// <param name="Name">名称。</param>
/// <param name="Conditions">条件（整组替换）。</param>
/// <param name="Description">说明。</param>
/// <param name="Priority">优先级。</param>
/// <param name="EffectiveAt">生效时间。</param>
/// <param name="ExpiresAt">失效时间。</param>
/// <param name="TemplateCode">来自哪个模板。</param>
/// <param name="Remark">备注。</param>
public sealed record UpdateAccessPolicyRequest(
    [Required] Guid Id,
    [Required, StringLength(100, MinimumLength = 1)] string Code,
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [Required, MinLength(1), MaxLength(SysAccessPolicy.MaxConditions)] IReadOnlyList<AccessPolicyConditionRequest> Conditions,
    [StringLength(1000)] string? Description = null,
    [Range(0, 100000)] int Priority = 100,
    DateTimeOffset? EffectiveAt = null,
    DateTimeOffset? ExpiresAt = null,
    [StringLength(100)] string? TemplateCode = null,
    [StringLength(500)] string? Remark = null)
{
    /// <summary>
    /// 转成策略内容。
    /// </summary>
    public AccessPolicyDefinition ToDefinition() => new(Code, Name, Description, Priority, EffectiveAt, ExpiresAt, TemplateCode, Remark, [.. Conditions.Select(static c => c.ToDefinition())]);
}

/// <summary>
/// <c>policies.set-action</c> 的请求。
/// </summary>
/// <param name="Id">策略 ID。</param>
/// <param name="Action">只记录、拒绝或放行。</param>
public sealed record SetAccessPolicyActionRequest([Required] Guid Id, AccessPolicyAction Action);

/// <summary>
/// <c>policies.set-status</c> 的请求。
/// </summary>
/// <param name="Id">策略 ID。</param>
/// <param name="Status">启用或停用。</param>
public sealed record SetAccessPolicyStatusRequest([Required] Guid Id, AccessPolicyStatus Status);

/// <summary>
/// <c>policies.delete</c> 的请求。
/// </summary>
/// <param name="Ids">策略 ID，1 到 100 个。</param>
public sealed record AccessPolicyIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);

/// <summary>
/// <c>policies.attributes</c> 的请求（无参数）。
/// </summary>
public sealed record AccessPolicyAttributesRequest;

/// <summary>
/// <c>policies.templates</c> 的请求（无参数）。
/// </summary>
public sealed record AccessPolicyTemplatesRequest;

/// <summary>
/// 一个预置模板。
/// </summary>
/// <param name="Code">编码（平台种子写入的策略同名）。</param>
/// <param name="Name">名称。</param>
/// <param name="Description">说明。</param>
/// <param name="Action">平台种子写入时的动作（从模板新建一律只记录）。</param>
/// <param name="Status">平台种子写入时的状态。</param>
/// <param name="Priority">优先级。</param>
/// <param name="Phase">在当前一侧的时机。</param>
/// <param name="Conditions">条件。</param>
public sealed record AccessPolicyTemplateResponse(string Code, string Name, string Description, AccessPolicyAction Action, AccessPolicyStatus Status, int Priority, ConditionHitPhase Phase, IReadOnlyList<AccessPolicyConditionResponse> Conditions);

/// <summary>
/// <c>policies.templates</c> 的响应。
/// </summary>
/// <param name="Items">模板。</param>
public sealed record AccessPolicyTemplatesResponse(IReadOnlyList<AccessPolicyTemplateResponse> Items);

/// <summary>
/// <c>policies.trial</c> 的请求。
/// </summary>
/// <remarks>
/// 已有策略（<paramref name="PolicyId"/>）或草稿条件（<paramref name="Conditions"/>），按给定的请求情形只求这一条、不看有效期。
/// </remarks>
/// <param name="PolicyId">已有策略；给了草稿条件即以草稿为准。</param>
/// <param name="Action">按哪个动作试；不填即策略的动作（草稿为只记录）。</param>
/// <param name="Conditions">草稿条件。</param>
/// <param name="Ip">客户端地址；不填即取不到。</param>
/// <param name="Time">时刻；不填即现在。</param>
/// <param name="UserAgent">User-Agent。</param>
/// <param name="Path">请求路径。</param>
/// <param name="Method">请求方法。</param>
/// <param name="UserId">已登录的用户（这一侧的用户）；不填即未登录。</param>
/// <param name="PermissionCodes">本次调用要求的权限码。</param>
public sealed record TrialAccessPolicyRequest(
    Guid? PolicyId = null,
    AccessPolicyAction? Action = null,
    [MaxLength(SysAccessPolicy.MaxConditions)] IReadOnlyList<AccessPolicyConditionRequest>? Conditions = null,
    [StringLength(64)] string? Ip = null,
    DateTimeOffset? Time = null,
    [StringLength(512)] string? UserAgent = null,
    [StringLength(512)] string? Path = null,
    [StringLength(16)] string? Method = null,
    Guid? UserId = null,
    [MaxLength(20)] IReadOnlyList<string>? PermissionCodes = null);

/// <summary>
/// <c>policies.trial</c> 的响应。
/// </summary>
/// <param name="Outcome">求值结果。</param>
/// <param name="Action">按哪个动作试的。</param>
/// <param name="WouldBlock">是否会拦下（命中且动作为拒绝；同层的放行不计）。</param>
/// <param name="Phase">时机。</param>
/// <param name="Errors">编译错误（按请求区域翻译）。</param>
/// <param name="Ip">客户端地址。</param>
/// <param name="Location">地址的归属地（国家、省份、城市与运营商）。</param>
/// <param name="Time">判定时刻。</param>
/// <param name="TimeZone">时段所按的时区。</param>
/// <param name="UserAgent">User-Agent。</param>
/// <param name="UserId">用户。</param>
/// <param name="UserName">登录名。</param>
/// <param name="Groups">逐组逐条件的说明。</param>
public sealed record AccessPolicyTrialResponse(
    ConditionOutcome? Outcome,
    AccessPolicyAction Action,
    bool WouldBlock,
    ConditionHitPhase Phase,
    IReadOnlyList<string> Errors,
    string? Ip,
    string? Location,
    DateTimeOffset Time,
    string TimeZone,
    string? UserAgent,
    Guid? UserId,
    string? UserName,
    IReadOnlyList<ConditionTrialGroupResponse> Groups);
