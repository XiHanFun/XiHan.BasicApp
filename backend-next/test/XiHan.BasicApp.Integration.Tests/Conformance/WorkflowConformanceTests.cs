// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.MultiTenancy.Abstractions.Tenants;
using XiHan.Framework.Workflow.Abstractions.Engine;
using XiHan.Framework.Workflow.Conformance.Engine;

namespace XiHan.BasicApp.Integration.Tests.Conformance;

/// <summary>
/// 工作流一致性套件的宿主
/// </summary>
/// <remarks>
/// 每个用例在宿主的真实装配上起一个新应用（数据库工作流、发件箱与 BasicApp 的全部中间件都在），库是新建的临时库
/// 套件以 <c>TryAdd</c> 登记的固定租户目录让不过宿主自己的租户目录，四个租户（三个启用、<c>t-off</c> 停用）照它写进框架的租户目录
/// 套件的事件记录器在消息代理一侧，宿主装了发件箱时夹具在每次操作之后把已提交的事件投递过去（框架 ADR-0096 第六节）；轮询一次取容器里的 <see cref="DueBookmarkPoller"/>
/// </remarks>
internal static class WorkflowConformanceHosts
{
    private static readonly IReadOnlyList<TenantInfo> _tenants =
    [
        new TenantInfo { Id = "t1" },
        new TenantInfo { Id = "t2" },
        new TenantInfo { Id = "t-other" },
        new TenantInfo { Id = "t-off", IsEnabled = false },
    ];

    public static async Task<WorkflowEngineHost> StartAsync(bool postgres, Action<IServiceCollection> configure, IEnumerable<KeyValuePair<string, string?>>? configuration)
    {
        BasicAppTestServer server;
        IDisposable? file = null;
        IAsyncDisposable? database = null;
        if (postgres)
        {
            var created = await PostgresDatabase.CreateAsync(PostgresDatabase.RequireServer(), "xihan_basicapp_workflow");
            database = created;
            server = await BasicAppTestServer.StartAsync(created.ConnectionString, DatabaseProviders.PostgreSql, configuration, builder => configure(builder.Services));
        }
        else
        {
            var sqlite = new SqliteDatabase();
            file = sqlite;
            server = await BasicAppTestServer.StartAsync(sqlite.ConnectionString, settings: configuration, configure: builder => configure(builder.Services));
        }

        await server.SeedAsync(_tenants, []);
        var poller = server.Services.GetRequiredService<DueBookmarkPoller>();
        return new WorkflowEngineHost(server.Services, ct => poller.PollOnceAsync(100, ct), new Owned(server, database, file));
    }

    private sealed class Owned(IAsyncDisposable host, IAsyncDisposable? database, IDisposable? file) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            // 先停宿主再删库
            await host.DisposeAsync();
            if (database is not null)
            {
                await database.DisposeAsync();
            }

            file?.Dispose();
        }
    }
}

public sealed class SqliteWorkflowEngineConformanceTests : WorkflowEngineConformance
{
    protected override Task<WorkflowEngineHost> StartAsync(Action<IServiceCollection> configure, IEnumerable<KeyValuePair<string, string?>>? configuration = null)
        => WorkflowConformanceHosts.StartAsync(postgres: false, configure, configuration);
}

public sealed class PostgresWorkflowEngineConformanceTests : WorkflowEngineConformance
{
    protected override Task<WorkflowEngineHost> StartAsync(Action<IServiceCollection> configure, IEnumerable<KeyValuePair<string, string?>>? configuration = null)
        => WorkflowConformanceHosts.StartAsync(postgres: true, configure, configuration);
}

public sealed class SqliteWorkflowTaskConformanceTests : WorkflowTaskConformance
{
    protected override Task<WorkflowEngineHost> StartAsync(Action<IServiceCollection> configure, IEnumerable<KeyValuePair<string, string?>>? configuration = null)
        => WorkflowConformanceHosts.StartAsync(postgres: false, configure, configuration);
}

public sealed class PostgresWorkflowTaskConformanceTests : WorkflowTaskConformance
{
    protected override Task<WorkflowEngineHost> StartAsync(Action<IServiceCollection> configure, IEnumerable<KeyValuePair<string, string?>>? configuration = null)
        => WorkflowConformanceHosts.StartAsync(postgres: true, configure, configuration);
}
