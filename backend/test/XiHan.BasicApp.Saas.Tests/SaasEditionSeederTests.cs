// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Infrastructure.Seeders;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 套餐种子的声明：四档套餐逐级包含，白名单只含租户能生效的权限，新租户默认免费版
/// </summary>
public sealed class SaasEditionSeederTests
{
    /// <summary>
    /// 四档套餐按编码唯一、按排序递增，只有免费版是默认套餐。
    /// </summary>
    [Fact]
    public void Editions_ShouldBeFourTiersWithFreeAsDefault()
    {
        var editions = SaasEditionSeeder.Editions;

        Assert.Equal(
            [SaasEditionSeeder.Codes.Free, SaasEditionSeeder.Codes.Basic, SaasEditionSeeder.Codes.Pro, SaasEditionSeeder.Codes.Enterprise],
            editions.Select(static edition => edition.Code));
        Assert.Equal(editions.Select(static edition => edition.Sort).Order(), editions.Select(static edition => edition.Sort));
        var defaultEdition = Assert.Single(editions, static edition => edition.IsDefault);
        Assert.Equal(SaasEditionSeeder.Codes.Free, defaultEdition.Code);
        Assert.True(defaultEdition.IsFree);
    }

    /// <summary>
    /// 手写白名单的每个码都是已声明的 SaaS 权限，且能在租户生效；平台侧的码进不了租户，写进来种子直接报错。
    /// </summary>
    [Fact]
    public void Whitelists_ShouldOnlyContainTenantEffectiveCodes()
    {
        var sideByCode = SaasPermissionDefinitions.All.ToDictionary(definition => definition.PermissionCode, definition => definition.Side, StringComparer.Ordinal);

        var offenders = SaasEditionSeeder.Editions
            .Where(static edition => edition.PermissionCodes is not null)
            .SelectMany(edition => edition.PermissionCodes!
                .Where(code => !sideByCode.TryGetValue(code, out var side) || !side.IsTenantEffective())
                .Select(code => $"{edition.Code}: {code}"))
            .ToList();

        Assert.True(offenders.Count == 0, $"套餐白名单含不存在或租户不能生效的权限：{string.Join("、", offenders)}");
    }

    /// <summary>
    /// 高一档包含低一档的全部功能；企业版不列清单，取租户能生效的全部权限（含各模块）。
    /// </summary>
    [Fact]
    public void Whitelists_ShouldBeCumulative()
    {
        var byCode = SaasEditionSeeder.Editions.ToDictionary(static edition => edition.Code, StringComparer.Ordinal);
        var free = byCode[SaasEditionSeeder.Codes.Free].PermissionCodes!;
        var basic = byCode[SaasEditionSeeder.Codes.Basic].PermissionCodes!;
        var pro = byCode[SaasEditionSeeder.Codes.Pro].PermissionCodes!;

        Assert.Empty(free.Except(basic, StringComparer.Ordinal));
        Assert.Empty(basic.Except(pro, StringComparer.Ordinal));
        Assert.True(basic.Count > free.Count && pro.Count > basic.Count);
        Assert.Null(byCode[SaasEditionSeeder.Codes.Enterprise].PermissionCodes);
    }

    /// <summary>
    /// 白名单里不重复列同一个码。
    /// </summary>
    [Fact]
    public void Whitelists_ShouldNotRepeatCodes()
    {
        foreach (var edition in SaasEditionSeeder.Editions.Where(static edition => edition.PermissionCodes is not null))
        {
            Assert.Equal(edition.PermissionCodes!.Count, edition.PermissionCodes.Distinct(StringComparer.Ordinal).Count());
        }
    }
}
