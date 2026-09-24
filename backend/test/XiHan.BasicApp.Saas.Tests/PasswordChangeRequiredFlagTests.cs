// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using System.Linq.Expressions;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authentication.Otp;
using XiHan.Framework.Authentication.Users;
using XiHan.Framework.Security.Password;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 「需要本人改密」标记的写入口径：密码由他人设置时置位，本人设置时清除。
/// </summary>
/// <remarks>
/// 标记只在参数「密码设置」的 forceChange 开启时让登录锁定到改密为止；这里只钉住标记本身。
/// 本人改密后标记必须清除，否则开启强制改密时用户每次登录都会被锁住。
/// </remarks>
public sealed class PasswordChangeRequiredFlagTests
{
    private const long UserId = 101;

    /// <summary>
    /// 管理员创建账号：初始密码由管理员设置，置位。
    /// </summary>
    [Fact]
    public async Task CreateUser_ShouldRequireChange()
    {
        var fixture = new UserAccountBoundaryTests.Fixture(tenantId: null);
        SysUserSecurity? added = null;
        _ = fixture.Securities
            .Setup(repo => repo.AddAsync(It.IsAny<SysUserSecurity>(), It.IsAny<CancellationToken>()))
            .Callback<SysUserSecurity, CancellationToken>((entity, _) => added = entity)
            .ReturnsAsync((SysUserSecurity entity, CancellationToken _) => entity);

        _ = await fixture.Service.CreateUserAsync(UserAccountBoundaryTests.CreateCommand());

        Assert.NotNull(added);
        Assert.True(added!.PasswordChangeRequired);
    }

    /// <summary>
    /// 重置密码：管理员重置置位，本人找回清除。
    /// </summary>
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task ResetPassword_ShouldFlagOnlyWhenSetByOthers(bool bySelf, bool expected)
    {
        var fixture = new UserAccountBoundaryTests.Fixture(tenantId: null);
        fixture.GivenHomeAccount();
        var security = new SysUserSecurity { UserId = UserId, PasswordChangeRequired = !expected };
        _ = fixture.Securities
            .Setup(repo => repo.GetFirstAsync(It.IsAny<Expression<Func<SysUserSecurity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(security);
        _ = fixture.Securities
            .Setup(repo => repo.UpdateAsync(It.IsAny<SysUserSecurity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysUserSecurity entity, CancellationToken _) => entity);

        _ = await fixture.Service.ResetUserPasswordAsync(new UserPasswordResetCommand(UserId, "Reset@Probe123", null, null, bySelf));

        Assert.Equal(expected, security.PasswordChangeRequired);
    }

    /// <summary>
    /// 本人修改密码：清除标记。
    /// </summary>
    [Fact]
    public async Task ChangeOwnPassword_ShouldClearFlag()
    {
        var user = new SysUser { UserName = "member" };
        SaasTestHelper.SetBasicId(user, UserId);
        var security = new SysUserSecurity { UserId = UserId, Password = "hashed", PasswordChangeRequired = true };

        var users = new Mock<IUserRepository>();
        _ = users.Setup(repo => repo.GetByIdIgnoreTenantAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var securities = new Mock<IUserSecurityRepository>();
        _ = securities.Setup(repo => repo.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(security);
        _ = securities
            .Setup(repo => repo.UpdateAsync(It.IsAny<SysUserSecurity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysUserSecurity entity, CancellationToken _) => entity);
        var hasher = new Mock<IPasswordHasher>();
        _ = hasher.Setup(value => value.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        var authentication = new Mock<IAuthenticationService>();
        _ = authentication
            .Setup(service => service.ValidatePasswordStrengthAsync(It.IsAny<string>(), It.IsAny<List<string>?>()))
            .ReturnsAsync(new PasswordValidationResult { IsValid = true });

        var service = new ProfileDomainService(
            users.Object,
            securities.Object,
            Mock.Of<IUserSessionRepository>(),
            Mock.Of<IExternalLoginRepository>(),
            Mock.Of<ITenantUserRepository>(),
            hasher.Object,
            authentication.Object,
            Mock.Of<IOtpService>(),
            Mock.Of<IPasswordHistoryDomainService>());

        _ = await service.ChangePasswordAsync(new ProfileChangePasswordCommand(UserId, "Old@Probe123", "New@Probe123"));

        Assert.False(security.PasswordChangeRequired);
    }
}
