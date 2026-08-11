// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Chat.Domain.Configurations;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Infrastructure.Seeders.System;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Chat.Infrastructure.Seeders.System;

/// <summary>
/// 聊天模块系统参数种子数据
/// </summary>
/// <remarks>补齐清理任务与敏感词拦截消费的两个全局配置键，运营可在参数配置页调整。</remarks>
public sealed class ChatConfigurationSeeder : PlatformDataSeederBase
{
    /// <summary>
    /// 配置定义（名称、键、种子值、数据类型、描述、排序）
    /// </summary>
    private static readonly (string Name, string Key, string Value, ConfigDataType DataType, string Description, int Sort)[] Definitions =
    [
        ("聊天消息保留天数", ChatConfigKeys.RetentionDays, "365", ConfigDataType.Number, "聊天消息物理删除前的保留天数（清理任务每日执行）", 200),
        ("聊天敏感词词库", ChatConfigKeys.SensitiveWords, "", ConfigDataType.String, "换行/中英文逗号/分号分隔的敏感词集合，空=关闭拦截", 201)
    ];

    /// <summary>
    /// 构造函数
    /// </summary>
    public ChatConfigurationSeeder(ISqlSugarClientResolver clientResolver, ILogger<ChatConfigurationSeeder> logger, IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 404;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Chat]系统参数种子数据";

    /// <summary>
    /// 种子数据实现（已存在不覆盖：允许运营调整）
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var client = DbClient;
        var keys = Definitions.Select(d => d.Key).ToList();
        var existingKeys = (await client.Queryable<SysConfig>()
                .Where(c => c.TenantId == 0 && keys.Contains(c.ConfigKey))
                .ToListAsync())
            .Select(c => c.ConfigKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var addList = Definitions
            .Where(d => !existingKeys.Contains(d.Key))
            .Select(d => new SysConfig
            {
                TenantId = 0,
                ConfigName = d.Name,
                ConfigGroup = ChatConfigKeys.Group,
                ConfigKey = d.Key,
                ConfigValue = d.Value,
                DefaultValue = d.Value,
                ConfigType = ConfigType.Feature,
                DataType = d.DataType,
                ConfigDescription = d.Description,
                Status = EnableStatus.Enabled,
                Sort = d.Sort,
                Remark = "系统初始化聊天模块参数"
            })
            .ToList();

        if (addList.Count == 0)
        {
            Logger.LogInformation("聊天参数数据已存在，跳过种子数据");
            return;
        }

        await BulkInsertAsync(addList);
        Logger.LogInformation("成功初始化 {Count} 个聊天参数", addList.Count);
    }
}
