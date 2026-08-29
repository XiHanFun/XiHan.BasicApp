// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
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
/// 因此凡是"吊销某个用户的全部会话"的路径都必须同时按这两列取，
/// 否则发起人被停用/注销后，他借来的身份仍然活着。
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
        var predicate = await CaptureRevokePredicateAsync(
            service => service.DeactivateAccountAsync(new ProfilePasswordConfirmCommand(OperatorUserId, "pwd", OperatorUserId)));

        Assert.True(predicate(OwnSession()), "自己的会话必须被吊销。");
        Assert.True(predicate(ImpersonationSession()), "由自己发起的模仿会话必须被吊销。");
    }

    /// <summary>
    /// 个人中心注销账号：同上。
    /// </summary>
    [Fact]
    public async Task DeleteAccountAsync_ShouldAlsoRevokeSessionsStartedByThisUser()
    {
        var predicate = await CaptureRevokePredicateAsync(
            service => service.DeleteAccountAsync(new ProfilePasswordConfirmCommand(OperatorUserId, "pwd", OperatorUserId)));

        Assert.True(predicate(OwnSession()), "自己的会话必须被吊销。");
        Assert.True(predicate(ImpersonationSession()), "由自己发起的模仿会话必须被吊销。");
    }

    /// <summary>
    /// 别人的会话与别人发起的模仿会话都不在吊销范围内。
    /// </summary>
    [Fact]
    public async Task DeactivateAccountAsync_ShouldNotRevokeUnrelatedSessions()
    {
        var predicate = await CaptureRevokePredicateAsync(
            service => service.DeactivateAccountAsync(new ProfilePasswordConfirmCommand(OperatorUserId, "pwd", OperatorUserId)));

        Assert.False(predicate(new SysUserSession { UserId = TargetUserId, Status = SessionStatus.Active }));
        Assert.False(predicate(new SysUserSession
        {
            UserId = TargetUserId,
            ImpersonatorUserId = TargetUserId,
            Status = SessionStatus.Active
        }));
    }

    /// <summary>
    /// 已吊销的会话不重复处理。
    /// </summary>
    [Fact]
    public async Task DeactivateAccountAsync_ShouldSkipAlreadyRevokedSessions()
    {
        var predicate = await CaptureRevokePredicateAsync(
            service => service.DeactivateAccountAsync(new ProfilePasswordConfirmCommand(OperatorUserId, "pwd", OperatorUserId)));

        Assert.False(predicate(new SysUserSession { UserId = OperatorUserId, Status = SessionStatus.Revoked }));
    }

    /// <summary>
    /// 个人中心「登出其他设备」：同样要带上由自己发起的模仿会话，且放过当前这条。
    /// </summary>
    [Fact]
    public async Task RevokeOtherSessionsAsync_ShouldAlsoRevokeSessionsStartedByThisUser()
    {
        var predicate = await CaptureRevokePredicateAsync(
            service => service.RevokeOtherSessionsAsync(
                new ProfileOtherSessionsRevokeCommand(OperatorUserId, "current-session", OperatorUserId)));

        Assert.True(predicate(new SysUserSession
        {
            UserId = OperatorUserId,
            UserSessionId = "other-session",
            Status = SessionStatus.Active
        }));
        Assert.True(predicate(new SysUserSession
        {
            UserId = TargetUserId,
            ImpersonatorUserId = OperatorUserId,
            UserSessionId = "impersonation-session",
            Status = SessionStatus.Active
        }));
        Assert.False(predicate(new SysUserSession
        {
            UserId = OperatorUserId,
            UserSessionId = "current-session",
            Status = SessionStatus.Active
        }));
    }

    private static SysUserSession OwnSession()
    {
        return new SysUserSession { UserId = OperatorUserId, Status = SessionStatus.Active };
    }

    private static SysUserSession ImpersonationSession()
    {
        return new SysUserSession
        {
            UserId = TargetUserId,
            ImpersonatorUserId = OperatorUserId,
            Status = SessionStatus.Active
        };
    }

    /// <summary>
    /// 驱动一次关闭账号流程，取回它下发给会话仓储的查询条件并编译成可执行谓词。
    /// </summary>
    private async Task<Func<SysUserSession, bool>> CaptureRevokePredicateAsync(
        Func<ProfileDomainService, Task> invoke)
    {
        var user = new SysUser { UserName = "operator", IsSystemAccount = false };
        SaasTestHelper.SetBasicId(user, OperatorUserId);
        var security = new SysUserSecurity { UserId = OperatorUserId, Password = "hashed" };

        _userRepository.Setup(repository => repository.GetByIdAsync(OperatorUserId, It.IsAny<CancellationToken>()))
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

        Expression<Func<SysUserSession, bool>>? captured = null;
        _userSessionRepository
            .Setup(repository => repository.GetListAsync(It.IsAny<Expression<Func<SysUserSession, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<SysUserSession, bool>> expression, CancellationToken _) =>
            {
                captured = expression;
                return [];
            });

        await invoke(CreateService());

        Assert.NotNull(captured);
        return captured.Compile();
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
