// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;

namespace XiHan.BasicApp.Saas.Domain.Configurations;

/// <summary>
/// 登录设置（配置键 <see cref="SaasConfigKeys.Auth.Login"/>）
/// </summary>
/// <remarks>
/// 只管登录页展示什么：第三方登录的 <c>name</c> 必须与 <c>XiHan:Authentication:OAuth:Providers</c> 里注册的方案名一致，
/// 能不能真正登录由那边决定，两边对不上会点出一个不存在的方案。
/// </remarks>
public sealed class SaasLoginSettings
{
    /// <summary>
    /// 登录页开放的登录方式编码（如 password）
    /// </summary>
    public List<string> Methods { get; set; } = ["password"];

    /// <summary>
    /// 登录页展示的第三方登录
    /// </summary>
    [JsonPropertyName("oauthProviders")]
    public List<SaasOAuthProviderSetting> OAuthProviders { get; set; } = [];
}

/// <summary>
/// 登录页展示的一个第三方登录
/// </summary>
public sealed class SaasOAuthProviderSetting
{
    /// <summary>
    /// 认证方案名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 展示名（缺省用方案名）
    /// </summary>
    public string? DisplayName { get; set; }
}

/// <summary>
/// 密码设置（配置键 <see cref="SaasConfigKeys.Auth.Password"/>）
/// </summary>
public sealed class SaasPasswordSettings
{
    /// <summary>
    /// 是否强制修改由他人设置的密码：开启后，密码由管理员创建或重置、由平台开通、由种子写入的账号，
    /// 登录后锁定到改密为止；关闭时直接放行
    /// </summary>
    public bool ForceChange { get; set; }
}

/// <summary>
/// 模仿登录设置（配置键 <see cref="SaasConfigKeys.Auth.Impersonation"/>）
/// </summary>
public sealed class SaasImpersonationSettings
{
    /// <summary>
    /// 模仿会话存活分钟数（越界按上下限归一）
    /// </summary>
    public int SessionMinutes { get; set; } = 30;

    /// <summary>
    /// 发起模仿时是否向被模仿者投递安全通知
    /// </summary>
    public bool NotifyTarget { get; set; } = true;
}
