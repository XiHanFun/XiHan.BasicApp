// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Configuration;

namespace XiHan.BasicApp.Realtime;

/// <summary>
/// 在线状态变化的发布参数
/// </summary>
/// <remarks>
/// 来自配置节 <see cref="ConfigurationSection"/>，不写即用缺省值；写错（不是 <c>hh:mm:ss</c> 的正时长）启动即失败
/// </remarks>
public sealed record RealtimePresenceOptions
{
    /// <summary>
    /// 配置节
    /// </summary>
    public const string ConfigurationSection = "BasicApp:Realtime:Presence";

    /// <summary>
    /// 最后一条连接断开之后多久才算离线
    /// </summary>
    /// <remarks>
    /// 宽限之内又连上（刷新页面、断线重连）对别人来说一直在线，不发布变化
    /// </remarks>
    public TimeSpan OfflineGrace { get; init; } = TimeSpan.FromSeconds(8);

    /// <summary>
    /// 从配置读取
    /// </summary>
    /// <param name="configuration">配置</param>
    /// <returns>参数</returns>
    public static RealtimePresenceOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection(ConfigurationSection);
        var defaults = new RealtimePresenceOptions();
        return new RealtimePresenceOptions
        {
            OfflineGrace = RealtimeSignalROptions.Duration(section, nameof(OfflineGrace), defaults.OfflineGrace),
        };
    }
}
