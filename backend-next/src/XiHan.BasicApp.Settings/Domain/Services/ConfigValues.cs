// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Text.Json;
using XiHan.BasicApp.Runtime.Security;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Domain.Services;

/// <summary>
/// 参数值的规则：按数据类型校验、加密值的保护与还原。
/// </summary>
/// <remarks>
/// 加密只针对值，缺省值不加密。
/// </remarks>
/// <param name="protector">数据保护。</param>
public sealed class ConfigValues(ISecretProtector protector)
{
    /// <summary>
    /// 参数值的数据保护用途。
    /// </summary>
    public const string Purpose = "XiHan.BasicApp.Settings.ConfigValue";

    /// <summary>
    /// 值是否符合数据类型。
    /// </summary>
    /// <remarks>
    /// 空值不校验（表示没有值）。
    /// </remarks>
    public static bool Matches(ConfigDataType type, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        return type switch
        {
            ConfigDataType.Text => true,
            ConfigDataType.Number => decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _),
            ConfigDataType.Boolean => value is "true" or "false",
            ConfigDataType.Json => RootKind(value) == JsonValueKind.Object,
            ConfigDataType.Array => RootKind(value) == JsonValueKind.Array,
            _ => false,
        };
    }

    /// <summary>
    /// 校验值与缺省值，不符的字段报 <see cref="SettingsErrors.ValueTypeMismatch"/>。
    /// </summary>
    public static Result Validate(ConfigDataType type, string? value, string? defaultValue)
    {
        if (!Matches(type, value))
        {
            return Error.Validation(SettingsErrors.ValueTypeMismatch, SettingsMessages.ValueTypeMismatch, MessageArguments.Of(("field", "value"), ("type", type.ToString())));
        }

        return Matches(type, defaultValue)
            ? Result.Success()
            : Error.Validation(SettingsErrors.ValueTypeMismatch, SettingsMessages.ValueTypeMismatch, MessageArguments.Of(("field", "defaultValue"), ("type", type.ToString())));
    }

    /// <summary>
    /// 加密明文。
    /// </summary>
    /// <returns>密文；空值原样返回。</returns>
    public string? Protect(string? plaintext) => string.IsNullOrEmpty(plaintext) ? plaintext : protector.Protect(Purpose, plaintext);

    /// <summary>
    /// 取参数存着的明文。
    /// </summary>
    /// <remarks>
    /// 加密的解密，解不开报 <see cref="SettingsErrors.ConfigUndecryptable"/>。
    /// </remarks>
    /// <returns>明文；没有值为 <c>None</c>。</returns>
    public Result<Maybe<string>> PlaintextOf(SysConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (string.IsNullOrEmpty(config.Value))
        {
            return Maybe.None<string>();
        }

        if (!config.IsEncrypted)
        {
            return Maybe.Some(config.Value);
        }

        return protector.TryUnprotect(Purpose, config.Value, out var plaintext)
            ? Maybe.Some(plaintext)
            : Error.Unexpected(SettingsErrors.ConfigUndecryptable, SettingsMessages.ConfigUndecryptable, details: MessageArguments.Of(("key", config.Key)));
    }

    private static JsonValueKind? RootKind(string value)
    {
        try
        {
            using var document = JsonDocument.Parse(value);
            return document.RootElement.ValueKind;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
