// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Settings;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Domain.Services;

/// <summary>
/// 密码设置
/// </summary>
/// <remarks>
/// 系统参数 <see cref="ConfigKey"/> 的 JSON 值，只认平台的（账号跨租户、登录在进入租户之前）；没建即缺省值，与参数的缺省值一致
/// 设密码时按复杂度与历史深度校验；用密码登录时，密码由他人设置且打开了强制改密，或密码超过有效期，会话一开即锁定到改密为止
/// 写错的值经参数配置保存时即拒绝（<c>SETTINGS_VALIDATION_011</c>）；库里的值仍不对（手工改库）即 <c>IDENTITY_VALIDATION_025</c>，设密码与用密码登录都失败
/// </remarks>
/// <param name="ForceChange">是否强制修改由他人设置的密码</param>
/// <param name="MinLength">最短长度（6 到 128）</param>
/// <param name="RequireLetter">须含字母</param>
/// <param name="RequireDigit">须含数字</param>
/// <param name="RequireUppercase">须含大写字母</param>
/// <param name="RequireLowercase">须含小写字母</param>
/// <param name="RequireSymbol">须含字母与数字以外的字符</param>
/// <param name="HistoryDepth">不得与最近多少个用过的密码相同（0 不检查，最多 1000）</param>
/// <param name="ExpiryDays">密码有效天数（0 不过期，最多 3650）</param>
public sealed record PasswordSettings(
    bool ForceChange = false,
    int MinLength = 8,
    bool RequireLetter = true,
    bool RequireDigit = true,
    bool RequireUppercase = false,
    bool RequireLowercase = false,
    bool RequireSymbol = false,
    int HistoryDepth = 3,
    int ExpiryDays = 0)
{
    /// <summary>
    /// 系统参数键
    /// </summary>
    public const string ConfigKey = "identity.password";

    /// <summary>
    /// 最短长度的下限
    /// </summary>
    public const int MinLengthFloor = 6;

    /// <summary>
    /// 最短长度的上限
    /// </summary>
    /// <remarks>
    /// 与各个设密码请求的长度上限一致
    /// </remarks>
    public const int MinLengthCeiling = 128;

    /// <summary>
    /// 有效天数的上限
    /// </summary>
    public const int ExpiryDaysCeiling = 3650;

    /// <summary>
    /// 缺省值
    /// </summary>
    /// <remarks>
    /// 至少 8 位、须含字母与数字、不得与最近 3 个相同、不过期、不强制改密
    /// </remarks>
    public static PasswordSettings Default { get; } = new();

    /// <summary>
    /// 内建参数的元数据
    /// </summary>
    public static ConfigDescriptor Config { get; } = new(
        ConfigKey,
        "密码设置",
        "身份认证",
        ConfigDataType.Json,
        """{"forceChange":false,"minLength":8,"requireLetter":true,"requireDigit":true,"requireUppercase":false,"requireLowercase":false,"requireSymbol":false,"historyDepth":3,"expiryDays":0}""",
        "forceChange：打开后，密码由他人设置的账号用密码登录要先改密；minLength：最短长度（6 到 128）；requireLetter / requireDigit / requireUppercase / requireLowercase / requireSymbol：须含字母、数字、大写、小写、其他字符；historyDepth：不得与最近几个用过的相同（0 不检查，最多 1000）；expiryDays：密码有效天数，过期后用密码登录要先改密（0 不过期，最多 3650）。只认平台的，写错的值保存时即拒绝",
        Order: 110,
        Accepts: static json => Parse(json) is not null);

    /// <summary>
    /// 取值是否在范围内
    /// </summary>
    public bool IsWithinRange() => MinLength is >= MinLengthFloor and <= MinLengthCeiling
        && HistoryDepth is >= 0 and <= EntityQuery.DefaultMaxTake
        && ExpiryDays is >= 0 and <= ExpiryDaysCeiling;

    /// <summary>
    /// 密码设置于 <paramref name="changedAt"/> 的密码在 <paramref name="now"/> 是否已过期
    /// </summary>
    /// <remarks>
    /// 不过期（<see cref="ExpiryDays"/> 为 0）即否；没有改密时间的按已过期算
    /// </remarks>
    /// <param name="changedAt">最近一次设密码的时间</param>
    /// <param name="now">现在</param>
    public bool IsExpired(DateTimeOffset? changedAt, DateTimeOffset now)
        => ExpiryDays > 0 && (changedAt is not { } at || at.AddDays(ExpiryDays) <= now);

    /// <summary>
    /// 读密码设置
    /// </summary>
    /// <remarks>
    /// 没装 Settings 或没建参数即缺省值；不是 JSON 或取值越界即失败
    /// </remarks>
    /// <param name="context">调用上下文</param>
    /// <returns>参数</returns>
    public static async ValueTask<Result<PasswordSettings>> ReadAsync(InvocationContext context)
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

        if (!value.TryGetValue(out var json))
        {
            return Default;
        }

        return Parse(json) is { } parsed
            ? parsed
            : Error.Validation(IdentityErrors.PasswordSettingsInvalid, IdentityMessages.PasswordSettingsInvalid, MessageArguments.Of(("key", ConfigKey)));
    }

    /// <summary>
    /// 解析参数值
    /// </summary>
    /// <returns>不是 JSON、字段类型不对或取值越界为 <see langword="null"/></returns>
    private static PasswordSettings? Parse(string json)
    {
        try
        {
            return JsonSerializer.Deserialize(json, IdentityJsonContext.Default.PasswordSettings) is { } parsed && parsed.IsWithinRange() ? parsed : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
