// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace CrmPlugin;

/// <summary>
/// 插件监听的本机端点
/// </summary>
/// <remarks>
/// Windows 上是只允许当前用户的命名管道，其他系统上是私有目录里的 Unix 域套接字；握手行把它公布给宿主
/// </remarks>
internal sealed class PluginListener
{
    /// <summary>
    /// 命名管道路径的前缀
    /// </summary>
    private const string PipePrefix = @"\\.\pipe\";

    /// <summary>
    /// 创建
    /// </summary>
    /// <param name="network">网络：<c>npipe</c> 或 <c>unix</c></param>
    /// <param name="address">地址</param>
    private PluginListener(string network, string address)
    {
        Network = network;
        Address = address;
    }

    /// <summary>
    /// 网络
    /// </summary>
    public string Network { get; }

    /// <summary>
    /// 地址
    /// </summary>
    public string Address { get; }

    /// <summary>
    /// 写到 stdout 的握手行
    /// </summary>
    public string HandshakeLine => $"xihan-plugin/1 {Network} {Address}";

    /// <summary>
    /// 选一个随机名字的端点
    /// </summary>
    public static PluginListener Create()
    {
        var name = "xihan-crm-" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        if (OperatingSystem.IsWindows())
        {
            return new PluginListener("npipe", PipePrefix + name);
        }

        // 新建只有本用户能进入的目录；随机名字已经存在说明有人抢先建了，不用
        var directory = Path.Combine(Path.GetTempPath(), name);
        if (Directory.Exists(directory) || File.Exists(directory))
        {
            throw new InvalidOperationException($"The plugin socket directory '{directory}' already exists.");
        }

        Directory.CreateDirectory(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        return new PluginListener("unix", Path.Combine(directory, "plugin.sock"));
    }

    /// <summary>
    /// 套接字建好后收紧为只有本用户可读写
    /// </summary>
    public void Restrict()
    {
        if (Network == "unix" && !OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(Address, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    /// <summary>
    /// 退出时删掉套接字与它的目录
    /// </summary>
    public void Delete()
    {
        if (Network != "unix")
        {
            return;
        }

        try
        {
            File.Delete(Address);
            Directory.Delete(Path.GetDirectoryName(Address)!);
        }
        catch (IOException)
        {
            // 留给临时目录清理
        }
    }

    /// <summary>
    /// 在端点上以 HTTP/2 监听
    /// </summary>
    /// <param name="kestrel">Kestrel 选项</param>
    public void Listen(KestrelServerOptions kestrel)
    {
        ArgumentNullException.ThrowIfNull(kestrel);
        if (Network == "npipe")
        {
            kestrel.ListenNamedPipe(Address[PipePrefix.Length..], static listen => listen.Protocols = HttpProtocols.Http2);
        }
        else
        {
            kestrel.ListenUnixSocket(Address, static listen => listen.Protocols = HttpProtocols.Http2);
        }
    }
}
