// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Conditions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// SaaS 访问策略：平台的预置策略
/// </summary>
/// <remarks>
/// 按 <see cref="AccessPolicyTemplates"/> 写入：拒绝类写成停用，只记录类写成启用。
/// 按编码判断写过没有（连同删掉的一起算），已有的不覆盖、删掉的不补回。
/// </remarks>
public sealed class SaasAccessPolicySeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasAccessPolicySeeder> logger,
    IServiceProvider serviceProvider)
    : PlatformDataSeederBase(clientResolver, logger, serviceProvider)
{
    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => SeedOrders.PlatformData + 5;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]预置访问策略";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var codes = AccessPolicyTemplates.All.Select(template => template.Code).ToList();
        var existing = (await DbClient.Queryable<SysAccessPolicy>()
                .IncludingDeleted()
                .Where(policy => policy.TenantId == 0 && codes.Contains(policy.PolicyCode))
                .Select(policy => policy.PolicyCode)
                .ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var added = 0;
        foreach (var template in AccessPolicyTemplates.All.Where(template => !existing.Contains(template.Code)))
        {
            var policy = await DbClient.Insertable(new SysAccessPolicy
            {
                TenantId = 0,
                PolicyCode = template.Code,
                PolicyName = template.Name,
                Description = template.Description,
                Action = template.Action,
                Priority = template.Priority,
                Status = template.PlatformStatus,
                TemplateCode = template.Code,
                Remark = "系统预置"
            }).ExecuteReturnEntityAsync();
            _ = await DbClient.Insertable(template.Conditions.Select(condition => new SysAccessPolicyCondition
            {
                TenantId = 0,
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
