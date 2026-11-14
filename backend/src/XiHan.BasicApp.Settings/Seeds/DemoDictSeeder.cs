// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Seeding;
using XiHan.BasicApp.Runtime.Seeding;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Infrastructure;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Seeds;

/// <summary>
/// 演示字典项
/// </summary>
/// <param name="Code">项编码</param>
/// <param name="Name">显示名</param>
/// <param name="Value">附加值</param>
/// <param name="Parent">上级项的编码（上级排在前面）</param>
/// <param name="IsDefault">是否缺省项</param>
/// <param name="Status">状态</param>
public sealed record DemoDictItem(string Code, string Name, string Value, string? Parent = null, bool IsDefault = false, SettingStatus Status = SettingStatus.Active);

/// <summary>
/// 演示字典
/// </summary>
/// <param name="Code">字典编码</param>
/// <param name="Name">名称</param>
/// <param name="Scenario">演示的情形（写进字典说明）</param>
/// <param name="Items">字典项</param>
public sealed record DemoDict(string Code, string Name, string Scenario, IReadOnlyList<DemoDictItem> Items);

/// <summary>
/// 演示字典种子
/// </summary>
/// <remarks>
/// 号段 900 + 2，<c>BasicApp:Seed:EnableDemoData</c> 打开才写
/// 平台字典（租户可见可用、改不了），演示缺省项、停用项与树形字典项；系统不依赖任何内置字典，这里只是示例
/// 按字典编码只首写：平台写过一次（含已删除的）或别的一侧已有同码（字典不覆盖）就不再碰
/// </remarks>
/// <param name="dicts">字典仓储</param>
/// <param name="items">字典项仓储</param>
/// <param name="options">种子配置</param>
/// <param name="ids">标识生成器</param>
/// <param name="sets">字典选项缓存</param>
public sealed class DemoDictSeeder(IRepository<SysDict, Guid> dicts, IRepository<SysDictItem, Guid> items, SeedOptions options, IIdGenerator<Guid> ids, DictOptionSets sets) : ISeeder
{
    /// <summary>
    /// 演示字典
    /// </summary>
    public static IReadOnlyList<DemoDict> Dicts { get; } =
    [
        new("demo_customer_level", "客户等级", "平铺字典：有缺省项，也有停用的项",
        [
            new("vip", "重要客户", "1"),
            new("normal", "普通客户", "2", IsDefault: true),
            new("potential", "潜在客户", "3"),
            new("blocked", "已拉黑（停用项）", "9", Status: SettingStatus.Disabled),
        ]),
        new("demo_region", "销售区域", "树形字典：大区下挂城市",
        [
            new("east", "华东", "east"),
            new("shanghai", "上海", "shanghai", "east"),
            new("hangzhou", "杭州", "hangzhou", "east"),
            new("north", "华北", "north"),
            new("beijing", "北京", "beijing", "north"),
        ]),
    ];

    /// <inheritdoc />
    public string Name => "settings.demo";

    /// <inheritdoc />
    public int Order => SeedOrders.Demo + 2;

    /// <inheritdoc />
    public async ValueTask<Result> SeedAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!options.EnableDemoData)
        {
            return Result.Success();
        }

        const string platform = TenantIds.Platform;
        foreach (var (demo, index) in Dicts.Select(static (dict, index) => (dict, index)))
        {
            var code = demo.Code;
            var written = EntityQuery.All<SysDict>().Where(new Specification<SysDict>(d => d.Code == code && (d.OwnerTenantId == platform || !d.IsDeleted))) with { IncludeDeleted = true };
            if (await dicts.CountAsync(written, context.CancellationToken).ConfigureAwait(false) > 0)
            {
                continue;
            }

            var dict = new SysDict { Id = ids.NewId(), OwnerTenantId = platform, Code = demo.Code, Name = demo.Name, Category = "business", Description = demo.Scenario, Order = (index + 1) * 10 };
            var added = await dicts.AddAsync(dict, context.CancellationToken).ConfigureAwait(false);
            if (added.IsFailure)
            {
                return added;
            }

            var rows = new Dictionary<string, SysDictItem>(StringComparer.Ordinal);
            foreach (var (item, itemIndex) in demo.Items.Select(static (item, itemIndex) => (item, itemIndex)))
            {
                var row = new SysDictItem { Id = ids.NewId(), OwnerTenantId = platform, DictId = dict.Id, ParentId = item.Parent is null ? null : rows[item.Parent].Id, Code = item.Code, Name = item.Name, Value = item.Value, IsDefault = item.IsDefault, Status = item.Status, Order = (itemIndex + 1) * 10 };
                rows[item.Code] = row;
            }

            foreach (var outcome in await items.AddRangeAsync([.. rows.Values], context.CancellationToken).ConfigureAwait(false))
            {
                if (outcome.IsFailure)
                {
                    return outcome;
                }
            }

            await sets.InvalidateAsync(platform, dict.Code, context.CancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }
}
