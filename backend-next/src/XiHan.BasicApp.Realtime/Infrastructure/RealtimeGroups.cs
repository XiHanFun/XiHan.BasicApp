// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Realtime.Infrastructure;

/// <summary>
/// 连接所在的 SignalR 组。
/// </summary>
/// <remarks>
/// 每条连接建立时加入自己的会话组、用户组、用户在当前侧的组与当前侧的组，推送按组发出（接背板后跨实例同样成立）。
/// </remarks>
internal static class RealtimeGroups
{
    public static string Session(Guid sessionId) => $"s:{sessionId:N}";

    public static string User(Guid userId) => $"u:{userId:N}";

    public static string UserIn(string side, Guid userId) => $"u:{side}:{userId:N}";

    public static string Side(string side) => $"t:{side}";

    /// <summary>
    /// 一条连接所在的全部组。
    /// </summary>
    public static string[] Of(Guid sessionId, Guid userId, string side) => [Session(sessionId), User(userId), UserIn(side, userId), Side(side)];
}
