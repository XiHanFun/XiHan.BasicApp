// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Profile;

/// <summary>
/// 开始绑定认证器：<c>POST /api/profile:setup-totp</c>。生成新密钥暂存十分钟（<see cref="TotpEnrollments"/>），账号此时不变；
/// 已经开着认证器时拒绝（先关闭再重新绑定，免得旧设备在用户不知情时失效）。模仿会话不能做。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="enrollments">绑定中的密钥。</param>
/// <param name="options">身份配置（签发方）。</param>
/// <param name="clock">时间源。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class SetupTotp(IRepository<SysUser, Guid> users, UserAccountService accounts, TotpEnrollments enrollments, IdentityOptions options, TimeProvider clock) : IOperation<ProfileRequest, TotpSetupResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "profile.setup-totp";

    /// <inheritdoc />
    public async ValueTask<Result<TotpSetupResponse>> HandleAsync(ProfileRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: true).ConfigureAwait(false);
        if (!subject.TryGetValue(out var found))
        {
            return subject.Error!;
        }

        if (found.Security.TwoFactorMethods.HasFlag(TwoFactorMethods.Totp))
        {
            return Error.Conflict(IdentityErrors.TotpAlreadyEnabled, IdentityMessages.TotpAlreadyEnabled);
        }

        var secret = Totp.GenerateSecret();
        await enrollments.StashAsync(found.User.Id, secret, context.CancellationToken).ConfigureAwait(false);
        var grouped = string.Join(' ', secret.Chunk(4).Select(static chunk => new string(chunk)));
        return new TotpSetupResponse(grouped, Totp.AuthenticatorUri(options.TotpIssuer, found.User.UserName, secret), clock.GetUtcNow() + TotpEnrollments.Lifetime);
    }
}
