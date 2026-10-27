// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Identity.Domain.Entities;

namespace XiHan.BasicApp.Identity.Operations.Users;

/// <summary>
/// 用户的响应形态；<c>users.create</c>、<c>users.get</c> 的响应与 <c>users.list</c> 的列表项共用。
/// </summary>
/// <param name="Id">用户 ID。</param>
/// <param name="UserName">登录名。</param>
/// <param name="DisplayName">显示名。</param>
/// <param name="Email">邮箱。</param>
/// <param name="HomeTenantId">注册地租户 ID。</param>
/// <param name="Status">状态。</param>
/// <param name="CreatedAt">创建时间（UTC）。</param>
/// <param name="CreatedBy">创建人（主体 ID）。</param>
public sealed record UserResponse(
    Guid Id,
    string UserName,
    string DisplayName,
    string? Email,
    string HomeTenantId,
    UserStatus Status,
    DateTimeOffset CreatedAt,
    string? CreatedBy)
{
    /// <summary>
    /// 由实体投影。
    /// </summary>
    public static UserResponse From(SysUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new UserResponse(user.Id, user.UserName, user.DisplayName, user.Email, user.HomeTenantId, user.Status, user.CreatedAt, user.CreatedBy);
    }
}
