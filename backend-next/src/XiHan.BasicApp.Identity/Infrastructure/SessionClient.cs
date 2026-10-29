// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Runtime.Clients;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Network;

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// 开会话时记下的客户端。
/// </summary>
/// <remarks>
/// 取调用来源（框架协议适配器填入的远端地址与 User-Agent，ADR-0043）；没有网络对端的调用（命令行、消息派发）为 <see langword="null"/>。
/// </remarks>
public static class SessionClient
{
    /// <summary>
    /// 会话的 IP 列与客户端信息列的长度上限（实体没写长度，建表缺省 512）。
    /// </summary>
    private const int MaxLength = 512;

    /// <summary>
    /// 远端地址。
    /// </summary>
    public static string? AddressOf(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.GetOrigin().TryGetValue(out var origin) ? Clip(origin.RemoteAddress) : null;
    }

    /// <summary>
    /// 客户端的 User-Agent。
    /// </summary>
    public static string? AgentOf(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.GetOrigin().TryGetValue(out var origin) ? Clip(origin.UserAgent) : null;
    }

    /// <summary>
    /// 填会话的浏览器、操作系统、设备类型与登录地点。
    /// </summary>
    /// <remarks>
    /// 按会话已记下的 User-Agent 与 IP 解析（在线用户页的列）。
    /// </remarks>
    public static void Describe(SysUserSession session, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(context);
        var agent = UserAgentInfo.Parse(session.ClientInfo);
        session.Browser = agent.Browser;
        session.Os = agent.Os;
        session.Device = agent.Device;
        session.Location = LocationOf(session.IpAddress, context.Services.GetService<IIpRegionResolver>());
    }

    private static string? LocationOf(string? address, IIpRegionResolver? regions)
    {
        if (regions is null || !IPAddress.TryParse(address, out var ip) || !regions.Resolve(ip).TryGetValue(out var region))
        {
            return null;
        }

        var location = region.Location;
        return location.Length == 0 ? null : location.Length <= 128 ? location : location[..128];
    }

    private static string? Clip(string? value) => value is null || value.Length <= MaxLength ? value : value[..MaxLength];
}
