// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Security.Claims;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 个人中心应用服务（密码与双因素安全关注点）。
/// </summary>
public sealed partial class ProfileAppService
{
    /// <summary>
    /// 修改当前用户密码
    /// </summary>
    [UnitOfWork(true)]
    public async Task ChangePasswordAsync(ProfileChangePasswordDto input, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureNotImpersonating("修改密码");
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var currentUserId = GetCurrentUserIdOrThrow();
        var result = await _profileDomainService.ChangePasswordAsync(
            ProfileApplicationMapper.ToChangePasswordCommand(input, currentUserId),
            cancellationToken);

        // 改密后踢掉其它设备：当前会话保留（否则改完密码自己先掉线），
        // 其余会话与由本人发起的模仿会话一并吊销，旧令牌不再可用
        var revoked = await _profileDomainService.RevokeOtherSessionsAsync(
            ProfileApplicationMapper.ToOtherSessionsRevokeCommand(currentUserId, GetCurrentSessionId(), currentUserId),
            cancellationToken);
        await PublishSessionRevokedEventsAsync(revoked.DomainEvents, cancellationToken);

        // 认证审计：密码修改落登录日志
        await PublishSecurityAuditAsync(LoginResult.PasswordChanged, "用户修改密码");

        await _notificationDispatchService.DispatchToUserAsync(
            result.User.BasicId,
            "密码已修改",
            "您的账号密码已更新，如非本人操作，请立即联系管理员。",
            NotificationType.Security,
            "profile.password.changed",
            result.User.BasicId,
            cancellationToken: cancellationToken);

        // 强制改密锁的解锁方式就是改密：改密成功即解除会话锁定，前端随后收起强制改密引导
        await ReleasePasswordChangeLockIfNeededAsync(cancellationToken);
    }

    /// <summary>
    /// 当前会话若处于强制改密锁定（默认密码登录），修改密码成功后解除锁定并立即失效会话状态缓存。
    /// 其它原因的锁定（锁屏等）不受影响。
    /// </summary>
    private async Task ReleasePasswordChangeLockIfNeededAsync(CancellationToken cancellationToken)
    {
        var sessionBusinessId = _currentUser.FindClaim(XiHanClaimTypes.SessionId)?.Value;
        if (string.IsNullOrWhiteSpace(sessionBusinessId))
        {
            return;
        }

        var session = await _userSessionRepository.GetByUserSessionIdAsync(sessionBusinessId, cancellationToken);
        if (session is null || !session.IsLocked || session.LockReason != SessionLockReasons.PasswordChangeRequired)
        {
            return;
        }

        session.IsLocked = false;
        session.LockReason = null;
        session.LockPasswordHash = null;
        _ = await _userSessionRepository.UpdateAsync(session, cancellationToken);
        await _cacheInvalidator.InvalidateSessionStateAsync(session.UserSessionId, cancellationToken);
    }

    /// <summary>
    /// 禁用双因素认证
    /// </summary>
    [UnitOfWork(true)]
    public async Task Disable2FAAsync(ProfileTwoFactorVerifyDto input, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureNotImpersonating("关闭两步验证");
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var userId = GetCurrentUserIdOrThrow();
        var context = await _profileQueryService.GetSecurityContextAsync(userId, cancellationToken);
        var method = ToTwoFactorMethod(input.Method);
        if (!context.Security.TwoFactorMethod.HasFlag(method))
        {
            return;
        }

        await _profileVerificationService.EnsureTwoFactorCodeValidAsync(context, method, input.Code, cancellationToken);
        await _profileDomainService.DisableTwoFactorAsync(ProfileApplicationMapper.ToTwoFactorCommand(userId, method), cancellationToken);

        // 认证审计：解绑 MFA 落登录日志
        await PublishSecurityAuditAsync(LoginResult.MfaUnbound, $"解绑两步验证（{method}）");
    }

    /// <summary>
    /// 启用双因素认证
    /// </summary>
    [UnitOfWork(true)]
    public async Task Enable2FAAsync(ProfileTwoFactorVerifyDto input, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureNotImpersonating("开启两步验证");
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var userId = GetCurrentUserIdOrThrow();
        var context = await _profileQueryService.GetSecurityContextAsync(userId, cancellationToken);
        var method = ToTwoFactorMethod(input.Method);
        await _profileVerificationService.EnsureTwoFactorCodeValidAsync(context, method, input.Code, cancellationToken);
        await _profileDomainService.EnableTwoFactorAsync(ProfileApplicationMapper.ToTwoFactorCommand(userId, method), cancellationToken);

        // 认证审计：绑定 MFA 落登录日志
        await PublishSecurityAuditAsync(LoginResult.MfaBound, $"绑定两步验证（{method}）");
    }

    /// <summary>
    /// 发送双因素设置验证码
    /// </summary>
    public async Task<ProfileVerificationCodeResultDto> Send2FASetupCodeAsync(ProfileTwoFactorMethodDto input, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureNotImpersonating("发送两步验证设置验证码");
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var context = await _profileQueryService.GetSecurityContextAsync(GetCurrentUserIdOrThrow(), cancellationToken);
        var method = ToTwoFactorMethod(input.Method);
        return method switch
        {
            TwoFactorMethod.Email => await _profileVerificationService.SendCodeAsync(context.User, ProfileVerificationPurpose.TwoFactorEmail, context.User.Email, "邮箱两步验证", cancellationToken),
            TwoFactorMethod.Phone => await _profileVerificationService.SendCodeAsync(context.User, ProfileVerificationPurpose.TwoFactorPhone, context.User.Phone, "手机两步验证", cancellationToken),
            _ => throw new InvalidOperationException("该双因素方式不需要发送验证码。")
        };
    }

    /// <summary>
    /// 初始化 TOTP 双因素认证
    /// </summary>
    [UnitOfWork(true)]
    public async Task<ProfileTwoFactorSetupDto> Setup2FAAsync(CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureNotImpersonating("设置两步验证");
        cancellationToken.ThrowIfCancellationRequested();

        var currentUserId = GetCurrentUserIdOrThrow();
        var result = await _profileDomainService.SetupTwoFactorAsync(
            ProfileApplicationMapper.ToTwoFactorSetupCommand(currentUserId, "XiHan BasicApp"),
            cancellationToken);

        return ProfileApplicationMapper.ToTwoFactorSetupDto(result);
    }
}
