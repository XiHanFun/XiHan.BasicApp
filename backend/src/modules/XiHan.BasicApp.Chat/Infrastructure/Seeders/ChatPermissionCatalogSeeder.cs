// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Chat.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Infrastructure.Seeders;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Chat.Infrastructure.Seeders;

/// <summary>
/// 聊天权限目录：查看、发送、会话管理、审计四个功能权限
/// </summary>
/// <remarks>平台与租户都能聊天（两侧生效）。</remarks>
public sealed class ChatPermissionCatalogSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<ChatPermissionCatalogSeeder> logger,
    IServiceProvider serviceProvider)
    : PermissionCatalogSeederBase(clientResolver, logger, serviceProvider)
{
    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => SeedOrders.PermissionCatalog + 40;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Chat]权限目录";

    /// <summary>
    /// 模块编码
    /// </summary>
    public override string ModuleCode => ChatPermissionCodes.Module;

    /// <summary>
    /// 本模块的权限
    /// </summary>
    public override IReadOnlyList<PermissionSeed> Permissions { get; } =
    [
        new(ChatPermissionCodes.Read, "聊天查看", "查看当前用户的聊天会话列表与消息历史", ChatPermissionCodes.Module, PermissionSide.Both, true, 3300),
        new(ChatPermissionCodes.Send, "聊天发送", "在所属会话内发送消息与撤回自己的消息", ChatPermissionCodes.Module, PermissionSide.Both, true, 3301),
        new(ChatPermissionCodes.Manage, "聊天会话管理", "创建群聊、添加/移除群成员", ChatPermissionCodes.Module, PermissionSide.Both, true, 3302),
        new(ChatPermissionCodes.Audit, "聊天审计", "管理侧跨会话查询聊天消息（合规审计）", ChatPermissionCodes.Module, PermissionSide.Both, false, 3303),
    ];
}
