// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authentication.Otp;
using XiHan.Framework.Authentication.Users;
using XiHan.Framework.Security.Password;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 吊销会话时对模仿会话的覆盖测试。
/// </summary>
/// <remarks>
/// 模仿会话行的 <c>UserId</c> 是被模仿者、<c>ImpersonatorUserId</c> 才是发起人，
/// 因此凡是"吊销某个用户的全部会话"的路径都必须从「自己的 + 由自己发起的」这一整组取
/// （<see cref="IUserSessionRepository.GetNotRevokedByUserIgnoreTenantAsync"/>），
/// 否则发起人被停用/注销后，他借来的身份仍然活着。
/// 这里断言的是领域服务对该组的取用与放行：整组按用户取、不再自己按租户过滤、只放过当前会话。
/// </remarks>
public sealed class SaasAppImpersonationRevokeTests
{
    private const long OperatorUserId = 1001;
    private const long TargetUserId = 2002;

    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUserSecurityRepository> _userSecurityRepository = new();
    private readonly Mock<IUserSessionRepository> _userSessionRepository = new();
    private readonly Mock<IExternalLoginRepository> _externalLoginRepository = new();
    private readonly Mock<ITenantUserRepository> _tenantUserRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IAuthenticationService> _authenticationService = new();
    private readonly Mock<IOtpService> _otpService = new();
    private readonly Mock<IPasswordHistoryDomainService> _passwordHistoryDomainService = new();

    /// <summary>
    /// 个人中心停用账号：既吊销自己的会话，也吊销由自己发起的模仿会话。
    /// </summary>
    [Fact]
    public async Task DeactivateAccountAsync_ShouldAlsoRevokeSessionsStartedByThisUser()
    {
        var revoked = await CaptureRevokedSessionsAsync(
            [OwnSession("own"), ImpersonationSession("impersonation")],
            service => service.DeactivateAccountAsync(new ProfilePasswordConfirmCommand(OperatorUserId, "pwd", OperatorUserId)));

        Assert.Equal(["impersonation", "own"], revoked);
    }

    /// <summary>
    /// 个人中心注销账号：同上。
    /// </summary>
    [Fact]
    public async Task DeleteAccountAsync_ShouldAlsoRevokeSessionsStartedByThisUser()
    {
        var revoked = await CaptureRevokedSessionsAsync(
            [OwnSession("own"), ImpersonationSession("impersonation")],
            service => service.DeleteAccountAsync(new ProfilePasswordConfirmCommand(OperatorUserId, "pwd", OperatorUserId)));

        Assert.Equal(["impersonation", "own"], revoked);
    }

    /// <summary>
    /// 会话组按发起人跨租户取，取用的是当前用户而不是别人的标识。
    /// </summary>
    [Fact]
    public async Task DeactivateAccountAsync_ShouldLoadSessionsOfCurrentUserAcrossTenants()
    {
        _ = await CaptureRevokedSessionsAsync(
            [],
            service => service.DeactivateAccountAsync(new ProfilePasswordConfirmCommand(OperatorUserId, "pwd", OperatorUserId)));

        _userSessionRepository.Verify(
            repository => repository.GetNotRevokedByUserIgnoreTenantAsync(OperatorUserId, It.IsAny<CancellationToken>()),
            Times.Once);
        _userSessionRepository.Verify(
            repository => repository.UpdateRangeAsync(It.IsAny<IEnumerable<SysUserSession>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 个人中心「登出其他设备」：同样要带上由自己发起的模仿会话，且放过当前这条。
    /// </summary>
    [Fact]
    public async Task RevokeOtherSessionsAsync_ShouldAlsoRevokeSessionsStartedByThisUser()
    {
        var revoked = await CaptureRevokedSessionsAsync(
            [OwnSession("other-session"), ImpersonationSession("impersonation-session"), OwnSession("current-session")],
            service => service.RevokeOtherSessionsAsync(
                new ProfileOtherSessionsRevokeCommand(OperatorUserId, "current-session", OperatorUserId)));

        Assert.Equal(["impersonation-session", "other-session"], revoked);
    }

    private static SysUserSession OwnSession(string sessionId)
    {
        return new SysUserSession { UserId = OperatorUserId, UserSessionId = sessionId, Status = SessionStatus.Active };
    }

    private static SysUserSession ImpersonationSession(string sessionId)
    {
        return new SysUserSession
        {
            UserId = TargetUserId,
            ImpersonatorUserId = OperatorUserId,
            UserSessionId = sessionId,
            Status = SessionStatus.Active
        };
    }

    /// <summary>
    /// 把会话组喂给仓储替身、驱动一次流程，取回被置为已吊销并写回的会话标识（按序）。
    /// </summary>
    private async Task<List<string>> CaptureRevokedSessionsAsync(
        IReadOnlyList<SysUserSession> candidates,
        Func<ProfileDomainService, Task> invoke)
    {
        var user = new SysUser { UserName = "operator", IsSystemAccount = false };
        SaasTestHelper.SetBasicId(user, OperatorUserId);
        var security = new SysUserSecurity { UserId = OperatorUserId, Password = "hashed" };

        _userRepository.Setup(repository => repository.GetByIdIgnoreTenantAsync(OperatorUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepository.Setup(repository => repository.UpdateAsync(It.IsAny<SysUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysUser entity, CancellationToken _) => entity);
        _userRepository.Setup(repository => repository.SoftDeleteAsync(It.IsAny<SysUser>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userSecurityRepository.Setup(repository => repository.GetByUserIdAsync(OperatorUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(security);
        _userSecurityRepository.Setup(repository => repository.UpdateAsync(It.IsAny<SysUserSecurity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysUserSecurity entity, CancellationToken _) => entity);
        _passwordHasher.Setup(hasher => hasher.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _tenantUserRepository
            .Setup(repository => repository.GetActiveByUserIdAsync(OperatorUserId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _userSessionRepository
            .Setup(repository => repository.GetNotRevokedByUserIgnoreTenantAsync(OperatorUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidates);

        List<SysUserSession>? written = null;
        _userSessionRepository
            .Setup(repository => repository.UpdateRangeAsync(It.IsAny<IEnumerable<SysUserSession>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<SysUserSession> sessions, CancellationToken _) =>
            {
                written = [.. sessions];
                return written;
            });

        await invoke(CreateService());

        return [.. (written ?? [])
            .Where(session => session.Status == SessionStatus.Revoked)
            .Select(session => session.UserSessionId)
            .Order(StringComparer.Ordinal)];
    }

    private ProfileDomainService CreateService()
    {
        return new ProfileDomainService(
            _userRepository.Object,
            _userSecurityRepository.Object,
            _userSessionRepository.Object,
            _externalLoginRepository.Object,
            _tenantUserRepository.Object,
            _passwordHasher.Object,
            _authenticationService.Object,
            _otpService.Object,
            _passwordHistoryDomainService.Object);
    }
}
