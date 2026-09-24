// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using Moq;
using SqlSugar;
using System.Linq.Expressions;
using XiHan.BasicApp.Chat.Domain.Entities;
using XiHan.BasicApp.Chat.Infrastructure.Tasks;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Infrastructure.MultiTenancy;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Chat.Tests;

/// <summary>
/// 聊天保留期清理任务测试：清理范围必须覆盖消息与其名下的表情回应，且逐数据作用域执行。
/// </summary>
/// <remarks>
/// 这个任务是「防止表无限增长」的唯一手段，而它自己漏掉一张表时不会报任何错：
/// 消息行被物理删除后，回应行的 MessageId 变成悬空外键并永久留存，
/// 回应表照样单调增长，只有等到磁盘告警才会被发现。故对清理范围逐张表钉死。
/// </remarks>
public sealed class ChatExtraRetentionCleanupTaskTests
{
    /// <summary>
    /// 回归锚点（缺陷清单条目 21）：清理必须级联删除过期消息名下的表情回应，且删回应在删消息之前。
    /// </summary>
    /// <remarks>
    /// 修复前 ExecuteAsync 只删 SysChatMessage，本用例在「回应删除被执行」一步即变红。
    /// 顺序同样是断言的一部分：消息行一旦先被删掉，就再也无法按「所属消息已过期」筛出回应。
    /// </remarks>
    [Fact]
    public async Task ExecuteAsync_ShouldCascadeDeleteReactionsBeforeMessages()
    {
        var context = new CleanupContext(messageRows: 7, reactionRows: 3);

        _ = await context.Task.ExecuteAsync();

        Assert.Equal(
            new[] { nameof(SysChatMessageReaction), nameof(SysChatMessage) },
            context.DeletedInOrder.ToArray(),
            StringComparer.Ordinal);
        context.ReactionDeleteable.Verify(value => value.ExecuteCommandAsync(), Times.Once);
        context.MessageDeleteable.Verify(value => value.ExecuteCommandAsync(), Times.Once);
    }

    /// <summary>
    /// 两张表的删除行数都必须进入任务摘要：摘要是调度日志里唯一能看到清理效果的地方。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_SummaryShouldReportBothDeletedCounts()
    {
        var context = new CleanupContext(messageRows: 7, reactionRows: 3);

        var summary = await context.Task.ExecuteAsync();

        Assert.Contains("消息 7 行", summary, StringComparison.Ordinal);
        Assert.Contains("表情回应 3 行", summary, StringComparison.Ordinal);
        // 未配置保留期，取默认保留天数
        Assert.Contains("保留 365 天", summary, StringComparison.Ordinal);
    }

    /// <summary>
    /// 聊天实体严格隔离：每个数据作用域各清一遍，删除行数合计进摘要；保留期配置在平台作用域读取。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldCleanupEveryDataScope()
    {
        var context = new CleanupContext(messageRows: 7, reactionRows: 3, scopeTenantIds: [null, 5, 9]);

        var summary = await context.Task.ExecuteAsync();

        Assert.Equal(
            Enumerable.Repeat(new[] { nameof(SysChatMessageReaction), nameof(SysChatMessage) }, 3).SelectMany(item => item).ToArray(),
            context.DeletedInOrder.ToArray(),
            StringComparer.Ordinal);
        Assert.Contains("消息 21 行", summary, StringComparison.Ordinal);
        Assert.Contains("表情回应 9 行", summary, StringComparison.Ordinal);
        context.CurrentTenant.Verify(value => value.Change(null, null), Times.Once);
    }

    /// <summary>
    /// 保留期配置非法时直接失败，不按默认值继续删数据
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithInvalidRetentionConfig_ShouldThrowWithoutDeleting()
    {
        var context = new CleanupContext(messageRows: 7, reactionRows: 3, retentionConfigValue: "abc");

        _ = await Assert.ThrowsAsync<InvalidOperationException>(() => context.Task.ExecuteAsync());

        Assert.Empty(context.DeletedInOrder);
    }

    /// <summary>
    /// 清理任务及其协作者替身：两张表的删除链路各自记录执行次序与返回行数。
    /// </summary>
    private sealed class CleanupContext
    {
        /// <summary>
        /// 组装清理任务与替身。
        /// </summary>
        /// <param name="messageRows">每个作用域消息删除链路返回的行数。</param>
        /// <param name="reactionRows">每个作用域回应删除链路返回的行数。</param>
        /// <param name="scopeTenantIds">逐作用域执行器依次切入的作用域（缺省只有平台）。</param>
        /// <param name="retentionConfigValue">保留期配置值（缺省未配置）。</param>
        public CleanupContext(int messageRows, int reactionRows, long?[]? scopeTenantIds = null, string? retentionConfigValue = null)
        {
            MessageDeleteable = CreateDeleteable<SysChatMessage>(messageRows, nameof(SysChatMessage));
            ReactionDeleteable = CreateDeleteable<SysChatMessageReaction>(reactionRows, nameof(SysChatMessageReaction));

            var configValue = new Mock<ISugarQueryable<string>>();
            configValue.Setup(value => value.FirstAsync()).ReturnsAsync(retentionConfigValue!);
            var configQuery = new Mock<ISugarQueryable<SysConfig>>();
            configQuery
                .Setup(value => value.Where(It.IsAny<Expression<Func<SysConfig, bool>>>()))
                .Returns(() => configQuery.Object);
            configQuery
                .Setup(value => value.Select(It.IsAny<Expression<Func<SysConfig, string>>>()))
                .Returns(configValue.Object);

            var client = new Mock<ISqlSugarClient>();
            client.Setup(value => value.Queryable<SysConfig>()).Returns(configQuery.Object);
            client.Setup(value => value.Deleteable<SysChatMessage>()).Returns(MessageDeleteable.Object);
            client.Setup(value => value.Deleteable<SysChatMessageReaction>()).Returns(ReactionDeleteable.Object);

            var resolver = new Mock<ISqlSugarClientResolver>();
            resolver.Setup(value => value.GetCurrentClient()).Returns(client.Object);

            CurrentTenant = new Mock<ICurrentTenant>();

            var scopes = scopeTenantIds ?? [null];
            var scopeRunner = new Mock<ITenantDataScopeRunner>();
            scopeRunner
                .Setup(value => value.RunAsync(It.IsAny<Func<long?, Task>>(), It.IsAny<CancellationToken>()))
                .Returns(async (Func<long?, Task> action, CancellationToken _) =>
                {
                    foreach (var scope in scopes)
                    {
                        await action(scope);
                    }
                });

            Task = new ChatRetentionCleanupTask(
                resolver.Object,
                scopeRunner.Object,
                CurrentTenant.Object,
                new Mock<ILogger<ChatRetentionCleanupTask>>().Object);
        }

        /// <summary>被测清理任务。</summary>
        public ChatRetentionCleanupTask Task { get; }

        /// <summary>消息删除链路替身。</summary>
        public Mock<IDeleteable<SysChatMessage>> MessageDeleteable { get; }

        /// <summary>回应删除链路替身。</summary>
        public Mock<IDeleteable<SysChatMessageReaction>> ReactionDeleteable { get; }

        /// <summary>当前租户上下文替身。</summary>
        public Mock<ICurrentTenant> CurrentTenant { get; }

        /// <summary>按执行先后记录的被清理实体名。</summary>
        public List<string> DeletedInOrder { get; } = [];

        /// <summary>
        /// 构造一个删除链路替身：Where 返回自身以支持链式调用，执行时记录次序并返回预设行数。
        /// </summary>
        /// <typeparam name="TEntity">被删除的实体类型。</typeparam>
        /// <param name="rows">执行返回的行数。</param>
        /// <param name="entityName">记录用的实体名。</param>
        /// <returns>删除链路替身。</returns>
        private Mock<IDeleteable<TEntity>> CreateDeleteable<TEntity>(int rows, string entityName)
            where TEntity : class, new()
        {
            var deleteable = new Mock<IDeleteable<TEntity>>();
            deleteable
                .Setup(value => value.Where(It.IsAny<Expression<Func<TEntity, bool>>>()))
                .Returns(() => deleteable.Object);
            deleteable
                .Setup(value => value.ExecuteCommandAsync())
                .ReturnsAsync(() =>
                {
                    DeletedInOrder.Add(entityName);
                    return rows;
                });
            return deleteable;
        }
    }
}
