#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IOAuthConsentAppService
// Guid:4b7e2a39-0c85-4d16-9f23-8e5a3b1c0d74
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

#pragma warning disable CS1591

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// OAuth 授权同意应用服务（已登录用户在同意页授权第三方应用访问其账号）
/// </summary>
public interface IOAuthConsentAppService : IApplicationService
{
    Task<OAuthConsentPreviewDto> ResolveAuthorizationAsync(OAuthAuthorizeRequestDto input, CancellationToken cancellationToken = default);

    Task<OAuthConsentResultDto> AuthorizeAsync(OAuthAuthorizeRequestDto input, CancellationToken cancellationToken = default);
}
