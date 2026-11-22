// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Access.Domain.Services;

/// <summary>
/// 权限目录（单例）
/// </summary>
/// <remarks>
/// 取 <see cref="RegisteredPermissions"/>：各包登记的 <see cref="PermissionCatalogSection"/> 加上运行期贡献的（已加载插件的权限码），随加载与卸载变化；按作用侧筛选（平台主体只命中 Platform / Both，租户主体只命中 Tenant / Both）
/// 目录来自操作注册表侧的描述，不落库（方案 5.14：权限目录从注册表派生，不再手写清单）
/// </remarks>
/// <param name="registered">已登记的全部权限描述</param>
public sealed class PermissionCatalog(RegisteredPermissions registered)
{
    /// <summary>
    /// 全部权限描述，按权限码排序
    /// </summary>
    public IReadOnlyList<PermissionDescriptor> All => registered.All;

    /// <summary>
    /// 按权限码取描述
    /// </summary>
    public PermissionDescriptor? Find(string code) => registered.Find(code);

    /// <summary>
    /// 取某一侧可见的权限
    /// </summary>
    /// <remarks>
    /// 平台侧取 Platform / Both，租户侧取 Tenant / Both
    /// </remarks>
    public IReadOnlyList<PermissionDescriptor> ForSide(bool platformSide) => All.Where(p => AllowsSide(p, platformSide)).ToList();

    /// <summary>
    /// 权限码是否属于该侧
    /// </summary>
    /// <remarks>
    /// 目录里没有的权限码（没有登记作用侧）不拦
    /// </remarks>
    public bool AllowsSide(string code, bool platformSide) => Find(code) is not { } permission || AllowsSide(permission, platformSide);

    /// <summary>
    /// 权限是否属于该侧
    /// </summary>
    public static bool AllowsSide(PermissionDescriptor permission, bool platformSide)
    {
        ArgumentNullException.ThrowIfNull(permission);
        return permission.Scope == PermissionScope.Both || (platformSide ? permission.Scope == PermissionScope.Platform : permission.Scope == PermissionScope.Tenant);
    }
}
