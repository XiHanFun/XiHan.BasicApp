// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;

namespace XiHan.BasicApp.Saas.Domain.Configurations;

/// <summary>
/// 登录方式编码（登录设置 <see cref="SaasLoginSettings.Methods"/> 的取值，前端登录页按它显示入口）
/// </summary>
public static class SaasLoginMethods
{
    /// <summary>
    /// 账号密码
    /// </summary>
    public const string Password = "password";

    /// <summary>
    /// 手机号验证码
    /// </summary>
    public const string Phone = "phone";

    /// <summary>
    /// 邮箱验证码
    /// </summary>
    public const string Email = "email";

    /// <summary>
    /// 扫码
    /// </summary>
    public const string QrCode = "qrcode";
}

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
    /// 登录页开放的登录方式编码（见 <see cref="SaasLoginMethods"/>）
    /// </summary>
    public List<string> Methods { get; set; } = [SaasLoginMethods.Password];

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
/// 登录失败自动封禁设置（配置键 <see cref="SaasConfigKeys.Security.LoginAutoBan"/>）
/// </summary>
/// <remarks>
/// 同一 IP 在 <see cref="WindowMinutes"/> 分钟内登录失败（跨账号累计）达到 <see cref="MaxFailures"/> 次即封禁 <see cref="BanMinutes"/> 分钟；
/// 24 小时内再次被封时长翻倍，最长 <see cref="MaxBanMinutes"/> 分钟。访问策略里「放行」的 IP 不计数、不封禁。
/// </remarks>
public sealed class SaasLoginAutoBanSettings
{
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 计数窗口（分钟）
    /// </summary>
    public int WindowMinutes { get; set; } = 10;

    /// <summary>
    /// 窗口内的失败次数上限
    /// </summary>
    public int MaxFailures { get; set; } = 20;

    /// <summary>
    /// 首次封禁时长（分钟）
    /// </summary>
    public int BanMinutes { get; set; } = 30;

    /// <summary>
    /// 封禁时长上限（分钟）
    /// </summary>
    public int MaxBanMinutes { get; set; } = 1440;

    /// <summary>
    /// 校验各项数值
    /// </summary>
    /// <returns>第一处不合规的说明；全部合规时为空</returns>
    public string? Validate()
    {
        if (WindowMinutes is < 1 or > 1440)
        {
            return "计数窗口须在 1 到 1440 分钟之间。";
        }

        if (MaxFailures is < 1 or > 10_000)
        {
            return "失败次数上限须在 1 到 10000 之间。";
        }

        if (MaxBanMinutes is < 1 or > 43_200)
        {
            return "封禁时长上限须在 1 到 43200 分钟之间。";
        }

        return BanMinutes < 1 || BanMinutes > MaxBanMinutes
            ? "首次封禁时长须在 1 分钟到封禁时长上限之间。"
            : null;
    }
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
