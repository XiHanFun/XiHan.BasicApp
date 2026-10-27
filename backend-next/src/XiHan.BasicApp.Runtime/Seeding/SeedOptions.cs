// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Configuration;

namespace XiHan.BasicApp.Runtime.Seeding;

/// <summary>
/// 种子配置 <c>BasicApp:Seed</c>：是否写演示数据、超级管理员的初始密码。
/// </summary>
public sealed record SeedOptions
{
    /// <summary>
    /// 配置节。
    /// </summary>
    public const string ConfigurationSection = "BasicApp:Seed";

    /// <summary>
    /// 初始超级管理员的登录名（沿用 4.x）。
    /// </summary>
    public const string SuperAdminUserName = "superadmin";

    /// <summary>
    /// 缺省初始密码（沿用 4.x）；部署时用配置覆盖并在首次登录后修改。
    /// </summary>
    public const string DefaultSuperAdminPassword = "SuperAdmin@123";

    /// <summary>
    /// 是否执行演示数据种子（号段 900）。
    /// </summary>
    public bool EnableDemoData { get; init; }

    /// <summary>
    /// 超级管理员的初始密码；只在账号不存在、首次写入时使用。
    /// </summary>
    public string SuperAdminPassword { get; init; } = DefaultSuperAdminPassword;

    /// <summary>
    /// 从配置读取。
    /// </summary>
    public static SeedOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var options = new SeedOptions();
        var demo = configuration[$"{ConfigurationSection}:EnableDemoData"];
        if (!string.IsNullOrEmpty(demo))
        {
            options = bool.TryParse(demo, out var enabled)
                ? options with { EnableDemoData = enabled }
                : throw new ArgumentException($"'{ConfigurationSection}:EnableDemoData' must be 'true' or 'false', but was '{demo}'.");
        }

        var password = configuration[$"{ConfigurationSection}:SuperAdminPassword"];
        if (!string.IsNullOrWhiteSpace(password))
        {
            options = options with { SuperAdminPassword = password };
        }

        return options;
    }
}
