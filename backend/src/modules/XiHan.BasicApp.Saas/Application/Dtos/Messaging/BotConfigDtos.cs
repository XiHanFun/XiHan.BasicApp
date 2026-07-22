// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 机器人配置分页查询 DTO
/// </summary>
public sealed class BotConfigPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字（配置编码、名称、备注）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 机器人服务商
    /// </summary>
    public BotProviderType? Provider { get; set; }

    /// <summary>
    /// 是否默认机器人
    /// </summary>
    public bool? IsDefault { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool? IsEnabled { get; set; }
}

/// <summary>
/// 机器人配置创建 DTO
/// </summary>
public sealed class BotConfigCreateDto : BasicAppCDto
{
    /// <summary>
    /// 配置编码（租户内唯一）
    /// </summary>
    public string ConfigCode { get; set; } = string.Empty;

    /// <summary>
    /// 配置名称
    /// </summary>
    public string ConfigName { get; set; } = string.Empty;

    /// <summary>
    /// 机器人服务商
    /// </summary>
    public BotProviderType Provider { get; set; } = BotProviderType.DingTalk;

    /// <summary>
    /// Webhook 地址（含凭证的完整地址）
    /// </summary>
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>
    /// 签名秘钥（敏感字段，仅写入；钉钉加签秘钥/飞书签名秘钥，企业微信不使用）
    /// </summary>
    public string? Secret { get; set; }

    /// <summary>
    /// 安全关键词（钉钉/飞书）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 是否默认机器人（同租户同服务商内互斥）
    /// </summary>
    public bool IsDefault { get; set; }

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
/// 机器人配置更新 DTO
/// </summary>
public sealed class BotConfigUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 配置名称
    /// </summary>
    public string ConfigName { get; set; } = string.Empty;

    /// <summary>
    /// 机器人服务商
    /// </summary>
    public BotProviderType Provider { get; set; } = BotProviderType.DingTalk;

    /// <summary>
    /// Webhook 地址（含凭证的完整地址）
    /// </summary>
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>
    /// 签名秘钥（为空表示保留原秘钥）
    /// </summary>
    public string? Secret { get; set; }

    /// <summary>
    /// 安全关键词（钉钉/飞书）
    /// </summary>
    public string? Keyword { get; set; }

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
/// 机器人配置状态更新 DTO
/// </summary>
public sealed class BotConfigStatusUpdateDto
{
    /// <summary>
    /// 机器人配置主键
    /// </summary>
    public long BasicId { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }
}

/// <summary>
/// 机器人配置默认更新 DTO
/// </summary>
public sealed class BotConfigDefaultUpdateDto
{
    /// <summary>
    /// 机器人配置主键
    /// </summary>
    public long BasicId { get; set; }
}

/// <summary>
/// 机器人配置列表项 DTO
/// </summary>
/// <remarks>Secret 为敏感字段不下发，仅以 HasSecret 标识是否已配置</remarks>
public class BotConfigListItemDto : BasicAppDto
{
    /// <summary>
    /// 配置编码
    /// </summary>
    public string ConfigCode { get; set; } = string.Empty;

    /// <summary>
    /// 配置名称
    /// </summary>
    public string ConfigName { get; set; } = string.Empty;

    /// <summary>
    /// 机器人服务商
    /// </summary>
    public BotProviderType Provider { get; set; }

    /// <summary>
    /// 是否已配置签名秘钥
    /// </summary>
    public bool HasSecret { get; set; }

    /// <summary>
    /// 安全关键词（钉钉/飞书）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 是否默认机器人
    /// </summary>
    public bool IsDefault { get; set; }

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
/// 机器人配置详情 DTO
/// </summary>
/// <remarks>Secret 为敏感字段不下发，仅以 HasSecret 标识是否已配置</remarks>
public sealed class BotConfigDetailDto : BotConfigListItemDto
{
    /// <summary>
    /// Webhook 地址（含凭证的完整地址）
    /// </summary>
    public string WebhookUrl { get; set; } = string.Empty;

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
