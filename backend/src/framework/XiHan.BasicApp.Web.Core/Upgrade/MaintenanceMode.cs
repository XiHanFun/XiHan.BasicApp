// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.Framework.Upgrade.Abstractions;

namespace XiHan.BasicApp.Web.Core.Upgrade;

/// <summary>
/// 维护模式开关：进程内共享的一个标志位。
/// </summary>
/// <remarks>
/// 注册为单例。升级引擎在开始迁移前置位、结束或失败后复位，
/// <see cref="MaintenanceModeMiddleware"/> 据此决定是否拦截请求。
/// <para>
/// 该状态不跨进程：多节点部署时每个节点各自维护自己的标志，
/// 拦截的是本节点收到的请求。跨节点统一入口需在网关层另行处理。
/// </para>
/// </remarks>
public sealed class MaintenanceModeState
{
    private int _active;

    /// <summary>
    /// 当前是否处于维护模式
    /// </summary>
    public bool IsActive => Volatile.Read(ref _active) == 1;

    /// <summary>
    /// 置位
    /// </summary>
    public void Enter()
    {
        _ = Interlocked.Exchange(ref _active, 1);
    }

    /// <summary>
    /// 复位
    /// </summary>
    public void Exit()
    {
        _ = Interlocked.Exchange(ref _active, 0);
    }
}

/// <summary>
/// 升级维护模式管理器：把框架升级引擎的进入/退出映射到 <see cref="MaintenanceModeState"/>。
/// </summary>
public sealed class BasicAppUpgradeMaintenanceModeManager : IUpgradeMaintenanceModeManager
{
    private readonly MaintenanceModeState _state;
    private readonly ILogger<BasicAppUpgradeMaintenanceModeManager> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public BasicAppUpgradeMaintenanceModeManager(MaintenanceModeState state, ILogger<BasicAppUpgradeMaintenanceModeManager> logger)
    {
        _state = state;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task EnterAsync(CancellationToken cancellationToken = default)
    {
        _state.Enter();
        _logger.LogWarning("已进入维护模式：除放行清单外的请求将返回 503。");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ExitAsync(CancellationToken cancellationToken = default)
    {
        _state.Exit();
        _logger.LogInformation("已退出维护模式，恢复正常服务。");
        return Task.CompletedTask;
    }
}

/// <summary>
/// 维护模式中间件：维护期间对业务请求返回 503，并带上 Retry-After。
/// </summary>
/// <remarks>
/// 放行健康检查与 OIDC 的发现文档、公钥集：前者是编排系统判定实例存活的依据，
/// 维护期间若一并拦掉会被误判为不健康而重启；后者是客户端验签的前置读取，
/// 拦掉会让所有已签发令牌在维护窗口内集体验不过。
/// </remarks>
public sealed class MaintenanceModeMiddleware
{
    private static readonly string[] AllowedPathPrefixes =
    [
        "/health",
        "/.well-known/"
    ];

    private readonly RequestDelegate _next;
    private readonly MaintenanceModeState _state;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MaintenanceModeMiddleware(RequestDelegate next, MaintenanceModeState state)
    {
        _next = next;
        _state = state;
    }

    /// <summary>
    /// 处理请求
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!_state.IsActive || IsAllowed(context.Request.Path))
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.Headers.RetryAfter = "30";
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            code = StatusCodes.Status503ServiceUnavailable,
            message = "系统正在升级维护，请稍后重试。"
        }));
    }

    private static bool IsAllowed(PathString path)
    {
        return AllowedPathPrefixes.Any(prefix => path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase)
                                                 || path.Value?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true);
    }
}

/// <summary>
/// 维护模式中间件注册扩展
/// </summary>
public static class MaintenanceModeApplicationBuilderExtensions
{
    /// <summary>
    /// 启用维护模式拦截。应尽量靠前注册，使维护期间的请求不进入后续管线。
    /// </summary>
    public static IApplicationBuilder UseMaintenanceMode(this IApplicationBuilder app)
    {
        return app.UseMiddleware<MaintenanceModeMiddleware>();
    }
}
