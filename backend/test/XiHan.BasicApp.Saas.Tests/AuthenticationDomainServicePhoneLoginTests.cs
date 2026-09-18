// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authentication.Users;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// <see cref="AuthenticationDomainService.AuthenticatePhoneLoginAsync"/> 测试：手机验证码登录的用户定位与
/// 应用级账号可用性校验（验证码本身由应用层单独校验，不在本类职责内）。
/// </summary>
public sealed class AuthenticationDomainServicePhoneLoginTests
{
    private readonly Mock<IAuthenticationService> _authenticationService = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUserSecurityRepository> _userSecurityRepository = new();
    private readonly Mock<ITenantUserRepository> _tenantUserRepository = new();
    private readonly AuthenticationDomainService _service;

    public AuthenticationDomainServicePhoneLoginTests()
    {
        _service = new AuthenticationDomainService(
            _authenticationService.Object,
            _userRepository.Object,
            _userSecurityRepository.Object,
            _tenantUserRepository.Object);
    }

    /// <summary>
    /// 手机号未注册（仓储查不到用户）：必须以 InvalidCredentials 失败，不泄露"号码是否存在"之外的信息。
    /// </summary>
    [Fact]
    public async Task AuthenticatePhoneLoginAsync_WithUnregisteredPhone_ShouldFailWithInvalidCredentials()
    {
        _userRepository
            .Setup(repo => repo.GetByPhoneAsync("+8613800138000", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysUser?)null);

        var result = await _service.AuthenticatePhoneLoginAsync("+8613800138000", tenantId: null, DateTimeOffset.UtcNow);

        Assert.False(result.Succeeded);
        Assert.Equal(LoginResult.InvalidCredentials, result.FailureResult);
        _userSecurityRepository.Verify(
            repo => repo.GetByUserIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 手机号已注册但账号被禁用：必须以 AccountDisabled 失败（应用级可用性校验挡下，而非当作号码未注册）。
    /// </summary>
    [Fact]
    public async Task AuthenticatePhoneLoginAsync_WithDisabledUser_ShouldFailWithAccessFailure()
    {
        var user = CreateUser(basicId: 7L, status: EnableStatus.Disabled);
        _userRepository
            .Setup(repo => repo.GetByPhoneAsync("+8613800138000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userSecurityRepository
            .Setup(repo => repo.GetByUserIdAsync(7L, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysUserSecurity?)null);

        var result = await _service.AuthenticatePhoneLoginAsync("+8613800138000", tenantId: null, DateTimeOffset.UtcNow);

        Assert.False(result.Succeeded);
        Assert.Equal(LoginResult.AccountDisabled, result.FailureResult);
    }

    /// <summary>
    /// 手机号已注册且账号启用：认证通过，返回定位到的用户。
    /// </summary>
    [Fact]
    public async Task AuthenticatePhoneLoginAsync_WithEnabledUser_ShouldSucceed()
    {
        var user = CreateUser(basicId: 9L, status: EnableStatus.Enabled);
        _userRepository
            .Setup(repo => repo.GetByPhoneAsync("+8613800138000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userSecurityRepository
            .Setup(repo => repo.GetByUserIdAsync(9L, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysUserSecurity?)null);

        var result = await _service.AuthenticatePhoneLoginAsync("+8613800138000", tenantId: null, DateTimeOffset.UtcNow);

        Assert.True(result.Succeeded);
        Assert.Same(user, result.User);
    }

    /// <summary>
    /// 空白手机号必须直接抛参数异常，不打一次无意义的仓储查询。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AuthenticatePhoneLoginAsync_WithBlankPhone_ShouldThrow(string phone)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => _service.AuthenticatePhoneLoginAsync(phone, tenantId: null, DateTimeOffset.UtcNow));

        _userRepository.Verify(
            repo => repo.GetByPhoneAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static SysUser CreateUser(long basicId, EnableStatus status)
    {
        var user = new SysUser
        {
            UserName = $"user-{basicId}",
            Status = status,
            IsActive = true,
        };
        SaasTestHelper.SetBasicId(user, basicId);
        return user;
    }
}
