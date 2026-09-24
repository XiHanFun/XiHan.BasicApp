// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Permissions;

/// <summary>
/// 权限作用侧判定
/// </summary>
/// <remarks>
/// 作用侧是权限目录（<see cref="SysPermission.Side"/>）上的属性，由各模块的权限定义声明、种子落库；
/// 生效、套餐白名单、租户授权统一按它判定，不再维护平行的平台专属清单。
/// </remarks>
public static class PermissionSideExtensions
{
    /// <summary>
    /// 是否为已声明的作用侧（平台 / 租户 / 两侧）
    /// </summary>
    /// <param name="side">作用侧</param>
    public static bool IsDeclared(this PermissionSide side)
    {
        return side is PermissionSide.Platform or PermissionSide.Tenant or PermissionSide.Both;
    }

    /// <summary>
    /// 权限在给定上下文是否生效：平台上下文生效平台侧，业务租户上下文生效租户侧，两侧两边都生效
    /// </summary>
    /// <param name="side">作用侧</param>
    /// <param name="isPlatformContext">当前是否平台上下文（0 号租户）</param>
    public static bool IsEffectiveIn(this PermissionSide side, bool isPlatformContext)
    {
        return (side & (isPlatformContext ? PermissionSide.Platform : PermissionSide.Tenant)) != 0;
    }

    /// <summary>
    /// 权限能否在租户里生效（进入套餐白名单、授予租户角色的前提）
    /// </summary>
    /// <param name="side">作用侧</param>
    public static bool IsTenantEffective(this PermissionSide side)
    {
        return side.IsEffectiveIn(isPlatformContext: false);
    }
}
