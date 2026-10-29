// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.Framework.Security.Abstractions.Conditions;

namespace XiHan.BasicApp.Access.Operations.PermissionConditions;

/// <summary>
/// <c>permission-conditions.attributes</c> 的请求（无参数）。
/// </summary>
public sealed record ConditionAttributesRequest;

/// <summary>
/// 条件属性目录里的一项。
/// </summary>
/// <param name="Name">登记名（族以 <c>.*</c> 结尾）。</param>
/// <param name="Kind">值种类。</param>
/// <param name="IsMultiValued">是否多值。</param>
/// <param name="IsFamily">是否一族属性。</param>
/// <param name="Aliases">旧写法别名。</param>
/// <param name="Operators">可用的运算符。</param>
/// <param name="IsBeforeAuthentication">认证前是否取得到（访问策略的认证前时机只能用它们）。</param>
public sealed record ConditionAttributeResponse(string Name, ConditionValueKind Kind, bool IsMultiValued, bool IsFamily, IReadOnlyList<string> Aliases, IReadOnlyList<ConditionOperator> Operators, bool IsBeforeAuthentication);

/// <summary>
/// <c>permission-conditions.attributes</c> 的响应。
/// </summary>
/// <param name="Items">属性。</param>
public sealed record ConditionAttributesResponse(IReadOnlyList<ConditionAttributeResponse> Items);

/// <summary>
/// <c>permission-conditions.list</c> 的请求：一个主体上的授权绑定与条件。
/// </summary>
/// <param name="SubjectKind">主体类别。</param>
/// <param name="SubjectId">主体 ID：这一侧的角色或用户。</param>
public sealed record PermissionConditionsRequest(GrantSubjectKind SubjectKind, [Required] Guid SubjectId);

/// <summary>
/// 主体上的一个授权绑定。
/// </summary>
/// <param name="Permission">权限码。</param>
/// <param name="Action">授予或拒绝。</param>
/// <param name="EffectiveAt">生效时间。</param>
/// <param name="ExpiresAt">到期时间。</param>
public sealed record ConditionBindingResponse(string Permission, PermissionAction Action, DateTimeOffset? EffectiveAt, DateTimeOffset? ExpiresAt);

/// <summary>
/// 一条条件子句。
/// </summary>
/// <param name="Id">子句 ID。</param>
/// <param name="Permission">绑定的权限码。</param>
/// <param name="Action">挂在授予还是拒绝上。</param>
/// <param name="GroupNo">组号。</param>
/// <param name="AttributeName">属性名。</param>
/// <param name="Operator">运算符。</param>
/// <param name="IsNegated">是否取反。</param>
/// <param name="Value">条件值。</param>
/// <param name="Description">说明。</param>
/// <param name="Status">状态。</param>
/// <param name="CreatedAt">创建时间。</param>
public sealed record PermissionConditionResponse(Guid Id, string Permission, PermissionAction Action, int GroupNo, string AttributeName, ConditionOperator Operator, bool IsNegated, string? Value, string? Description, ConditionStatus Status, DateTimeOffset CreatedAt)
{
    /// <summary>
    /// 由实体组装。
    /// </summary>
    public static PermissionConditionResponse From(SysPermissionCondition condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        return new(condition.Id, condition.Permission, condition.Action, condition.GroupNo, condition.AttributeName, condition.Operator, condition.IsNegated, condition.Value, condition.Description, condition.Status, condition.CreatedAt);
    }
}

/// <summary>
/// <c>permission-conditions.list</c> 的响应。
/// </summary>
/// <param name="SubjectKind">主体类别。</param>
/// <param name="SubjectId">主体 ID。</param>
/// <param name="Bindings">此刻的授权绑定（授予与拒绝）。</param>
/// <param name="Conditions">全部子句（含停用的），按组号与创建时间排列。</param>
public sealed record PermissionConditionsResponse(GrantSubjectKind SubjectKind, Guid SubjectId, IReadOnlyList<ConditionBindingResponse> Bindings, IReadOnlyList<PermissionConditionResponse> Conditions);

/// <summary>
/// <c>permission-conditions.create</c> 的请求：在一个授权绑定上加一条子句。
/// </summary>
/// <param name="SubjectKind">主体类别。</param>
/// <param name="SubjectId">主体 ID。</param>
/// <param name="Permission">绑定的权限码。</param>
/// <param name="Action">挂在授予（满足才授予）还是拒绝（满足才拒绝）上。</param>
/// <param name="AttributeName">属性名（见 <c>permission-conditions.attributes</c>）。</param>
/// <param name="Operator">运算符。</param>
/// <param name="GroupNo">组号（同组取且、组间取或）。</param>
/// <param name="IsNegated">是否取反。</param>
/// <param name="Value">条件值（判空类运算符不填）。</param>
/// <param name="Description">说明。</param>
/// <param name="Status">状态。</param>
public sealed record CreatePermissionConditionRequest(
    GrantSubjectKind SubjectKind,
    [Required] Guid SubjectId,
    [Required, StringLength(128, MinimumLength = 1)] string Permission,
    PermissionAction Action,
    [Required, StringLength(200, MinimumLength = 1)] string AttributeName,
    ConditionOperator Operator,
    [Range(0, 99)] int GroupNo = 0,
    bool IsNegated = false,
    [StringLength(SysPermissionCondition.MaxValueLength)] string? Value = null,
    [StringLength(500)] string? Description = null,
    ConditionStatus Status = ConditionStatus.Active)
{
    /// <summary>
    /// 转成子句定义。
    /// </summary>
    public ConditionClauseDefinition ToDefinition() => new(GroupNo, AttributeName, Operator, IsNegated, Value, Description, Status);
}

/// <summary>
/// <c>permission-conditions.update</c> 的请求：整体修改一条子句（含启停），绑定不变。
/// </summary>
/// <param name="Id">子句 ID。</param>
/// <param name="AttributeName">属性名。</param>
/// <param name="Operator">运算符。</param>
/// <param name="GroupNo">组号。</param>
/// <param name="IsNegated">是否取反。</param>
/// <param name="Value">条件值。</param>
/// <param name="Description">说明。</param>
/// <param name="Status">状态。</param>
public sealed record UpdatePermissionConditionRequest(
    [Required] Guid Id,
    [Required, StringLength(200, MinimumLength = 1)] string AttributeName,
    ConditionOperator Operator,
    [Range(0, 99)] int GroupNo = 0,
    bool IsNegated = false,
    [StringLength(SysPermissionCondition.MaxValueLength)] string? Value = null,
    [StringLength(500)] string? Description = null,
    ConditionStatus Status = ConditionStatus.Active)
{
    /// <summary>
    /// 转成子句定义。
    /// </summary>
    public ConditionClauseDefinition ToDefinition() => new(GroupNo, AttributeName, Operator, IsNegated, Value, Description, Status);
}

/// <summary>
/// <c>permission-conditions.delete</c> 的请求。
/// </summary>
/// <param name="Ids">子句 ID，1 到 100 个。</param>
public sealed record PermissionConditionIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);
