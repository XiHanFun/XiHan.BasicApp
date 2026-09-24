// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Reflection;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Infrastructure.Seeders.System;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 套餐种子的手写权限清单只能含租户能生效的权限：平台侧的码进不了租户，写进清单只会被种子剔除并告警。
/// </summary>
public sealed class SaasTenantEditionSeederSideTests
{
    /// <summary>
    /// 免费版 / 基础版 / 专业版的手写清单：每个码都是已声明的 Saas 权限，且作用侧含租户
    /// </summary>
    [Fact]
    public void HandWrittenEditionLists_ShouldOnlyContainTenantEffectiveCodes()
    {
        var sideByCode = SaasPermissionDefinitions.All.ToDictionary(
            definition => definition.PermissionCode,
            definition => definition.Side,
            StringComparer.OrdinalIgnoreCase);

        var offenders = new List<string>();
        foreach (var (editionCode, permissionCodes) in ReadEditionDefinitions())
        {
            foreach (var code in permissionCodes)
            {
                if (!sideByCode.TryGetValue(code, out var side))
                {
                    offenders.Add($"{editionCode}: {code}（未声明）");
                }
                else if (!side.IsTenantEffective())
                {
                    offenders.Add($"{editionCode}: {code}（{side}）");
                }
            }
        }

        Assert.True(offenders.Count == 0, $"套餐手写清单含租户不能生效的权限：{string.Join("、", offenders)}");
    }

    /// <summary>
    /// 读取种子里的套餐定义（私有嵌套记录，按编码与权限码清单展开）
    /// </summary>
    private static IEnumerable<(string EditionCode, IReadOnlyCollection<string> PermissionCodes)> ReadEditionDefinitions()
    {
        var method = typeof(SaasTenantEditionSeeder).GetMethod("BuildDefinitions", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SaasTenantEditionSeeder 未找到 BuildDefinitions。");
        var definitions = (IEnumerable)method.Invoke(null, ["free"])!;

        foreach (var definition in definitions)
        {
            var type = definition.GetType();
            var editionCode = (string)type.GetProperty("EditionCode")!.GetValue(definition)!;
            var permissionCodes = (IReadOnlyCollection<string>)type.GetProperty("PermissionCodes")!.GetValue(definition)!;
            yield return (editionCode, permissionCodes);
        }
    }
}
