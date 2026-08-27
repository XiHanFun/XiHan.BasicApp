// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text;
using System.Text.Json;
using XiHan.BasicApp.Web.Core.Upgrade;

namespace XiHan.BasicApp.Web.Core.Tests;

/// <summary>
/// 维护模式测试公共夹具：构造可断言响应体的请求上下文、真实中间件管线与内存日志替身。
/// </summary>
/// <remarks>
/// 全部为纯内存构件，不启动宿主、不触网、不落盘，保证用例可离线、并行、任意顺序执行。
/// </remarks>
internal static class MaintenanceModeTestHelper
{
    /// <summary>
    /// 中间件拦截时约定返回给前端展示的中文提示（对外契约，改动必须显式改测试）。
    /// </summary>
    internal const string MaintenanceMessage = "系统正在升级维护，请稍后重试。";

    /// <summary>
    /// 构造一个响应体可回读的请求上下文。
    /// </summary>
    /// <param name="path">请求路径；传 null 表示 default(PathString)（Value 为 null 的退化值）</param>
    /// <param name="method">HTTP 方法，默认 GET</param>
    /// <param name="queryString">查询串（含问号），默认无</param>
    /// <returns>响应体挂在 MemoryStream 上的上下文</returns>
    internal static DefaultHttpContext CreateContext(string? path, string method = "GET", string? queryString = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path is null ? default : new PathString(path);
        if (queryString is not null)
        {
            context.Request.QueryString = new QueryString(queryString);
        }

        context.Response.Body = new MemoryStream();
        return context;
    }

    /// <summary>
    /// 按 UTF-8 回读响应体，用于校验 503 响应的 JSON 契约与中文不乱码。
    /// </summary>
    /// <param name="context">已被中间件处理过的上下文</param>
    /// <returns>响应体文本</returns>
    internal static async Task<string> ReadResponseBodyAsync(HttpContext context)
    {
        _ = context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(
            context.Response.Body,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    /// <summary>
    /// 直接驱动中间件实例，返回下游委托被调用的次数。
    /// </summary>
    /// <param name="state">维护模式状态</param>
    /// <param name="context">请求上下文</param>
    /// <returns>下游被调用次数（0 表示被维护闸门拦截）</returns>
    internal static async Task<int> InvokeMiddlewareAsync(MaintenanceModeState state, HttpContext context)
    {
        var nextCalls = 0;
        var middleware = new MaintenanceModeMiddleware(
            _ =>
            {
                nextCalls++;
                return Task.CompletedTask;
            },
            state);
        await middleware.InvokeAsync(context);
        return nextCalls;
    }

    /// <summary>
    /// 通过公开的 InvokeAsync 端到端观察私有放行判定：未被拦截即视为放行。
    /// </summary>
    /// <param name="path">请求路径；null 表示 default(PathString)</param>
    /// <returns>true 表示该路径在维护期被放行</returns>
    internal static async Task<bool> IsPathAllowedAsync(string? path)
    {
        var state = new MaintenanceModeState();
        state.Enter();
        var context = CreateContext(path);
        var nextCalls = await InvokeMiddlewareAsync(state, context);
        return nextCalls == 1;
    }

    /// <summary>
    /// 反射读取中间件的私有静态放行清单，用于把清单内容钉死。
    /// </summary>
    /// <returns>放行前缀数组</returns>
    internal static string[] GetAllowedPathPrefixes()
    {
        var field = typeof(MaintenanceModeMiddleware).GetField(
            "AllowedPathPrefixes",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(field);
        return (string[])field.GetValue(null)!;
    }

    /// <summary>
    /// 构造仅包含维护模式状态的最小容器。
    /// </summary>
    /// <param name="state">要注册的状态实例；null 表示故意不注册</param>
    /// <returns>服务提供者</returns>
    internal static ServiceProvider CreateServiceProvider(MaintenanceModeState? state)
    {
        var services = new ServiceCollection();
        if (state is not null)
        {
            _ = services.AddSingleton(state);
        }

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// 用真实的 ApplicationBuilder 装配维护模式中间件并构建管线。
    /// </summary>
    /// <param name="serviceProvider">提供中间件构造依赖的容器</param>
    /// <param name="terminal">终端委托，用于观察请求是否落到管线末端</param>
    /// <returns>可直接驱动的请求委托</returns>
    internal static RequestDelegate BuildPipeline(IServiceProvider serviceProvider, RequestDelegate terminal)
    {
        var app = new ApplicationBuilder(serviceProvider);
        _ = app.UseMaintenanceMode();
        app.Run(terminal);
        return app.Build();
    }

    /// <summary>
    /// 解析 503 响应体中的业务码与提示文案。
    /// </summary>
    /// <param name="body">响应体文本</param>
    /// <returns>业务码与提示文案</returns>
    internal static (int Code, string? Message) ParseMaintenancePayload(string body)
    {
        using var document = JsonDocument.Parse(body);
        return (
            document.RootElement.GetProperty("code").GetInt32(),
            document.RootElement.GetProperty("message").GetString());
    }
}

/// <summary>
/// 内存日志替身：记录日志级别与渲染后的消息，用于断言维护模式进出的日志级别约定。
/// </summary>
/// <typeparam name="T">日志分类类型</typeparam>
/// <remarks>
/// 不用 Moq 断言 <c>ILogger</c>：LogWarning/LogInformation 都是扩展方法，Moq 无法 Verify 扩展方法。
/// </remarks>
internal sealed class RecordingLogger<T> : ILogger<T>
{
    /// <summary>
    /// 已记录的日志条目
    /// </summary>
    internal List<(LogLevel Level, string Message)> Entries { get; } = [];

    /// <summary>
    /// 作用域：测试不关心，返回 null。
    /// </summary>
    /// <typeparam name="TState">作用域状态类型</typeparam>
    /// <param name="state">作用域状态</param>
    /// <returns>始终为 null</returns>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    /// <summary>
    /// 所有级别一律启用，避免级别过滤把待断言的日志吃掉。
    /// </summary>
    /// <param name="logLevel">日志级别</param>
    /// <returns>始终为 true</returns>
    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    /// <summary>
    /// 记录日志级别与渲染后的消息。
    /// </summary>
    /// <typeparam name="TState">日志状态类型</typeparam>
    /// <param name="logLevel">日志级别</param>
    /// <param name="eventId">事件标识</param>
    /// <param name="state">日志状态</param>
    /// <param name="exception">异常</param>
    /// <param name="formatter">消息格式化器</param>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Entries.Add((logLevel, formatter(state, exception)));
    }
}
