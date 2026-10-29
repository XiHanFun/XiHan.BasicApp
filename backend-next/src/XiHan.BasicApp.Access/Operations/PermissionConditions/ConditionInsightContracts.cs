// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.Framework.Security.Abstractions.Conditions;

namespace XiHan.BasicApp.Access.Operations.PermissionConditions;

/// <summary>
/// <c>permission-conditions.trial</c> 的请求：一个用户在一个权限码上、按给定的请求情形判定（4.x 的条件试算）。不填的情形即取不到。
/// </summary>
/// <param name="UserId">用户：这一侧的用户，按他此刻的有效指派与委托。</param>
/// <param name="Permission">权限码。</param>
/// <param name="Ip">客户端地址。</param>
/// <param name="Time">判定时刻；不填即现在。</param>
/// <param name="UserAgent">客户端 User-Agent。</param>
/// <param name="Path">请求路径。</param>
/// <param name="Method">请求方法。</param>
/// <param name="IsImpersonating">是否模仿登录。</param>
public sealed record ConditionTrialRequest(
    [Required] Guid UserId,
    [Required, StringLength(128, MinimumLength = 1)] string Permission,
    [StringLength(64)] string? Ip = null,
    DateTimeOffset? Time = null,
    [StringLength(512)] string? UserAgent = null,
    [StringLength(512)] string? Path = null,
    [StringLength(16)] string? Method = null,
    bool IsImpersonating = false);

/// <summary>
/// 一条子句的求值说明。
/// </summary>
/// <param name="Key">子句键（子句 ID）。</param>
/// <param name="AttributeName">属性名。</param>
/// <param name="Operator">运算符。</param>
/// <param name="IsNegated">是否取反。</param>
/// <param name="Value">条件值。</param>
/// <param name="Outcome">结果。</param>
/// <param name="AttributeValues">属性此刻的取值。</param>
/// <param name="Reason">无法判定的原因（已按请求的区域翻译）。</param>
public sealed record ConditionTrialClauseResponse(string? Key, string AttributeName, ConditionOperator Operator, bool IsNegated, string? Value, ConditionOutcome Outcome, IReadOnlyList<string> AttributeValues, string? Reason);

/// <summary>
/// 一个条件组的求值说明。
/// </summary>
/// <param name="Key">组号。</param>
/// <param name="Outcome">结果。</param>
/// <param name="Clauses">子句。</param>
public sealed record ConditionTrialGroupResponse(string? Key, ConditionOutcome Outcome, IReadOnlyList<ConditionTrialClauseResponse> Clauses);

/// <summary>
/// 一个绑定上的条件集求值说明。
/// </summary>
/// <param name="SubjectKind">主体类别。</param>
/// <param name="SubjectId">主体 ID。</param>
/// <param name="SubjectName">主体名称（角色名或「显示名 (@登录名)」）。</param>
/// <param name="Binding">绑定的权限码（可能是 <c>*</c>）。</param>
/// <param name="Action">授予或拒绝。</param>
/// <param name="Applies">是否生效：授予侧为命中，拒绝侧为命中或无法编译。</param>
/// <param name="Outcome">求值结果（无法编译时为空）。</param>
/// <param name="Groups">各组。</param>
/// <param name="Errors">编译错误（已翻译）。</param>
public sealed record ConditionTrialSetResponse(GrantSubjectKind SubjectKind, Guid SubjectId, string? SubjectName, string Binding, PermissionAction Action, bool Applies, ConditionOutcome? Outcome, IReadOnlyList<ConditionTrialGroupResponse> Groups, IReadOnlyList<string> Errors);

/// <summary>
/// 一条授予路径的求值说明。
/// </summary>
/// <param name="Source">来源：<c>user</c>、<c>delegation</c>、<c>role</c> 或 <c>delegation-role</c>。</param>
/// <param name="RoleId">角色链的链首角色。</param>
/// <param name="RoleName">链首角色的名称。</param>
/// <param name="HasUnconditionalGrant">有没有无条件的授予。</param>
/// <param name="GrantSets">有条件的授予。</param>
/// <param name="DenySets">有条件的拒绝。</param>
/// <param name="Holds">是否成立。</param>
public sealed record ConditionTrialPathResponse(string Source, Guid? RoleId, string? RoleName, bool HasUnconditionalGrant, IReadOnlyList<ConditionTrialSetResponse> GrantSets, IReadOnlyList<ConditionTrialSetResponse> DenySets, bool Holds);

/// <summary>
/// <c>permission-conditions.trial</c> 的响应。
/// </summary>
/// <param name="Verdict">结论。</param>
/// <param name="UserId">用户。</param>
/// <param name="UserName">登录名。</param>
/// <param name="Permission">权限码。</param>
/// <param name="Ip">所用的客户端地址。</param>
/// <param name="Time">所用的判定时刻。</param>
/// <param name="TimeZone">条件所按的时区。</param>
/// <param name="Denies">用户本身有条件的拒绝。</param>
/// <param name="Paths">授予路径。</param>
public sealed record ConditionTrialResponse(ConditionVerdict Verdict, Guid UserId, string UserName, string Permission, string? Ip, DateTimeOffset Time, string TimeZone, IReadOnlyList<ConditionTrialSetResponse> Denies, IReadOnlyList<ConditionTrialPathResponse> Paths);

/// <summary>
/// <c>permission-conditions.batch-trial</c> 的请求：一个绑定影响到的人此刻各自的结论（4.x 的影响试算，最多列 200 人）。
/// </summary>
/// <param name="SubjectKind">主体类别：角色即它及其下级角色的有效成员，用户即本人。</param>
/// <param name="SubjectId">主体 ID。</param>
/// <param name="Permission">权限码。</param>
/// <param name="Ip">客户端地址。</param>
/// <param name="Time">判定时刻；不填即现在。</param>
/// <param name="UserAgent">客户端 User-Agent。</param>
/// <param name="Path">请求路径。</param>
/// <param name="Method">请求方法。</param>
public sealed record ConditionBatchTrialRequest(
    GrantSubjectKind SubjectKind,
    [Required] Guid SubjectId,
    [Required, StringLength(128, MinimumLength = 1)] string Permission,
    [StringLength(64)] string? Ip = null,
    DateTimeOffset? Time = null,
    [StringLength(512)] string? UserAgent = null,
    [StringLength(512)] string? Path = null,
    [StringLength(16)] string? Method = null);

/// <summary>
/// 影响试算里一个人的结论。
/// </summary>
/// <param name="UserId">用户。</param>
/// <param name="UserName">登录名。</param>
/// <param name="Verdict">结论。</param>
public sealed record ConditionBatchTrialItemResponse(Guid UserId, string UserName, ConditionVerdict Verdict);

/// <summary>
/// <c>permission-conditions.batch-trial</c> 的响应。
/// </summary>
/// <param name="Permission">权限码。</param>
/// <param name="TotalUsers">影响到的总人数。</param>
/// <param name="Truncated">是否只列了前 200 人（按登录名）。</param>
/// <param name="Items">各人的结论。</param>
public sealed record ConditionBatchTrialResponse(string Permission, int TotalUsers, bool Truncated, IReadOnlyList<ConditionBatchTrialItemResponse> Items);

/// <summary>
/// <c>permission-conditions.inventory</c> 的请求（无参数）。
/// </summary>
public sealed record ConditionInventoryRequest;

/// <summary>
/// 盘点诊断（4.x 同，去掉 6.0 不会出现的「绑定不明」）。
/// </summary>
public enum ConditionDiagnostic
{
    /// <summary>
    /// 属性未登记。
    /// </summary>
    UnregisteredAttribute = 0,

    /// <summary>
    /// 用了旧写法（别名）。
    /// </summary>
    LegacyName = 1,

    /// <summary>
    /// 子句编译不过（运算符与值种类不配、值写错等）。
    /// </summary>
    InvalidCondition = 2,

    /// <summary>
    /// 绑定已不在（授予或拒绝撤了、角色停用或已删、用户已删）。
    /// </summary>
    BindingInactive = 3,
}

/// <summary>
/// 盘点里一个条件组的结论。
/// </summary>
public enum ConditionGroupConclusion
{
    /// <summary>
    /// 正常参与判定。
    /// </summary>
    Evaluable = 0,

    /// <summary>
    /// 组里有用不了的子句，这组永远不成立。
    /// </summary>
    NeverMatches = 1,

    /// <summary>
    /// 绑定已不在，这组不参与。
    /// </summary>
    BindingInactive = 2,

    /// <summary>
    /// 组里的子句全部停用，这组不参与。
    /// </summary>
    Disabled = 3,
}

/// <summary>
/// 盘点里的一条子句。
/// </summary>
/// <param name="ConditionId">子句 ID。</param>
/// <param name="SubjectKind">主体类别。</param>
/// <param name="SubjectId">主体 ID。</param>
/// <param name="SubjectName">主体名称。</param>
/// <param name="Permission">权限码。</param>
/// <param name="Action">授予或拒绝。</param>
/// <param name="GroupNo">组号。</param>
/// <param name="AttributeName">属性名。</param>
/// <param name="Operator">运算符。</param>
/// <param name="IsNegated">是否取反。</param>
/// <param name="Value">条件值。</param>
/// <param name="Status">状态。</param>
/// <param name="Diagnostics">诊断。</param>
/// <param name="Message">编译不过的原因（已翻译）。</param>
/// <param name="GroupConclusion">所在组的结论。</param>
public sealed record ConditionInventoryItemResponse(Guid ConditionId, GrantSubjectKind SubjectKind, Guid SubjectId, string? SubjectName, string Permission, PermissionAction Action, int GroupNo, string AttributeName, ConditionOperator Operator, bool IsNegated, string? Value, ConditionStatus Status, IReadOnlyList<ConditionDiagnostic> Diagnostics, string? Message, ConditionGroupConclusion GroupConclusion);

/// <summary>
/// <c>permission-conditions.inventory</c> 的响应：当前一侧全部条件子句的诊断与各组结论。
/// </summary>
/// <param name="GeneratedAt">生成时间。</param>
/// <param name="ConditionCount">子句数。</param>
/// <param name="BindingCount">挂了条件的绑定数。</param>
/// <param name="Items">子句，按主体、权限码、动作、组号排列。</param>
public sealed record ConditionInventoryResponse(DateTimeOffset GeneratedAt, int ConditionCount, int BindingCount, IReadOnlyList<ConditionInventoryItemResponse> Items);
