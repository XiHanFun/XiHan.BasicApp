// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// OAuth 授权同意应用服务
/// </summary>
/// <remarks>
/// 同意流走正常动态 API 管道（SPA + Bearer + JSON），仅要求已登录（继承基类 <c>[Authorize]</c>，不加具体权限码——
/// 任何登录用户均可授权第三方访问自身账号）。协议逻辑委托普通 Scoped 的 <see cref="IOAuthServerService"/>。
/// </remarks>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "OAuth 授权", RouteTemplate = "api/OAuthConnect")]
public sealed class OAuthConsentAppService
    : SaasApplicationService, IOAuthConsentAppService
{
    private readonly IOAuthServerService _oauthServerService;

    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 构造函数
    /// </summary>
    public OAuthConsentAppService(IOAuthServerService oauthServerService, ICurrentUser currentUser)
    {
        _oauthServerService = oauthServerService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 校验授权请求并返回同意页所需的客户端信息与授权范围
    /// </summary>
    [HttpPost]
    public async Task<OAuthConsentPreviewDto> ResolveAuthorizationAsync(OAuthAuthorizeRequestDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var validation = await _oauthServerService.ValidateAuthorizeRequestAsync(ToRequest(input), cancellationToken);
        return new OAuthConsentPreviewDto
        {
            Valid = validation.Success,
            Error = validation.Error,
            ErrorDescription = validation.ErrorDescription,
            ClientId = validation.Client?.ClientId,
            AppName = validation.Client?.AppName,
            AppDescription = validation.Client?.AppDescription,
            Logo = validation.Client?.Logo,
            Homepage = validation.Client?.Homepage,
            SkipConsent = validation.Client?.SkipConsent ?? false,
            Scopes = [.. validation.Scopes]
        };
    }

    /// <summary>
    /// 用户同意授权：创建授权码并返回携带 code 的最终跳转地址
    /// </summary>
    [HttpPost]
    [UnitOfWork(true)]
    public async Task<OAuthConsentResultDto> AuthorizeAsync(OAuthAuthorizeRequestDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var userId = _currentUser.UserId ?? throw new InvalidOperationException("当前用户未登录。");
        var result = await _oauthServerService.CreateAuthorizationCodeAsync(
            new OAuthCreateCodeCommand(ToRequest(input), userId, _currentUser.TenantId),
            cancellationToken);

        return new OAuthConsentResultDto
        {
            Success = result.Success,
            Error = result.Error,
            ErrorDescription = result.ErrorDescription,
            RedirectUri = result.RedirectUri
        };
    }

    private static OAuthAuthorizeRequest ToRequest(OAuthAuthorizeRequestDto input)
    {
        return new OAuthAuthorizeRequest(
            input.ResponseType,
            input.ClientId,
            input.RedirectUri,
            input.Scope,
            input.State,
            input.CodeChallenge,
            input.CodeChallengeMethod);
    }
}
