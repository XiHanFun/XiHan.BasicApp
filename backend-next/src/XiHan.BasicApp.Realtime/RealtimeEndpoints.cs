// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Realtime.Infrastructure;

namespace XiHan.BasicApp.Realtime;

/// <summary>
/// 宿主映射 Hub 用的扩展。
/// </summary>
public static class RealtimeEndpoints
{
    /// <summary>
    /// 映射全部登记过的 Hub。
    /// </summary>
    /// <remarks>
    /// 通知 Hub 与各包经 <see cref="RealtimeHubServiceCollectionExtensions.AddRealtimeHub{THub}"/> 登记的 Hub（包没装配即没有登记、不映射）。
    /// 绕过内核管道（长连接不受 <c>xihan-deadline</c> 约束，内核停止后不被 503 打断），由框架的认证方案 <c>XiHan</c> 与 ASP.NET Core 授权把关。
    /// 宿主须在 <c>UseXiHan()</c> 之前 <c>UseAuthentication()</c> / <c>UseAuthorization()</c>。
    /// </remarks>
    public static IEndpointRouteBuilder MapRealtimeHubs(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        foreach (var registration in endpoints.ServiceProvider.GetRequiredService<RealtimeHubCatalog>().Registrations)
        {
            registration.Map(endpoints);
        }

        return endpoints;
    }
}
