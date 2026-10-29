// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using XiHan.BasicApp.Contracts.Realtime;
using XiHan.BasicApp.Realtime.Infrastructure;
using XiHan.Framework.Hosting.AspNetCore.Endpoints;

namespace XiHan.BasicApp.Realtime;

/// <summary>
/// 宿主映射 Hub 用的扩展。
/// </summary>
public static class RealtimeEndpoints
{
    /// <summary>
    /// 映射通知 Hub：绕过内核管道（长连接不受 <c>xihan-deadline</c> 约束，内核停止后不被 503 打断），由 ASP.NET Core 的 JWT 认证与授权把关。
    /// 宿主须在 <c>UseXiHan()</c> 之前 <c>UseAuthentication()</c> / <c>UseAuthorization()</c>。
    /// </summary>
    public static IEndpointRouteBuilder MapRealtimeHubs(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        PipelineBypassMetadata.BypassXiHanPipeline(endpoints.MapHub<NotificationHub>(RealtimeHubs.Notification).RequireAuthorization(RealtimeAuthentication.Policy));
        return endpoints;
    }
}
