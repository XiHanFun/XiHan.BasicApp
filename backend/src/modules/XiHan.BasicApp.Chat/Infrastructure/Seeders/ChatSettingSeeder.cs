// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Chat.Domain.Configurations;
using XiHan.BasicApp.Saas.Infrastructure.Seeders;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Chat.Infrastructure.Seeders;

/// <summary>
/// 聊天参数配置：聊天策略（消息保留天数、敏感词）
/// </summary>
public sealed class ChatSettingSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<ChatSettingSeeder> logger,
    IServiceProvider serviceProvider)
    : SettingSeederBase(clientResolver, logger, serviceProvider)
{
    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => SeedOrders.PlatformData + 40;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Chat]参数配置";

    /// <summary>
    /// 参数配置声明
    /// </summary>
    public override IReadOnlyList<SettingSeed> Settings { get; } =
    [
        SettingSeed.Json(
            ChatConfigKeys.Policy, "聊天策略", ChatConfigKeys.Group,
            new ChatPolicySettings(), new ChatPolicySettings(),
            "retentionDays：消息保留天数（清理任务物理删除更早的消息）；sensitiveWords：敏感词数组，空表示不拦截",
            10),
    ];
}
