// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Chat.Application.Services;
using XiHan.BasicApp.Chat.Domain.Configurations;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Application.Services;

namespace XiHan.BasicApp.Chat.Tests;

/// <summary>
/// 聊天敏感词守卫测试，覆盖命中拦截、大小写不敏感匹配、空词库放行、空内容短路和词库写错时拒绝。
/// </summary>
public sealed class ChatSensitiveWordGuardTests
{
    /// <summary>
    /// 内容命中词库中任一敏感词必须拒绝发送。
    /// </summary>
    [Fact]
    public async Task EnsureAllowedAsync_HitWordShouldReject()
    {
        var guard = CreateGuard("""{"sensitiveWords":["赌博","诈骗","垃圾"]}""", out _);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => guard.EnsureAllowedAsync("这条消息涉及诈骗内容"));

        Assert.Contains("敏感词", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 敏感词匹配必须忽略大小写。
    /// </summary>
    [Fact]
    public async Task EnsureAllowedAsync_HitShouldIgnoreCase()
    {
        var guard = CreateGuard("""{"sensitiveWords":["SPAM"]}""", out _);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => guard.EnsureAllowedAsync("this is spam content"));
    }

    /// <summary>
    /// 未配置聊天策略、或词库为空、或词条全是空白时必须放行任意内容。
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("""{"sensitiveWords":[]}""")]
    [InlineData("""{"sensitiveWords":[""," "]}""")]
    [InlineData("""{"retentionDays":30}""")]
    public async Task EnsureAllowedAsync_EmptyLexiconShouldPass(string? policy)
    {
        var guard = CreateGuard(policy, out _);

        await guard.EnsureAllowedAsync("任意内容都放行");
    }

    /// <summary>
    /// 未命中词库的内容必须放行。
    /// </summary>
    [Fact]
    public async Task EnsureAllowedAsync_CleanContentShouldPass()
    {
        var guard = CreateGuard("""{"sensitiveWords":["赌博"]}""", out _);

        await guard.EnsureAllowedAsync("正常聊天内容");
    }

    /// <summary>
    /// 空白内容必须直接放行且不读取聊天策略。
    /// </summary>
    [Fact]
    public async Task EnsureAllowedAsync_BlankContentShouldPassWithoutQuery()
    {
        var guard = CreateGuard("""{"sensitiveWords":["赌博"]}""", out var query);

        await guard.EnsureAllowedAsync("   ");
        await guard.EnsureAllowedAsync(null);

        query.Verify(value => value.GetValueItemAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 聊天策略写错（不是合法 JSON、词库不是字符串数组）时拒绝发送，不当作空词库放行。
    /// </summary>
    [Theory]
    [InlineData("赌博，诈骗")]
    [InlineData("""{"sensitiveWords":"赌博"}""")]
    public async Task EnsureAllowedAsync_MalformedPolicyShouldReject(string policy)
    {
        var guard = CreateGuard(policy, out _);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => guard.EnsureAllowedAsync("正常聊天内容"));

        Assert.Contains(ChatConfigKeys.Policy, exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 创建守卫实例，让配置值查询返回指定的聊天策略原文。
    /// </summary>
    /// <param name="policy">聊天策略原文；null 表示未配置。</param>
    /// <param name="query">配置值查询替身。</param>
    /// <returns>敏感词守卫实例。</returns>
    private static ChatSensitiveWordGuard CreateGuard(string? policy, out Mock<ISaasConfigValueQueryService> query)
    {
        query = new Mock<ISaasConfigValueQueryService>();
        query
            .Setup(value => value.GetValueItemAsync(ChatConfigKeys.Policy, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SaasConfigValueCacheItem { ConfigKey = ChatConfigKeys.Policy, Value = policy, Exists = policy is not null });

        return new ChatSensitiveWordGuard(new SaasConfigurationService(query.Object));
    }
}
