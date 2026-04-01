#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthController
// Guid:a1b2c3d4-5e6f-7890-abcd-ef1234567830
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/02 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using XiHan.BasicApp.Saas.Application.AppServices;
using XiHan.BasicApp.Saas.Application.UseCases.Commands;
using XiHan.Framework.Authentication.OAuth;

namespace XiHan.BasicApp.WebHost.Controllers;

/// <summary>
/// 第三方登录控制器（处理 OAuth 重定向流程）
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OAuthController : ControllerBase
{
    private readonly IAuthAppService _authAppService;
    private readonly OAuthOptions _oauthOptions;

    /// <summary>
    /// 构造函数
    /// </summary>
    public OAuthController(IAuthAppService authAppService, IOptions<OAuthOptions> oauthOptions)
    {
        _authAppService = authAppService;
        _oauthOptions = oauthOptions.Value;
    }

    /// <summary>
    /// 发起第三方登录（重定向到提供商授权页）
    /// </summary>
    /// <param name="provider">提供商名称（google、github、qq）</param>
    /// <param name="tenantId">租户ID（可选）</param>
    [HttpGet("ExternalLogin")]
    public IActionResult ExternalLogin([FromQuery] string provider, [FromQuery] long? tenantId = null)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return BadRequest("提供商名称不能为空");
        }

        var providerConfig = _oauthOptions.Providers
            .FirstOrDefault(p => string.Equals(p.Name, provider, StringComparison.OrdinalIgnoreCase) && p.Enabled);

        if (providerConfig is null)
        {
            return BadRequest($"不支持的登录提供商: {provider}");
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(ExternalLoginCallback), new { provider, tenantId }),
            Items =
            {
                ["provider"] = provider,
                ["tenantId"] = tenantId?.ToString() ?? string.Empty
            }
        };

        // 使用提供商名称作为 AuthenticationScheme 触发 Challenge
        return Challenge(properties, providerConfig.Name);
    }

    /// <summary>
    /// 第三方登录回调（由提供商重定向回来）
    /// </summary>
    /// <param name="provider">提供商名称</param>
    /// <param name="tenantId">租户ID</param>
    [HttpGet("ExternalLoginCallback")]
    public async Task<IActionResult> ExternalLoginCallback([FromQuery] string provider, [FromQuery] long? tenantId = null)
    {
        // 从临时 Cookie 读取第三方登录结果
        var result = await HttpContext.AuthenticateAsync("ExternalCookie");
        if (result?.Succeeded != true || result.Principal is null)
        {
            return Redirect(BuildFrontendErrorUrl("第三方登录认证失败"));
        }

        var claims = result.Principal.Claims.ToList();
        var providerKey = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(providerKey))
        {
            return Redirect(BuildFrontendErrorUrl("无法获取第三方用户标识"));
        }

        var displayName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                          ?? claims.FirstOrDefault(c => c.Type == "urn:github:name")?.Value;
        var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var avatarUrl = claims.FirstOrDefault(c => c.Type == "urn:google:picture")?.Value
                        ?? claims.FirstOrDefault(c => c.Type == "urn:github:avatar_url")?.Value
                        ?? claims.FirstOrDefault(c => c.Type == "urn:qq:figureurl_qq_2")?.Value;

        // 清除临时 Cookie
        await HttpContext.SignOutAsync("ExternalCookie");

        try
        {
            var command = new ExternalLoginCommand
            {
                Provider = provider,
                ProviderKey = providerKey,
                DisplayName = displayName,
                Email = email,
                AvatarUrl = avatarUrl,
                TenantId = tenantId
            };

            var tokenDto = await _authAppService.ExternalLoginAsync(command);

            // 重定向到前端回调页面，通过 URL 参数传递 token
            var callbackUrl = _oauthOptions.FrontendCallbackUrl;
            var redirectUrl = $"{callbackUrl}?accessToken={Uri.EscapeDataString(tokenDto.AccessToken)}" +
                              $"&refreshToken={Uri.EscapeDataString(tokenDto.RefreshToken)}" +
                              $"&expiresIn={tokenDto.ExpiresIn}" +
                              $"&provider={Uri.EscapeDataString(provider)}";
            return Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            return Redirect(BuildFrontendErrorUrl(ex.Message));
        }
    }

    private string BuildFrontendErrorUrl(string error)
    {
        var callbackUrl = _oauthOptions.FrontendCallbackUrl;
        return $"{callbackUrl}?error={Uri.EscapeDataString(error)}";
    }
}
