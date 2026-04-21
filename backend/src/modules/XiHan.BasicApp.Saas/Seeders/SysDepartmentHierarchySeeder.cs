#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysDepartmentHierarchySeeder
// Guid:18ea37b2-53cc-4da8-b0d5-a4f8fc9552ed
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/03 17:05:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Saas.Seeders;

/// <summary>
/// 系统部门层级种子数据。
/// </summary>
public class SysDepartmentHierarchySeeder : DataSeederBase
{
    public SysDepartmentHierarchySeeder(
        ISqlSugarClientResolver clientResolver,
        ILogger<SysDepartmentHierarchySeeder> logger,
        IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    public override int Order => SaasSeedOrder.DepartmentHierarchies;

    public override string Name => "[Saas]系统部门层级种子数据";

    protected override async Task SeedInternalAsync()
    {
        var departments = await DbClient.Queryable<SysDepartment>().ToListAsync();
        if (departments.Count == 0)
        {
            Logger.LogInformation("部门数据为空，跳过部门层级初始化");
            return;
        }

        var rows = BuildHierarchyRows(departments);
        if (rows.Count == 0)
        {
            Logger.LogInformation("未生成任何部门层级数据");
            return;
        }

        await DbClient.Deleteable<SysDepartmentHierarchy>().ExecuteCommandAsync();
        await DbClient.Insertable(rows).ExecuteCommandAsync();
        Logger.LogInformation("成功重建 {Count} 条部门层级数据", rows.Count);
    }

    private static List<SysDepartmentHierarchy> BuildHierarchyRows(IReadOnlyCollection<SysDepartment> departments)
    {
        var rows = new List<SysDepartmentHierarchy>(departments.Count * 3);
        foreach (var tenantGroup in departments.GroupBy(department => department.TenantId))
        {
            var map = tenantGroup.ToDictionary(department => department.BasicId);
            foreach (var department in tenantGroup)
            {
                var chain = BuildAncestorChain(department, map);
                for (var index = 0; index < chain.Count; index++)
                {
                    var ancestor = chain[index];
                    var pathDepartments = chain.Skip(index).ToArray();
                    rows.Add(new SysDepartmentHierarchy
                    {
                        TenantId = tenantGroup.Key,
                        AncestorId = ancestor.BasicId,
                        DescendantId = department.BasicId,
                        Depth = pathDepartments.Length - 1,
                        Path = string.Join("/", pathDepartments.Select(item => item.BasicId)),
                        PathName = string.Join("/", pathDepartments.Select(item => item.DepartmentName))
                    });
                }
            }
        }

        return rows;
    }

    private static IReadOnlyList<SysDepartment> BuildAncestorChain(
        SysDepartment department,
        IReadOnlyDictionary<long, SysDepartment> departmentMap)
    {
        var stack = new Stack<SysDepartment>();
        var visited = new HashSet<long>();
        var current = department;

        while (visited.Add(current.BasicId))
        {
            stack.Push(current);
            if (!current.ParentId.HasValue || current.ParentId.Value <= 0)
            {
                break;
            }

            if (!departmentMap.TryGetValue(current.ParentId.Value, out var parent))
            {
                break;
            }

            current = parent;
        }

        return stack.ToArray();
    }
}
