// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 机器人配置应用层映射器
/// </summary>
public static class BotConfigApplicationMapper
{
    /// <summary>
    /// 映射机器人配置创建命令
    /// </summary>
    public static BotConfigCreateCommand ToCreateCommand(BotConfigCreateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new BotConfigCreateCommand(
            input.ConfigCode,
            input.ConfigName,
            input.Provider,
            input.WebhookUrl,
            input.Secret,
            input.Keyword,
            input.IsDefault,
            input.IsEnabled,
            input.Sort,
            input.Remark);
    }

    /// <summary>
    /// 映射机器人配置更新命令
    /// </summary>
    public static BotConfigUpdateCommand ToUpdateCommand(BotConfigUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new BotConfigUpdateCommand(
            input.BasicId,
            input.ConfigName,
            input.Provider,
            input.WebhookUrl,
            input.Secret,
            input.Keyword,
            input.Sort,
            input.Remark);
    }

    /// <summary>
    /// 映射机器人配置状态命令
    /// </summary>
    public static BotConfigStatusChangeCommand ToStatusCommand(BotConfigStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new BotConfigStatusChangeCommand(input.BasicId, input.IsEnabled);
    }

    /// <summary>
    /// 映射机器人配置默认变更命令
    /// </summary>
    public static BotConfigDefaultChangeCommand ToDefaultCommand(BotConfigDefaultUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new BotConfigDefaultChangeCommand(input.BasicId);
    }

    /// <summary>
    /// 映射机器人配置列表项（Secret 脱敏，仅标识是否已配置）
    /// </summary>
    /// <param name="config">机器人配置</param>
    /// <returns>机器人配置列表项 DTO</returns>
    public static BotConfigListItemDto ToListItemDto(SysBotConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return new BotConfigListItemDto
        {
            BasicId = config.BasicId,
            ConfigCode = config.ConfigCode,
            ConfigName = config.ConfigName,
            Provider = config.Provider,
            HasSecret = !string.IsNullOrEmpty(config.Secret),
            Keyword = config.Keyword,
            IsDefault = config.IsDefault,
            IsEnabled = config.IsEnabled,
            Sort = config.Sort,
            CreatedTime = config.CreatedTime,
            ModifiedTime = config.ModifiedTime
        };
    }

    /// <summary>
    /// 映射机器人配置详情（Secret 脱敏，仅标识是否已配置）
    /// </summary>
    /// <param name="config">机器人配置</param>
    /// <returns>机器人配置详情 DTO</returns>
    public static BotConfigDetailDto ToDetailDto(SysBotConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return new BotConfigDetailDto
        {
            BasicId = config.BasicId,
            ConfigCode = config.ConfigCode,
            ConfigName = config.ConfigName,
            Provider = config.Provider,
            WebhookUrl = config.WebhookUrl,
            HasSecret = !string.IsNullOrEmpty(config.Secret),
            Keyword = config.Keyword,
            IsDefault = config.IsDefault,
            IsEnabled = config.IsEnabled,
            Sort = config.Sort,
            Remark = config.Remark,
            CreatedTime = config.CreatedTime,
            CreatedId = config.CreatedId,
            CreatedBy = config.CreatedBy,
            ModifiedTime = config.ModifiedTime,
            ModifiedId = config.ModifiedId,
            ModifiedBy = config.ModifiedBy
        };
    }
}
