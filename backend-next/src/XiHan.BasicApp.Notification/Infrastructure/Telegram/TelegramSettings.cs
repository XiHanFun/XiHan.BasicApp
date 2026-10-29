// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Settings;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Notification.Infrastructure.Telegram;

/// <summary>
/// Telegram 的网络设置。
/// </summary>
public sealed record TelegramNetwork
{
    /// <summary>
    /// 代理地址。
    /// </summary>
    /// <remarks>
    /// 为空即直连；地址里的用户名与密码作代理凭据。
    /// </remarks>
    public string? ProxyUrl { get; init; }

    /// <summary>
    /// Bot API 的根地址。
    /// </summary>
    /// <remarks>
    /// 为空即官方 <c>https://api.telegram.org</c>；自建 Bot API Server 时改这里。
    /// </remarks>
    public string? BaseUrl { get; init; }

    /// <summary>
    /// 请求超时秒数。
    /// </summary>
    /// <remarks>
    /// ≤ 0 即 100，4.x 同。
    /// </remarks>
    public int TimeoutSeconds { get; init; } = 100;

    /// <summary>
    /// 生效的根地址。
    /// </summary>
    public string EffectiveBaseUrl => string.IsNullOrWhiteSpace(BaseUrl) ? "https://api.telegram.org" : BaseUrl.Trim().TrimEnd('/');

    /// <summary>
    /// 生效的超时。
    /// </summary>
    public TimeSpan EffectiveTimeout => TimeSpan.FromSeconds(TimeoutSeconds > 0 ? TimeoutSeconds : 100);
}

/// <summary>
/// Telegram 的平台设置。
/// </summary>
/// <remarks>
/// 系统参数 <see cref="ConfigKey"/> 的 JSON 值（只认平台的），Webhook 密钥另存加密参数 <see cref="SecretConfigKey"/>（4.x 同两个参数，键改到 notification 下）。
/// 没建参数即缺省值（总开关关）；运行中改参数，下一个刷新周期生效。
/// </remarks>
public sealed record TelegramSettings
{
    /// <summary>
    /// 设置的参数键。
    /// </summary>
    public const string ConfigKey = "notification.telegram";

    /// <summary>
    /// Webhook 密钥的参数键（应设为加密参数）。
    /// </summary>
    public const string SecretConfigKey = "notification.telegram.webhook-secret-token";

    /// <summary>
    /// Webhook 的路由前缀。
    /// </summary>
    /// <remarks>
    /// 4.x 可配，6.0 固定：端点在宿主启动时映射。
    /// </remarks>
    public const string RoutePrefix = "/api/telegram-bot/webhook";

    /// <summary>
    /// 缺省设置。
    /// </summary>
    public static TelegramSettings Default { get; } = new();

    /// <summary>
    /// 总开关（缺省关）。
    /// </summary>
    /// <remarks>
    /// 4.x 同。
    /// </remarks>
    public bool Enabled { get; init; }

    /// <summary>
    /// Webhook 的对外根地址。
    /// </summary>
    /// <remarks>
    /// 如 <c>https://admin.example.com</c>；为空即长轮询。
    /// </remarks>
    public string? WebhookBaseUrl { get; init; }

    /// <summary>
    /// 刷新周期秒数（缺省 5）。
    /// </summary>
    /// <remarks>
    /// ≤ 0 即只在启动时加载一次。
    /// </remarks>
    public int ManagerRefreshSeconds { get; init; } = 5;

    /// <summary>
    /// 平台级兜底回复开关（与机器人自己的开关取「或」）。
    /// </summary>
    public bool EnableFallbackReply { get; init; }

    /// <summary>
    /// 网络。
    /// </summary>
    public TelegramNetwork Network { get; init; } = new();

    /// <summary>
    /// Webhook 密钥（来自加密参数，不在 JSON 里）。
    /// </summary>
    public string? SecretToken { get; init; }

    /// <summary>
    /// 是否走 Webhook。
    /// </summary>
    public bool UsesWebhook => !string.IsNullOrWhiteSpace(WebhookBaseUrl);

    /// <summary>
    /// 某个机器人的 Webhook 地址。
    /// </summary>
    /// <param name="botName">机器人名称。</param>
    /// <returns>地址。</returns>
    public string WebhookUrlOf(string botName) => $"{WebhookBaseUrl!.Trim().TrimEnd('/')}{RoutePrefix}/{Uri.EscapeDataString(botName)}";

    /// <summary>
    /// 在平台调用里读两个参数。
    /// </summary>
    /// <remarks>
    /// 没装 Settings 即缺省值。
    /// </remarks>
    /// <param name="context">调用上下文（平台）。</param>
    /// <returns>设置；JSON 写错即失败。</returns>
    public static async ValueTask<Result<TelegramSettings>> LoadAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Services.GetService<IConfigValueReader>() is not { } reader)
        {
            return Default;
        }

        var configured = await reader.GetAsync(ConfigKey, context).ConfigureAwait(false);
        if (!configured.TryGetValue(out var value))
        {
            return configured.Error!;
        }

        TelegramSettings settings;
        if (!value.TryGetValue(out var json))
        {
            settings = Default;
        }
        else
        {
            try
            {
                settings = JsonSerializer.Deserialize(json, NotificationJsonContext.Default.TelegramSettings) ?? Default;
            }
            catch (JsonException exception)
            {
                return Error.Validation(NotificationErrors.TelegramSettingsInvalid, NotificationMessages.TelegramSettingsInvalid, MessageArguments.Of(("key", ConfigKey), ("detail", exception.Message)));
            }
        }

        var secret = await reader.GetAsync(SecretConfigKey, context).ConfigureAwait(false);
        if (!secret.TryGetValue(out var token))
        {
            return secret.Error!;
        }

        // 源生成的反序列化对没写的嵌套对象给 null，不走属性的初始值
        return settings with
        {
            Network = settings.Network ?? new TelegramNetwork(),
            SecretToken = token.TryGetValue(out var plain) && !string.IsNullOrWhiteSpace(plain) ? plain.Trim() : null,
        };
    }
}

/// <summary>
/// Telegram 机器人的回复文案。
/// </summary>
/// <remarks>
/// 配置节 <c>BasicApp:Notification:Telegram:Texts</c>，缺省即 4.x 的中文文案；为空即不发。
/// </remarks>
public sealed record TelegramTexts
{
    /// <summary>
    /// <c>/start</c> 的回复（<c>{botUsername}</c> 换成机器人用户名）。
    /// </summary>
    public string StartReply { get; init; } = "你好，欢迎使用 {botUsername}！发送 /help 查看可用命令。";

    /// <summary>
    /// <c>/help</c> 的标题。
    /// </summary>
    public string HelpTitle { get; init; } = "可用命令：";

    /// <summary>
    /// <c>/myid</c> 的回复。
    /// </summary>
    public string MyIdReply { get; init; } = "ChatId：{chatId}\nUserId：{userId}";

    /// <summary>
    /// 命令不在白名单里。
    /// </summary>
    public string CommandDisabledReply { get; init; } = "该命令未开启。";

    /// <summary>
    /// 命令只给管理员。
    /// </summary>
    public string CommandAdminOnlyReply { get; init; } = "该命令仅管理员可用。";

    /// <summary>
    /// 回调只给管理员。
    /// </summary>
    public string CallbackAdminOnlyReply { get; init; } = "该操作仅管理员可用。";

    /// <summary>
    /// 兜底回复。
    /// </summary>
    public string FallbackReply { get; init; } = "暂不支持处理该消息，发送 /help 查看可用命令。";

    /// <summary>
    /// 处理出错时的回复。
    /// </summary>
    public string InternalErrorReply { get; init; } = "处理消息时出错，请稍后再试。";

    /// <summary>
    /// 发送最终失败时私发给管理员的告警标题。
    /// </summary>
    public string SendFailureAdminNotifyTitle { get; init; } = "消息发送失败告警";

    /// <summary>
    /// <c>/start</c> 在命令菜单里的说明。
    /// </summary>
    public string StartDescription { get; init; } = "开始使用机器人";

    /// <summary>
    /// <c>/help</c> 在命令菜单里的说明。
    /// </summary>
    public string HelpDescription { get; init; } = "查看帮助";

    /// <summary>
    /// <c>/myid</c> 在命令菜单里的说明。
    /// </summary>
    public string MyIdDescription { get; init; } = "查看我的 Telegram Id";
}

/// <summary>
/// Telegram 出站的重试。
/// </summary>
/// <remarks>
/// 配置节 <c>BasicApp:Notification:Telegram:Retry</c>，缺省同 4.x。
/// </remarks>
public sealed record TelegramRetryOptions
{
    /// <summary>
    /// 失败后最多再试的次数（缺省 3，即最多 4 次）。
    /// </summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// 退避的基础毫秒数（第 n 次等 min(上限, 基础 × 2^(n-1))）。
    /// </summary>
    public int BaseDelayMs { get; init; } = 500;

    /// <summary>
    /// 退避上限毫秒数。
    /// </summary>
    public int MaxDelayMs { get; init; } = 10_000;

    /// <summary>
    /// 429 的 <c>retry_after</c> 最多等多少秒。
    /// </summary>
    /// <remarks>
    /// 4.x 不设上限，6.0 超过即按失败处理，免得占住分发。
    /// </remarks>
    public int MaxRetryAfterSeconds { get; init; } = 60;

    /// <summary>
    /// 最终失败时是否私发管理员告警。
    /// </summary>
    public bool NotifyAdminOnFinalFailure { get; init; } = true;
}

/// <summary>
/// Telegram 的进程内选项。
/// </summary>
/// <remarks>
/// 读 appsettings 的 <c>BasicApp:Notification:Telegram</c>。
/// </remarks>
public sealed record TelegramOptions
{
    /// <summary>
    /// 配置节。
    /// </summary>
    public const string Section = "BasicApp:Notification:Telegram";

    /// <summary>
    /// 回复文案。
    /// </summary>
    public TelegramTexts Texts { get; init; } = new();

    /// <summary>
    /// 重试。
    /// </summary>
    public TelegramRetryOptions Retry { get; init; } = new();

    /// <summary>
    /// 全部机器人共用的同时处理上限。
    /// </summary>
    /// <remarks>
    /// 4.x 同 16。
    /// </remarks>
    public int MaxDispatchConcurrency { get; init; } = 16;

    /// <summary>
    /// 去重标记的保留时长。
    /// </summary>
    /// <remarks>
    /// 4.x 同 30 分钟。
    /// </remarks>
    public TimeSpan DedupLifetime { get; init; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// 长轮询每次等待的秒数。
    /// </summary>
    public int PollingTimeoutSeconds { get; init; } = 50;

    /// <summary>
    /// 从配置读取（没写的项用缺省值）。
    /// </summary>
    /// <param name="configuration">配置。</param>
    /// <returns>选项。</returns>
    public static TelegramOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection(Section);
        var defaults = new TelegramOptions();
        var texts = section.GetSection(nameof(Texts));
        var retry = section.GetSection(nameof(Retry));
        string Text(string name, string fallback) => texts[name] ?? fallback;
        int Number(IConfigurationSection s, string name, int fallback, int min) => int.TryParse(s[name], out var n) && n >= min ? n : fallback;
        return new TelegramOptions
        {
            Texts = new TelegramTexts
            {
                StartReply = Text(nameof(TelegramTexts.StartReply), defaults.Texts.StartReply),
                HelpTitle = Text(nameof(TelegramTexts.HelpTitle), defaults.Texts.HelpTitle),
                MyIdReply = Text(nameof(TelegramTexts.MyIdReply), defaults.Texts.MyIdReply),
                CommandDisabledReply = Text(nameof(TelegramTexts.CommandDisabledReply), defaults.Texts.CommandDisabledReply),
                CommandAdminOnlyReply = Text(nameof(TelegramTexts.CommandAdminOnlyReply), defaults.Texts.CommandAdminOnlyReply),
                CallbackAdminOnlyReply = Text(nameof(TelegramTexts.CallbackAdminOnlyReply), defaults.Texts.CallbackAdminOnlyReply),
                FallbackReply = Text(nameof(TelegramTexts.FallbackReply), defaults.Texts.FallbackReply),
                InternalErrorReply = Text(nameof(TelegramTexts.InternalErrorReply), defaults.Texts.InternalErrorReply),
                SendFailureAdminNotifyTitle = Text(nameof(TelegramTexts.SendFailureAdminNotifyTitle), defaults.Texts.SendFailureAdminNotifyTitle),
                StartDescription = Text(nameof(TelegramTexts.StartDescription), defaults.Texts.StartDescription),
                HelpDescription = Text(nameof(TelegramTexts.HelpDescription), defaults.Texts.HelpDescription),
                MyIdDescription = Text(nameof(TelegramTexts.MyIdDescription), defaults.Texts.MyIdDescription),
            },
            Retry = new TelegramRetryOptions
            {
                MaxRetries = Number(retry, nameof(TelegramRetryOptions.MaxRetries), defaults.Retry.MaxRetries, 0),
                BaseDelayMs = Number(retry, nameof(TelegramRetryOptions.BaseDelayMs), defaults.Retry.BaseDelayMs, 0),
                MaxDelayMs = Number(retry, nameof(TelegramRetryOptions.MaxDelayMs), defaults.Retry.MaxDelayMs, 0),
                MaxRetryAfterSeconds = Number(retry, nameof(TelegramRetryOptions.MaxRetryAfterSeconds), defaults.Retry.MaxRetryAfterSeconds, 0),
                NotifyAdminOnFinalFailure = !bool.TryParse(retry[nameof(TelegramRetryOptions.NotifyAdminOnFinalFailure)], out var notify) || notify,
            },
            MaxDispatchConcurrency = Number(section, nameof(MaxDispatchConcurrency), defaults.MaxDispatchConcurrency, 1),
            DedupLifetime = TimeSpan.TryParse(section[nameof(DedupLifetime)], out var dedup) && dedup > TimeSpan.Zero ? dedup : defaults.DedupLifetime,
            PollingTimeoutSeconds = Number(section, nameof(PollingTimeoutSeconds), defaults.PollingTimeoutSeconds, 0),
        };
    }
}
