// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Tenancy.Domain.Entities;

namespace XiHan.BasicApp.Tenancy.Operations.TenantMembers;

/// <summary>
/// 租户成员的响应形态：成员行加上用户的登录名与显示名（用户在平台库，按主键补）。
/// </summary>
/// <param name="Id">成员行主键。</param>
/// <param name="TenantId">租户 ID（目录键）。</param>
/// <param name="UserId">用户 ID。</param>
/// <param name="UserName">登录名。</param>
/// <param name="DisplayName">显示名。</param>
/// <param name="HomeTenantId">用户的注册地租户。</param>
/// <param name="MemberType">成员类型。</param>
/// <param name="CreatedAt">加入时间（UTC）。</param>
public sealed record TenantMemberResponse(
    Guid Id,
    string TenantId,
    Guid UserId,
    string UserName,
    string DisplayName,
    string HomeTenantId,
    TenantMemberType MemberType,
    DateTimeOffset CreatedAt)
{
    /// <summary>
    /// 由成员行与用户投影。
    /// </summary>
    public static TenantMemberResponse From(SysTenantMember member, SysUser user)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(user);
        return new TenantMemberResponse(member.Id, member.TenantId, member.UserId, user.UserName, user.DisplayName, user.HomeTenantId, member.MemberType, member.CreatedAt);
    }
}
