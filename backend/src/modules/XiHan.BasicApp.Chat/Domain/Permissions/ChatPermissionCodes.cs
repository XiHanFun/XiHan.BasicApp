// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Chat.Domain.Permissions;

/// <summary>
/// 在线聊天权限码常量
/// </summary>
public static class ChatPermissionCodes
{
    /// <summary>
    /// 模块编码
    /// </summary>
    public const string Module = "chat";

    /// <summary>查看聊天（会话列表/消息历史）。</summary>
    public const string Read = "chat:read";

    /// <summary>发送聊天消息（含撤回自己的消息）。</summary>
    public const string Send = "chat:send";

    /// <summary>聊天会话管理（建群/成员管理）。</summary>
    public const string Manage = "chat:manage";

    /// <summary>聊天审计（管理侧跨会话消息查询）。</summary>
    public const string Audit = "chat:audit";

    /// <summary>
    /// 全部权限码
    /// </summary>
    public static readonly IReadOnlyList<string> All =
    [
        Read, Send, Manage, Audit
    ];

    /// <summary>
    /// 可授予租户的权限码（聊天无平台专属码，与全集一致）
    /// </summary>
    public static readonly IReadOnlyList<string> TenantGrantable =
    [
        Read, Send, Manage, Audit
    ];
}
