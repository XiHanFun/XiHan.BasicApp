#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IAuthTokenIssueService
// Guid:ba155e91-6fcf-4523-9c1e-819de334ce84
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 认证令牌签发服务
/// </summary>
public interface IAuthTokenIssueService
{
    /// <summary>
    /// 签发访问令牌
    /// </summary>
    AuthAccessTokenIssueResult IssueAccessToken(AuthAccessTokenIssueCommand command);

    /// <summary>
    /// 刷新访问令牌
    /// </summary>
    LoginTokenDto RefreshAccessToken(string accessToken, string refreshToken);

    /// <summary>
    /// 从访问令牌解析用户身份（不校验有效期，仅用于审计归属），解析失败返回 null
    /// </summary>
    AuthTokenIdentity? ResolveTokenIdentity(string accessToken);
}

/// <summary>
/// 访问令牌中的用户身份信息
/// </summary>
/// <param name="UserId">用户标识</param>
/// <param name="UserName">用户名</param>
/// <param name="TenantId">租户标识</param>
public sealed record AuthTokenIdentity(long? UserId, string? UserName, long? TenantId, string? SessionId = null);
