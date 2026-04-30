#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthCodePageQueryDto
// Guid:2bdc3355-6722-40b5-9495-05d97798aa5f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
    public DateTimeOffset? ExpiresTimeStart { get; set; }

    /// <summary>
    /// 过期结束时间
    /// </summary>
    public DateTimeOffset? ExpiresTimeEnd { get; set; }

    /// <summary>
    /// 创建开始时间
    /// </summary>
    public DateTimeOffset? CreatedTimeStart { get; set; }

    /// <summary>
    /// 创建结束时间
    /// </summary>
    public DateTimeOffset? CreatedTimeEnd { get; set; }
}
