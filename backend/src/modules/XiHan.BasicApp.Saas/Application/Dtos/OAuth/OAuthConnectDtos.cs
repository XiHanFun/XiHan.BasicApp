// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// OAuth 授权请求 DTO（同意页把 /connect/authorize 的查询参数原样上送后端校验/建码）
/// </summary>
public sealed class OAuthAuthorizeRequestDto
{
    /// <summary>
    /// 响应类型（仅支持 code）
    /// </summary>
    public string? ResponseType { get; set; }

    /// <summary>
    /// 客户端ID
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// 重定向URI
    /// </summary>
    public string? RedirectUri { get; set; }

    /// <summary>
    /// 请求的权限范围（空格/逗号分隔）
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// 防 CSRF 状态串（原样回传第三方）
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// PKCE 质询码
    /// </summary>
    public string? CodeChallenge { get; set; }

    /// <summary>
    /// PKCE 质询方法（S256/plain）
    /// </summary>
    public string? CodeChallengeMethod { get; set; }
}

/// <summary>
/// OAuth 同意页预览 DTO（渲染客户端信息与授权范围；非法请求携带错误码）
/// </summary>
public sealed class OAuthConsentPreviewDto
{
    /// <summary>
    /// 请求是否合法
    /// </summary>
    public bool Valid { get; set; }

    /// <summary>
    /// OAuth 错误码（非法时）
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// 错误描述
    /// </summary>
    public string? ErrorDescription { get; set; }

    /// <summary>
    /// 客户端ID
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// 应用名称
    /// </summary>
    public string? AppName { get; set; }

    /// <summary>
    /// 应用描述
    /// </summary>
    public string? AppDescription { get; set; }

    /// <summary>
    /// 应用Logo
    /// </summary>
    public string? Logo { get; set; }

    /// <summary>
    /// 应用主页
    /// </summary>
    public string? Homepage { get; set; }

    /// <summary>
    /// 是否跳过授权确认（可直接静默授权）
    /// </summary>
    public bool SkipConsent { get; set; }

    /// <summary>
    /// 解析后的授权范围列表
    /// </summary>
    public List<string> Scopes { get; set; } = [];
}

/// <summary>
/// OAuth 同意结果 DTO（成功返回携带 code 的最终跳转地址）
/// </summary>
public sealed class OAuthConsentResultDto
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// OAuth 错误码（失败时）
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// 错误描述
    /// </summary>
    public string? ErrorDescription { get; set; }

    /// <summary>
    /// 最终跳转地址（redirect_uri?code=...&amp;state=...）
    /// </summary>
    public string? RedirectUri { get; set; }
}
