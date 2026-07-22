// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// OAuth 授权码分页查询 DTO
/// </summary>
public sealed class OAuthCodePageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 客户端 ID
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// 用户主键
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 是否已使用
    /// </summary>
    public bool? IsUsed { get; set; }

    /// <summary>
    /// 是否已过期
    /// </summary>
    public bool? IsExpired { get; set; }

    /// <summary>
    /// 过期开始时间
    /// </summary>
    public DateTimeOffset? ExpirationTimeStart { get; set; }

    /// <summary>
    /// 过期结束时间
    /// </summary>
    public DateTimeOffset? ExpirationTimeEnd { get; set; }

    /// <summary>
    /// 创建开始时间
    /// </summary>
    public DateTimeOffset? CreatedTimeStart { get; set; }

    /// <summary>
    /// 创建结束时间
    /// </summary>
    public DateTimeOffset? CreatedTimeEnd { get; set; }
}
