// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Jobs;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.BasicApp.Workflow.Jobs;
using XiHan.Framework.Primitives;
using XiHan.Framework.Scheduling.Abstractions.Jobs;
using XiHan.Framework.Scheduling.Abstractions.Schedulers;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证发送记录与工作流事件回执的保留清理
/// </summary>
/// <remarks>
/// 发件箱只删过了保留期且已结束的（含软删除的），排队中与发送中的再老也留着；Telegram 出站审计按时间删；保留天数写错即失败、什么都不删；
/// 工作流回执平台与租户两侧各删 90 天前的
/// </remarks>
public sealed class RetentionJobsTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static async Task<Result> RunAsync<TJob>(BasicAppTestServer server, string name)
        where TJob : IJob
        => await SettingsTestHost.InvokeAsync(server, null, async ctx => await ctx.Services.GetRequiredService<TJob>().RunAsync(JobRun.Create(new JobName(name), DateTimeOffset.UtcNow, JobTrigger.Manual), ctx));

    private static async Task InsertAsync<TEntity>(BasicAppTestServer server, params TEntity[] rows)
        where TEntity : class, new()
    {
        await using var scope = server.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ISqlSugarClient>().Insertable(rows.ToList()).ExecuteCommandAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<List<string>> RemainingAsync<TEntity>(BasicAppTestServer server, Func<TEntity, string> label)
        where TEntity : class, new()
    {
        await using var scope = server.Services.CreateAsyncScope();
        var rows = await scope.ServiceProvider.GetRequiredService<ISqlSugarClient>().Queryable<TEntity>().ClearFilter().ToListAsync(TestContext.Current.CancellationToken);
        return [.. rows.Select(label).Order(StringComparer.Ordinal)];
    }

    private static SysEmail Email(string to, MessageSendStatus status, DateTimeOffset createdAt, bool deleted = false)
        => new() { Id = Guid.NewGuid(), OwnerTenantId = "platform", ToEmail = to, Subject = "s", Status = status, IsDeleted = deleted, ConcurrencyStamp = Guid.NewGuid().ToString("N"), CreatedAt = createdAt };

    [Fact]
    public async Task MessageRecords_FinishedAndOld_AreRemoved_UnfinishedAndRecentStay()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var now = DateTimeOffset.UtcNow;
        var old = now.AddDays(-200);
        var recent = now.AddDays(-10);
        await InsertAsync(server,
            Email("old-sent", MessageSendStatus.Success, old),
            Email("old-failed-deleted", MessageSendStatus.Failed, old, deleted: true),
            Email("old-sending", MessageSendStatus.Sending, old),
            Email("recent-sent", MessageSendStatus.Success, recent));
        await InsertAsync(server,
            new SysSms { Id = Guid.NewGuid(), OwnerTenantId = "platform", ToPhone = "old-cancelled", Content = "c", Status = MessageSendStatus.Cancelled, ConcurrencyStamp = Guid.NewGuid().ToString("N"), CreatedAt = old },
            new SysSms { Id = Guid.NewGuid(), OwnerTenantId = "platform", ToPhone = "recent-failed", Content = "c", Status = MessageSendStatus.Failed, ConcurrencyStamp = Guid.NewGuid().ToString("N"), CreatedAt = recent });
        await InsertAsync(server, new SysBotMessage { Id = Guid.NewGuid(), OwnerTenantId = "platform", Status = MessageSendStatus.Failed, ConcurrencyStamp = Guid.NewGuid().ToString("N"), CreatedAt = old });
        await InsertAsync(server,
            new SysTelegramMessage { Id = Guid.NewGuid(), BotName = "old", ApiMethod = "sendMessage", MessageType = "text", SentAt = old, ConcurrencyStamp = Guid.NewGuid().ToString("N"), CreatedAt = old },
            new SysTelegramMessage { Id = Guid.NewGuid(), BotName = "recent", ApiMethod = "sendMessage", MessageType = "text", SentAt = recent, ConcurrencyStamp = Guid.NewGuid().ToString("N"), CreatedAt = recent });

        // 保留天数写错：作业失败，不按错误的值删
        var (set, _) = await SettingsTestHost.SetBuiltInConfigAsync(server, platform, MessageRetentionJob.RetentionDaysConfigKey, "0");
        Assert.Equal(HttpStatusCode.OK, set);
        Assert.Equal("NOTIFICATION_VALIDATION_017", (await RunAsync<MessageRetentionJob>(server, MessageRetentionJob.Name)).Error?.Code);
        Assert.Equal(4, (await RemainingAsync<SysEmail>(server, static e => e.ToEmail)).Count);

        // 缺省 180 天：过期且已结束的删掉（连同软删除的），发送中的与近期的留着
        await SettingsTestHost.SetBuiltInConfigAsync(server, platform, MessageRetentionJob.RetentionDaysConfigKey, null);
        Assert.True((await RunAsync<MessageRetentionJob>(server, MessageRetentionJob.Name)).IsSuccess);
        Assert.Equal(["old-sending", "recent-sent"], await RemainingAsync<SysEmail>(server, static e => e.ToEmail));
        Assert.Equal(["recent-failed"], await RemainingAsync<SysSms>(server, static s => s.ToPhone));
        Assert.Empty(await RemainingAsync<SysBotMessage>(server, static b => b.Id.ToString()));
        Assert.Equal(["recent"], await RemainingAsync<SysTelegramMessage>(server, static t => t.BotName));
    }

    [Fact]
    public async Task WorkflowReceipts_OlderThanTheRetention_AreRemovedOnEverySide()
    {
        var (server, _, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var now = DateTimeOffset.UtcNow;
        SysWorkflowMessageReceipt Receipt(string tenant, string message, DateTimeOffset createdAt)
            => new() { Id = Guid.NewGuid(), TenantId = tenant, MessageId = message, Topic = "workflow.task.created", ConcurrencyStamp = Guid.NewGuid().ToString("N"), CreatedAt = createdAt };
        await InsertAsync(server,
            Receipt("platform", "platform-old", now - ReceiptCleanupJob.Retention - TimeSpan.FromDays(1)),
            Receipt("platform", "platform-recent", now.AddDays(-1)),
            Receipt("contoso", "contoso-old", now - ReceiptCleanupJob.Retention - TimeSpan.FromDays(1)),
            Receipt("contoso", "contoso-recent", now.AddDays(-1)));

        Assert.True((await RunAsync<ReceiptCleanupJob>(server, ReceiptCleanupJob.Name)).IsSuccess);
        Assert.Equal(["contoso-recent", "platform-recent"], await RemainingAsync<SysWorkflowMessageReceipt>(server, static r => r.MessageId));
    }
}
