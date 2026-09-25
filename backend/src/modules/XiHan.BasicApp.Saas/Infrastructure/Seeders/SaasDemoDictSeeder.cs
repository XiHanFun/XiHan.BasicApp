// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// 演示字典项（子项排在父项之后）
/// </summary>
/// <param name="Code">字典项编码</param>
/// <param name="Name">字典项名称</param>
/// <param name="Value">字典项值</param>
/// <param name="Parent">父项编码</param>
/// <param name="IsDefault">是否默认项</param>
/// <param name="Status">启停</param>
public sealed record DemoDictItem(string Code, string Name, string Value, string? Parent = null, bool IsDefault = false, EnableStatus Status = EnableStatus.Enabled);

/// <summary>
/// 演示字典
/// </summary>
/// <param name="Code">字典编码</param>
/// <param name="Name">字典名称</param>
/// <param name="Scenario">演示的情况（写入字典说明）</param>
/// <param name="Items">字典项</param>
public sealed record DemoDict(string Code, string Name, string Scenario, IReadOnlyList<DemoDictItem> Items);

/// <summary>
/// SaaS 演示字典：平台全局字典（租户可见可用、改不了），演示默认项、停用项与树形字典项
/// </summary>
/// <remarks>系统不依赖任何内置字典，这里只是示例。按字典编码判断写过没有（连同删掉的一起算），已有的不覆盖。</remarks>
public sealed class SaasDemoDictSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasDemoDictSeeder> logger,
    IServiceProvider serviceProvider)
    : DemoDataSeederBase(clientResolver, logger, serviceProvider)
{
    /// <summary>
    /// 演示字典
    /// </summary>
    public static IReadOnlyList<DemoDict> Dicts { get; } =
    [
        new("demo_customer_level", "客户等级", "平铺字典：有默认项，也有停用的项",
        [
            new("vip", "重要客户", "1"),
            new("normal", "普通客户", "2", IsDefault: true),
            new("potential", "潜在客户", "3"),
            new("blocked", "已拉黑（停用项）", "9", Status: EnableStatus.Disabled),
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

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => SeedOrders.Demo + 2;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]演示字典";

    /// <summary>
    /// 写入演示数据
    /// </summary>
    protected override async Task SeedDemoAsync()
    {
        var codes = Dicts.Select(static dict => dict.Code).ToList();
        var existing = (await DbClientFor<SysDict>().Queryable<SysDict>()
                .IncludingDeleted()
                .Where(dict => dict.TenantId == 0 && codes.Contains(dict.DictCode))
                .Select(dict => dict.DictCode)
                .ToListAsync())
            .ToHashSet(StringComparer.Ordinal);

        var added = 0;
        foreach (var (demo, index) in Dicts.Select((dict, index) => (dict, index)))
        {
            if (existing.Contains(demo.Code))
            {
                continue;
            }

            var dict = await DbClientFor<SysDict>().Insertable(new SysDict
            {
                TenantId = 0,
                DictCode = demo.Code,
                DictName = demo.Name,
                DictType = "business",
                DictDescription = demo.Scenario,
                IsBuiltIn = false,
                Status = EnableStatus.Enabled,
                Sort = (index + 1) * 10,
                Remark = "演示数据"
            }).ExecuteReturnEntityAsync();

            var itemIds = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (var (item, itemIndex) in demo.Items.Select((item, itemIndex) => (item, itemIndex)))
            {
                var saved = await DbClientFor<SysDictItem>().Insertable(new SysDictItem
                {
                    TenantId = 0,
                    DictId = dict.BasicId,
                    ParentId = item.Parent is null ? null : itemIds[item.Parent],
                    ItemCode = item.Code,
                    ItemName = item.Name,
                    ItemValue = item.Value,
                    IsDefault = item.IsDefault,
                    Status = item.Status,
                    Sort = (itemIndex + 1) * 10,
                    Remark = "演示数据"
                }).ExecuteReturnEntityAsync();
                itemIds[item.Code] = saved.BasicId;
            }

            added++;
        }

        Logger.LogInformation("{Seeder}：新增 {Count} 个字典，已有的不覆盖", Name, added);
    }
}
