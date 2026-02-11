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
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Rbac.Seeders;

/// <summary>
/// 系统部门种子数据
/// </summary>
public class SysDepartmentSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysDepartmentSeeder(ISqlSugarDbContext dbContext, ILogger<SysDepartmentSeeder> logger, IServiceProvider serviceProvider)
        : base(dbContext, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 10;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Rbac]系统部门种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        if (await HasDataAsync<SysDepartment>(d => true))
        {
            Logger.LogInformation("系统部门数据已存在，跳过种子数据");
            return;
        }

        var departments = new List<SysDepartment>
        {
            // 根部门
            new()
            {
                ParentId = null,
                DepartmentCode = "ROOT",
                DepartmentName = "曦寒科技",
                DepartmentType = DepartmentType.Company,
                Remark = "公司根部门",
                Status = YesOrNo.Yes,
                Sort = 0
            },
            // 技术部
            new()
            {
                ParentId = null, // 将在插入后更新
                DepartmentCode = "TECH",
                DepartmentName = "技术部",
                DepartmentType = DepartmentType.Department,
                Remark = "技术研发部门",
                Status = YesOrNo.Yes,
                Sort = 1
            },
            // 产品部
            new()
            {
                ParentId = null,
                DepartmentCode = "PRODUCT",
                DepartmentName = "产品部",
                DepartmentType = DepartmentType.Department,
                Remark = "产品规划部门",
                Status = YesOrNo.Yes,
                Sort = 2
            },
            // 运营部
            new()
            {
                ParentId = null,
                DepartmentCode = "OPERATION",
                DepartmentName = "运营部",
                DepartmentType = DepartmentType.Department,
                Remark = "运营推广部门",
                Status = YesOrNo.Yes,
                Sort = 3
            },
            // 行政部
            new()
            {
                ParentId = null,
                DepartmentCode = "ADMIN",
                DepartmentName = "行政部",
                DepartmentType = DepartmentType.Department,
                Remark = "行政管理部门",
                Status = YesOrNo.Yes,
                Sort = 4
            }
        };

        // 批量插入
        await BulkInsertAsync(departments);

        // 更新子部门的 ParentId
        var rootDept = await DbContext.GetClient().Queryable<SysDepartment>().FirstAsync(d => d.DepartmentCode == "ROOT");
        if (rootDept != null)
        {
            await DbContext.GetClient().Updateable<SysDepartment>()
                .SetColumns(d => d.ParentId == rootDept.BasicId)
                .Where(d => d.DepartmentCode != "ROOT")
                .ExecuteCommandAsync();
        }

        Logger.LogInformation($"成功初始化 {departments.Count} 个系统部门");
    }
}
