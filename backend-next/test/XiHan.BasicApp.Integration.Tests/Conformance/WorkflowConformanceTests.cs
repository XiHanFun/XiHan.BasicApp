// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Testing;
using XiHan.Framework.Workflow.Abstractions.Engine;
using XiHan.Framework.Workflow.Conformance.Engine;
using XiHan.Framework.Workflow.Database;

namespace XiHan.BasicApp.Integration.Tests.Conformance;

/// <summary>
/// 工作流一致性套件的宿主。
/// </summary>
/// <remarks>
/// 套件把自己的事件记录器登记成 <c>IMessagePublisher</c>，而 BasicApp 宿主必装的发件箱以 <c>Replace</c> 换成发件箱发布者，
/// 在宿主的完整装配上跑时套件看不到引擎发布的事件（框架问题，已报）。这里用框架的 <see cref="TestHost"/> 装 BasicApp 的 SqlSugar 回调
/// （库类型、snake_case 命名、字符串列长）与 <see cref="DatabaseWorkflowFeature"/>，每个用例一个新建的临时库；
/// 宿主完整装配下框架调用经过 BasicApp 中间件、事件经发件箱发出由 <see cref="WorkflowHostingTests"/> 核对。
/// 没有装多租户，轮询器取套件的固定租户目录；轮询一次取容器里的 <see cref="DueBookmarkPoller"/>。
/// </remarks>
internal static class WorkflowConformanceHosts
{
    public static async Task<WorkflowEngineHost> StartAsync(bool postgres, Action<IServiceCollection> configure, IEnumerable<KeyValuePair<string, string?>>? configuration)
    {
        IDisposable? file = null;
        IAsyncDisposable? database = null;
        string connectionString;
        string provider;
        if (postgres)
        {
            var created = await PostgresDatabase.CreateAsync(PostgresDatabase.RequireServer(), "xihan_basicapp_workflow");
            database = created;
            (connectionString, provider) = (created.ConnectionString, DatabaseProviders.PostgreSql);
        }
        else
        {
            var sqlite = new SqliteDatabase();
            file = sqlite;
            (connectionString, provider) = (sqlite.ConnectionString, DatabaseProviders.Sqlite);
        }

        List<KeyValuePair<string, string?>> settings =
        [
            new("XiHan:Bindings:db:type", "connection:relational"),
            new("XiHan:Bindings:db:value", connectionString),
            new("XiHan:Bindings:db:provider", provider),
            new("XiHan:Persistence:SqlSugar:InitTables", "true"),
            .. BasicAppTestServer.WorkflowLeases(connectionString, provider),
            .. configuration ?? [],
        ];
        var host = await TestHost.StartAsync(static builder => builder.UseFeature(new SqlSugarPersistenceFeature(SqlSugarRuntime.Configure)).UseFeature<DatabaseWorkflowFeature>(), settings, configure);
        var poller = host.Services.GetRequiredService<DueBookmarkPoller>();
        return new WorkflowEngineHost(host.Services, ct => poller.PollOnceAsync(100, ct), new Owned(host, database, file));
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
