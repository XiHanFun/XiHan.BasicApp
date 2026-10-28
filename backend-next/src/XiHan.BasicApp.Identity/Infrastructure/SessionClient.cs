// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// 开会话时记下的客户端：取调用来源（框架协议适配器填入的远端地址与 User-Agent，ADR-0043）；没有网络对端的调用（命令行、消息派发）为 <see langword="null"/>。
/// </summary>
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

    private static string? Clip(string? value) => value is null || value.Length <= MaxLength ? value : value[..MaxLength];
}
