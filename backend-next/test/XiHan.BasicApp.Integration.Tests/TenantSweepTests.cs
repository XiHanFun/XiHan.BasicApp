// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Auditing.Domain.Entities;
using XiHan.BasicApp.Auditing.Domain.Services;
using XiHan.BasicApp.Auditing.Jobs;
using XiHan.BasicApp.AI;
using XiHan.BasicApp.Chat.Domain.Entities;
using XiHan.BasicApp.Chat.Jobs;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Contracts.Files;
using XiHan.BasicApp.Files.Domain.Entities;
using XiHan.BasicApp.Files.Jobs;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Runtime;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;
using XiHan.Framework.Scheduling.Abstractions.Jobs;
using XiHan.Framework.Scheduling.Abstractions.Schedulers;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证后台遍历各侧的作业与查询在独立库上的行为
/// </summary>
/// <remarks>
/// 清理作业逐个侧绑定分片，清的是租户自己的库；配了连接还没初始化的独立库不在遍历之列，既不算失败也不挡删除提供商
/// 已就绪的库读写失败时作业报失败（其余侧照常处理），删除提供商也失败，不当作没有引用
/// </remarks>
public sealed class TenantSweepTests : IDisposable
{
    private const string Dedicated = "iso";
    private const string Uninitialized = "pending";

    private readonly SqliteDatabase _platform = new();
    private readonly SqliteDatabase _dedicated = new();
    private readonly SqliteDatabase _uninitialized = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "xihan-sweep-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        _platform.Dispose();
        _dedicated.Dispose();
        _uninitialized.Dispose();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, string? body, string token)
    {
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(method, path, body, token));
        return (response.StatusCode, json);
    }

    private static async Task ProvisionAsync(BasicAppTestServer server, string superadmin, string tenantId, string connectionString, string provider, bool initialize)
    {
        var enterprise = await EditionsTests.EditionIdAsync(server, superadmin, "enterprise");
        var (created, body) = await SendAsync(server, HttpMethod.Post, "/api/tenants", $$"""{"tenantId":"{{tenantId}}","name":"{{tenantId}}","editionId":"{{enterprise}}"}""", superadmin);
        Assert.Equal(HttpStatusCode.Created, created);
        var id = body.GetProperty("id").GetString()!;
        var (configured, _) = await SendAsync(server, HttpMethod.Post, $"/api/tenants/{id}:configure-database", JsonSerializer.Serialize(new { isolationMode = "Database", connectionString, databaseProvider = provider }), superadmin);
        Assert.Equal(HttpStatusCode.OK, configured);
        if (initialize)
        {
            var (initialized, initializedBody) = await SendAsync(server, HttpMethod.Post, $"/api/tenants/{id}:initialize-database", "{}", superadmin);
            Assert.True(initialized == HttpStatusCode.OK, initializedBody.ToString());
            var (admin, _) = await SendAsync(server, HttpMethod.Post, $"/api/tenants/{id}:initialize-admin", """{"userName":"admin","password":"Admin@123456"}""", superadmin);
            Assert.Equal(HttpStatusCode.OK, admin);
        }
    }

    // 在一侧写一条老消息（只为保留清理，不走发送流程）
    private static Task<Guid> SeedOldMessageAsync(BasicAppTestServer server, string? tenantId)
        => SettingsTestHost.InvokeAsync(server, tenantId, async ctx =>
        {
            var unitOfWork = ctx.Services.GetRequiredService<SqlSugarUnitOfWork>();
            await unitOfWork.EnsureTransactionAsync(ctx.CancellationToken);
            var id = Guid.CreateVersion7();
            var row = new SysChatMessage { Id = id, TenantId = tenantId ?? TenantIds.Platform, ConversationId = Guid.CreateVersion7(), Seq = 1, Type = ChatMessageType.Text, Content = "old", CreatedAt = DateTimeOffset.UtcNow.AddDays(-400) };
            await unitOfWork.ConnectionFor(typeof(SysChatMessage)).Insertable(row).ExecuteCommandAsync(ctx.CancellationToken);
            return id;
        });

    private static Task<int> CountAsync<T>(BasicAppTestServer server, string? tenantId)
        where T : class, new()
        => SettingsTestHost.InvokeAsync(server, tenantId, async ctx =>
            await ctx.Services.GetRequiredService<SqlSugarUnitOfWork>().ConnectionFor(typeof(T)).Queryable<T>().CountAsync(ctx.CancellationToken));

    private static Task<Guid> SeedExpiredUploadAsync(BasicAppTestServer server, string tenantId)
        => SettingsTestHost.InvokeAsync(server, tenantId, async ctx =>
        {
            var unitOfWork = ctx.Services.GetRequiredService<SqlSugarUnitOfWork>();
            await unitOfWork.EnsureTransactionAsync(ctx.CancellationToken);
            var id = Guid.CreateVersion7();
            var row = new SysFileUpload
            {
                Id = id,
                TenantId = tenantId,
                ConcurrencyStamp = Guid.NewGuid().ToString("N"),
                UserId = Guid.CreateVersion7(),
                FileId = Guid.CreateVersion7(),
                StorageConfigId = Guid.CreateVersion7(),
                Bucket = "b",
                Key = "k",
                UploadId = "u",
                OriginalName = "a.txt",
                Size = 1,
                PartSize = 1,
                PartCount = 1,
                Status = FileUploadStatus.Uploading,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1),
            };
            await unitOfWork.ConnectionFor(typeof(SysFileUpload)).Insertable(row).ExecuteCommandAsync(ctx.CancellationToken);
            return id;
        });

    private static Task<FileUploadStatus> UploadStatusAsync(BasicAppTestServer server, string tenantId, Guid id)
        => SettingsTestHost.InvokeAsync(server, tenantId, async ctx =>
            (await ctx.Services.GetRequiredService<SqlSugarUnitOfWork>().ConnectionFor(typeof(SysFileUpload)).Queryable<SysFileUpload>().FirstAsync(u => u.Id == id, ctx.CancellationToken)).Status);

    private static Task<Guid> SeedExpiredTemporaryFileAsync(BasicAppTestServer server, string tenantId)
        => SettingsTestHost.InvokeAsync(server, tenantId, async ctx =>
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("temporary"));
            var saved = await ctx.Services.GetRequiredService<ITemporaryFiles>().SaveAsync(ctx, "test", stream, "temporary.txt", "text/plain", TimeSpan.FromMinutes(-1));
            return saved.ValueOrThrow().Id;
        });

    private static Task<int> DropTableAsync(BasicAppTestServer server, string tenantId, string table)
        => SettingsTestHost.InvokeAsync(server, tenantId, async ctx =>
        {
            var unitOfWork = ctx.Services.GetRequiredService<SqlSugarUnitOfWork>();
            await unitOfWork.EnsureTransactionAsync(ctx.CancellationToken);
            return await unitOfWork.ConnectionFor(typeof(SysChatMessage)).Ado.ExecuteCommandAsync($"DROP TABLE {table}");
        });

    private static Task<Result> RunAsync<TJob>(BasicAppTestServer server, string name)
        where TJob : IJob
        => SettingsTestHost.InvokeAsync(server, null, async ctx => await ctx.Services.GetRequiredService<TJob>().RunAsync(JobRun.Create(name, DateTimeOffset.UtcNow, JobTrigger.Manual), ctx, ctx.CancellationToken));

    private async Task SweepScenarioAsync(string platform, string dedicated, string uninitialized, string provider)
    {
        await using var server = await BasicAppTestServer.StartAsync(platform, provider, configure: builder => builder.Configuration.AddInMemoryCollection([new("BasicApp:Files:LocalRootPath", _root)]));
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var superadmin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        await ProvisionAsync(server, superadmin, Dedicated, dedicated, provider, initialize: true);
        await ProvisionAsync(server, superadmin, Uninitialized, uninitialized, provider, initialize: false);
        var admin = (await AuthFlowTests.LoginAsync(server, "admin", "Admin@123456", Dedicated)).GetProperty("accessToken").GetString()!;

        // 聊天保留清理：独立库里的老消息被删，没初始化的租户不算失败
        await SeedOldMessageAsync(server, Dedicated);
        Assert.Equal(1, await CountAsync<SysChatMessage>(server, Dedicated));
        var chat = await RunAsync<ChatRetentionJob>(server, ChatRetentionJob.Name);
        Assert.True(chat.IsSuccess, chat.Error?.Code);
        Assert.Equal(0, await CountAsync<SysChatMessage>(server, Dedicated));

        // 日志保留清理
        var writer = server.Services.GetRequiredService<AuditLogWriter>();
        Assert.True(writer.Enqueue(new SysLoginLog { Id = Guid.CreateVersion7(), TenantId = Dedicated, OccurredAt = DateTime.UtcNow.AddDays(-400), TraceId = "old", Event = LoginEvent.Login, UserName = "ghost" }));
        await writer.FlushAsync(CancellationToken.None);
        var audit = await RunAsync<AuditRetentionJob>(server, AuditRetentionJob.Name);
        Assert.True(audit.IsSuccess, audit.Error?.Code);
        var logs = await SettingsTestHost.InvokeAsync(server, Dedicated, async ctx => await ctx.Services.GetRequiredService<SqlSugarUnitOfWork>().ConnectionFor(typeof(SysLoginLog)).Queryable<SysLoginLog>().SplitTable().Where(l => l.UserName == "ghost").CountAsync(ctx.CancellationToken));
        Assert.Equal(0, logs);

        // 分片上传过期与到期临时文件：处理的是独立库里的行
        var upload = await SeedExpiredUploadAsync(server, Dedicated);
        var expiry = await RunAsync<FileUploadExpiryJob>(server, FileUploadExpiryJob.Name);
        Assert.True(expiry.IsSuccess, expiry.Error?.Code);
        Assert.Equal(FileUploadStatus.Expired, await UploadStatusAsync(server, Dedicated, upload));
        var temporary = await SeedExpiredTemporaryFileAsync(server, Dedicated);
        var purge = await RunAsync<TemporaryFilePurgeJob>(server, TemporaryFilePurgeJob.Name);
        Assert.True(purge.IsSuccess, purge.Error?.Code);
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(server, HttpMethod.Get, $"/api/files/{temporary}", null, admin)).Status);

        // 没初始化的租户不挡删除提供商
        var (created, createdBody) = await SendAsync(server, HttpMethod.Post, "/api/ai-providers", JsonSerializer.Serialize(new { code = "spare", name = "Spare", chatModel = "m", vendor = "OpenAI", endpoint = "https://example.invalid/v1" }), superadmin);
        Assert.True(created == HttpStatusCode.Created, createdBody.ToString());
        var spare = createdBody.GetProperty("id").GetString()!;
        var (removed, removedBody) = await SendAsync(server, HttpMethod.Delete, $"/api/ai-providers?ids={spare}", null, superadmin);
        Assert.True(removed == HttpStatusCode.OK, removedBody.ToString());
        Assert.Equal(spare, Assert.Single(removedBody.GetProperty("succeeded").EnumerateArray()).GetString());

        // 已就绪的库读写失败：作业报失败，其余侧照常；删除提供商也失败，不当作没有引用
        await DropTableAsync(server, Dedicated, "sys_chat_message_reaction");
        await SeedOldMessageAsync(server, null);
        var failed = await RunAsync<ChatRetentionJob>(server, ChatRetentionJob.Name);
        Assert.Equal(RuntimeErrors.SweepIncomplete, failed.Error?.Code);
        Assert.Equal(0, await CountAsync<SysChatMessage>(server, null));

        var (another, anotherBody) = await SendAsync(server, HttpMethod.Post, "/api/ai-providers", JsonSerializer.Serialize(new { code = "other", name = "Other", chatModel = "m", vendor = "OpenAI", endpoint = "https://example.invalid/v1" }), superadmin);
        Assert.Equal(HttpStatusCode.Created, another);
        await DropTableAsync(server, Dedicated, "sys_knowledge_document");
        var (blocked, blockedBody) = await SendAsync(server, HttpMethod.Delete, $"/api/ai-providers?ids={anotherBody.GetProperty("id").GetString()}", null, superadmin);
        Assert.Equal((HttpStatusCode.ServiceUnavailable, AIErrors.ProviderReferencesUnavailable), (blocked, blockedBody.GetProperty("code").GetString()));
    }

    [Fact]
    public Task Sweeps_ReachDedicatedDatabases_AndSkipUninitializedOnes()
        => SweepScenarioAsync(_platform.ConnectionString, _dedicated.ConnectionString, _uninitialized.ConnectionString, "sqlite");

    [Fact]
    public async Task Sweeps_ReachDedicatedDatabases_OnPostgres()
    {
        var host = PostgresDatabase.RequireServer();
        await using var platform = await PostgresDatabase.CreateAsync(host, "xihan_sweep");
        await using var dedicated = await PostgresDatabase.CreateAsync(host, "xihan_sweep_iso");
        // 没初始化的库不建：连接串指向的库还不存在
        await SweepScenarioAsync(platform.ConnectionString, dedicated.ConnectionString, $"{host.TrimEnd(';')};Database=xihan_sweep_absent_{Guid.NewGuid():N}", "postgresql");
    }
}
