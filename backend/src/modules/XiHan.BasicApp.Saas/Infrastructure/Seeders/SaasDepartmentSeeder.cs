#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasDepartmentSeeder
// Guid:5f3a1e2d-8b6c-4790-ae12-c6d7890f1a3b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// SaaS 默认部门种子数据
/// </summary>
public sealed class SaasDepartmentSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasDepartmentSeeder> logger,
    IServiceProvider serviceProvider,
    ICurrentTenant currentTenant)
    : DataSeederBase(clientResolver, logger, serviceProvider)
{
    private const long DefaultTenantId = 1;
    private const string DefaultTenantCode = "default";

    private readonly ICurrentTenant _currentTenant = currentTenant;

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 30;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]默认部门种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var tenantId = await ResolveTenantIdAsync();
        if (tenantId == 0)
        {
            Logger.LogWarning("未找到默认租户，跳过部门种子数据");
            return;
        }

        using var tenantScope = _currentTenant.Change(tenantId, tenantId.ToString());
        var client = DbClient;

        var existingCount = await client.Queryable<SysDepartment>()
            .Where(dept => dept.TenantId == tenantId && !dept.IsDeleted)
            .CountAsync();
        if (existingCount > 0)
        {
            Logger.LogInformation("默认租户已存在部门数据，跳过种子数据");
            return;
        }

        var rootDept = await CreateDepartmentAsync(client, null, "XiHan", "xihan", DepartmentType.Company, "总公司", 10);
        var techDept = await CreateDepartmentAsync(client, rootDept.BasicId, "技术部", "tech", DepartmentType.Department, "技术研发中心", 20);
        var productDept = await CreateDepartmentAsync(client, rootDept.BasicId, "产品部", "product", DepartmentType.Department, "产品设计与管理", 30);
        var salesDept = await CreateDepartmentAsync(client, rootDept.BasicId, "销售部", "sales", DepartmentType.Department, "销售与客户管理", 40);
        var hrDept = await CreateDepartmentAsync(client, rootDept.BasicId, "人事部", "hr", DepartmentType.Department, "人力资源与行政", 50);
        var financeDept = await CreateDepartmentAsync(client, rootDept.BasicId, "财务部", "finance", DepartmentType.Department, "财务管理与审计", 60);
        _ = await CreateDepartmentAsync(client, techDept.BasicId, "前端组", "frontend", DepartmentType.Team, "Web 前端开发团队", 21);
        _ = await CreateDepartmentAsync(client, techDept.BasicId, "后端组", "backend", DepartmentType.Team, "后端服务开发团队", 22);
        _ = await CreateDepartmentAsync(client, techDept.BasicId, "测试组", "qa", DepartmentType.Team, "质量保证与测试团队", 23);

        Logger.LogInformation("成功初始化默认租户部门结构，创建 {Count} 个部门", 9);
    }

    private async Task<SysDepartment> CreateDepartmentAsync(
        ISqlSugarClient client,
        long? parentId,
        string name,
        string code,
        DepartmentType type,
        string description,
        int sort)
    {
        var dept = new SysDepartment
        {
            TenantId = DefaultTenantId,
            ParentId = parentId,
            DepartmentName = name,
            DepartmentCode = code,
            DepartmentType = type,
            Status = EnableStatus.Enabled,
            Sort = sort,
            Remark = description
        };

        var saved = await client.Insertable(dept).ExecuteReturnEntityAsync();
        return saved;
    }

    private async Task<long> ResolveTenantIdAsync()
    {
        using var platformScope = _currentTenant.Change(null);
        var client = DbClient;
        var tenant = await client.Queryable<SysTenant>()
            .FirstAsync(tenant => tenant.TenantCode == DefaultTenantCode);
        return tenant?.BasicId ?? 0;
    }
}
