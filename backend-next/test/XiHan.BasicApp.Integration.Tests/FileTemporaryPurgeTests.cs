// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Files;
using XiHan.BasicApp.Files.Domain.Entities;
using XiHan.BasicApp.Files.Domain.Services;
using XiHan.BasicApp.Files.Jobs;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Primitives;
using XiHan.Framework.Scheduling.Abstractions.Jobs;
using XiHan.Framework.Scheduling.Abstractions.Schedulers;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证到期临时文件的清理作业
/// </summary>
/// <remarks>
/// 平台与租户两侧过了到期时间的临时文件（产物包与导出结果都这样存）连同不再被引用的对象一起删，跨批处理；
/// 没到期的、没标为临时的（即使写了到期时间）不动；与正常文件共用对象的临时文件删掉后对象留给正常文件
/// </remarks>
public sealed class FileTemporaryPurgeTests : IDisposable
{
    private readonly SqliteDatabase _database = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "xihan-files-purge-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        _database.Dispose();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private Task<(BasicAppTestServer Server, string Platform, string Tenant)> StartAsync()
        => SettingsTestHost.StartAsync(_database.ConnectionString, configure: builder => builder.Configuration.AddInMemoryCollection(
        [
            new("BasicApp:Files:LocalRootPath", _root),
        ]));

    // 在一侧存一个临时文件：寿命为负即已到期
    private static Task<Guid> SaveTemporaryAsync(BasicAppTestServer server, string? tenantId, string content, TimeSpan lifetime)
        => SettingsTestHost.InvokeAsync(server, tenantId, async ctx =>
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var saved = await ctx.Services.GetRequiredService<ITemporaryFiles>().SaveAsync(ctx, "test", stream, "temporary.txt", "text/plain", lifetime);
            return saved.ValueOrThrow().Id;
        });

    // 在平台一侧存一个不是临时的文件，可以带到期时间
    private static Task<Guid> SaveNormalAsync(BasicAppTestServer server, string content, DateTimeOffset? expiresAt = null)
        => SettingsTestHost.InvokeAsync(server, null, async ctx =>
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var details = new FileDetails(FileAccessLevel.Authorized, IsTemporary: false, expiresAt, Tags: null, Remark: null);
            var saved = await ctx.Services.GetRequiredService<FileService>().UploadAsync(ctx, stream, "normal.txt", "text/plain", details);
            return saved.ValueOrThrow().Id;
        });

    private static Task<Result> PurgeAsync(BasicAppTestServer server)
        => SettingsTestHost.InvokeAsync(server, null, async ctx =>
        {
            var job = ctx.Services.GetRequiredService<TemporaryFilePurgeJob>();
            return await job.RunAsync(JobRun.Create(TemporaryFilePurgeJob.Name, DateTimeOffset.UtcNow, JobTrigger.Manual), ctx, ctx.CancellationToken);
        });

    private static async Task<HttpStatusCode> StatusOfAsync(BasicAppTestServer server, string token, Guid id)
        => (await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/files/{id}", null, token))).Response.StatusCode;

    // 存储里的对象（本地存储的 data 目录下）
    private int Objects() => Directory.Exists(_root)
        ? Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories).Count(static f => f.Contains(Path.DirectorySeparatorChar + "data" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        : 0;

    [Fact]
    public async Task ExpiredTemporaryFiles_AreDeleted_WithTheirObjects_AcrossBatchesAndSides()
    {
        var (server, platform, tenant) = await StartAsync();
        await using var running = server;
        var expired = TimeSpan.FromMinutes(-1);

        // 正常文件与一个内容相同的到期临时文件共用一个对象
        var kept = await SaveNormalAsync(server, "shared content");
        var sharing = await SaveTemporaryAsync(server, null, "shared content", expired);
        var valid = await SaveTemporaryAsync(server, null, "still valid", TimeSpan.FromHours(1));
        var dated = await SaveNormalAsync(server, "not temporary", DateTimeOffset.UtcNow.AddDays(-1));
        // 多于一批的到期临时文件（内容相同，共用一个对象）
        var bulk = new List<Guid>();
        for (var i = 0; i <= FileService.PurgeBatchSize; i++)
        {
            bulk.Add(await SaveTemporaryAsync(server, null, "bulk content", expired));
        }

        var tenantExpired = await SaveTemporaryAsync(server, "contoso", "tenant content", expired);
        Assert.Equal(5, Objects());

        var ran = await PurgeAsync(server);
        Assert.True(ran.IsSuccess, ran.Error?.Code);

        // 到期的临时文件都删了（含租户一侧），对象删到只剩还被引用的
        Assert.Equal(HttpStatusCode.NotFound, await StatusOfAsync(server, platform, sharing));
        Assert.Equal(HttpStatusCode.NotFound, await StatusOfAsync(server, platform, bulk[0]));
        Assert.Equal(HttpStatusCode.NotFound, await StatusOfAsync(server, platform, bulk[^1]));
        Assert.Equal(HttpStatusCode.NotFound, await StatusOfAsync(server, tenant, tenantExpired));
        Assert.Equal(3, Objects());

        // 没到期的、不是临时的（即使过了到期时间）、与删掉的临时文件共用对象的正常文件都还在，内容读得出
        Assert.Equal(HttpStatusCode.OK, await StatusOfAsync(server, platform, valid));
        Assert.Equal(HttpStatusCode.OK, await StatusOfAsync(server, platform, dated));
        Assert.Equal(HttpStatusCode.OK, await StatusOfAsync(server, platform, kept));
        var read = await SettingsTestHost.InvokeAsync(server, null, async ctx =>
        {
            var file = (await ctx.Services.GetRequiredService<FileService>().FindAsync("platform", kept, ctx.CancellationToken)).ValueOrThrow();
            await using var content = (await ctx.Services.GetRequiredService<FileService>().OpenAsync(file, range: null, ctx.CancellationToken)).ValueOrThrow().Content;
            using var reader = new StreamReader(content);
            return await reader.ReadToEndAsync(ctx.CancellationToken);
        });
        Assert.Equal("shared content", read);

        // 再跑一次没有可删的
        Assert.True((await PurgeAsync(server)).IsSuccess);
        Assert.Equal(3, Objects());
    }
}
