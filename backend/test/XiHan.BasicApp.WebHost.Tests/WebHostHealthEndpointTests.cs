// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using XiHan.Framework.Core.Application;
using XiHan.Framework.Core.DependencyInjection;
using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.WebHost.Tests;

/// <summary>
/// 宿主模块生命周期钩子与 /health 端点契约测试。
/// </summary>
/// <remarks>
/// 守三件在源码注释里被写死的事：Webhook 中间件必须落在预初始化阶段（早于鉴权），
/// /health 必须是匿名可达的固定路由（否则鉴权 FallbackPolicy 会把探针 401 打死），
/// 以及 /health 响应体只能吐总状态和各项名字、不得外泄描述与异常细节。
/// </remarks>
public sealed class WebHostHealthEndpointTests
{
    /// <summary>
    /// 预初始化钩子必须由本模块自己 override，且恰好只往管线里追加一个中间件。
    /// </summary>
    /// <remarks>
    /// 这个钩子先于框架 WebApi 模块的管线注册执行，Telegram Webhook 中间件因此位于认证/授权之前。
    /// 一旦被挪到 OnApplicationInitialization，Webhook 请求会先被 401 拦掉、机器人静默失联。
    /// </remarks>
    [Fact]
    public void OnPreApplicationInitialization_ShouldRegisterExactlyOneMiddlewareInPreStage()
    {
        var method = typeof(XiHanBasicAppWebHostModule).GetMethod(
            nameof(XiHanBasicAppWebHostModule.OnPreApplicationInitialization),
            [typeof(ApplicationInitializationContext)]);
        Assert.NotNull(method);
        Assert.Equal(typeof(XiHanBasicAppWebHostModule), method.DeclaringType);

        var provider = BuildHostServices();
        var builder = new RecordingApplicationBuilder(provider);
        var context = CreateInitializationContext(provider, builder);

        new XiHanBasicAppWebHostModule().OnPreApplicationInitialization(context);

        Assert.Equal(1, builder.UseCount);
    }

    /// <summary>
    /// 应用初始化钩子必须由本模块自己 override，移走即等于 /health 端点整块消失。
    /// </summary>
    [Fact]
    public void OnApplicationInitialization_ShouldBeOverriddenByWebHostModule()
    {
        var method = typeof(XiHanBasicAppWebHostModule).GetMethod(
            nameof(XiHanBasicAppWebHostModule.OnApplicationInitialization),
            [typeof(ApplicationInitializationContext)]);

        Assert.NotNull(method);
        Assert.Equal(typeof(XiHanBasicAppWebHostModule), method.DeclaringType);
    }

    /// <summary>
    /// 构建器同时是端点路由构建器时，必须走端点分支并注册路由恰为 /health 的端点。
    /// </summary>
    /// <remarks>
    /// 探针地址是部署契约：K8s / 负载均衡按它配置，升级维护模式的 503 放行白名单也是按 /health 写死的，
    /// 带前缀或带版本段都会让这一串配置同时失效。
    /// </remarks>
    [Fact]
    public void OnApplicationInitialization_ShouldMapHealthEndpointAtFixedRoute()
    {
        var endpoints = InitializeAndReadEndpoints();

        var routes = endpoints.OfType<RouteEndpoint>().Select(endpoint => endpoint.RoutePattern.RawText).ToList();

        Assert.Contains("/health", routes, StringComparer.Ordinal);
    }

    /// <summary>
    /// /health 端点必须带匿名访问元数据，否则会被框架的鉴权 FallbackPolicy 401 掉、探针全挂。
    /// </summary>
    [Fact]
    public void OnApplicationInitialization_HealthEndpointShouldAllowAnonymous()
    {
        var endpoints = InitializeAndReadEndpoints();

        var healthEndpoint = endpoints.OfType<RouteEndpoint>().Single(endpoint =>
            string.Equals(endpoint.RoutePattern.RawText, "/health", StringComparison.Ordinal));

        Assert.NotNull(healthEndpoint.Metadata.GetMetadata<IAllowAnonymous>());
    }

    /// <summary>
    /// 构建器不是端点路由构建器时必须有中间件兜底，不得直接跳过导致压根没有 /health。
    /// </summary>
    [Fact]
    public void OnApplicationInitialization_NonEndpointBuilderShouldFallBackToMiddleware()
    {
        var provider = BuildHostServices();
        var builder = new RecordingApplicationBuilder(provider);
        var context = CreateInitializationContext(provider, builder);

        new XiHanBasicAppWebHostModule().OnApplicationInitialization(context);

        Assert.Equal(1, builder.UseCount);
    }

    /// <summary>
    /// 最小化响应写入器只能吐 status / totalDurationMs / checks 三个顶层字段，
    /// 每个检查项只能吐 name 与 status，多一个字段就是信息外泄面扩大。
    /// </summary>
    [Fact]
    public async Task WriteMinimalHealthResponseAsync_ShouldEmitOnlyWhitelistedFields()
    {
        var report = CreateReportWithSensitiveDetails();

        using var document = JsonDocument.Parse(await WriteResponseAsync(report));
        var root = document.RootElement;

        Assert.Equal(
            ["status", "totalDurationMs", "checks"],
            root.EnumerateObject().Select(property => property.Name));

        foreach (var check in root.GetProperty("checks").EnumerateArray())
        {
            Assert.Equal(["name", "status"], check.EnumerateObject().Select(property => property.Name));
        }
    }

    /// <summary>
    /// 描述、异常消息与堆栈一律不得出现在响应体里：/health 是匿名端点，
    /// 泄漏这些等于把连接串、内网拓扑、组件版本喂给公网扫描器。
    /// </summary>
    [Fact]
    public async Task WriteMinimalHealthResponseAsync_ShouldNotLeakDescriptionOrException()
    {
        var report = CreateReportWithSensitiveDetails();

        var body = await WriteResponseAsync(report);

        Assert.DoesNotContain(SensitiveExceptionMessage, body, StringComparison.Ordinal);
        Assert.DoesNotContain("description", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exception", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stackTrace", body, StringComparison.OrdinalIgnoreCase);
        // Data 字典里的内网地址同样不能露出去（不能直接搜 "data"，它是 "database" 的子串）
        Assert.DoesNotContain(SensitiveDataValue, body, StringComparison.Ordinal);
    }

    /// <summary>
    /// 状态必须是枚举名字符串、总耗时必须是数值毫秒，检查项名与状态必须与报告一致。
    /// </summary>
    /// <remarks>
    /// 探针脚本按字符串匹配状态；总耗时若被序列化成 TimeSpan 对象或 "00:00:00.123" 形态，
    /// 采集侧的数值面板会直接取不到数。
    /// </remarks>
    [Fact]
    public async Task WriteMinimalHealthResponseAsync_ShouldEmitEnumNamesAndNumericDuration()
    {
        var report = CreateReportWithSensitiveDetails();

        using var document = JsonDocument.Parse(await WriteResponseAsync(report));
        var root = document.RootElement;

        Assert.Equal(nameof(HealthStatus.Unhealthy), root.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Number, root.GetProperty("totalDurationMs").ValueKind);
        Assert.Equal(1234d, root.GetProperty("totalDurationMs").GetDouble());

        var checks = root.GetProperty("checks").EnumerateArray()
            .ToDictionary(
                check => check.GetProperty("name").GetString()!,
                check => check.GetProperty("status").GetString(),
                StringComparer.Ordinal);

        Assert.Equal(nameof(HealthStatus.Unhealthy), checks["database"]);
        Assert.Equal(nameof(HealthStatus.Healthy), checks["redis"]);
    }

    /// <summary>
    /// 内容类型必须带 charset：中文场景下缺了它部分客户端会按 ISO-8859-1 解码出乱码。
    /// </summary>
    [Fact]
    public async Task WriteMinimalHealthResponseAsync_ShouldSetJsonContentTypeWithCharset()
    {
        var httpContext = CreateHttpContext(out _);

        await InvokeWriterAsync(httpContext, CreateReportWithSensitiveDetails());

        Assert.Equal("application/json; charset=utf-8", httpContext.Response.ContentType);
    }

    /// <summary>
    /// 空报告（一个检查项都没有）必须能正常写出空数组而不是抛异常。
    /// </summary>
    [Fact]
    public async Task WriteMinimalHealthResponseAsync_EmptyReportShouldEmitEmptyChecksArray()
    {
        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>(StringComparer.Ordinal),
            HealthStatus.Healthy,
            TimeSpan.Zero);

        using var document = JsonDocument.Parse(await WriteResponseAsync(report));

        Assert.Equal(JsonValueKind.Array, document.RootElement.GetProperty("checks").ValueKind);
        Assert.Empty(document.RootElement.GetProperty("checks").EnumerateArray());
        Assert.Equal(nameof(HealthStatus.Healthy), document.RootElement.GetProperty("status").GetString());
    }

    /// <summary>
    /// 刻意塞进报告里的敏感异常消息，用于验证它绝不会出现在响应体中。
    /// </summary>
    private const string SensitiveExceptionMessage =
        "Server=10.0.0.7;Database=XiHanBasicApp;Username=postgres;Password=postgres";

    /// <summary>
    /// 刻意塞进检查项 Data 字典的内网地址，用于验证它绝不会出现在响应体中。
    /// </summary>
    private const string SensitiveDataValue = "10.0.0.7";

    /// <summary>
    /// 构造一份带敏感描述与敏感异常的健康报告。
    /// </summary>
    /// <returns>健康报告。</returns>
    private static HealthReport CreateReportWithSensitiveDetails()
    {
        var entries = new Dictionary<string, HealthReportEntry>(StringComparer.Ordinal)
        {
            ["database"] = new(
                HealthStatus.Unhealthy,
                "数据库连接失败",
                TimeSpan.FromMilliseconds(12),
                new InvalidOperationException(SensitiveExceptionMessage),
                new Dictionary<string, object> { ["host"] = SensitiveDataValue }),
            ["redis"] = new(
                HealthStatus.Healthy,
                "Redis 未启用（进程内回退）",
                TimeSpan.FromMilliseconds(3),
                null,
                null)
        };

        return new HealthReport(entries, HealthStatus.Unhealthy, TimeSpan.FromMilliseconds(1234));
    }

    /// <summary>
    /// 调用私有响应写入器并读回响应体文本。
    /// </summary>
    /// <param name="report">健康报告。</param>
    /// <returns>响应体 UTF-8 文本。</returns>
    private static async Task<string> WriteResponseAsync(HealthReport report)
    {
        var httpContext = CreateHttpContext(out var body);

        await InvokeWriterAsync(httpContext, report);

        return Encoding.UTF8.GetString(body.ToArray());
    }

    /// <summary>
    /// 反射调用宿主模块的私有静态响应写入器。
    /// </summary>
    /// <param name="httpContext">请求上下文。</param>
    /// <param name="report">健康报告。</param>
    /// <returns>异步任务。</returns>
    private static async Task InvokeWriterAsync(HttpContext httpContext, HealthReport report)
    {
        var method = typeof(XiHanBasicAppWebHostModule).GetMethod(
            "WriteMinimalHealthResponseAsync",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var task = method.Invoke(null, [httpContext, report]) as Task;
        Assert.NotNull(task);

        await task;
    }

    /// <summary>
    /// 构造一个响应体可读回的请求上下文。
    /// </summary>
    /// <param name="body">响应体缓冲区。</param>
    /// <returns>请求上下文。</returns>
    private static DefaultHttpContext CreateHttpContext(out MemoryStream body)
    {
        body = new MemoryStream();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        httpContext.Response.Body = body;

        return httpContext;
    }

    /// <summary>
    /// 执行应用初始化并读回注册出来的全部端点。
    /// </summary>
    /// <returns>端点列表。</returns>
    private static List<Endpoint> InitializeAndReadEndpoints()
    {
        var provider = BuildHostServices();
        var builder = new RecordingEndpointApplicationBuilder(provider);
        var context = CreateInitializationContext(provider, builder);

        new XiHanBasicAppWebHostModule().OnApplicationInitialization(context);

        return [.. builder.DataSources.SelectMany(source => source.Endpoints)];
    }

    /// <summary>
    /// 用给定构建器组装应用初始化上下文。
    /// </summary>
    /// <param name="provider">服务提供者。</param>
    /// <param name="builder">应用构建器假件。</param>
    /// <returns>应用初始化上下文。</returns>
    private static ApplicationInitializationContext CreateInitializationContext(
        IServiceProvider provider,
        IApplicationBuilder builder)
    {
        provider.GetRequiredService<ObjectAccessor<IApplicationBuilder>>().Value = builder;

        return new ApplicationInitializationContext(provider);
    }

    /// <summary>
    /// 组装宿主初始化所需的最小服务容器（全内存，不连任何外部组件）。
    /// </summary>
    /// <returns>服务提供者。</returns>
    private static IServiceProvider BuildHostServices()
    {
        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddRouting();
        new XiHanBasicAppWebHostModule().ConfigureServices(new ServiceConfigurationContext(services));
        _ = services.AddSingleton(new Mock<ISqlSugarClientResolver>().Object);

        // 应用构建器要等容器建好之后才能创建，故先注册空访问器、稍后回填
        var accessor = new ObjectAccessor<IApplicationBuilder>();
        _ = services.AddSingleton(accessor);
        _ = services.AddSingleton<IObjectAccessor<IApplicationBuilder>>(accessor);

        return services.BuildServiceProvider();
    }
}
