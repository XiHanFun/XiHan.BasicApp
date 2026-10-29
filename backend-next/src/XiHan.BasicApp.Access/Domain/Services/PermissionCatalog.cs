// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Frozen;
using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Access.Domain.Services;

/// <summary>
/// 权限目录（单例）。
/// </summary>
/// <remarks>
/// 汇总各包登记的 <see cref="PermissionCatalogSection"/>；按作用侧筛选（平台主体只命中 Platform / Both，租户主体只命中 Tenant / Both）。
/// 目录来自操作注册表侧的描述，不落库（方案 5.14：权限目录从注册表派生，不再手写清单）。
/// </remarks>
public sealed class PermissionCatalog
{
    private readonly FrozenDictionary<string, PermissionDescriptor> _byCode;

    /// <summary>
    /// 从登记的片段汇总。
    /// </summary>
    public PermissionCatalog(IEnumerable<PermissionCatalogSection> sections)
    {
        ArgumentNullException.ThrowIfNull(sections);
        var all = sections.SelectMany(static s => s.Permissions).OrderBy(static p => p.Code, StringComparer.Ordinal).ToList();
        _byCode = all.ToFrozenDictionary(static p => p.Code, StringComparer.Ordinal);
        All = all;
    }

    /// <summary>
    /// 全部权限描述，按权限码排序。
    /// </summary>
    public IReadOnlyList<PermissionDescriptor> All { get; }

    /// <summary>
    /// 按权限码取描述。
    /// </summary>
    public PermissionDescriptor? Find(string code) => _byCode.GetValueOrDefault(code);

    /// <summary>
    /// 取某一侧可见的权限。
    /// </summary>
    /// <remarks>
    /// 平台侧取 Platform / Both，租户侧取 Tenant / Both。
    /// </remarks>
    public IReadOnlyList<PermissionDescriptor> ForSide(bool platformSide) => All.Where(p => AllowsSide(p, platformSide)).ToList();

    /// <summary>
    /// 权限是否属于该侧。
    /// </summary>
    public static bool AllowsSide(PermissionDescriptor permission, bool platformSide)
    {
        ArgumentNullException.ThrowIfNull(permission);
        return permission.Scope == PermissionScope.Both || (platformSide ? permission.Scope == PermissionScope.Platform : permission.Scope == PermissionScope.Tenant);
    }
}
