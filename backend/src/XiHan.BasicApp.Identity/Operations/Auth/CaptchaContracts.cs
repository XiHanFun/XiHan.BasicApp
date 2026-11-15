// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Identity.Domain.Services;

namespace XiHan.BasicApp.Identity.Operations.Auth;

/// <summary>
/// <c>auth.captcha</c> 的请求：没有输入
/// </summary>
public sealed record CaptchaRequest;

/// <summary>
/// 一个图形验证码：登录时把标识与用户看到的码一起提交
/// </summary>
/// <param name="CaptchaId">验证码标识</param>
/// <param name="Image">SVG 图片（<c>data:</c> 链接，直接放进 <c>img</c>）</param>
/// <param name="ExpiresInSeconds">有效秒数</param>
public sealed record CaptchaResponse(string CaptchaId, string Image, int ExpiresInSeconds);

/// <summary>
/// <c>auth.login-config</c> 的请求：没有输入
/// </summary>
public sealed record LoginConfigRequest;

/// <summary>
/// 登录页的配置：登录方式、第三方登录、验证码、注册与密码规则
/// </summary>
/// <param name="LoginMethods">登录方式：<c>password</c> 总在，<c>email</c> / <c>phone</c>（验证码登录）按系统参数 <c>identity.login-methods</c> 开放</param>
/// <param name="OAuthProviders">第三方登录提供方（配置 <c>BasicApp:Auth:OAuth</c> 里启用且填了 ClientId 的）</param>
/// <param name="CaptchaEnabled">密码登录与注册是否要求图形验证码</param>
/// <param name="RegistrationEnabled">是否开放自助注册（<c>BasicApp:Auth:SelfRegistrationEnabled</c>）</param>
/// <param name="PasswordPolicy">设新密码的规则（注册、找回与改密的表单据此提示）</param>
public sealed record LoginConfigResponse(IReadOnlyList<string> LoginMethods, IReadOnlyList<LoginProviderResponse> OAuthProviders, bool CaptchaEnabled, bool RegistrationEnabled, PasswordPolicyResponse PasswordPolicy);

/// <summary>
/// 设新密码的规则
/// </summary>
/// <remarks>
/// 取系统参数 <c>identity.password</c>，后端设密码时按同一份校验
/// </remarks>
/// <param name="MinLength">最短长度</param>
/// <param name="RequireLetter">须含字母</param>
/// <param name="RequireDigit">须含数字</param>
/// <param name="RequireUppercase">须含大写字母</param>
/// <param name="RequireLowercase">须含小写字母</param>
/// <param name="RequireSymbol">须含字母与数字以外的字符</param>
public sealed record PasswordPolicyResponse(int MinLength, bool RequireLetter, bool RequireDigit, bool RequireUppercase, bool RequireLowercase, bool RequireSymbol)
{
    /// <summary>
    /// 由密码设置构造
    /// </summary>
    public static PasswordPolicyResponse From(PasswordSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return new(settings.MinLength, settings.RequireLetter, settings.RequireDigit, settings.RequireUppercase, settings.RequireLowercase, settings.RequireSymbol);
    }
}

/// <summary>
/// 一个第三方登录提供方
/// </summary>
/// <param name="Name">提供方标识</param>
/// <param name="DisplayName">显示名</param>
/// <param name="Type">提供商类型（<c>github</c>、<c>wechat</c> 等规范名，前端据此选图标）</param>
public sealed record LoginProviderResponse(string Name, string DisplayName, string Type);
