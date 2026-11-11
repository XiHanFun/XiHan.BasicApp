// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// 一项授予或撤销的结果
/// </summary>
/// <remarks>
/// 框架的批量授予只给与入参同序的结果（ADR-0154），这里按序配上授予本身，便于按权限码报告逐项失败
/// </remarks>
/// <param name="Grant">授予</param>
/// <param name="Outcome">结果</param>
public sealed record PermissionOutcome(PermissionGrant Grant, Result Outcome)
{
    /// <summary>
    /// 把同序的结果配上入参
    /// </summary>
    /// <param name="grants">入参</param>
    /// <param name="outcomes">与入参同序的结果</param>
    public static IReadOnlyList<PermissionOutcome> Zip(IReadOnlyList<PermissionGrant> grants, IReadOnlyList<Result> outcomes)
    {
        ArgumentNullException.ThrowIfNull(grants);
        ArgumentNullException.ThrowIfNull(outcomes);
        return [.. grants.Zip(outcomes, static (grant, outcome) => new PermissionOutcome(grant, outcome))];
    }
}
