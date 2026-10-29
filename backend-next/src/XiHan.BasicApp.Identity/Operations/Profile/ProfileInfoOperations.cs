// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Profile;

/// <summary>
/// <c>profile.update</c> 的请求。
/// </summary>
/// <remarks>
/// 整体覆盖，4.x 同。头像另走头像端点与 <c>profile.remove-avatar</c>。
/// </remarks>
/// <param name="DisplayName">显示名（4.x 的昵称）。</param>
/// <param name="RealName">真实姓名。</param>
/// <param name="Gender">性别。</param>
/// <param name="Birthday">生日。</param>
/// <param name="Country">国家或地区。</param>
/// <param name="Remark">个人简介。</param>
public sealed record UpdateProfileRequest(
    [Required, StringLength(64, MinimumLength = 1)] string DisplayName,
    [StringLength(50)] string? RealName,
    UserGender Gender,
    DateTimeOffset? Birthday,
    [StringLength(50)] string? Country,
    [StringLength(500)] string? Remark);

/// <summary>
/// <c>profile.change-username</c> 的请求。
/// </summary>
/// <param name="UserName">新用户名（规则与新建用户相同）。</param>
/// <param name="Password">当前密码。</param>
public sealed record ChangeUserNameRequest(
    [Required, StringLength(64, MinimumLength = 3), RegularExpression("^[a-z][a-z0-9._-]*$")] string UserName,
    [Required, StringLength(128)] string Password);

/// <summary>
/// 本人改用户名的限制。
/// </summary>
/// <remarks>
/// 4.x 同：90 天一次。
/// </remarks>
internal static class UserNameChange
{
    /// <summary>
    /// 两次改名的最短间隔。
    /// </summary>
    public static readonly TimeSpan Cooldown = TimeSpan.FromDays(90);
}

/// <summary>
/// 保存本人的资料。
/// </summary>
/// <remarks>
/// <c>PUT /api/profile</c>（4.x 同，整体覆盖）。空白当作没填。已登录即可，模仿会话不拒绝（4.x 同）。
/// </remarks>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class UpdateProfile(IRepository<SysUser, Guid> users, UserAccountService accounts) : IOperation<UpdateProfileRequest, ProfileResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "profile.update";

    /// <inheritdoc />
    public async ValueTask<Result<ProfileResponse>> HandleAsync(UpdateProfileRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: false).ConfigureAwait(false);
        if (!subject.TryGetValue(out var found))
        {
            return subject.Error!;
        }

        var user = found.User;
        user.DisplayName = request.DisplayName.Trim();
        user.RealName = Blank(request.RealName);
        user.Gender = request.Gender;
        user.Birthday = request.Birthday;
        user.Country = Blank(request.Country);
        user.Remark = Blank(request.Remark);
        var updated = await users.UpdateAsync(user, context.CancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : ProfileResponse.From(user, found.Security);
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>
/// 改本人的用户名。
/// </summary>
/// <remarks>
/// <c>POST /api/profile:change-username</c>（4.x <c>Profile/ChangeUserName</c>）。
/// 要当前密码（不对即 <c>IDENTITY_DENIED_003</c>），90 天一次（<c>IDENTITY_CONFLICT_009</c>），
/// 与现在的相同即 <c>IDENTITY_VALIDATION_017</c>，同一注册地里已被占用即 <c>IDENTITY_CONFLICT_001</c>。模仿会话不能做（4.x 同）。
/// 规则与新建用户相同（4.x 是 3–30 位字母数字下划线）；已签发的令牌里的用户名到下次续签才换。
/// </remarks>
/// <param name="users">用户仓储。</param>
/// <param name="securities">账号安全仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="clock">时钟。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class ChangeUserName(IRepository<SysUser, Guid> users, IRepository<SysUserSecurity, Guid> securities, UserAccountService accounts, TimeProvider clock) : IOperation<ChangeUserNameRequest, ProfileResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "profile.change-username";

    /// <inheritdoc />
    public async ValueTask<Result<ProfileResponse>> HandleAsync(ChangeUserNameRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: true).ConfigureAwait(false);
        if (!subject.TryGetValue(out var found))
        {
            return subject.Error!;
        }

        var (user, security) = found;
        if (!PasswordHasher.Verify(request.Password, security.PasswordHash))
        {
            return Error.Denied(IdentityErrors.InvalidCredentials, IdentityMessages.InvalidCredentials);
        }

        var userName = request.UserName.Trim();
        if (string.Equals(userName, user.UserName, StringComparison.Ordinal))
        {
            return Error.Validation(IdentityErrors.UserNameUnchanged, IdentityMessages.UserNameUnchanged);
        }

        var now = clock.GetUtcNow();
        if (security.UserNameChangedAt?.Add(UserNameChange.Cooldown) is { } next && next > now)
        {
            return Error.Conflict(IdentityErrors.UserNameChangeTooSoon, IdentityMessages.UserNameChangeTooSoon, MessageArguments.Of(("days", (int)Math.Ceiling((next - now).TotalDays))));
        }

        if ((await accounts.FindByNameAsync(user.HomeTenantId, userName, context.CancellationToken).ConfigureAwait(false)).HasValue)
        {
            return Error.Conflict(IdentityErrors.UserNameTaken, IdentityMessages.UserNameTaken, MessageArguments.Of(("userName", userName), ("tenant", user.HomeTenantId)));
        }

        user.UserName = userName;
        security.UserNameChangedAt = now;
        var updated = await users.UpdateAsync(user, context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        var saved = await securities.UpdateAsync(security, context.CancellationToken).ConfigureAwait(false);
        return saved.IsFailure ? saved.Error! : ProfileResponse.From(user, security);
    }
}

/// <summary>
/// 去掉本人的头像。
/// </summary>
/// <remarks>
/// <c>POST /api/profile:remove-avatar</c>（4.x 把头像存空）。头像文件随之回收；本来没有即无事。已登录即可，模仿会话不拒绝（4.x 同）。
/// 上传新头像走宿主的头像端点 <c>POST /api/profile/avatar</c>。
/// </remarks>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="avatars">头像。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class RemoveAvatar(IRepository<SysUser, Guid> users, UserAccountService accounts, ProfileAvatars avatars) : IOperation<ProfileRequest, ProfileResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "profile.remove-avatar";

    /// <inheritdoc />
    public async ValueTask<Result<ProfileResponse>> HandleAsync(ProfileRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: false).ConfigureAwait(false);
        if (!subject.TryGetValue(out var found))
        {
            return subject.Error!;
        }

        var set = await avatars.SetAsync(found.User, null, context.CancellationToken).ConfigureAwait(false);
        return set.IsFailure ? set.Error! : ProfileResponse.From(found.User, found.Security);
    }
}
