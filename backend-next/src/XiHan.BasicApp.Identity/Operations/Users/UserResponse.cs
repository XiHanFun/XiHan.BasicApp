// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;

namespace XiHan.BasicApp.Identity.Operations.Users;

/// <summary>
/// 用户的响应形态；<c>users.create / get / update</c> 的响应、<c>users.list</c> 的列表项与登录响应里的资料共用。
/// </summary>
/// <param name="Id">用户 ID。</param>
/// <param name="UserName">登录名。</param>
/// <param name="DisplayName">显示名。</param>
/// <param name="Email">邮箱。</param>
/// <param name="PhoneNumber">手机号。</param>
/// <param name="HomeTenantId">注册地租户 ID；平台账号为 <c>platform</c>。</param>
/// <param name="Status">状态。</param>
/// <param name="LastLoginAt">最近登录时间（UTC）。</param>
/// <param name="CreatedAt">创建时间（UTC）。</param>
/// <param name="CreatedBy">创建人（主体 ID）。</param>
public sealed record UserResponse(
    Guid Id,
    string UserName,
    string DisplayName,
    string? Email,
    string? PhoneNumber,
    string HomeTenantId,
    UserStatus Status,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    string? CreatedBy)
{
    /// <summary>
    /// 由实体投影。
    /// </summary>
    public static UserResponse From(SysUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new UserResponse(user.Id, user.UserName, user.DisplayName, user.Email, user.PhoneNumber, user.HomeTenantId, user.Status, user.LastLoginAt, user.CreatedAt, user.CreatedBy);
    }

    /// <summary>
    /// 由实体投影并按字段安全规则打码（用户目录的响应）；登录名与显示名不可空，规则写入时已拒绝对它们隐藏，打码后不会为空。
    /// </summary>
    public static UserResponse From(SysUser user, FieldRules rules)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(rules);
        return new UserResponse(
            user.Id,
            rules.Mask(IdentityFieldSecurity.UserName, user.UserName)!,
            rules.Mask(IdentityFieldSecurity.DisplayName, user.DisplayName)!,
            rules.Mask(IdentityFieldSecurity.Email, user.Email),
            rules.Mask(IdentityFieldSecurity.PhoneNumber, user.PhoneNumber),
            user.HomeTenantId,
            user.Status,
            rules.Mask(IdentityFieldSecurity.LastLoginAt, user.LastLoginAt),
            user.CreatedAt,
            user.CreatedBy);
    }
}
