// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Realtime;

/// <summary>
/// Hub 路径。
/// </summary>
public static class RealtimeHubs
{
    /// <summary>
    /// 通知 Hub：强制下线、站内通知、后台任务进度、用户设置变更都经它推送。
    /// </summary>
    public const string Notification = "/hubs/notification";

    /// <summary>
    /// 全部 Hub 的路径前缀。
    /// </summary>
    /// <remarks>
    /// 访问令牌只在这个前缀下从查询串 <c>access_token</c> 读取（WebSocket 与 SSE 带不了请求头）。
    /// </remarks>
    public const string Prefix = "/hubs";
}

/// <summary>
/// 服务端调用的客户端方法名。
/// </summary>
/// <remarks>
/// 与前端壳层 <c>use-signalr-integration</c> 订阅的一致，<c>contracts export</c> 一并输出 <c>realtime.json</c>。
/// </remarks>
public static class RealtimeMethods
{
    /// <summary>
    /// 会话被吊销：载荷带原因与目标会话，客户端核对自己的 <c>sid</c> 后退出登录。
    /// </summary>
    public const string ForceLogout = "ForceLogout";

    /// <summary>
    /// 收到站内通知。
    /// </summary>
    public const string ReceiveNotification = "ReceiveNotification";

    /// <summary>
    /// 后台任务进度（导出、通知发布）。
    /// </summary>
    public const string TaskProgress = "TaskProgress";

    /// <summary>
    /// 同一用户在别的端改了设置（偏好、收藏、页面设置）。
    /// </summary>
    public const string UserSettingChanged = "UserSettingChanged";

    /// <summary>
    /// 全部方法名（按字母序）。
    /// </summary>
    public static IReadOnlyList<string> All { get; } = [ForceLogout, ReceiveNotification, TaskProgress, UserSettingChanged];
}
