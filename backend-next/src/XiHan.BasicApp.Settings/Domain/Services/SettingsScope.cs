// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Domain.Services;

/// <summary>
/// 本包数据的两侧规则。
/// </summary>
/// <remarks>
/// 读共享（平台的加当前租户自己的），写不共享（只写自己这一侧）。
/// </remarks>
public static class SettingsScope
{
    /// <summary>
    /// 当前主体的一侧：租户主体是它的租户，平台主体是 <see cref="TenantIds.Platform"/>。
    /// </summary>
    public static string OwnerOf(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.GetSecurity().Tenant?.Id ?? TenantIds.Platform;
    }

    /// <summary>
    /// 是否为平台一侧。
    /// </summary>
    public static bool IsPlatform(string owner) => string.Equals(owner, TenantIds.Platform, StringComparison.Ordinal);

    /// <summary>
    /// 这行数据对这一侧是否可见：平台的谁都能读，租户的只有它自己能读。
    /// </summary>
    public static bool IsVisible(IOwnedByTenant row, string owner)
    {
        ArgumentNullException.ThrowIfNull(row);
        return IsPlatform(row.OwnerTenantId) || string.Equals(row.OwnerTenantId, owner, StringComparison.Ordinal);
    }

    /// <summary>
    /// 这一侧能否改这行数据：只有属主自己。
    /// </summary>
    public static bool IsOwnedBy(IOwnedByTenant row, string owner)
    {
        ArgumentNullException.ThrowIfNull(row);
        return string.Equals(row.OwnerTenantId, owner, StringComparison.Ordinal);
    }
}
