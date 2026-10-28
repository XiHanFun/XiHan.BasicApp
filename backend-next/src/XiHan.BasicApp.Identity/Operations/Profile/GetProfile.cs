// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Profile;

/// <summary>
/// 本人的资料与账号安全概况：<c>GET /api/profile</c>。已登录即可；模仿会话看到的是被模仿人的。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class GetProfile(IRepository<SysUser, Guid> users, UserAccountService accounts) : IOperation<ProfileRequest, ProfileResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "profile.get";

    /// <inheritdoc />
    public async ValueTask<Result<ProfileResponse>> HandleAsync(ProfileRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: false).ConfigureAwait(false);
        return subject.TryGetValue(out var found) ? ProfileResponse.From(found.User, found.Security) : subject.Error!;
    }
}
