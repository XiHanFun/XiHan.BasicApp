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
}
