#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AiProviderDtos
// Guid:a11c0de0-4001-4a10-9a00-00000000ai41
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/05 14:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

#pragma warning disable CS1591

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.AI.Application.Dtos;

/// <summary>
/// AI Provider 创建 DTO
/// </summary>
public sealed class AiProviderCreateDto
{
    public string ConfigCode { get; set; } = string.Empty;
    public string ConfigName { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? EmbeddingModel { get; set; }
    public string? BaseUrl { get; set; }

    /// <summary>API 密钥（明文提交，服务端加密落库；写入专用，永不回读）</summary>
    public string? ApiKey { get; set; }

    public int? MaxOutputTokens { get; set; }
    public float? Temperature { get; set; }
    public int? TimeoutSeconds { get; set; }
    public string? ExtraJson { get; set; }
    public bool IsDefault { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int Sort { get; set; }
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;
    public string? Remark { get; set; }
}

/// <summary>
/// AI Provider 更新 DTO
/// </summary>
/// <remarks>ConfigCode 不可变，不在此 DTO；ApiKey 为空表示保留原密钥。</remarks>
public sealed class AiProviderUpdateDto : BasicAppUDto
{
    public string ConfigName { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? EmbeddingModel { get; set; }
    public string? BaseUrl { get; set; }

    /// <summary>API 密钥（留空保留原密钥；非空则替换并加密）</summary>
    public string? ApiKey { get; set; }

    public int? MaxOutputTokens { get; set; }
    public float? Temperature { get; set; }
    public int? TimeoutSeconds { get; set; }
    public string? ExtraJson { get; set; }
    public bool IsDefault { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int Sort { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// AI Provider 状态更新 DTO
/// </summary>
public sealed class AiProviderStatusUpdateDto : BasicAppDto
{
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;
    public string? Remark { get; set; }
}

/// <summary>
/// AI Provider 单体动作 DTO（设为默认 / 测试连接，POST 携带主键）
/// </summary>
public sealed class AiProviderActionDto : BasicAppDto
{
}

/// <summary>
/// AI Provider 分页查询 DTO
/// </summary>
public sealed class AiProviderPageQueryDto : BasicAppPRDto
{
    public string? Keyword { get; set; }
    public string? Provider { get; set; }
    public bool? IsDefault { get; set; }
    public bool? IsEnabled { get; set; }
    public EnableStatus? Status { get; set; }
}

/// <summary>
/// AI Provider 列表项 DTO（不含密钥）
/// </summary>
public class AiProviderListItemDto : BasicAppDto
{
    public string ConfigCode { get; set; } = string.Empty;
    public string ConfigName { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? EmbeddingModel { get; set; }
    public string? BaseUrl { get; set; }
    public int? MaxOutputTokens { get; set; }
    public float? Temperature { get; set; }
    public int? TimeoutSeconds { get; set; }
    public bool IsDefault { get; set; }
    public bool IsEnabled { get; set; }

    /// <summary>是否已配置密钥（仅暴露布尔标志，不回读明文/密文）</summary>
    public bool HasApiKey { get; set; }

    public int Sort { get; set; }
    public EnableStatus Status { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset? ModifiedTime { get; set; }
}

/// <summary>
/// AI Provider 详情 DTO（不含密钥明文）
/// </summary>
public sealed class AiProviderDetailDto : AiProviderListItemDto
{
    public string? ExtraJson { get; set; }
    public string? Remark { get; set; }
    public long? CreatedId { get; set; }
    public string? CreatedBy { get; set; }
    public long? ModifiedId { get; set; }
    public string? ModifiedBy { get; set; }
}

/// <summary>
/// AI Provider 连接测试结果 DTO
/// </summary>
public sealed class AiProviderTestConnectionResultDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public long LatencyMs { get; set; }
    public string? Model { get; set; }
}
