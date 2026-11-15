// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Files;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Infrastructure;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Domain.Services;

/// <summary>
/// 站点设置与租户品牌的读写
/// </summary>
/// <remarks>
/// 存在系统参数 <see cref="SiteSettings.ConfigKey"/>：平台的是内建参数，租户的是同键的覆盖参数（第一次保存时建）。读取走参数缓存，写入后失效
/// 换掉或去掉的图片经 Contracts 的 <see cref="ISiteAssetFiles"/> 回收（没装 Files 即不回收）
/// </remarks>
/// <param name="configs">参数仓储</param>
/// <param name="entries">参数缓存</param>
/// <param name="ids">标识生成器</param>
/// <param name="assets">站点图片</param>
public sealed class SiteProfiles(IRepository<SysConfig, Guid> configs, ConfigEntries entries, IIdGenerator<Guid> ids, IEnumerable<ISiteAssetFiles> assets)
{
    private readonly ISiteAssetFiles? _assets = assets.FirstOrDefault();

    /// <summary>
    /// 一侧自己存着的设置
    /// </summary>
    /// <remarks>
    /// 平台没有值即缺省值；租户没有覆盖即全空。参数写坏了（手工改库）即失败
    /// </remarks>
    /// <param name="owner">所属侧</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<Result<SiteSettings>> OwnAsync(string owner, CancellationToken cancellationToken)
    {
        var entry = await entries.OfAsync(owner, SiteSettings.ConfigKey, cancellationToken).ConfigureAwait(false);
        var json = entry is null ? null : string.IsNullOrEmpty(entry.Value) ? entry.DefaultValue : entry.Value;
        if (string.IsNullOrEmpty(json))
        {
            return SettingsScope.IsPlatform(owner) ? SiteSettings.Default : new SiteSettings();
        }

        return SiteSettings.Parse(json) is { } parsed
            ? parsed
            : Error.Validation(SettingsErrors.ValueRejected, SettingsMessages.ValueRejected, MessageArguments.Of(("field", "value"), ("key", SiteSettings.ConfigKey)));
    }

    /// <summary>
    /// 一侧生效的设置：租户的叠在平台的之上
    /// </summary>
    /// <param name="owner">所属侧</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<Result<SiteSettings>> EffectiveAsync(string owner, CancellationToken cancellationToken)
    {
        var platform = await OwnAsync(TenantIds.Platform, cancellationToken).ConfigureAwait(false);
        if (SettingsScope.IsPlatform(owner) || !platform.TryGetValue(out var baseline))
        {
            return platform;
        }

        var own = await OwnAsync(owner, cancellationToken).ConfigureAwait(false);
        return own.TryGetValue(out var settings) ? settings.Over(baseline) : own;
    }

    /// <summary>
    /// 保存一侧的设置，回收不再用的图片
    /// </summary>
    /// <param name="owner">所属侧</param>
    /// <param name="settings">新的设置</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<Result<SiteSettings>> SaveAsync(string owner, SiteSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var trimmed = settings.Trimmed();
        if (!trimmed.IsWithinRange())
        {
            return Error.Validation(SettingsErrors.ValueRejected, SettingsMessages.ValueRejected, MessageArguments.Of(("field", "value"), ("key", SiteSettings.ConfigKey)));
        }

        var previous = await OwnAsync(owner, cancellationToken).ConfigureAwait(false);
        var released = previous.TryGetValue(out var old) ? old.Assets().Except(trimmed.Assets()).ToList() : [];
        var rows = await configs.ListAsync(EntityQuery.All<SysConfig>().Where(new Specification<SysConfig>(c => c.OwnerTenantId == owner && c.Key == SiteSettings.ConfigKey)).Page(0, 1), cancellationToken).ConfigureAwait(false);
        Result written;
        if (rows.Count > 0)
        {
            rows[0].Value = trimmed.ToJson();
            written = await configs.UpdateAsync(rows[0], cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var descriptor = SiteSettings.Config;
            written = await configs.AddAsync(
                new SysConfig
                {
                    Id = ids.NewId(),
                    OwnerTenantId = owner,
                    Key = descriptor.Key,
                    Name = descriptor.Name,
                    Group = descriptor.Group,
                    DataType = descriptor.DataType,
                    Value = trimmed.ToJson(),
                    DefaultValue = SettingsScope.IsPlatform(owner) ? descriptor.DefaultValue : null,
                    Description = descriptor.Description,
                    AllowTenantOverride = SettingsScope.IsPlatform(owner) && descriptor.AllowTenantOverride,
                    Order = descriptor.Order,
                    IsBuiltIn = SettingsScope.IsPlatform(owner),
                },
                cancellationToken).ConfigureAwait(false);
        }

        if (written.IsFailure)
        {
            return written.Error!;
        }

        await entries.InvalidateAsync(owner, SiteSettings.ConfigKey, cancellationToken).ConfigureAwait(false);
        if (_assets is not null)
        {
            foreach (var id in released)
            {
                await _assets.ReleaseAsync(id, cancellationToken).ConfigureAwait(false);
            }
        }

        return trimmed;
    }
}
