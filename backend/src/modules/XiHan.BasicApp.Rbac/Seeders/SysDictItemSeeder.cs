#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysDictItemSeeder
// Guid:de2d749e-0e7a-41ac-bfbc-6ff29bc0cb21
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/12 12:11:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Rbac.Seeders;

/// <summary>
/// 系统字典项种子数据
/// </summary>
public class SysDictItemSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysDictItemSeeder(ISqlSugarDbContext dbContext, ILogger<SysDictItemSeeder> logger, IServiceProvider serviceProvider)
        : base(dbContext, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 17;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Rbac]系统字典项种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        if (await HasDataAsync<SysDictItem>(d => true))
        {
            Logger.LogInformation("系统字典项数据已存在，跳过种子数据");
            return;
        }

        var dicts = await DbContext.GetClient().Queryable<SysDict>().ToListAsync();
        if (dicts.Count == 0)
        {
            Logger.LogWarning("系统字典数据不存在，跳过字典项种子数据");
            return;
        }

        var dictMap = dicts.ToDictionary(d => d.DictCode, d => d);
        var items = new List<SysDictItem>();

        AddItems(items, dictMap, "sys_yes_no", new[]
        {
            ("yes", "是", "1"),
            ("no", "否", "0")
        });

        AddItems(items, dictMap, "sys_user_gender", new[]
        {
            ("unknown", "未知", "0"),
            ("male", "男", "1"),
            ("female", "女", "2")
        });

        AddItems(items, dictMap, "sys_menu_type", new[]
        {
            ("directory", "目录", "0"),
            ("menu", "菜单", "1"),
            ("button", "按钮", "2")
        });

        AddItems(items, dictMap, "sys_resource_type", new[]
        {
            ("menu", "菜单", "0"),
            ("api", "接口", "1"),
            ("button", "按钮", "2"),
            ("file", "文件", "3"),
            ("data_table", "数据表", "4"),
            ("element", "页面元素", "5"),
            ("business_object", "业务对象", "6"),
            ("other", "其他", "99")
        });

        AddItems(items, dictMap, "codegen_template_type", new[]
        {
            ("single", "单表", "0"),
            ("tree", "树表", "1"),
            ("master_detail", "主子表", "2")
        });

        AddItems(items, dictMap, "codegen_gen_status", new[]
        {
            ("not_generated", "未生成", "0"),
            ("generated", "已生成", "1"),
            ("failed", "生成失败", "2")
        });

        if (items.Count == 0)
        {
            Logger.LogWarning("未生成任何字典项数据，跳过插入");
            return;
        }

        await BulkInsertAsync(items);
        Logger.LogInformation("成功初始化 {Count} 个系统字典项", items.Count);
    }

    private static void AddItems(
        List<SysDictItem> target,
        Dictionary<string, SysDict> dictMap,
        string dictCode,
        IEnumerable<(string itemCode, string itemName, string itemValue)> values)
    {
        if (!dictMap.TryGetValue(dictCode, out var dict))
        {
            return;
        }

        var sort = 1;
        foreach (var (itemCode, itemName, itemValue) in values)
        {
            target.Add(new SysDictItem
            {
                DictId = dict.BasicId,
                DictCode = dictCode,
                ItemCode = itemCode,
                ItemName = itemName,
                ItemValue = itemValue,
                ItemDescription = $"{dict.DictName}-{itemName}",
                Status = YesOrNo.Yes,
                IsDefault = sort == 1,
                Sort = sort++
            });
        }
    }
}
