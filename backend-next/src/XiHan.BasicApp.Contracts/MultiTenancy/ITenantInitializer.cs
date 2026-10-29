// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.MultiTenancy;

/// <summary>
/// 租户开通管理员时的扩展点。
/// </summary>
/// <remarks>
/// 上层包（Access）为新租户准备内建角色并把管理员指派进去。
/// Tenancy 不能引用 Access，所以经此契约倒置依赖；没有任何实现时开通管理员失败（否则管理员会没有权限）。
/// </remarks>
public interface ITenantInitializer
{
    /// <summary>
    /// 在 <paramref name="tenantId"/> 里为 <paramref name="adminUserId"/> 准备管理员身份。
    /// </summary>
    /// <remarks>
    /// 重复调用必须幂等。
    /// </remarks>
    Task<Result> InitializeAsync(string tenantId, Guid adminUserId, InvocationContext context);
}
