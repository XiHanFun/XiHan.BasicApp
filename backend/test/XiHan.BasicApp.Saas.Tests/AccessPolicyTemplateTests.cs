// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Conditions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Authorization.Conditions;
using static XiHan.BasicApp.Saas.Tests.AccessPolicyTestKit;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 预置模板：条件都能通过保存校验；拒绝类写成停用；几条典型模板按预期命中
/// </summary>
public sealed class AccessPolicyTemplateTests
{
    /// <summary>
    /// 每个模板的条件都能保存，编码不重复，拒绝类在平台写成停用
    /// </summary>
    [Fact]
    public void Templates_AreValidAndDenyTemplatesStartDisabled()
    {
        Assert.Equal(AccessPolicyTemplates.All.Count, AccessPolicyTemplates.All.Select(template => template.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        foreach (var template in AccessPolicyTemplates.All)
        {
            Compiler().Validate(template.Conditions);
            if (template.Action == AccessPolicyAction.Deny)
            {
                Assert.Equal(EnableStatus.Disabled, template.PlatformStatus);
            }
        }
    }

    /// <summary>
    /// 非工作时间：周末全天、工作日 08:00–20:00 之外（Asia/Shanghai）
    /// </summary>
    [Theory]
    [InlineData("2026-10-24T02:00:00Z", true)]
    [InlineData("2026-10-22T02:00:00Z", false)]
    [InlineData("2026-10-22T13:30:00Z", true)]
    public async Task OffHours_MatchesWeekendAndEvenings(string utc, bool matched)
    {
        var now = DateTimeOffset.Parse(utc, System.Globalization.CultureInfo.InvariantCulture);
        var source = new DictionaryConditionAttributeSource(new Dictionary<string, object?>
        {
            [SaasConditionAttributes.EnvironmentWeekday] = ((int)TimeZoneInfo.ConvertTime(now, TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai")).DayOfWeek + 6) % 7 + 1,
            [SaasConditionAttributes.EnvironmentTime] = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai")).DateTime)
        });

        Assert.Equal(matched, await MatchesAsync("preset.off-hours", source));
    }

    /// <summary>
    /// 境外访问：国家不是中国的命中，查不到国家的不算
    /// </summary>
    [Theory]
    [InlineData("Japan", true)]
    [InlineData("中国", false)]
    [InlineData(null, false)]
    public async Task Overseas_IgnoresUnknownCountry(string? country, bool matched)
    {
        var source = new DictionaryConditionAttributeSource(new Dictionary<string, object?> { [SaasConditionAttributes.EnvironmentGeoCountry] = country });

        Assert.Equal(matched, await MatchesAsync("preset.overseas", source));
    }

    /// <summary>
    /// 爬虫与脚本：空 UA 或脚本工具 UA 命中，开放接口签名调用除外
    /// </summary>
    [Theory]
    [InlineData("curl/8.4.0", SaasConditionClientKinds.Anonymous, true)]
    [InlineData(null, SaasConditionClientKinds.Web, true)]
    [InlineData("curl/8.4.0", SaasConditionClientKinds.OpenApi, false)]
    [InlineData("Mozilla/5.0 (Windows NT 10.0) Chrome/130.0", SaasConditionClientKinds.Web, false)]
    public async Task BlockScripts_ExemptsOpenApi(string? userAgent, string clientKind, bool matched)
    {
        var source = new DictionaryConditionAttributeSource(new Dictionary<string, object?>
        {
            [SaasConditionAttributes.EnvironmentClientUserAgent] = userAgent,
            [SaasConditionAttributes.EnvironmentClientKind] = clientKind
        });

        Assert.Equal(matched, await MatchesAsync("preset.block-scripts", source));
    }

    private static async Task<bool> MatchesAsync(string code, IConditionAttributeSource source)
    {
        var template = AccessPolicyTemplates.Find(code)!;
        var result = await new DefaultConditionEvaluator().EvaluateAsync(Compiler().Compile(template.Conditions), source);
        return result.IsMatched;
    }
}
