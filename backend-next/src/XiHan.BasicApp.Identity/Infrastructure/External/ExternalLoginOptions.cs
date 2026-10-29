// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Configuration;

namespace XiHan.BasicApp.Identity.Infrastructure.External;

/// <summary>
/// 第三方登录的配置（<c>BasicApp:Auth:OAuth</c>）。
/// </summary>
/// <remarks>
/// 4.x 是 <c>XiHan:Authentication:OAuth</c>。不分租户、不存库。启用且填了 ClientId 的提供商才登记、才出现在登录页
/// （4.x 登录页的按钮另在系统参数里列，与配置只靠名称对齐，占位凭据的照样展示、点了必然失败）。
/// </remarks>
public sealed class ExternalLoginOptions
{
    /// <summary>
    /// 配置节。
    /// </summary>
    public const string Section = "BasicApp:Auth:OAuth";

    /// <summary>
    /// 总开关。
    /// </summary>
    /// <remarks>
    /// 缺省开。没有提供商时等于关。
    /// </remarks>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// 回调后带着票据跳去的前端页面。
    /// </summary>
    /// <remarks>
    /// 缺省 <c>/#/auth/oauth-callback</c>，前后端同源时即可用。
    /// </remarks>
    public string FrontendCallbackUrl { get; init; } = "/#/auth/oauth-callback";

    /// <summary>
    /// 回调地址的来源。
    /// </summary>
    /// <remarks>
    /// 如 <c>https://example.com</c>，在第三方后台登记的回调地址是它加 <c>/api/oauth/{名称}/callback</c>。不配即取请求的来源（经反向代理时要转发头正确）。
    /// </remarks>
    public string? CallbackBaseUrl { get; init; }

    /// <summary>
    /// 登记的提供商（按配置顺序，同名只取第一个）。
    /// </summary>
    public IReadOnlyList<ExternalProviderConfig> Providers { get; init; } = [];

    /// <summary>
    /// 按名称取启用的提供商。
    /// </summary>
    public ExternalProviderConfig? Find(string? name)
        => Enabled ? Providers.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) : null;

    /// <summary>
    /// 从配置读取：只留启用、填了 ClientId 且类型认识的。
    /// </summary>
    public static ExternalLoginOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection(Section);
        var providers = new List<ExternalProviderConfig>();
        foreach (var child in section.GetSection("Providers").GetChildren())
        {
            var name = child["Name"]?.Trim();
            var type = ExternalProviderTypes.Normalize(child["Provider"] ?? name);
            if (string.IsNullOrEmpty(name) || type is null || !child.GetValue("Enabled", true) || string.IsNullOrWhiteSpace(child["ClientId"]))
            {
                continue;
            }

            if (providers.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            providers.Add(new ExternalProviderConfig
            {
                Name = name.ToLowerInvariant(),
                Type = type,
                DisplayName = child["DisplayName"] is { Length: > 0 } display ? display : name,
                ClientId = child["ClientId"]!.Trim(),
                ClientSecret = child["ClientSecret"] ?? string.Empty,
                Mode = string.Equals(child["Mode"], "Account", StringComparison.OrdinalIgnoreCase) ? ExternalLoginMode.Account : ExternalLoginMode.QrCode,
                AgentId = child["AgentId"],
                CorpId = child["CorpId"],
                LoadMemberProfile = child.GetValue("LoadMemberProfile", false),
                Scopes = [.. child.GetSection("Scopes").GetChildren().Select(static s => s.Value).OfType<string>().Where(static s => s.Length > 0)],
                AuthorizationEndpoint = child["AuthorizationEndpoint"] is { Length: > 0 } endpoint ? endpoint : null,
            });
        }

        return new ExternalLoginOptions
        {
            Enabled = section.GetValue("Enabled", true),
            FrontendCallbackUrl = section["FrontendCallbackUrl"] is { Length: > 0 } callback ? callback : "/#/auth/oauth-callback",
            CallbackBaseUrl = section["CallbackBaseUrl"] is { Length: > 0 } origin ? origin.TrimEnd('/') : null,
            Providers = providers,
        };
    }
}

/// <summary>
/// 一个提供商的配置。
/// </summary>
public sealed class ExternalProviderConfig
{
    /// <summary>
    /// 名称（路由与登录页用，小写）。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 规范的类型（<see cref="ExternalProviderTypes"/>）。
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// 登录页上的显示名。
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// ClientId（微信 / 企业微信为 appid / corpid）。
    /// </summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// 密钥（企业微信为应用的 corpsecret）。
    /// </summary>
    public string ClientSecret { get; init; } = string.Empty;

    /// <summary>
    /// 扫码或账号授权。
    /// </summary>
    public ExternalLoginMode Mode { get; init; }

    /// <summary>
    /// 企业微信的 AgentId。
    /// </summary>
    public string? AgentId { get; init; }

    /// <summary>
    /// 钉钉的 corpId（限定组织）。
    /// </summary>
    public string? CorpId { get; init; }

    /// <summary>
    /// 企业微信是否再取成员资料（姓名通常只在这里有）。
    /// </summary>
    public bool LoadMemberProfile { get; init; }

    /// <summary>
    /// 额外的范围（微信 / 企业微信整体替换缺省，其余追加）。
    /// </summary>
    public IReadOnlyList<string> Scopes { get; init; } = [];

    /// <summary>
    /// 授权页地址覆盖。
    /// </summary>
    public string? AuthorizationEndpoint { get; init; }
}

/// <summary>
/// 登录方式。
/// </summary>
/// <remarks>
/// 4.x <c>OAuthLoginMode</c> 同值。
/// </remarks>
public enum ExternalLoginMode
{
    /// <summary>
    /// 扫码（缺省）。
    /// </summary>
    QrCode = 0,

    /// <summary>
    /// 账号授权（微信公众号网页授权、企业微信网页授权、飞书开放平台、钉钉账号登录）。
    /// </summary>
    Account = 1,
}

/// <summary>
/// 提供商类型与别名。
/// </summary>
/// <remarks>
/// 4.x 同：weixin = wechat、workweixin = wecom、feishu = lark。
/// </remarks>
public static class ExternalProviderTypes
{
    /// <summary>
    /// 全部类型。
    /// </summary>
    public static IReadOnlyList<string> All { get; } = ["github", "gitee", "google", "qq", "wechat", "wecom", "lark", "dingtalk"];

    /// <summary>
    /// 规整成类型。
    /// </summary>
    /// <returns>规整后的类型；不认识的为空。</returns>
    public static string? Normalize(string? value)
        => value?.Trim().ToLowerInvariant() switch
        {
            "github" => "github",
            "gitee" => "gitee",
            "google" => "google",
            "qq" => "qq",
            "wechat" or "weixin" => "wechat",
            "wecom" or "workweixin" => "wecom",
            "lark" or "feishu" => "lark",
            "dingtalk" => "dingtalk",
            _ => null,
        };
}
