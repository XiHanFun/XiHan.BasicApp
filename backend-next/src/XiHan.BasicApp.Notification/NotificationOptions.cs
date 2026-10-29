// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Configuration;
using XiHan.BasicApp.Contracts;

namespace XiHan.BasicApp.Notification;

/// <summary>
/// 通知包的选项（<c>BasicApp:Notification</c>）。
/// </summary>
public sealed class NotificationOptions
{
    /// <summary>
    /// 配置节。
    /// </summary>
    public const string Section = "BasicApp:Notification";

    /// <summary>
    /// 邮件与短信模板里的品牌名 <c>brand</c>（调用方没传时补上），缺省为应用名。
    /// </summary>
    public string Brand { get; init; } = AppInfo.Name;

    /// <summary>
    /// 发送循环的轮询间隔（缺省 5 秒；有新记录时提前唤醒）。
    /// </summary>
    public TimeSpan DispatchInterval { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// 失败后重试的基础间隔（第 n 次失败后等 n 倍，缺省 30 秒，4.x 同）。
    /// </summary>
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 每条记录最多尝试的次数（缺省 3，4.x 同）。
    /// </summary>
    public int MaxAttempts { get; init; } = 3;

    /// <summary>
    /// 发送中超过这么久还没结果即视为进程中断、改回排队中（缺省 10 分钟）。
    /// </summary>
    public TimeSpan StuckAfter { get; init; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Telegram Bot API 的根地址（缺省官方地址；走自建反向代理时改这里）。
    /// </summary>
    public string TelegramApiBase { get; init; } = "https://api.telegram.org";

    /// <summary>
    /// 从配置读取。
    /// </summary>
    /// <param name="configuration">配置。</param>
    /// <returns>选项。</returns>
    public static NotificationOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection(Section);
        var defaults = new NotificationOptions();
        return new NotificationOptions
        {
            Brand = section[nameof(Brand)] is { Length: > 0 } brand ? brand : defaults.Brand,
            DispatchInterval = TimeSpan.TryParse(section[nameof(DispatchInterval)], out var interval) && interval > TimeSpan.Zero ? interval : defaults.DispatchInterval,
            RetryDelay = TimeSpan.TryParse(section[nameof(RetryDelay)], out var delay) && delay >= TimeSpan.Zero ? delay : defaults.RetryDelay,
            MaxAttempts = int.TryParse(section[nameof(MaxAttempts)], out var attempts) && attempts > 0 ? attempts : defaults.MaxAttempts,
            StuckAfter = TimeSpan.TryParse(section[nameof(StuckAfter)], out var stuck) && stuck > TimeSpan.Zero ? stuck : defaults.StuckAfter,
            TelegramApiBase = Uri.TryCreate(section[nameof(TelegramApiBase)], UriKind.Absolute, out var api) && api.Scheme == Uri.UriSchemeHttps ? api.ToString().TrimEnd('/') : defaults.TelegramApiBase,
        };
    }
}
