// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Settings;
using XiHan.BasicApp.Settings.Infrastructure;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Domain.Services;

/// <summary>
/// <see cref="IConfigValueReader"/> 的实现
/// </summary>
/// <remarks>
/// 只看启用的参数；当前租户的同键参数优先，其次平台参数；值为空回落缺省值。两侧各从缓存读（<see cref="ConfigEntries"/>）
/// </remarks>
/// <param name="entries">参数缓存</param>
/// <param name="values">参数值规则</param>
public sealed class ConfigValueReader(ConfigEntries entries, ConfigValues values) : IConfigValueReader
{
    /// <inheritdoc />
    public async ValueTask<Result<Maybe<string>>> GetAsync(string key, InvocationContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(context);
        var owner = SettingsScope.OwnerOf(context);
        var chosen = (SettingsScope.IsPlatform(owner) ? null : await entries.OfAsync(owner, key, context.CancellationToken).ConfigureAwait(false))
            ?? await entries.OfAsync(TenantIds.Platform, key, context.CancellationToken).ConfigureAwait(false);
        if (chosen is null)
        {
            return Maybe.None<string>();
        }

        var plaintext = values.PlaintextOf(key, chosen.Value, chosen.IsEncrypted);
        if (!plaintext.TryGetValue(out var value))
        {
            return plaintext.Error!;
        }

        return value.HasValue || string.IsNullOrEmpty(chosen.DefaultValue) ? value : Maybe.Some(chosen.DefaultValue);
    }
}
