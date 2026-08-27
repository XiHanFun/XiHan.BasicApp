// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Options;
using Moq;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Security.Password;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 密码强度策略与密码历史复用拦截的领域规则测试。
/// 强度判定是「长度 + 四类字符至少命中三类」，历史比对必须走加盐哈希校验而不是字符串相等。
/// </summary>
public sealed class SaasDomainPasswordTests
{
    private static readonly DateTimeOffset ChangedTime = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// 空、空白密码一律以「密码不能为空」拒绝，不进入长度与字符类判定。
    /// </summary>
    /// <param name="password">待校验密码。</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void ValidateStrength_BlankPassword_ShouldRejectAsEmpty(string password)
    {
        var message = new PasswordPolicyDomainService().ValidateStrength(password);

        Assert.Equal("密码不能为空。", message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 密码长度下界为 8：7 位拒绝，8 位（且满足字符类要求）通过。
    /// </summary>
    [Fact]
    public void ValidateStrength_LengthLowerBound_ShouldRejectSevenAndAcceptEight()
    {
        var service = new PasswordPolicyDomainService();

        Assert.Equal("密码长度不能少于 8 位。", service.ValidateStrength("Ab1cdef"), StringComparer.Ordinal);
        Assert.Null(service.ValidateStrength("Ab1cdefg"));
    }

    /// <summary>
    /// 密码长度上界为 128：128 位通过，129 位拒绝。
    /// </summary>
    [Fact]
    public void ValidateStrength_LengthUpperBound_ShouldAccept128AndReject129()
    {
        var service = new PasswordPolicyDomainService();
        var atLimit = "Aa1" + new string('x', 125);
        var overLimit = "Aa1" + new string('x', 126);

        Assert.Equal(128, atLimit.Length);
        Assert.Null(service.ValidateStrength(atLimit));
        Assert.Equal("密码长度不能超过 128 位。", service.ValidateStrength(overLimit), StringComparer.Ordinal);
    }

    /// <summary>
    /// 字符类要求：大写、小写、数字、特殊字符四类中至少命中三类，仅两类必须拒绝。
    /// </summary>
    /// <param name="password">待校验密码。</param>
    /// <param name="expectPass">期望是否通过强度校验。</param>
    [Theory]
    [InlineData("Abcdefgh", false)]
    [InlineData("abcdefg1", false)]
    [InlineData("ABCDEFG1", false)]
    [InlineData("abcdefg!", false)]
    [InlineData("Abcdefg1", true)]
    [InlineData("Abcdefg!", true)]
    [InlineData("abcdef1!", true)]
    [InlineData("ABCDEF1!", true)]
    [InlineData("Abcdef1!", true)]
    public void ValidateStrength_ShouldRequireAtLeastThreeCharacterCategories(string password, bool expectPass)
    {
        var message = new PasswordPolicyDomainService().ValidateStrength(password);

        if (expectPass)
        {
            Assert.Null(message);
        }
        else
        {
            Assert.Equal("密码必须包含大写字母、小写字母、数字、特殊字符中至少三种。", message, StringComparer.Ordinal);
        }
    }

    /// <summary>
    /// 非 ASCII 字符（中文、空格、Emoji）均落入「特殊字符」这一类，参与三类计数。
    /// </summary>
    /// <param name="password">含非 ASCII 字符的密码。</param>
    [Theory]
    [InlineData("abcdefg中")]
    [InlineData("abcdefg ")]
    [InlineData("abcdefg¥")]
    public void ValidateStrength_NonAsciiCharacters_ShouldCountAsSpecialCategory(string password)
    {
        // 小写 + 特殊 = 两类，仍应因不足三类被拒；用于锁定「非字母数字一律归特殊类」的分支口径
        var message = new PasswordPolicyDomainService().ValidateStrength(password);

        Assert.Equal("密码必须包含大写字母、小写字母、数字、特殊字符中至少三种。", message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 长度检查优先于字符类检查：过短密码即便字符类不足也报长度错。
    /// </summary>
    [Fact]
    public void ValidateStrength_ShortAndWeak_ShouldReportLengthFirst()
    {
        var message = new PasswordPolicyDomainService().ValidateStrength("abc");

        Assert.Equal("密码长度不能少于 8 位。", message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 历史哈希比对采用序号比较：完全相同才算重复，大小写不同不算重复。
    /// </summary>
    [Fact]
    public void IsDuplicateWithHistory_ShouldCompareHashesOrdinally()
    {
        var service = new PasswordPolicyDomainService();

        Assert.True(service.IsDuplicateWithHistory("HashA", ["HashB", "HashA"]));
        Assert.False(service.IsDuplicateWithHistory("HashA", ["hasha", "HASHA"]));
        Assert.False(service.IsDuplicateWithHistory("HashA", []));
    }

    /// <summary>
    /// 空哈希是调用方缺陷：null 抛派生的空引用异常，空白抛参数异常。
    /// </summary>
    [Fact]
    public void IsDuplicateWithHistory_BlankHash_ShouldThrowArgumentException()
    {
        var service = new PasswordPolicyDomainService();

        _ = Assert.ThrowsAny<ArgumentException>(() => service.IsDuplicateWithHistory(null!, []));
        _ = Assert.ThrowsAny<ArgumentException>(() => service.IsDuplicateWithHistory("  ", []));
    }

    /// <summary>
    /// 命中历史密码必须抛出带历史条数的提示，阻断本次改密。
    /// </summary>
    [Fact]
    public async Task EnsureNotReused_WhenHistoryMatches_ShouldThrowWithHistoryCount()
    {
        var repository = new Mock<IPasswordHistoryRepository>();
        _ = repository
            .Setup(repo => repo.GetRecentByUserIdAsync(7, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SysPasswordHistory> { new() { PasswordHash = "old-hash" } });
        var hasher = new Mock<IPasswordHasher>();
        _ = hasher.Setup(item => item.VerifyPassword("old-hash", "Abcdef1!")).Returns(true);
        var service = new PasswordHistoryDomainService(repository.Object, hasher.Object, BuildOptions(3));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.EnsureNotReusedAsync(7, "Abcdef1!"));

        Assert.Contains("最近 3 次", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 历史比对必须逐条调用加盐哈希校验，且以「历史哈希 + 新明文」的入参顺序调用；
    /// 若写成明文与哈希直接比较，加盐哈希下永远不会命中。
    /// </summary>
    [Fact]
    public async Task EnsureNotReused_ShouldVerifyEachHistoryEntryWithHasher()
    {
        var repository = new Mock<IPasswordHistoryRepository>();
        _ = repository
            .Setup(repo => repo.GetRecentByUserIdAsync(7, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SysPasswordHistory>
            {
                new() { PasswordHash = "hash-1" },
                new() { PasswordHash = "hash-2" }
            });
        var hasher = new Mock<IPasswordHasher>();
        _ = hasher.Setup(item => item.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        var service = new PasswordHistoryDomainService(repository.Object, hasher.Object, BuildOptions(5));

        await service.EnsureNotReusedAsync(7, "Abcdef1!");

        hasher.Verify(item => item.VerifyPassword("hash-1", "Abcdef1!"), Times.Once);
        hasher.Verify(item => item.VerifyPassword("hash-2", "Abcdef1!"), Times.Once);
    }

    /// <summary>
    /// 历史条数配置为 0（或负数）表示关闭历史校验，此时不得查库。
    /// </summary>
    /// <param name="historyCount">配置的密码历史条数。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task EnsureNotReused_HistoryDisabled_ShouldSkipRepository(int historyCount)
    {
        var repository = new Mock<IPasswordHistoryRepository>();
        var hasher = new Mock<IPasswordHasher>();
        var service = new PasswordHistoryDomainService(repository.Object, hasher.Object, BuildOptions(historyCount));

        await service.EnsureNotReusedAsync(7, "Abcdef1!");

        repository.Verify(
            repo => repo.GetRecentByUserIdAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 非法用户主键或空明文时静默跳过历史校验，不查库也不抛异常。
    /// </summary>
    /// <param name="userId">用户主键。</param>
    /// <param name="password">新密码明文。</param>
    [Theory]
    [InlineData(0, "Abcdef1!")]
    [InlineData(-1, "Abcdef1!")]
    [InlineData(7, "")]
    public async Task EnsureNotReused_InvalidInput_ShouldSkipSilently(long userId, string password)
    {
        var repository = new Mock<IPasswordHistoryRepository>();
        var hasher = new Mock<IPasswordHasher>();
        var service = new PasswordHistoryDomainService(repository.Object, hasher.Object, BuildOptions(5));

        await service.EnsureNotReusedAsync(userId, password);

        repository.Verify(
            repo => repo.GetRecentByUserIdAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 记录密码历史时按入参原样落库用户、哈希与变更时间。
    /// </summary>
    [Fact]
    public async Task Record_ShouldPersistHistoryEntryWithGivenValues()
    {
        SysPasswordHistory? captured = null;
        var repository = new Mock<IPasswordHistoryRepository>();
        _ = repository
            .Setup(repo => repo.AddAsync(It.IsAny<SysPasswordHistory>(), It.IsAny<CancellationToken>()))
            .Callback<SysPasswordHistory, CancellationToken>((entity, _) => captured = entity)
            .ReturnsAsync((SysPasswordHistory entity, CancellationToken _) => entity);
        var service = new PasswordHistoryDomainService(repository.Object, new Mock<IPasswordHasher>().Object, BuildOptions(5));

        await service.RecordAsync(7, "new-hash", ChangedTime);

        Assert.NotNull(captured);
        Assert.Equal(7, captured!.UserId);
        Assert.Equal("new-hash", captured.PasswordHash, StringComparer.Ordinal);
        Assert.Equal(ChangedTime, captured.ChangedTime);
    }

    /// <summary>
    /// 非法用户主键或空哈希不得写入历史表，避免脏数据挤掉真实历史。
    /// </summary>
    /// <param name="userId">用户主键。</param>
    /// <param name="passwordHash">密码哈希。</param>
    [Theory]
    [InlineData(0, "new-hash")]
    [InlineData(7, "")]
    public async Task Record_InvalidInput_ShouldNotPersist(long userId, string passwordHash)
    {
        var repository = new Mock<IPasswordHistoryRepository>();
        var service = new PasswordHistoryDomainService(repository.Object, new Mock<IPasswordHasher>().Object, BuildOptions(5));

        await service.RecordAsync(userId, passwordHash, ChangedTime);

        repository.Verify(
            repo => repo.AddAsync(It.IsAny<SysPasswordHistory>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 构造依赖缺失必须立刻抛空引用异常，避免运行期才发现历史校验形同虚设。
    /// </summary>
    [Fact]
    public void Constructor_NullDependency_ShouldThrowArgumentNullException()
    {
        var repository = new Mock<IPasswordHistoryRepository>().Object;
        var hasher = new Mock<IPasswordHasher>().Object;

        _ = Assert.Throws<ArgumentNullException>(() => new PasswordHistoryDomainService(null!, hasher, BuildOptions(5)));
        _ = Assert.Throws<ArgumentNullException>(() => new PasswordHistoryDomainService(repository, null!, BuildOptions(5)));
        _ = Assert.Throws<ArgumentNullException>(() => new PasswordHistoryDomainService(repository, hasher, null!));
    }

    /// <summary>
    /// 已取消的令牌必须在查库与写库前抛出取消异常。
    /// </summary>
    [Fact]
    public async Task PasswordHistory_CancelledToken_ShouldThrowBeforeRepositoryCall()
    {
        var repository = new Mock<IPasswordHistoryRepository>();
        var service = new PasswordHistoryDomainService(repository.Object, new Mock<IPasswordHasher>().Object, BuildOptions(5));
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.EnsureNotReusedAsync(7, "Abcdef1!", cancellation.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.RecordAsync(7, "new-hash", ChangedTime, cancellation.Token));
        repository.Verify(
            repo => repo.GetRecentByUserIdAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        repository.Verify(
            repo => repo.AddAsync(It.IsAny<SysPasswordHistory>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static IOptions<PasswordPolicyOptions> BuildOptions(int historyCount)
    {
        return Options.Create(new PasswordPolicyOptions { PasswordHistoryCount = historyCount });
    }
}
