// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Seeding;
using XiHan.BasicApp.Contracts.Settings;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Seeds;

/// <summary>
/// 按各包登记的 <see cref="ConfigCatalogSection"/> 写平台的内建参数
/// </summary>
/// <remarks>
/// 目录类种子：名称、分组、数据类型、缺省值、是否允许租户覆盖、是否加密与说明每次对齐，没变不写；值、状态与排序号只在首次写入，之后归运营
/// 加密与否变了即就地加密或解密现值；运营先手建了同键的平台参数即收为内建
/// 现有的值不符合登记的数据类型即 <c>SETTINGS_VALIDATION_002</c>（消息带参数键），不改它：值要由人改对
/// </remarks>
/// <param name="configs">参数仓储</param>
/// <param name="values">参数值规则</param>
/// <param name="sections">各包登记的内建参数</param>
/// <param name="ids">标识生成器</param>
public sealed class ConfigCatalogSeeder(IRepository<SysConfig, Guid> configs, ConfigValues values, IEnumerable<ConfigCatalogSection> sections, IIdGenerator<Guid> ids) : ISeeder
{
    /// <inheritdoc />
    public string Name => "settings.configs";

    /// <inheritdoc />
    public int Order => SeedOrders.PlatformData + 2;

    /// <inheritdoc />
    public async ValueTask<Result> SeedAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        const string platform = TenantIds.Platform;
        foreach (var descriptor in sections.SelectMany(static s => s.Configs))
        {
            var key = descriptor.Key;
            if (!ConfigValues.Matches(descriptor.DataType, descriptor.DefaultValue))
            {
                return Mismatch($"{key} defaultValue", descriptor.DataType);
            }

            var found = await configs.ListAsync(EntityQuery.All<SysConfig>().Where(new Specification<SysConfig>(c => c.OwnerTenantId == platform && c.Key == key)).Page(0, 1), context.CancellationToken).ConfigureAwait(false);
            if (found.Count == 0)
            {
                var config = new SysConfig
                {
                    Id = ids.NewId(),
                    OwnerTenantId = platform,
                    Key = key,
                    Order = descriptor.Order,
                    IsBuiltIn = true,
                    IsEncrypted = descriptor.IsEncrypted,
                };
                Align(config, descriptor);
                var added = await configs.AddAsync(config, context.CancellationToken).ConfigureAwait(false);
                if (added.IsFailure)
                {
                    return added.Error!;
                }

                continue;
            }

            var existing = found[0];
            var plaintext = values.PlaintextOf(existing);
            if (!plaintext.TryGetValue(out var value))
            {
                return plaintext.Error!;
            }

            var text = value.TryGetValue(out var stored) ? stored : null;
            if (!ConfigValues.Matches(descriptor.DataType, text))
            {
                return Mismatch(key, descriptor.DataType);
            }

            if (existing.IsBuiltIn && existing.IsEncrypted == descriptor.IsEncrypted && IsAligned(existing, descriptor))
            {
                continue;
            }

            existing.IsBuiltIn = true;
            if (existing.IsEncrypted != descriptor.IsEncrypted)
            {
                existing.Value = descriptor.IsEncrypted ? values.Protect(text) : text;
                existing.IsEncrypted = descriptor.IsEncrypted;
            }

            Align(existing, descriptor);
            var updated = await configs.UpdateAsync(existing, context.CancellationToken).ConfigureAwait(false);
            if (updated.IsFailure)
            {
                return updated.Error!;
            }
        }

        return Result.Success();
    }

    private static Error Mismatch(string field, ConfigDataType type)
        => Error.Validation(SettingsErrors.ValueTypeMismatch, SettingsMessages.ValueTypeMismatch, MessageArguments.Of(("field", field), ("type", type.ToString())));

    private static bool IsAligned(SysConfig config, ConfigDescriptor descriptor)
        => config.Name == descriptor.Name
            && config.Group == descriptor.Group
            && config.DataType == descriptor.DataType
            && config.DefaultValue == descriptor.DefaultValue
            && config.AllowTenantOverride == descriptor.AllowTenantOverride
            && config.Description == descriptor.Description;

    private static void Align(SysConfig config, ConfigDescriptor descriptor)
    {
        config.Name = descriptor.Name;
        config.Group = descriptor.Group;
        config.DataType = descriptor.DataType;
        config.DefaultValue = descriptor.DefaultValue;
        config.AllowTenantOverride = descriptor.AllowTenantOverride;
        config.Description = descriptor.Description;
    }
}
