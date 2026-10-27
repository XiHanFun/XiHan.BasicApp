// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Identity.Domain.Entities;

namespace XiHan.BasicApp.Identity.Operations.Impersonation;

/// <summary>
/// 可被模仿的一个用户（<c>impersonation.candidates</c> 的列表项）。
/// </summary>
/// <param name="Id">用户 ID。</param>
/// <param name="UserName">登录名。</param>
/// <param name="DisplayName">显示名。</param>
/// <param name="HomeTenantId">注册地租户。</param>
public sealed record ImpersonationCandidateResponse(Guid Id, string UserName, string DisplayName, string HomeTenantId)
{
    /// <summary>
    /// 由实体投影。
    /// </summary>
    public static ImpersonationCandidateResponse From(SysUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new ImpersonationCandidateResponse(user.Id, user.UserName, user.DisplayName, user.HomeTenantId);
    }
}
