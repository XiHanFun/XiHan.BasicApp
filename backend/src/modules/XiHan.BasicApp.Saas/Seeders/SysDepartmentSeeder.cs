#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysDepartmentSeeder
// Guid:4d5e6f78-9012-3456-def0-334455667788
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/07 12:30:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Saas.Seeders;

/// <summary>
/// 系统部门种子数据。
/// </summary>
public class SysDepartmentSeeder : DataSeederBase
{
    public SysDepartmentSeeder(
        ISqlSugarClientResolver clientResolver,
        ILogger<SysDepartmentSeeder> logger,
        IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    public override int Order => SaasSeedOrder.Departments;

    public override string Name => "[Saas]系统部门种子数据";

    protected override async Task SeedInternalAsync()
    {
        var bootstrapTenant = await DbClient
            .Queryable<SysTenant>()
            .FirstAsync(tenant => tenant.TenantCode == SaasSeedDefaults.BootstrapTenantCode);

        if (bootstrapTenant is null)
        {
            Logger.LogWarning("默认租户不存在，跳过部门初始化");
            return;
        }

        var templates = new[]
        {
            new DepartmentTemplate("ROOT", "默认租户总部", DepartmentType.Company, null, 0, "默认租户组织根节点"),
            new DepartmentTemplate("OPS", "平台运营部", DepartmentType.Department, "ROOT", 10, "负责默认租户运营与客服"),
            new DepartmentTemplate("DELIVERY", "交付实施部", DepartmentType.Department, "ROOT", 20, "负责租户交付与实施"),
            new DepartmentTemplate("SECURITY", "安全与合规部", DepartmentType.Department, "ROOT", 30, "负责权限、审计与合规治理")
        };

        var existingDepartments = await DbClient
            .Queryable<SysDepartment>()
            .Where(department => department.TenantId == bootstrapTenant.BasicId)
            .ToListAsync();
        var existingMap = existingDepartments.ToDictionary(department => department.DepartmentCode, StringComparer.OrdinalIgnoreCase);

        var inserts = templates
            .Where(template => !existingMap.ContainsKey(template.Code))
            .Select(template => new SysDepartment
            {
                TenantId = bootstrapTenant.BasicId,
                DepartmentCode = template.Code,
                DepartmentName = template.Name,
                DepartmentType = template.Type,
                Status = YesOrNo.Yes,
                Sort = template.Sort,
                Remark = template.Remark
            })
            .ToList();

        if (inserts.Count > 0)
        {
            await BulkInsertAsync(inserts);
            existingDepartments = await DbClient
                .Queryable<SysDepartment>()
                .Where(department => department.TenantId == bootstrapTenant.BasicId)
                .ToListAsync();
            existingMap = existingDepartments.ToDictionary(department => department.DepartmentCode, StringComparer.OrdinalIgnoreCase);
        }

        foreach (var template in templates)
        {
            if (!existingMap.TryGetValue(template.Code, out var department))
            {
                continue;
            }

            long? parentId = null;
            if (!string.IsNullOrWhiteSpace(template.ParentCode) && existingMap.TryGetValue(template.ParentCode, out var parent))
            {
                parentId = parent.BasicId;
            }

            if (department.ParentId == parentId && department.Status == YesOrNo.Yes)
            {
                continue;
            }

            await DbClient
                .Updateable<SysDepartment>()
                .SetColumns(item => new SysDepartment
                {
                    ParentId = parentId,
                    Status = YesOrNo.Yes
                })
                .Where(item => item.BasicId == department.BasicId)
                .ExecuteCommandAsync();
        }

        Logger.LogInformation(
            "默认租户部门种子完成：新增 {InsertCount} 个部门模板",
            inserts.Count);
    }

    private sealed record DepartmentTemplate(
        string Code,
        string Name,
        DepartmentType Type,
        string? ParentCode,
        int Sort,
        string Remark);
}
