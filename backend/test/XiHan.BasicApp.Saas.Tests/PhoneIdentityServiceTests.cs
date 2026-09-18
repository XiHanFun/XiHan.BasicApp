// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 手机号码写入口径单元测试。
/// </summary>
/// <remarks>
/// 手机号码是登录身份标识：写入时不正规化，同一个号码会以多种写法并存，唯一性检查形同虚设；
/// 不查重则两个账号共用一个号码，登录无法判定是谁。这两件事必须在同一个入口上保证。
/// </remarks>
public sealed class PhoneIdentityServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly PhoneIdentityService _service;

    public PhoneIdentityServiceTests()
    {
        _service = new PhoneIdentityService(_userRepository.Object, new PhoneNumberNormalizer());
    }

    [Fact]
    public async Task ResolveForWriteAsync_ShouldReturnNullForBlank()
    {
        Assert.Null(await _service.ResolveForWriteAsync("   ", excludeUserId: null));
        _userRepository.Verify(
            repo => repo.ExistsPhoneGloballyAsync(It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResolveForWriteAsync_ShouldNormalizeToE164BeforeUniquenessCheck()
    {
        _userRepository
            .Setup(repo => repo.ExistsPhoneGloballyAsync("+8613800138000", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.ResolveForWriteAsync("+86 138 0013 8000", excludeUserId: null);

        Assert.Equal("+8613800138000", result);
        _userRepository.Verify(
            repo => repo.ExistsPhoneGloballyAsync("+8613800138000", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResolveForWriteAsync_ShouldThrowForInvalidFormat()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ResolveForWriteAsync("0912345", excludeUserId: null));

        Assert.Equal("手机号码格式无效。", exception.Message);
    }

    [Fact]
    public async Task ResolveForWriteAsync_ShouldThrowWhenPhoneTakenByAnotherUser()
    {
        _userRepository
            .Setup(repo => repo.ExistsPhoneGloballyAsync("+886912345678", 7L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ResolveForWriteAsync("+886912345678", excludeUserId: 7L));

        Assert.Equal("手机号码已被其他账号使用。", exception.Message);
    }

    [Fact]
    public async Task ResolveForWriteAsync_ShouldPassExcludeUserIdThrough()
    {
        _userRepository
            .Setup(repo => repo.ExistsPhoneGloballyAsync("+886912345678", 42L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.ResolveForWriteAsync("+886912345678", excludeUserId: 42L);

        Assert.Equal("+886912345678", result);
    }
}
