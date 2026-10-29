// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Architecture.Tests.Model;

/// <summary>
/// 读各包的 <c>&lt;包&gt;Permissions.All</c>。
/// </summary>
/// <remarks>
/// <c>&lt;包&gt;Permissions.All</c> 是静态属性，类型为 <c>IReadOnlyList&lt;PermissionDescriptor&gt;</c>；没有权限类的包视为没有权限。
/// </remarks>
public static class PermissionCatalog
{
    public static IReadOnlyList<PermissionDescriptor> Of(string package)
    {
        var assembly = Packages.Assemblies.Single(a => string.Equals(Packages.ShortName(a.GetName().Name!), package, StringComparison.Ordinal));
        var type = assembly.GetType($"{Packages.Prefix}{package}.{package}Permissions");
        if (type is null)
        {
            return [];
        }

        var property = type.GetProperty("All", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(property);
        return Assert.IsAssignableFrom<IReadOnlyList<PermissionDescriptor>>(property.GetValue(null));
    }
}
