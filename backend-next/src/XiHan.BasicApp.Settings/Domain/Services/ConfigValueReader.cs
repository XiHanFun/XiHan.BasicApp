// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Settings;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Domain.Services;

/// <summary>
/// <see cref="IConfigValueReader"/> 的实现：只看启用的参数；当前租户的同键参数优先，其次平台参数；值为空回落缺省值。
/// </summary>
/// <param name="configs">参数仓储。</param>
/// <param name="values">参数值规则。</param>
public sealed class ConfigValueReader(IRepository<SysConfig, Guid> configs, ConfigValues values) : IConfigValueReader
{
    /// <inheritdoc />
    public async ValueTask<Result<Maybe<string>>> GetAsync(string key, InvocationContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(context);
        var owner = SettingsScope.OwnerOf(context);
        const string platform = TenantIds.Platform;
        var rows = await configs.ListAsync(
            EntityQuery.All<SysConfig>().Where(new Specification<SysConfig>(c => c.Key == key && c.Status == SettingStatus.Active && (c.OwnerTenantId == owner || c.OwnerTenantId == platform))),
            context.CancellationToken).ConfigureAwait(false);
        var chosen = rows.FirstOrDefault(c => !SettingsScope.IsPlatform(owner) && SettingsScope.IsOwnedBy(c, owner))
            ?? rows.FirstOrDefault(static c => SettingsScope.IsPlatform(c.OwnerTenantId));
        if (chosen is null)
        {
            return Maybe.None<string>();
        }

        var plaintext = values.PlaintextOf(chosen);
        if (!plaintext.TryGetValue(out var value))
        {
            return plaintext.Error!;
        }

        return value.HasValue || string.IsNullOrEmpty(chosen.DefaultValue) ? value : Maybe.Some(chosen.DefaultValue);
    }
}
