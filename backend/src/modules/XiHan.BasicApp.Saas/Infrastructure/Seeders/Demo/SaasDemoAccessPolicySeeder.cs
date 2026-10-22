// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Conditions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// SaaS 演示访问策略：企业版演示租户里两条只记录的策略（非工作时间、境外访问），让命中记录页有数据
/// </summary>
/// <remarks>
/// 按编码判断写过没有（连同删掉的一起算），已有的不覆盖。
/// </remarks>
public sealed class SaasDemoAccessPolicySeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasDemoAccessPolicySeeder> logger,
    IServiceProvider serviceProvider)
    : DemoDataSeederBase(clientResolver, logger, serviceProvider)
{
    private static readonly (string Code, string TemplateCode)[] DemoPolicies =
    [
        ("demo.off-hours", "preset.off-hours"),
        ("demo.overseas", "preset.overseas")
    ];

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => SeedOrders.Demo + 3;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]演示访问策略";

    /// <summary>
    /// 写入演示数据
    /// </summary>
    protected override async Task SeedDemoAsync()
    {
        var tenant = await DbClientFor<SysTenant>().Queryable<SysTenant>().FirstAsync(item => item.TenantCode == "demo-enterprise");
        if (tenant is null)
        {
            Logger.LogInformation("{Seeder}：没有企业版演示租户，跳过", Name);
            return;
        }

        var client = DbClientFor<SysAccessPolicy>();
        var codes = DemoPolicies.Select(item => item.Code).ToList();
        var existing = (await client.Queryable<SysAccessPolicy>()
                .IncludingDeleted()
                .Where(policy => policy.TenantId == tenant.BasicId && codes.Contains(policy.PolicyCode))
                .Select(policy => policy.PolicyCode)
                .ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var added = 0;
        using var tenantScope = CurrentTenant.Change(tenant.BasicId, tenant.TenantName);
        foreach (var (code, templateCode) in DemoPolicies.Where(item => !existing.Contains(item.Code)))
        {
            var template = AccessPolicyTemplates.Find(templateCode)
                ?? throw new InvalidOperationException($"{Name}：找不到模板 {templateCode}。");
            var policy = await client.Insertable(new SysAccessPolicy
            {
                TenantId = tenant.BasicId,
                PolicyCode = code,
                PolicyName = template.Name,
                Description = template.Description,
                Action = AccessPolicyAction.LogOnly,
                Priority = template.Priority,
                Status = EnableStatus.Enabled,
                TemplateCode = template.Code,
                Remark = "演示：只记录，命中记录页可见"
            }).ExecuteReturnEntityAsync();
            _ = await client.Insertable(template.Conditions.Select(condition => new SysAccessPolicyCondition
            {
                TenantId = tenant.BasicId,
                PolicyId = policy.BasicId,
                ConditionGroup = condition.ConditionGroup,
                AttributeName = condition.AttributeName,
                Operator = condition.Operator,
                IsNegated = condition.IsNegated,
                ValueType = condition.ValueType,
                ConditionValue = condition.ConditionValue,
                Description = condition.Description
            }).ToList()).ExecuteCommandAsync();
            added++;
        }

        Logger.LogInformation("{Seeder}：新增 {Added} 条，已有 {Existing} 条不覆盖", Name, added, existing.Count);
    }
}
