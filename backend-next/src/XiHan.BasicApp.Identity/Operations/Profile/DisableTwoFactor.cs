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
/// 关闭一种双因素方式。
/// </summary>
/// <remarks>
/// <c>POST /api/profile:disable-two-factor</c>。认证器要用当前认证器的码确认（防重放），通过即清掉密钥；
/// 邮箱与短信用 <c>profile.send-two-factor-code</c> 发到本人已验证联系方式的码确认；
/// 方式本就没开时原样返回、不要验证码。最后一种方式关掉即整体关闭双因素。登录日志记为 <see cref="LoginEvent.TwoFactorDisable"/>。模仿会话不能做。
/// </remarks>
/// <param name="users">用户仓储。</param>
/// <param name="securities">账号安全仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="codes">邮件与短信验证码。</param>
/// <param name="protector">密钥解密。</param>
/// <param name="clock">时间源。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class DisableTwoFactor(
    IRepository<SysUser, Guid> users,
    IRepository<SysUserSecurity, Guid> securities,
    UserAccountService accounts,
    VerificationCodes codes,
    ISecretProtector protector,
    TimeProvider clock) : IOperation<TwoFactorChangeRequest, ProfileResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "profile.disable-two-factor";

    /// <inheritdoc />
    public async ValueTask<Result<ProfileResponse>> HandleAsync(TwoFactorChangeRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: true).ConfigureAwait(false);
        if (!subject.TryGetValue(out var found))
        {
            return subject.Error!;
        }

        var (user, security) = found;
        var flag = TwoFactorSettings.FlagOf(request.Method);
        if (!security.TwoFactorMethods.HasFlag(flag))
        {
            return ProfileResponse.From(user, security);
        }

        context.AddAuditLog(new LoginLogEntry(LoginEvent.TwoFactorDisable) { Detail = request.Method.ToString() });
        if (TwoFactorSettings.ChannelOf(request.Method) is { } channel)
        {
            var confirmed = await TwoFactorSetup.ConfirmAsync(codes, user, channel, request.Code, context.CancellationToken).ConfigureAwait(false);
            if (confirmed.IsFailure)
            {
                return confirmed.Error!;
            }
        }
        else
        {
            var step = security.TwoFactorSecret is { } stored && protector.TryUnprotect(VerifyTwoFactor.SecretPurpose, stored, out var secret)
                ? Totp.Verify(secret, request.Code, clock.GetUtcNow(), security.TwoFactorLastStep)
                : null;
            if (step is null)
            {
                return Error.Validation(IdentityErrors.TwoFactorCodeRejected, IdentityMessages.TwoFactorCodeInvalid);
            }

            security.TwoFactorSecret = null;
            security.TwoFactorLastStep = null;
        }

        security.TwoFactorMethods &= ~flag;
        security.TwoFactorEnabled = security.TwoFactorMethods != TwoFactorMethods.None;
        security.SecurityStamp = UserAccountService.NewStamp();
        var updated = await securities.UpdateAsync(security, context.CancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : ProfileResponse.From(user, security);
    }
}
