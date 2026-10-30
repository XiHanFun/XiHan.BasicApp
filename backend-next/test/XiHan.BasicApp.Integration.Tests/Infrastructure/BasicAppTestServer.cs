// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Host;
using XiHan.BasicApp.Runtime.MultiTenancy;
using XiHan.BasicApp.Runtime.Security;
using XiHan.BasicApp.Runtime.Seeding;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.MultiTenancy.Abstractions.Sharding;
using XiHan.Framework.MultiTenancy.Abstractions.Tenants;
using XiHan.Framework.MultiTenancy.Database.Tenants;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions;
using XiHan.Framework.Security.Abstractions.Authentication;
using XiHan.Framework.Security.Abstractions.Permissions;
using XiHan.Framework.Security.Database.Grants;
using XiHanApp = XiHan.Framework.Kernel.Hosting.App;

namespace XiHan.BasicApp.Integration.Tests.Infrastructure;

/// <summary>
/// 用宿主的真实装配（<see cref="BasicAppHost.Create"/>）在 TestServer 里启动应用。
/// </summary>
/// <remarks>
/// 数据库换成 SQLite 文件或测试 PostgreSQL，JWT 用测试密钥。
/// </remarks>
public sealed class BasicAppTestServer : IAsyncDisposable
{
    public const string ApiPrefix = "/api";

    private BasicAppTestServer(WebApplication app)
    {
        App = app;
        Server = app.GetTestServer();
        Client = app.GetTestClient();
        Kernel = app.Services.GetRequiredService<XiHanApp>();
    }

    public WebApplication App { get; }

    public TestServer Server { get; }

    public HttpClient Client { get; }

    public XiHanApp Kernel { get; }

    public IServiceProvider Services => App.Services;

    public static async Task<BasicAppTestServer> StartAsync(
        string connectionString,
        string provider = "sqlite",
        IEnumerable<KeyValuePair<string, string?>>? settings = null,
        Action<WebApplicationBuilder>? configure = null,
        Action<WebApplication>? configureApp = null,
        Action<WebApplication>? afterMap = null)
    {
        var app = BasicAppHost.Create(
            [],
            builder =>
            {
                builder.WebHost.UseTestServer();
                // 用例的设置单独一层，叠在缺省值之上（同键覆盖，不在一个字典里撞键）
                builder.Configuration.AddInMemoryCollection(Settings(connectionString, provider));
                builder.Configuration.AddInMemoryCollection(settings ?? []);
                configure?.Invoke(builder);
            },
            configureApp);
        afterMap?.Invoke(app);
        await app.StartAsync();
        return new BasicAppTestServer(app);
    }

    public static IEnumerable<KeyValuePair<string, string?>> Settings(string connectionString, string provider) =>
    [
        new("Logging:LogLevel:Default", "Warning"),
        new("XiHan:Bindings:db:type", "connection:relational"),
        new("XiHan:Bindings:db:value", connectionString),
        new("XiHan:Bindings:db:provider", provider),
        new("XiHan:Persistence:SqlSugar:InitTables", "true"),
        // 宿主用数据库调度：测试库是 SQLite，引擎每秒轮询会和用例的写入抢锁；拉长到 5 秒，作业相关的用例自己等同步
        new("XiHan:Scheduling:PollIntervalSeconds", "5"),
        new("XiHan:Security:Jwt:Secret", TestTokens.Secret),
        new("XiHan:Security:Jwt:Issuer", TestTokens.Issuer),
        new("XiHan:Security:Jwt:Audience", TestTokens.Audience),
        new("XiHan:ObjectStorage:SignedUrls:Secret", "integration-tests-signed-url-secret-0123456789"),
        new("XiHan:Security:RoleClaim", "role"),
        new("XiHan:MultiTenancy:TenantClaim", "tenant"),
        new("XiHan:Protocols:Http:RoutePrefix", ApiPrefix),
        new("XiHan:Protocols:Grpc:Package", "xihan.basicapp"),
        new("BasicApp:ApiDocs:Enabled", "true"),
        // 图形验证码缺省关：需要它的用例自己打开并换上固定码源
        new("BasicApp:Auth:CaptchaEnabled", "false"),
    ];

    /// <summary>
    /// 在一次独立调用（完整内核管道）里执行。
    /// </summary>
    /// <remarks>
    /// 完整内核管道：认证、租户、授权、工作单元。带令牌时主体由 JWT 处理器解析。
    /// </remarks>
    public async Task<Result> InvokeAsync(Func<InvocationContext, Task<Result>> action, string? token = null, IReadOnlyDictionary<string, string>? baggage = null)
    {
        await using var scope = Services.CreateAsyncScope();
        var context = XiHanApp.CreateContext(scope.ServiceProvider, CancellationToken.None, baggage: baggage);
        if (token is not null)
        {
            context.SetCredential(new AuthenticationCredential("Bearer", token));
        }

        return await Kernel.InvokeAsync(context, async ctx => ctx.SetOutcome(await action(ctx)));
    }

    /// <summary>
    /// 写入租户目录与权限授予。
    /// </summary>
    /// <remarks>
    /// 经框架的数据库 Provider 存储，在 Transaction 阶段内。
    /// </remarks>
    public async Task SeedAsync(IReadOnlyList<TenantInfo> tenants, IReadOnlyList<PermissionGrant> grants)
    {
        var result = await InvokeAsync(async ctx =>
        {
            if (tenants.Count > 0)
            {
                // 应用写进租户目录的连接串是密文（Tenancy 同样这样写），由 Runtime 的分片解析器解密
                var protector = ctx.Services.GetRequiredService<ISecretProtector>();
                var stored = tenants.Select(tenant => tenant.Properties.TryGetValue(TenantShardKeys.ConnectionString, out var connection)
                    ? tenant with { Properties = new Dictionary<string, string>(tenant.Properties, StringComparer.Ordinal) { [TenantShardKeys.ConnectionString] = protector.Protect(ProtectedTenantShardResolver.Purpose, connection) } }
                    : tenant).ToList();
                foreach (var outcome in await ctx.Services.GetRequiredService<TenantRecordStore>().UpsertAsync(stored, ctx.CancellationToken))
                {
                    outcome.Outcome.ThrowIfFailure();
                }
            }

            if (grants.Count > 0)
            {
                foreach (var outcome in await ctx.Services.GetRequiredService<PermissionGrantRecordStore>().GrantAsync(grants, ctx.CancellationToken))
                {
                    outcome.Outcome.ThrowIfFailure();
                }
            }

            return Result.Success();
        });
        result.ThrowIfFailure();
    }

    /// <summary>
    /// 执行各包登记的种子。
    /// </summary>
    /// <remarks>
    /// 包括超级管理员 <c>superadmin</c> 与它的全局授予等，与 <c>seed</c> 命令同一条路径。
    /// </remarks>
    public async Task RunSeedersAsync(IReadOnlyCollection<string>? only = null)
        => (await Services.GetRequiredService<SeedRunner>().RunAsync(only, CancellationToken.None)).ThrowIfFailure();

    /// <summary>
    /// 写入缺省的测试数据。
    /// </summary>
    /// <remarks>
    /// 租户 <c>acme</c> 与 <c>globex</c>；角色 <c>admin</c> 可读可建，<c>viewer</c> 只读。
    /// </remarks>
    public Task SeedDefaultsAsync(IReadOnlyDictionary<string, string>? acmeProperties = null) => SeedAsync(
        [
            new TenantInfo { Id = "acme", DisplayName = "Acme", Properties = acmeProperties ?? new Dictionary<string, string>(StringComparer.Ordinal) },
            new TenantInfo { Id = "globex", DisplayName = "Globex" },
        ],
        [
            new PermissionGrant(GrantSubject.ForRole("admin"), "identity.users.read"),
            new PermissionGrant(GrantSubject.ForRole("admin"), "identity.users.create"),
            new PermissionGrant(GrantSubject.ForRole("viewer"), "identity.users.read"),
        ]);

    public static HttpRequestMessage Request(HttpMethod method, string path, string? json = null, string? token = null, string? acceptLanguage = null)
    {
        var request = new HttpRequestMessage(method, path);
        if (json is not null)
        {
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (acceptLanguage is not null)
        {
            request.Headers.AcceptLanguage.ParseAdd(acceptLanguage);
        }

        return request;
    }

    public async Task<(HttpResponseMessage Response, JsonElement Body)> SendAsync(HttpRequestMessage request)
    {
        var response = await Client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        var body = text.Length == 0 ? default : JsonDocument.Parse(text).RootElement.Clone();
        return (response, body);
    }

    public GrpcChannel CreateGrpcChannel() => GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions { HttpHandler = Server.CreateHandler() });

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await App.StopAsync();
        await App.DisposeAsync();
    }
}
