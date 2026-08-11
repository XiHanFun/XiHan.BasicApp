// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using SqlSugar;
using System.Linq.Expressions;
using System.Reflection;
using XiHan.BasicApp.Chat.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Chat.Tests;

/// <summary>
/// 聊天敏感词守卫测试，覆盖命中拦截、大小写不敏感匹配、空词库放行和空内容短路。
/// </summary>
public sealed class ChatSensitiveWordGuardTests
{
    /// <summary>
    /// 内容命中词库中任一敏感词必须拒绝发送。
    /// </summary>
    [Fact]
    public async Task EnsureAllowedAsync_HitWordShouldReject()
    {
        var guard = CreateGuard("赌博，诈骗；垃圾", out _);

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
        var guard = CreateGuard("SPAM", out _);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => guard.EnsureAllowedAsync("this is spam content"));
    }

    /// <summary>
    /// 词库为空时必须放行任意内容。
    /// </summary>
    [Fact]
    public async Task EnsureAllowedAsync_EmptyLexiconShouldPass()
    {
        var guard = CreateGuard(null, out _);

        await guard.EnsureAllowedAsync("任意内容都放行");
    }

    /// <summary>
    /// 未命中词库的内容必须放行。
    /// </summary>
    [Fact]
    public async Task EnsureAllowedAsync_CleanContentShouldPass()
    {
        var guard = CreateGuard("赌博", out _);

        await guard.EnsureAllowedAsync("正常聊天内容");
    }

    /// <summary>
    /// 空白内容必须直接放行且不查询词库。
    /// </summary>
    [Fact]
    public async Task EnsureAllowedAsync_BlankContentShouldPassWithoutQuery()
    {
        var guard = CreateGuard("赌博", out var resolver);

        await guard.EnsureAllowedAsync("   ");
        await guard.EnsureAllowedAsync(null);

        resolver.Verify(value => value.GetCurrentClient(), Times.Never);
    }

    /// <summary>
    /// 创建守卫实例并让配置查询返回指定词库原文。
    /// </summary>
    /// <param name="configValue">配置查询返回的词库原文；null 表示未配置。</param>
    /// <param name="resolver">客户端解析器替身。</param>
    /// <returns>敏感词守卫实例。</returns>
    private static ChatSensitiveWordGuard CreateGuard(string? configValue, out Mock<ISqlSugarClientResolver> resolver)
    {
        ResetCache();

        var valueQueryable = new Mock<ISugarQueryable<string>>();
        valueQueryable
            .Setup(value => value.FirstAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(configValue!);

        var configQueryable = new Mock<ISugarQueryable<SysConfig>>();
        configQueryable
            .Setup(value => value.Where(It.IsAny<Expression<Func<SysConfig, bool>>>()))
            .Returns(configQueryable.Object);
        configQueryable
            .Setup(value => value.Select(It.IsAny<Expression<Func<SysConfig, string>>>()))
            .Returns(valueQueryable.Object);

        var client = new Mock<ISqlSugarClient>();
        client
            .Setup(value => value.Queryable<SysConfig>())
            .Returns(configQueryable.Object);

        resolver = new Mock<ISqlSugarClientResolver>();
        resolver
            .Setup(value => value.GetCurrentClient())
            .Returns(client.Object);

        return new ChatSensitiveWordGuard(resolver.Object);
    }

    /// <summary>
    /// 清空守卫的进程内词库缓存，保证每个用例读取本用例的配置。
    /// </summary>
    private static void ResetCache()
    {
        var field = typeof(ChatSensitiveWordGuard).GetField(
            "_cache",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("未找到词库缓存字段。");
        field.SetValue(null, null);
    }
}
