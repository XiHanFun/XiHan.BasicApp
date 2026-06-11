#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ProfileApiCredentialDtos
// Guid:d7b3f9e2-4c81-4a56-9e0d-1f8a5c2b7d49
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 个人 API 凭证列表项 DTO
/// </summary>
public sealed class ProfileApiCredentialDto
{
    /// <summary>
    /// 凭证主键
    /// </summary>
    public long BasicId { get; set; }

    /// <summary>
    /// 凭证名称
    /// </summary>
    public string CredentialName { get; set; } = string.Empty;

    /// <summary>
    /// 应用键
    /// </summary>
    public string AppKey { get; set; } = string.Empty;

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; }

    /// <summary>
    /// 最后使用时间
    /// </summary>
    public DateTimeOffset? LastUsedTime { get; set; }

    /// <summary>
    /// 过期时间（为空表示永不过期）
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }
}

/// <summary>
/// 个人 API 凭证创建 DTO
/// </summary>
public sealed class ProfileApiCredentialCreateDto
{
    /// <summary>
    /// 凭证名称（用途备注，缺省为"默认凭证"）
    /// </summary>
    public string? CredentialName { get; set; }
}

/// <summary>
/// 个人 API 凭证密钥 DTO（明文 Secret 仅创建/滚动时返回一次）
/// </summary>
public sealed class ProfileApiCredentialSecretDto
{
    /// <summary>
    /// 凭证主键
    /// </summary>
    public long BasicId { get; set; }

    /// <summary>
    /// 应用键
    /// </summary>
    public string AppKey { get; set; } = string.Empty;

    /// <summary>
    /// 应用密钥明文（仅本次返回，请立即保存）
    /// </summary>
    public string AppSecret { get; set; } = string.Empty;
}

/// <summary>
/// 个人 API 凭证主键 DTO
/// </summary>
public sealed class ProfileApiCredentialIdDto
{
    /// <summary>
    /// 凭证主键
    /// </summary>
    public long BasicId { get; set; }
}

/// <summary>
/// 个人 API 凭证状态变更 DTO
/// </summary>
public sealed class ProfileApiCredentialStatusDto
{
    /// <summary>
    /// 凭证主键
    /// </summary>
    public long BasicId { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; }
}
