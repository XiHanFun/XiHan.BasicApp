#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TelegramBotDtos
// Guid:0ba87adc-aa11-45e3-bb9f-4c2f2e2038e9
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 18:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// Telegram 机器人分页查询 DTO
/// </summary>
public sealed class TelegramBotPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字（机器人名称、备注）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool? IsEnabled { get; set; }
}

/// <summary>
/// Telegram 机器人创建 DTO
/// </summary>
public sealed class TelegramBotCreateDto : BasicAppCDto
{
    /// <summary>
    /// 机器人名称（租户内唯一）
    /// </summary>
    public string BotName { get; set; } = string.Empty;

    /// <summary>
    /// Bot Token（敏感字段，仅写入）
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 超级管理员 Telegram 用户 Id 列表（逗号分隔）
    /// </summary>
    public string? AdminUsers { get; set; }

    /// <summary>
    /// 允许的群组 ChatId 白名单（逗号分隔；空 = 拒收所有群消息，fail-closed）
    /// </summary>
    public string? AllowedGroupChatIds { get; set; }

    /// <summary>
    /// 允许执行的命令白名单（逗号分隔；空 = 不限制命令）
    /// </summary>
    public string? AllowedCommands { get; set; }

    /// <summary>
    /// 是否启用兜底回复
    /// </summary>
    public bool EnableFallbackReply { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 排序（数字越小越靠前）
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// Telegram 机器人更新 DTO
/// </summary>
public sealed class TelegramBotUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 机器人名称（租户内唯一）
    /// </summary>
    public string BotName { get; set; } = string.Empty;

    /// <summary>
    /// Bot Token（为空表示保留原 Token）
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// 超级管理员 Telegram 用户 Id 列表（逗号分隔）
    /// </summary>
    public string? AdminUsers { get; set; }

    /// <summary>
    /// 允许的群组 ChatId 白名单（逗号分隔；空 = 拒收所有群消息，fail-closed）
    /// </summary>
    public string? AllowedGroupChatIds { get; set; }

    /// <summary>
    /// 允许执行的命令白名单（逗号分隔；空 = 不限制命令）
    /// </summary>
    public string? AllowedCommands { get; set; }

    /// <summary>
    /// 是否启用兜底回复
    /// </summary>
    public bool EnableFallbackReply { get; set; }

    /// <summary>
    /// 排序（数字越小越靠前）
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// Telegram 机器人状态更新 DTO
/// </summary>
public sealed class TelegramBotStatusUpdateDto
{
    /// <summary>
    /// Telegram 机器人主键
    /// </summary>
    public long BasicId { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }
}

/// <summary>
/// Telegram 机器人列表项 DTO
/// </summary>
/// <remarks>Token 为敏感字段不下发，仅以 HasToken 标识是否已配置</remarks>
public class TelegramBotListItemDto : BasicAppDto
{
    /// <summary>
    /// 机器人名称
    /// </summary>
    public string BotName { get; set; } = string.Empty;

    /// <summary>
    /// 是否已配置 Token
    /// </summary>
    public bool HasToken { get; set; }

    /// <summary>
    /// 是否启用兜底回复
    /// </summary>
    public bool EnableFallbackReply { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}

/// <summary>
/// Telegram 机器人详情 DTO
/// </summary>
/// <remarks>Token 为敏感字段不下发，仅以 HasToken 标识是否已配置</remarks>
public sealed class TelegramBotDetailDto : TelegramBotListItemDto
{
    /// <summary>
    /// 超级管理员 Telegram 用户 Id 列表（逗号分隔）
    /// </summary>
    public string? AdminUsers { get; set; }

    /// <summary>
    /// 允许的群组 ChatId 白名单（逗号分隔；空 = 拒收所有群消息，fail-closed）
    /// </summary>
    public string? AllowedGroupChatIds { get; set; }

    /// <summary>
    /// 允许执行的命令白名单（逗号分隔；空 = 不限制命令）
    /// </summary>
    public string? AllowedCommands { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 创建人主键
    /// </summary>
    public long? CreatedId { get; set; }

    /// <summary>
    /// 创建人
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// 修改人主键
    /// </summary>
    public long? ModifiedId { get; set; }

    /// <summary>
    /// 修改人
    /// </summary>
    public string? ModifiedBy { get; set; }
}
