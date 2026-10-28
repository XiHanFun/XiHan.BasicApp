// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Identity.Operations.Auth;

/// <summary>
/// <c>auth.captcha</c> 的请求：没有输入。
/// </summary>
public sealed record CaptchaRequest;

/// <summary>
/// 一个图形验证码：登录时把标识与用户看到的码一起提交。
/// </summary>
/// <param name="CaptchaId">验证码标识。</param>
/// <param name="Image">SVG 图片（<c>data:</c> 链接，直接放进 <c>img</c>）。</param>
/// <param name="ExpiresInSeconds">有效秒数。</param>
public sealed record CaptchaResponse(string CaptchaId, string Image, int ExpiresInSeconds);

/// <summary>
/// <c>auth.login-config</c> 的请求：没有输入。
/// </summary>
public sealed record LoginConfigRequest;

/// <summary>
/// 登录页的配置：可用的登录方式、第三方登录与是否要求图形验证码。
/// </summary>
/// <param name="LoginMethods">登录方式（目前只有 <c>password</c>；验证码登录随 Notification、第三方登录随 OpenPlatform 加入）。</param>
/// <param name="OAuthProviders">第三方登录提供方（目前为空）。</param>
/// <param name="CaptchaEnabled">密码登录是否要求图形验证码。</param>
public sealed record LoginConfigResponse(IReadOnlyList<string> LoginMethods, IReadOnlyList<LoginProviderResponse> OAuthProviders, bool CaptchaEnabled);

/// <summary>
/// 一个第三方登录提供方。
/// </summary>
/// <param name="Name">提供方标识。</param>
/// <param name="DisplayName">显示名。</param>
public sealed record LoginProviderResponse(string Name, string DisplayName);
