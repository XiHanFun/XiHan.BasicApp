// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.RegularExpressions;

namespace XiHan.BasicApp.Contracts;

/// <summary>
/// 租户 ID 的约定：字符串，文法 <c>^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$</c>（与框架 <c>TenantIdentifier</c> 一致）。
/// 平台数据不实现 <c>IMultiTenant</c>；需要区分「平台的还是某租户的」用 <see cref="Entities.IOwnedByTenant"/>，平台值固定为 <see cref="Platform"/>。
/// </summary>
public static partial class TenantIds
{
    /// <summary>
    /// 平台属主的常量值，用在 <see cref="Entities.IOwnedByTenant.OwnerTenantId"/> 上表示「平台的」。
    /// </summary>
    public const string Platform = "platform";

    /// <summary>
    /// 判断一个字符串是否符合租户 ID 文法。
    /// </summary>
    public static bool IsValid(string? tenantId) => tenantId is not null && Pattern().IsMatch(tenantId);

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$")]
    private static partial Regex Pattern();
}
