#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthCodeDto
// Guid:f1g2h3i4-j5k6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 18:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Services.Base.Dtos;

namespace XiHan.BasicApp.Rbac.Services.OAuthCodes.Dtos;

/// <summary>
/// OAuth授权码 DTO
/// </summary>
public class OAuthCodeDto : RbacFullAuditedDtoBase
{
    /// <summary>
    /// 授权码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 客户端ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// 用户ID
    /// </summary>
    public XiHanBasicAppIdType UserId { get; set; }

    /// <summary>
    /// 重定向URI
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// 权限范围
    /// </summary>
    public string? Scopes { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// 质询码
    /// </summary>
    public string? CodeChallenge { get; set; }

    /// <summary>
    /// 质询方法
    /// </summary>
    public string? CodeChallengeMethod { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// 是否已使用
    /// </summary>
    public bool IsUsed { get; set; } = false;

    /// <summary>
    /// 使用时间
    /// </summary>
    public DateTimeOffset? UsedAt { get; set; }
}

/// <summary>
/// 创建OAuth授权码 DTO
/// </summary>
public class CreateOAuthCodeDto : RbacCreationDtoBase
{
    /// <summary>
    /// 授权码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 客户端ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// 用户ID
    /// </summary>
    public XiHanBasicAppIdType UserId { get; set; }

    /// <summary>
    /// 重定向URI
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// 权限范围
    /// </summary>
    public string? Scopes { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// 质询码
    /// </summary>
    public string? CodeChallenge { get; set; }

    /// <summary>
    /// 质询方法
    /// </summary>
    public string? CodeChallengeMethod { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }
}

/// <summary>
/// 更新OAuth授权码 DTO
/// </summary>
public class UpdateOAuthCodeDto : RbacUpdateDtoBase
{
    /// <summary>
    /// 是否已使用
    /// </summary>
    public bool? IsUsed { get; set; }

    /// <summary>
    /// 使用时间
    /// </summary>
    public DateTimeOffset? UsedAt { get; set; }
}

