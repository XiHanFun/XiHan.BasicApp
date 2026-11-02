// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Realtime;
using XiHan.Framework.Hosting.AspNetCore.Authentication;
using XiHan.Framework.Hosting.AspNetCore.Endpoints;

namespace XiHan.BasicApp.Realtime.Infrastructure;

/// <summary>
/// 一个 Hub 的发送面：给组发消息与管理组成员。
/// </summary>
/// <param name="Clients">客户端代理。</param>
/// <param name="Groups">组管理。</param>
internal sealed record RealtimeHubContext(IHubClients Clients, IGroupManager Groups);

/// <summary>
/// 已登记的 Hub。
/// </summary>
/// <param name="Path">Hub 路径。</param>
/// <param name="HubType">Hub 类型。</param>
/// <param name="Map">映射端点。</param>
/// <param name="ContextOf">取发送面。</param>
internal sealed record RealtimeHubRegistration(string Path, Type HubType, Action<IEndpointRouteBuilder> Map, Func<IServiceProvider, RealtimeHubContext> ContextOf);

/// <summary>
/// Hub 登记表。
/// </summary>
/// <remarks>
/// 装配期由 <see cref="RealtimeHubServiceCollectionExtensions.AddRealtimeHub{THub}"/> 填充，宿主经 <see cref="RealtimeEndpoints.MapRealtimeHubs"/> 一并映射，推送按路径找到发送面。
/// </remarks>
internal sealed class RealtimeHubCatalog
{
    private readonly List<RealtimeHubRegistration> _registrations = [];

    public IReadOnlyList<RealtimeHubRegistration> Registrations => _registrations;

    public void Add(RealtimeHubRegistration registration)
    {
        if (_registrations.Any(r => string.Equals(r.Path, registration.Path, StringComparison.Ordinal) || r.HubType == registration.HubType))
        {
            throw new InvalidOperationException($"Realtime hub '{registration.Path}' ({registration.HubType.Name}) is already registered.");
        }

        _registrations.Add(registration);
    }

    public string PathOf(Type hubType)
        => _registrations.FirstOrDefault(r => r.HubType == hubType)?.Path ?? throw new InvalidOperationException($"Realtime hub type '{hubType.Name}' is not registered.");

    public RealtimeHubContext ContextOf(string path, IServiceProvider services)
        => (_registrations.FirstOrDefault(r => string.Equals(r.Path, path, StringComparison.Ordinal)) ?? throw new InvalidOperationException($"Realtime hub '{path}' is not registered.")).ContextOf(services);
}

/// <summary>
/// 登记实时 Hub 的扩展。
/// </summary>
public static class RealtimeHubServiceCollectionExtensions
{
    /// <summary>
    /// 登记一个 Hub。
    /// </summary>
    /// <remarks>
    /// 路径必须在 <see cref="RealtimeHubs.Prefix"/> 之下（浏览器的 WebSocket / SSE 只在这个前缀下用查询串带令牌）。
    /// 登记了的 Hub 由宿主随通知 Hub 一并映射、可经 <see cref="IRealtimePush"/> 按路径推送；包没装配即不映射。同一路径或同一类型重复登记即装配失败。
    /// </remarks>
    /// <typeparam name="THub">Hub 类型。</typeparam>
    /// <param name="services">服务集合。</param>
    /// <param name="path">Hub 路径（<see cref="RealtimeHubs"/>）。</param>
    public static IServiceCollection AddRealtimeHub<THub>(this IServiceCollection services, string path)
        where THub : RealtimeHub
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!path.StartsWith(RealtimeHubs.Prefix + "/", StringComparison.Ordinal))
        {
            throw new ArgumentException($"Realtime hub paths must start with '{RealtimeHubs.Prefix}/', but was '{path}'.", nameof(path));
        }

        CatalogOf(services).Add(new RealtimeHubRegistration(
            path,
            typeof(THub),
            endpoints => PipelineBypassMetadata.BypassXiHanPipeline(endpoints
                .MapHub<THub>(path)
                .RequireAuthorization(static policy => policy.AddAuthenticationSchemes(InvocationAuthenticationDefaults.AuthenticationScheme).RequireAuthenticatedUser())),
            static provider =>
            {
                var context = provider.GetRequiredService<IHubContext<THub>>();
                return new RealtimeHubContext(context.Clients, context.Groups);
            }));
        return services;
    }

    /// <summary>
    /// 取（必要时创建）登记表。
    /// </summary>
    /// <remarks>
    /// 以实例注册，多次调用返回同一对象，各包共同填充。
    /// </remarks>
    internal static RealtimeHubCatalog CatalogOf(IServiceCollection services)
    {
        foreach (var descriptor in services)
        {
            if (descriptor.ServiceType == typeof(RealtimeHubCatalog) && descriptor.ImplementationInstance is RealtimeHubCatalog existing)
            {
                return existing;
            }
        }

        var catalog = new RealtimeHubCatalog();
        services.AddSingleton(catalog);
        return catalog;
    }
}
