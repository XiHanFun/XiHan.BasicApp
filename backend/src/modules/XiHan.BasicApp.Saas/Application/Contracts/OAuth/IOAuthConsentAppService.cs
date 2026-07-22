// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
