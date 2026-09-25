// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Configurations;
using XiHan.BasicApp.Saas.Infrastructure.Messaging;
using XiHan.BasicApp.Saas.Infrastructure.Tasks;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// SaaS 参数配置：登录、密码、模仿登录、Telegram 机器人、日志保留
/// </summary>
/// <remarks>
/// 只放运行期真有代码读取的配置；同一功能的设置合成一条 JSON，结构见对应的设置类型。
/// </remarks>
public sealed class SaasSettingSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasSettingSeeder> logger,
    IServiceProvider serviceProvider)
    : SettingSeederBase(clientResolver, logger, serviceProvider)
{
    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => SeedOrders.PlatformData;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]参数配置";

    /// <summary>
    /// 参数配置声明
    /// </summary>
    public override IReadOnlyList<SettingSeed> Settings { get; } =
    [
        SettingSeed.Json(
            SaasConfigKeys.Auth.Login, "登录设置", SaasConfigKeys.Groups.Auth,
            new SaasLoginSettings
            {
                Methods = ["password"],
                // name 必须与 XiHan:Authentication:OAuth:Providers 里注册的方案名一致
                OAuthProviders =
                [
                    new() { Name = "github", DisplayName = "Github" },
                    new() { Name = "gitee", DisplayName = "Gitee" },
                    new() { Name = "google", DisplayName = "Google" },
                    new() { Name = "qq", DisplayName = "QQ" },
                    new() { Name = "wechat", DisplayName = "微信" },
                    new() { Name = "wecom", DisplayName = "企业微信" },
                    new() { Name = "feishu", DisplayName = "飞书" },
                    new() { Name = "dingtalk", DisplayName = "钉钉" },
                ],
            },
            new SaasLoginSettings(),
            "登录页开放的登录方式（methods）与展示的第三方登录（oauthProviders，name 须与已注册的认证方案一致）",
            10),
        SettingSeed.Json(
            SaasConfigKeys.Auth.Password, "密码设置", SaasConfigKeys.Groups.Auth,
            new SaasPasswordSettings(), new SaasPasswordSettings(),
            "forceChange：开启后，密码由管理员创建或重置、由平台开通、由种子写入的账号，登录后要先改密才能继续使用",
            20),
        SettingSeed.Json(
            SaasConfigKeys.Auth.Impersonation, "模仿登录设置", SaasConfigKeys.Groups.Auth,
            new SaasImpersonationSettings(), new SaasImpersonationSettings(),
            "sessionMinutes：模仿会话存活分钟数（按 1~480 归一）；notifyTarget：是否向被模仿者投递安全通知",
            30),
        SettingSeed.Json(
            SaasConfigKeys.Bot.Telegram.Settings, "Telegram 机器人平台设置", SaasConfigKeys.Groups.Bot,
            new SaasTelegramBotConfig(), new SaasTelegramBotConfig(),
            "enabled 总开关；webhookBaseUrl 留空走长轮询；webhookRoutePrefix 接收路由前缀；managerRefreshSeconds / configCacheSeconds 刷新与缓存周期；enableFallbackReply 兜底回复；network 代理、自建 API 地址与超时",
            100),
        SettingSeed.Secret(
            SaasConfigKeys.Bot.Telegram.WebhookSecretToken, "Telegram Webhook 密钥令牌", SaasConfigKeys.Groups.Bot,
            "Webhook 模式必填的 secret_token（未配置一律拒绝 Webhook 请求）",
            101),
        SettingSeed.Number(
            SaasConfigKeys.Log.RetentionDays, "日志保留天数", SaasConfigKeys.Groups.Log,
            LogRetentionCleanupTask.DefaultRetentionDays,
            "访问、接口、异常、操作、差异、登录日志的保留天数，清理任务删除更早的记录",
            200),
    ];
}
