// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Application.Pages;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// 业务模块菜单的汇总种子：在菜单阶段最后写入全部 <see cref="IMenuPageContribution"/>
/// </summary>
/// <remarks>
/// 排在全部平台模块的菜单之后，业务页面可以挂到平台模块的目录下（如开发中心）。
/// 业务模块（含代码生成产物）不各自占种子顺序号，也不需要登记种子：登记类按约定注册即被这里收齐。
/// 不同登记里出现相同的菜单码直接报错——两处声明同一个码，谁覆盖谁取决于执行顺序，不能静默。
/// </remarks>
public sealed class ContributedMenuSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<ContributedMenuSeeder> logger,
    IServiceProvider serviceProvider,
    IEnumerable<IMenuPageContribution> contributions)
    : PageRegistryMenuSeederBase(clientResolver, logger, serviceProvider)
{
    private readonly IReadOnlyList<IMenuPageContribution> _contributions =
        [.. contributions.OrderBy(contribution => contribution.Name, StringComparer.Ordinal)];

    /// <summary>
    /// 种子数据优先级：菜单阶段的业务号段，排在全部平台模块之后
    /// </summary>
    public override int Order => SeedOrders.Menus + SeedOrders.BusinessBand;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]业务模块菜单";

    /// <summary>
    /// 全部登记的页面（按登记名排序后依次展开，同一登记内父目录在前）
    /// </summary>
    protected override IReadOnlyList<PageDescriptor> Pages => [.. _contributions.SelectMany(contribution => contribution.Pages)];

    /// <summary>
    /// 全部登记的页面内按钮
    /// </summary>
    protected override IReadOnlyList<ButtonDescriptor> Buttons => [.. _contributions.SelectMany(contribution => contribution.Buttons)];

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override Task SeedInternalAsync()
    {
        if (_contributions.Count == 0)
        {
            return Task.CompletedTask;
        }

        var duplicated = _contributions
            .SelectMany(contribution => contribution.Pages.Select(page => (contribution.Name, page.Code))
                .Concat(contribution.Buttons.Select(button => (contribution.Name, button.Code))))
            .GroupBy(entry => entry.Code, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key}（{string.Join("、", group.Select(entry => entry.Name))}）")
            .ToList();
        if (duplicated.Count > 0)
        {
            throw new InvalidOperationException($"{Name}：以下菜单码被多处登记：{string.Join("；", duplicated)}。");
        }

        return base.SeedInternalAsync();
    }
}
