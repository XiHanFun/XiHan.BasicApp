// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Infrastructure.Seeders;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Extensions;
using XiHan.Framework.Workflow.Abstractions.Definitions;

namespace XiHan.BasicApp.Workflow.Infrastructure.Seeders;

/// <summary>
/// 工作流演示流程种子：在演示租户「示例企业」里创建并发布四个演示流程，演示开关打开时才写
/// </summary>
/// <remarks>
/// 按流程编码先查后写（含已删除的行），已有的不改、删掉的不补回；演示租户或演示账号不在时跳过并记日志。
/// 定义经流程定义管理器创建与发布，发布时走引擎同一套结构校验。流程内容见 <see cref="WorkflowDemoCatalog"/>。
/// </remarks>
public sealed class WorkflowDemoDefinitionSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<WorkflowDemoDefinitionSeeder> logger,
    IServiceProvider serviceProvider)
    : DemoDataSeederBase(clientResolver, logger, serviceProvider)
{
    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => SeedOrders.Demo + 30;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Workflow]演示流程";

    /// <summary>
    /// 写入演示流程
    /// </summary>
    protected override async Task SeedDemoAsync()
    {
        var tenant = await DbClientFor<SysTenant>().Queryable<SysTenant>()
            .FirstAsync(tenant => tenant.TenantCode == WorkflowDemoCatalog.TenantCode);
        if (tenant is null)
        {
            Logger.LogWarning("{Seeder}：演示租户 {TenantCode} 不存在，跳过", Name, WorkflowDemoCatalog.TenantCode);
            return;
        }

        var tenantId = tenant.BasicId;
        var codes = WorkflowDemoCatalog.Codes.ToList();
        var existing = (await DbClientFor<SysWorkflowDefinition>().Queryable<SysWorkflowDefinition>()
                .ClearTenantAndSoftDeleteFilter()
                .Where(definition => definition.TenantId == tenantId && codes.Contains(definition.Code))
                .Select(definition => definition.Code)
                .ToListAsync())
            .ToHashSet(StringComparer.Ordinal);
        if (existing.Count == codes.Count)
        {
            Logger.LogInformation("{Seeder}：演示流程已齐全（{Existing} 个）", Name, existing.Count);
            return;
        }

        var userNames = WorkflowDemoCatalog.UserNames.ToList();
        var userIds = (await DbClientFor<SysUser>().Queryable<SysUser>()
                .ClearTenantFilter()
                .Where(user => user.TenantId == tenantId && userNames.Contains(user.UserName))
                .Select(user => new { user.UserName, user.BasicId })
                .ToListAsync())
            .ToDictionary(user => user.UserName, user => user.BasicId.ToString(CultureInfo.InvariantCulture), StringComparer.Ordinal);
        var missing = userNames.Where(name => !userIds.ContainsKey(name)).ToList();
        if (missing.Count > 0)
        {
            Logger.LogWarning("{Seeder}：演示租户缺少账号 {UserNames}，跳过", Name, string.Join("、", missing));
            return;
        }

        using var tenantScope = CurrentTenant.Change(tenantId, tenant.TenantName);
        using var scope = ServiceProvider.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionManager>();

        var added = 0;
        foreach (var definition in WorkflowDemoCatalog.Build(userIds).Where(definition => !existing.Contains(definition.Code)))
        {
            var created = await manager.CreateAsync(definition);
            _ = await manager.PublishAsync(created.Id);
            added++;
        }

        Logger.LogInformation("{Seeder}：新增并发布 {Added} 个演示流程，已有 {Existing} 个", Name, added, existing.Count);
    }
}
