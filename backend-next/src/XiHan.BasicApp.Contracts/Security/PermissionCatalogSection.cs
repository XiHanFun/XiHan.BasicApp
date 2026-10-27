// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;

namespace XiHan.BasicApp.Contracts.Security;

/// <summary>
/// 一个能力包登记的权限目录片段（包名 + 它的 <see cref="PermissionDescriptor"/>）；各包在 <c>Configure</c> 里 <see cref="PermissionCatalogServiceCollectionExtensions.AddPermissionCatalog"/>，Access 汇总成权限目录。
/// </summary>
/// <param name="Package">包名（权限码的首段，小写）。</param>
/// <param name="Permissions">该包的全部权限描述。</param>
public sealed record PermissionCatalogSection(string Package, IReadOnlyList<PermissionDescriptor> Permissions);

/// <summary>
/// 权限目录片段的登记。
/// </summary>
public static class PermissionCatalogServiceCollectionExtensions
{
    /// <summary>
    /// 登记一个包的权限描述；同一容器里可以登记多个包，按 <see cref="PermissionCatalogSection"/> 枚举取回。
    /// </summary>
    public static IServiceCollection AddPermissionCatalog(this IServiceCollection services, string package, IReadOnlyList<PermissionDescriptor> permissions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(package);
        ArgumentNullException.ThrowIfNull(permissions);
        foreach (var permission in permissions)
        {
            if (!string.Equals(permission.Package, package, StringComparison.Ordinal))
            {
                throw new ArgumentException($"Permission '{permission.Code}' does not belong to package '{package}'.", nameof(permissions));
            }
        }

        services.AddSingleton(new PermissionCatalogSection(package, permissions));
        return services;
    }
}
