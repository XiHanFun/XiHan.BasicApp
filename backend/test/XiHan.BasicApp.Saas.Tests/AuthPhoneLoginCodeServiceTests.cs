// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.Framework.Authentication.OneTimeCode;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 手机验证码登录发码/消费服务测试：跑真实的框架 <see cref="DistributedOneTimeCodeService"/>（内存分布式缓存），
/// 不 mock <see cref="IOneTimeCodeService"/>——要测的正是"签发一次、只能消费一次"这条契约本身是否成立。
/// </summary>
public sealed class AuthPhoneLoginCodeServiceTests
{
    private static AuthPhoneLoginCodeService CreateService()
    {
        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        return new AuthPhoneLoginCodeService(new DistributedOneTimeCodeService(cache));
    }

    /// <summary>
    /// 签发后立即消费一次应当成功；同一枚码第二次消费必须失败（消费即销毁）。
    /// </summary>
    [Fact]
    public async Task IssueThenConsume_ShouldSucceedOnceAndFailOnSecondAttempt()
    {
        var service = CreateService();

        var code = await service.IssueCodeAsync(tenantId: null, "+8613800138000");

        Assert.True(await service.TryConsumeAsync(tenantId: null, "+8613800138000", code));
        Assert.False(await service.TryConsumeAsync(tenantId: null, "+8613800138000", code));
    }

    /// <summary>
    /// 错误的验证码必须失败，且会烧掉这一枚在途码（同一枚正确码之后也不能再用）。
    /// </summary>
    [Fact]
    public async Task Consume_WithWrongCode_ShouldFailAndBurnTheIssuedCode()
    {
        var service = CreateService();

        var code = await service.IssueCodeAsync(tenantId: null, "+8613800138000");

        Assert.False(await service.TryConsumeAsync(tenantId: null, "+8613800138000", "000000"));
        // 错误尝试烧掉了这枚在途码：正确的码此后也不能再用
        Assert.False(await service.TryConsumeAsync(tenantId: null, "+8613800138000", code));
    }

    /// <summary>
    /// 为手机号 A 签发的验证码不能用于手机号 B（目标是签发/消费键的一部分）。
    /// </summary>
    [Fact]
    public async Task Consume_ForDifferentPhone_ShouldFail()
    {
        var service = CreateService();

        var code = await service.IssueCodeAsync(tenantId: null, "+8613800138000");

        Assert.False(await service.TryConsumeAsync(tenantId: null, "+8613900139000", code));
        // 手机号 A 自己的码仍然有效（没有被手机号 B 的失败尝试连累烧掉）
        Assert.True(await service.TryConsumeAsync(tenantId: null, "+8613800138000", code));
    }

    /// <summary>
    /// 不同租户上下文（平台态 null vs 具体租户）签发的验证码互不影响，即便手机号相同。
    /// </summary>
    [Fact]
    public async Task Consume_WithDifferentTenantId_ShouldFail()
    {
        var service = CreateService();

        var code = await service.IssueCodeAsync(tenantId: null, "+8613800138000");

        Assert.False(await service.TryConsumeAsync(tenantId: 1L, "+8613800138000", code));
    }

    /// <summary>
    /// 空手机号 / 空验证码：消费必须直接失败，不触发底层查询。
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TryConsumeAsync_WithBlankCode_ShouldReturnFalse(string? code)
    {
        var service = CreateService();
        await service.IssueCodeAsync(tenantId: null, "+8613800138000");

        Assert.False(await service.TryConsumeAsync(tenantId: null, "+8613800138000", code));
    }
}
