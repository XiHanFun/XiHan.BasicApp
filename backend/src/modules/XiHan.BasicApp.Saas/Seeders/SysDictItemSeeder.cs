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
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Saas.Seeders;

/// <summary>
/// 系统字典项种子数据。
/// </summary>
public class SysDictItemSeeder : DataSeederBase
{
    public SysDictItemSeeder(
        ISqlSugarClientResolver clientResolver,
        ILogger<SysDictItemSeeder> logger,
        IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    public override int Order => SaasSeedOrder.DictItems;

    public override string Name => "[Saas]系统字典项种子数据";

    protected override async Task SeedInternalAsync()
    {
        var dicts = await DbClient.Queryable<SysDict>().ToListAsync();
        if (dicts.Count == 0)
        {
            Logger.LogWarning("系统字典不存在，跳过字典项初始化");
            return;
        }

        var dictMap = dicts.ToDictionary(dict => dict.DictCode, StringComparer.OrdinalIgnoreCase);
        var templates = new List<SysDictItem>();

        AddItems(templates, dictMap, "sys_yes_no", [("yes", "是", "1"), ("no", "否", "0")]);
        AddItems(templates, dictMap, "sys_user_gender", [("unknown", "未知", "0"), ("male", "男", "1"), ("female", "女", "2")]);
        AddItems(templates, dictMap, "sys_menu_type", [("directory", "目录", "0"), ("menu", "菜单", "1"), ("button", "按钮", "2")]);
        AddItems(templates, dictMap, "sys_resource_type", [("api", "接口", "1"), ("file", "文件", "3"), ("data_table", "数据表", "4"), ("business_object", "业务对象", "6")]);
        AddItems(templates, dictMap, "sys_tenant_member_type", [("owner", "租户所有者", "0"), ("admin", "租户管理员", "1"), ("member", "普通成员", "2"), ("external", "外部协作者", "3"), ("guest", "访客", "4"), ("consultant", "顾问", "5"), ("platform_admin", "平台管理员", "99")]);
        AddItems(templates, dictMap, "sys_tenant_invite_status", [("pending", "待接受", "0"), ("accepted", "已接受", "1"), ("rejected", "已拒绝", "2"), ("revoked", "已撤销", "3"), ("expired", "已过期", "4")]);
        AddItems(templates, dictMap, "sys_data_scope", [("self_only", "仅本人", "0"), ("department_only", "本部门", "1"), ("department_and_children", "本部门及子部门", "2"), ("all", "全部数据", "3"), ("custom", "自定义", "99")]);

        var existing = await DbClient.Queryable<SysDictItem>().ToListAsync();
        var existingSet = existing
            .Select(item => $"{item.DictId}:{item.ItemCode}")
            .ToHashSet(StringComparer.Ordinal);

        var inserts = templates
            .Where(template => !existingSet.Contains($"{template.DictId}:{template.ItemCode}"))
            .ToList();

        if (inserts.Count > 0)
        {
            await BulkInsertAsync(inserts);
        }

        Logger.LogInformation("系统字典项种子完成：新增 {Count} 个字典项模板", inserts.Count);
    }

    private static void AddItems(
        ICollection<SysDictItem> target,
        IReadOnlyDictionary<string, SysDict> dictMap,
        string dictCode,
        IEnumerable<(string code, string name, string value)> values)
    {
        if (!dictMap.TryGetValue(dictCode, out var dict))
        {
            return;
        }

        var sort = 10;
        var first = true;
        foreach (var (code, name, value) in values)
        {
            target.Add(new SysDictItem
            {
                TenantId = dict.TenantId,
                DictId = dict.BasicId,
                ItemCode = code,
                ItemName = name,
                ItemValue = value,
                ItemDescription = $"{dict.DictName}-{name}",
                IsDefault = first,
                Status = YesOrNo.Yes,
                Sort = sort
            });

            sort += 10;
            first = false;
        }
    }
}
