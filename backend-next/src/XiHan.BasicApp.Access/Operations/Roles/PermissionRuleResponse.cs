// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// 一条授予规则：权限码上的 Deny，或带有效期 / 原因的 Grant（普通授予没有规则行）。
/// </summary>
/// <param name="Permission">权限码。</param>
/// <param name="Action">授予还是拒绝。</param>
/// <param name="EffectiveAt">生效时间。</param>
/// <param name="ExpiresAt">到期时间（不含）。</param>
/// <param name="Reason">原因。</param>
public sealed record PermissionRuleResponse(string Permission, PermissionAction Action, DateTimeOffset? EffectiveAt, DateTimeOffset? ExpiresAt, string? Reason)
{
    /// <summary>
    /// 由规则行构造。
    /// </summary>
    public static PermissionRuleResponse From(SysPermissionRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return new PermissionRuleResponse(rule.Permission, rule.Action, rule.EffectiveAt, rule.ExpiresAt, rule.Reason);
    }
}

/// <summary>
/// 有效期的校验：两头都有时到期必须晚于生效。
/// </summary>
internal static class GrantWindow
{
    /// <summary>
    /// 校验。
    /// </summary>
    public static Result Validate(DateTimeOffset? effectiveAt, DateTimeOffset? expiresAt)
        => effectiveAt is { } from && expiresAt is { } to && to <= from
            ? Error.Validation(AccessErrors.WindowInvalid, AccessMessages.WindowInvalid, MessageArguments.Of(("effectiveAt", from), ("expiresAt", to)))
            : Result.Success();
}
