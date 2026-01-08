#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOAuthCodeDto
// Guid:f8a9b0c1-d2e3-4567-8901-234f56789012
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>


#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOAuthCodeDto
// Guid:f8a9b0c1-d2e3-4567-8901-234f56789012
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Dtos.Base;

namespace XiHan.BasicApp.Rbac.Services.Dtos;

/// <summary>
/// 系统 OAuth 授权码创建 DTO
/// </summary>
public class SysOAuthCodeCreateDto : RbacCreationDtoBase
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
    public long UserId { get; set; }

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
/// 系统 OAuth 授权码更新 DTO
/// </summary>
public class SysOAuthCodeUpdateDto : RbacUpdateDtoBase
{
    /// <summary>
    /// 是否已使用
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// 使用时间
    /// </summary>
    public DateTimeOffset? UsedAt { get; set; }
}

/// <summary>
/// 系统 OAuth 授权码查询 DTO
/// </summary>
public class SysOAuthCodeGetDto : RbacFullAuditedDtoBase
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
    public long UserId { get; set; }

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
