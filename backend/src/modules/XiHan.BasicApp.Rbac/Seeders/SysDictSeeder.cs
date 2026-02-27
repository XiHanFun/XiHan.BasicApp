#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysDictSeeder
// Guid:5f9170a3-f8ea-4a40-8fcf-c61e0cc6cc11
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/12 12:10:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Rbac.Seeders;

/// <summary>
/// 系统字典种子数据
/// </summary>
public class SysDictSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysDictSeeder(ISqlSugarDbContext dbContext, ILogger<SysDictSeeder> logger, IServiceProvider serviceProvider)
        : base(dbContext, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 16;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Rbac]系统字典种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        if (await HasDataAsync<SysDict>(d => true))
        {
            Logger.LogInformation("系统字典数据已存在，跳过种子数据");
            return;
        }

        var dicts = new List<SysDict>
        {
            new() { DictCode = "sys_yes_no", DictName = "是否", DictType = "enum", DictDescription = "通用是否枚举", IsBuiltIn = true, Status = YesOrNo.Yes, Sort = 1 },
            new() { DictCode = "sys_user_gender", DictName = "用户性别", DictType = "enum", DictDescription = "用户性别枚举", IsBuiltIn = true, Status = YesOrNo.Yes, Sort = 2 },
            new() { DictCode = "sys_menu_type", DictName = "菜单类型", DictType = "enum", DictDescription = "系统菜单类型枚举", IsBuiltIn = true, Status = YesOrNo.Yes, Sort = 3 },
            new() { DictCode = "sys_resource_type", DictName = "资源类型", DictType = "enum", DictDescription = "系统资源类型枚举", IsBuiltIn = true, Status = YesOrNo.Yes, Sort = 4 },
            new() { DictCode = "codegen_template_type", DictName = "代码生成模板类型", DictType = "enum", DictDescription = "代码生成模板类型", IsBuiltIn = true, Status = YesOrNo.Yes, Sort = 5 },
            new() { DictCode = "codegen_gen_status", DictName = "代码生成状态", DictType = "enum", DictDescription = "代码生成执行状态", IsBuiltIn = true, Status = YesOrNo.Yes, Sort = 6 }
        };

        await BulkInsertAsync(dicts);
        Logger.LogInformation("成功初始化 {Count} 个系统字典", dicts.Count);
    }
}
