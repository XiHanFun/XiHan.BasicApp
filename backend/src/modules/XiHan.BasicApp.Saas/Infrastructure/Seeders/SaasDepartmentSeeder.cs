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
/// SaaS 默认部门种子数据（与演示用户部门名称对齐，并维护闭包表）
/// </summary>
public sealed class SaasDepartmentSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasDepartmentSeeder> logger,
    IServiceProvider serviceProvider,
    ICurrentTenant currentTenant)
    : DataSeederBase(clientResolver, logger, serviceProvider)
{
    private const string DefaultTenantCode = "default";
    private const string RootDepartmentCode = "xihan";

    private readonly ICurrentTenant _currentTenant = currentTenant;

    /// <summary>
    /// 种子数据优先级（需在演示用户之前）
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
        var created = 0;

        var rootDept = await EnsureDepartmentAsync(
            client, tenantId, null, "XiHan", RootDepartmentCode, DepartmentType.Company, "总公司", 10);
        created += rootDept.Created ? 1 : 0;

        var techDept = await EnsureDepartmentAsync(
            client, tenantId, rootDept.Department.BasicId, "技术部", "tech", DepartmentType.Department, "技术研发中心", 20);
        created += techDept.Created ? 1 : 0;

        created += (await EnsureDepartmentAsync(client, tenantId, rootDept.Department.BasicId, "产品部", "product", DepartmentType.Department, "产品设计与管理", 30)).Created ? 1 : 0;
        created += (await EnsureDepartmentAsync(client, tenantId, rootDept.Department.BasicId, "销售部", "sales", DepartmentType.Department, "销售与客户管理", 40)).Created ? 1 : 0;
        created += (await EnsureDepartmentAsync(client, tenantId, rootDept.Department.BasicId, "运营部", "ops", DepartmentType.Department, "运营与增长", 45)).Created ? 1 : 0;
        created += (await EnsureDepartmentAsync(client, tenantId, rootDept.Department.BasicId, "市场部", "marketing", DepartmentType.Department, "市场与品牌", 46)).Created ? 1 : 0;
        created += (await EnsureDepartmentAsync(client, tenantId, rootDept.Department.BasicId, "人事部", "hr", DepartmentType.Department, "人力资源与行政", 50)).Created ? 1 : 0;
        created += (await EnsureDepartmentAsync(client, tenantId, rootDept.Department.BasicId, "财务部", "finance", DepartmentType.Department, "财务管理与审计", 60)).Created ? 1 : 0;

        created += (await EnsureDepartmentAsync(client, tenantId, techDept.Department.BasicId, "前端组", "frontend", DepartmentType.Team, "Web 前端开发团队", 21)).Created ? 1 : 0;
        created += (await EnsureDepartmentAsync(client, tenantId, techDept.Department.BasicId, "后端组", "backend", DepartmentType.Team, "后端服务开发团队", 22)).Created ? 1 : 0;
        created += (await EnsureDepartmentAsync(client, tenantId, techDept.Department.BasicId, "测试组", "qa", DepartmentType.Team, "质量保证与测试团队", 23)).Created ? 1 : 0;

        await RebuildDepartmentHierarchyAsync(client, tenantId);

        Logger.LogInformation(
            "默认租户部门结构已就绪（本次新增 {Created} 个，编码标记 {Marker}）",
            created,
            RootDepartmentCode);
    }

    private static async Task<(SysDepartment Department, bool Created)> EnsureDepartmentAsync(
        ISqlSugarClient client,
        long tenantId,
        long? parentId,
        string name,
        string code,
        DepartmentType type,
        string description,
        int sort)
    {
        var existing = await client.Queryable<SysDepartment>()
            .FirstAsync(dept => dept.TenantId == tenantId && dept.DepartmentCode == code && !dept.IsDeleted);
        if (existing is not null)
        {
            return (existing, false);
        }

        var dept = new SysDepartment
        {
            TenantId = tenantId,
            ParentId = parentId,
            DepartmentName = name,
            DepartmentCode = code,
            DepartmentType = type,
            Status = EnableStatus.Enabled,
            Sort = sort,
            Remark = description,
        };

        var saved = await client.Insertable(dept).ExecuteReturnEntityAsync();
        return (saved, true);
    }

    private static async Task RebuildDepartmentHierarchyAsync(ISqlSugarClient client, long tenantId)
    {
        _ = await client.Deleteable<SysDepartmentHierarchy>()
            .Where(row => row.TenantId == tenantId)
            .ExecuteCommandAsync();

        var departments = await client.Queryable<SysDepartment>()
            .Where(dept => dept.TenantId == tenantId && !dept.IsDeleted)
            .ToListAsync();
        if (departments.Count == 0)
        {
            return;
        }

        var rows = BuildHierarchyRows(departments);
        foreach (var row in rows)
        {
            row.TenantId = tenantId;
        }

        if (rows.Count > 0)
        {
            _ = await client.Insertable(rows).ExecuteCommandAsync();
        }
    }

    private static List<SysDepartmentHierarchy> BuildHierarchyRows(IReadOnlyList<SysDepartment> departments)
    {
        var departmentMap = departments.ToDictionary(department => department.BasicId);
        var rows = new List<SysDepartmentHierarchy>();

        foreach (var department in departments
                     .OrderBy(department => department.ParentId ?? 0)
                     .ThenBy(department => department.Sort)
                     .ThenBy(department => department.DepartmentCode, StringComparer.Ordinal))
        {
            var chain = BuildAncestorChain(department, departmentMap);
            for (var depth = 0; depth < chain.Count; depth++)
            {
                var ancestor = chain[depth];
                var pathNodes = chain.Take(depth + 1).Reverse().ToArray();
                rows.Add(new SysDepartmentHierarchy
                {
                    AncestorId = ancestor.BasicId,
                    DescendantId = department.BasicId,
                    Depth = depth,
                    Path = string.Join("/", pathNodes.Select(node => node.BasicId)),
                    PathName = string.Join("/", pathNodes.Select(node => node.DepartmentName)),
                });
            }
        }

        return rows;
    }

    private static List<SysDepartment> BuildAncestorChain(
        SysDepartment department,
        IReadOnlyDictionary<long, SysDepartment> departmentMap)
    {
        var chain = new List<SysDepartment>();
        var visited = new HashSet<long>();
        var cursor = department;

        while (true)
        {
            if (!visited.Add(cursor.BasicId))
            {
                throw new InvalidOperationException("部门层级存在环路，不能重建闭包表。");
            }

            chain.Add(cursor);
            if (!cursor.ParentId.HasValue)
            {
                return chain;
            }

            if (!departmentMap.TryGetValue(cursor.ParentId.Value, out var parent))
            {
                throw new InvalidOperationException($"部门 {cursor.DepartmentCode} 的父级不存在，无法重建闭包表。");
            }

            cursor = parent;
        }
    }

    private async Task<long> ResolveTenantIdAsync()
    {
        using var platformScope = _currentTenant.Change(null);
        var client = DbClient;
        var tenant = await client.Queryable<SysTenant>()
            .FirstAsync(t => t.TenantCode == DefaultTenantCode);
        return tenant?.BasicId ?? 0;
    }
}
