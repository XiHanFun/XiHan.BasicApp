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
using XiHan.BasicApp.Saas.Constants.Basic;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Saas.Seeders;

/// <summary>
/// 系统字典种子数据。
/// </summary>
public class SysDictSeeder : DataSeederBase
{
    public SysDictSeeder(
        ISqlSugarClientResolver clientResolver,
        ILogger<SysDictSeeder> logger,
        IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    public override int Order => SaasSeedOrder.Dicts;

    public override string Name => "[Saas]系统字典种子数据";

    protected override async Task SeedInternalAsync()
    {
        var templates = new List<SysDict>
        {
            Create("sys_yes_no", "是否", "enum", "通用是否枚举", 10),
            Create("sys_user_gender", "用户性别", "enum", "用户性别枚举", 20),
            Create("sys_menu_type", "菜单类型", "enum", "系统菜单类型枚举", 30),
            Create("sys_resource_type", "资源类型", "enum", "系统资源类型枚举", 40),
            Create("sys_tenant_member_type", "租户成员类型", "enum", "租户成员类型枚举", 50),
            Create("sys_tenant_invite_status", "租户成员邀请状态", "enum", "租户成员邀请状态枚举", 60),
            Create("sys_data_scope", "数据权限范围", "enum", "角色数据范围枚举", 70)
        };

        var codes = templates.Select(item => item.DictCode).ToArray();
        var existing = await DbClient
            .Queryable<SysDict>()
            .Where(dict => codes.Contains(dict.DictCode))
            .ToListAsync();

        var existingMap = existing.ToDictionary(dict => dict.DictCode, StringComparer.OrdinalIgnoreCase);
        var inserts = templates.Where(template => !existingMap.ContainsKey(template.DictCode)).ToList();
        if (inserts.Count > 0)
        {
            await BulkInsertAsync(inserts);
        }

        Logger.LogInformation("系统字典种子完成：新增 {Count} 个字典模板", inserts.Count);
    }

    private static SysDict Create(string code, string name, string type, string description, int sort)
    {
        return new SysDict
        {
            TenantId = RoleBasicConstants.PlatformTenantId,
            DictCode = code,
            DictName = name,
            DictType = type,
            DictDescription = description,
            IsBuiltIn = true,
            Status = YesOrNo.Yes,
            Sort = sort
        };
    }
}
