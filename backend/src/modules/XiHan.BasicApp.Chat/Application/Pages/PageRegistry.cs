// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Chat.Domain.Permissions;
using XiHan.BasicApp.Saas.Application.Pages;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Chat.Application.Pages;

/// <summary>
/// 聊天模块页面登记表 — 本模块页面的单一事实源，菜单种子数据从此处生成
/// </summary>
/// <remarks>
/// 沿用 Saas 侧 <see cref="PageDescriptor"/> 的一致性约定（Component 对应 src/views 目录、
/// I18nKey 为 menu.{Code 中 . 与 - 替换为 _} 并在前端 menu.ts 维护双语文案）。
/// 聊天目录由本模块自持；页面路由路径与组件路径维持 message/chat 现状，删除模块即整目录消失。
/// </remarks>
public static class PageRegistry
{
    /// <summary>
    /// 聊天目录码
    /// </summary>
    public const string ChatDirectoryCode = "chat";

    /// <summary>
    /// 聊天目录定义
    /// </summary>
    public static PageDescriptor ChatDirectory { get; } =
        new(ChatDirectoryCode, "聊天", "menu.chat", MenuType.Directory, "/chat", "Chat", null, null, null, "lucide:messages-square", 480, "/message/chat");

    /// <summary>
    /// 所有已登记页面（父目录必须排在子项之前，种子依顺序解析 ParentId）
    /// </summary>
    public static IReadOnlyList<PageDescriptor> All { get; } =
    [
        ChatDirectory,
        new("message.chat", "在线聊天", "menu.message_chat", MenuType.Menu, "/message/chat", "MessageChat", "message/chat/index", ChatDirectoryCode, ChatPermissionCodes.Read, "lucide:messages-square", 481),
        new("message.chat-audit", "聊天审计", "menu.message_chat_audit", MenuType.Menu, "/message/chat-audit", "MessageChatAudit", "message/chat-audit/index", ChatDirectoryCode, ChatPermissionCodes.Audit, "lucide:shield-check", 482),
    ];

    /// <summary>
    /// 页面内按钮
    /// </summary>
    public static IReadOnlyList<ButtonDescriptor> Buttons { get; } =
    [
        new("message.chat.send", "发送", "message.chat", ChatPermissionCodes.Send, 1),
        new("message.chat.manage", "会话管理", "message.chat", ChatPermissionCodes.Manage, 2),
    ];
}
