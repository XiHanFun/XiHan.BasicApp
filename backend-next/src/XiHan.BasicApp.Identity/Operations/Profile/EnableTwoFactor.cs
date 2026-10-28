// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.BasicApp.Identity.Operations.Auth;
using XiHan.BasicApp.Runtime.Security;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Profile;

/// <summary>
/// 开启一种双因素方式：<c>POST /api/profile:enable-two-factor</c>。目前只有认证器——用 <c>profile.setup-totp</c> 暂存的密钥校验认证器算出的码，
/// 通过才把密钥加密落库、开启双因素（这一步就算用过一次，同一步的码不能再用来登录）。登录日志记为 <see cref="LoginEvent.TwoFactorEnable"/>。模仿会话不能做。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="securities">账号安全仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="enrollments">绑定中的密钥。</param>
/// <param name="protector">密钥加密。</param>
/// <param name="clock">时间源。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class EnableTwoFactor(
    IRepository<SysUser, Guid> users,
    IRepository<SysUserSecurity, Guid> securities,
    UserAccountService accounts,
    TotpEnrollments enrollments,
    ISecretProtector protector,
    TimeProvider clock) : IOperation<TwoFactorChangeRequest, ProfileResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "profile.enable-two-factor";

    /// <inheritdoc />
    public async ValueTask<Result<ProfileResponse>> HandleAsync(TwoFactorChangeRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var log = new LoginLogEntry(LoginEvent.TwoFactorEnable) { Detail = request.Method.ToString() };
        context.AddAuditLog(log);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: true).ConfigureAwait(false);
        if (!subject.TryGetValue(out var found))
        {
            return subject.Error!;
        }

        var (user, security) = found;
        if (request.Method != TwoFactorMethod.Totp)
        {
            return Error.Validation(IdentityErrors.TwoFactorMethodUnavailable, IdentityMessages.TwoFactorMethodUnavailable, MessageArguments.Of(("method", request.Method.ToString())));
        }

        if (security.TwoFactorMethods.HasFlag(TwoFactorMethods.Totp))
        {
            return Error.Conflict(IdentityErrors.TotpAlreadyEnabled, IdentityMessages.TotpAlreadyEnabled);
        }

        if (!(await enrollments.FindAsync(user.Id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var secret))
        {
            return Error.Validation(IdentityErrors.TotpSetupMissing, IdentityMessages.TotpSetupMissing);
        }

        if (Totp.Verify(secret, request.Code, clock.GetUtcNow(), lastStep: null) is not { } step)
        {
            return Error.Validation(IdentityErrors.TwoFactorCodeRejected, IdentityMessages.TwoFactorCodeInvalid);
        }

        security.TwoFactorSecret = protector.Protect(VerifyTwoFactor.SecretPurpose, secret);
        security.TwoFactorMethods |= TwoFactorMethods.Totp;
        security.TwoFactorEnabled = true;
        security.TwoFactorLastStep = step;
        security.SecurityStamp = UserAccountService.NewStamp();
        var updated = await securities.UpdateAsync(security, context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        await enrollments.DiscardAsync(user.Id, context.CancellationToken).ConfigureAwait(false);
        return ProfileResponse.From(user, security);
    }
}
