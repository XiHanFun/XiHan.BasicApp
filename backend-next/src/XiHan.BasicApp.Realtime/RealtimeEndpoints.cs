// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using XiHan.BasicApp.Contracts.Realtime;
using XiHan.BasicApp.Realtime.Infrastructure;
using XiHan.Framework.Hosting.AspNetCore.Authentication;
using XiHan.Framework.Hosting.AspNetCore.Endpoints;

namespace XiHan.BasicApp.Realtime;

/// <summary>
/// 宿主映射 Hub 用的扩展。
/// </summary>
public static class RealtimeEndpoints
{
    /// <summary>
    /// 映射通知 Hub。
    /// </summary>
    /// <remarks>
    /// 绕过内核管道（长连接不受 <c>xihan-deadline</c> 约束，内核停止后不被 503 打断），由框架的认证方案 <c>XiHan</c> 与 ASP.NET Core 授权把关。
    /// 宿主须在 <c>UseXiHan()</c> 之前 <c>UseAuthentication()</c> / <c>UseAuthorization()</c>。
    /// </remarks>
    public static IEndpointRouteBuilder MapRealtimeHubs(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        PipelineBypassMetadata.BypassXiHanPipeline(endpoints.MapHub<NotificationHub>(RealtimeHubs.Notification).RequireAuthorization(static policy => policy.AddAuthenticationSchemes(InvocationAuthenticationDefaults.AuthenticationScheme).RequireAuthenticatedUser()));
        return endpoints;
    }
}
