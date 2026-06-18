#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasDictSeeder
// Guid:e4819afa-1304-4a15-99f7-b59a11e2e586
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/04 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using SqlSugar;
using System.ComponentModel;
using System.Text;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders.System;

/// <summary>
/// SaaS 系统字典种子数据
/// </summary>
public sealed class SaasDictSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasDictSeeder> logger,
    IServiceProvider serviceProvider,
    ICurrentTenant currentTenant)
    : DataSeederBase(clientResolver, logger, serviceProvider)
{
    /// <summary>
    /// 枚举绑定字典注册表：以枚举为唯一事实来源，字典项由反射自动生成。
    /// 新增/调整枚举值后，字典随之自动同步，无需两处维护。
    /// </summary>
    private static readonly IReadOnlyList<EnumDictRegistration> EnumDictRegistrations =
    [
        new("enable_status", "启用状态", "通用启用/禁用状态", 10, typeof(EnableStatus), 1),
        new("tenant_status", "租户状态", "租户生命周期状态", 30, typeof(TenantStatus), 0),
        new("tenant_config_status", "租户配置状态", "租户数据库和运行环境配置状态", 40, typeof(TenantConfigStatus), 0),
        new("tenant_isolation_mode", "租户隔离模式", "租户数据隔离策略", 50, typeof(TenantIsolationMode), 0),
        new("tenant_member_type", "租户成员类型", "用户在租户内的成员身份", 60, typeof(TenantMemberType), 0),
        new("tenant_member_invite_status", "租户成员邀请状态", "租户成员邀请和响应生命周期", 70, typeof(TenantMemberInviteStatus), 0),
        new("role_type", "角色类型", "角色定义类型", 80, typeof(RoleType), 0),
        new("data_permission_scope", "数据权限范围", "角色或用户授权的数据访问范围", 90, typeof(DataPermissionScope), 0),
        new("permission_type", "权限类型", "权限定义类型", 100, typeof(PermissionType), 1),
        new("user_gender", "用户性别", "用户资料性别选项", 110, typeof(UserGender), 0),
        new("config_type", "配置类型", "系统参数配置类型", 120, typeof(ConfigType), 0),
        new("config_data_type", "配置数据类型", "系统参数值数据类型", 130, typeof(ConfigDataType), 0),
        new("department_type", "部门类型", "组织架构部门分类", 140, typeof(DepartmentType), 6),
        new("message_channel", "消息发送渠道", "系统通知和消息的发送渠道", 150, typeof(MessageChannel), 0),
        new("task_status", "任务状态", "系统任务生命周期状态", 180, typeof(RunTaskStatus), 0)
    ];

    private readonly ICurrentTenant _currentTenant = currentTenant;

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 23;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]系统字典种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        using var platformScope = _currentTenant.Change(null);
        var client = DbClient;
        var definitions = BuildDefinitions();
        var dictResult = await EnsureDictsAsync(client, definitions);
        var itemResult = await EnsureDictItemsAsync(client, definitions, dictResult.Dicts);

        if (dictResult.AddCount == 0
            && dictResult.UpdateCount == 0
            && itemResult.AddCount == 0
            && itemResult.UpdateCount == 0)
        {
            Logger.LogInformation("SaaS 系统字典数据已存在，跳过种子数据");
            return;
        }

        Logger.LogInformation(
            "成功初始化 SaaS 系统字典，字典新增 {DictAddCount} 个，字典更新 {DictUpdateCount} 个，字典项新增 {ItemAddCount} 个，字典项更新 {ItemUpdateCount} 个",
            dictResult.AddCount,
            dictResult.UpdateCount,
            itemResult.AddCount,
            itemResult.UpdateCount);
    }

    private static async Task<DictSeedResult> EnsureDictsAsync(ISqlSugarClient client, IReadOnlyList<DictSeedDefinition> definitions)
    {
        var dictCodes = definitions.Select(definition => definition.DictCode).ToArray();
        var existingDicts = await client.Queryable<SysDict>()
            .Where(dict => dict.TenantId == 0 && dictCodes.Contains(dict.DictCode))
            .ToListAsync();
        var dictMap = existingDicts
            .GroupBy(dict => dict.DictCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.OrdinalIgnoreCase);

        var addCount = 0;
        var updateCount = 0;
        foreach (var definition in definitions)
        {
            if (dictMap.TryGetValue(definition.DictCode, out var existing))
            {
                if (ApplyDictDefinition(existing, definition))
                {
                    _ = await client.Updateable(existing).ExecuteCommandAsync();
                    updateCount++;
                }

                continue;
            }

            var dict = CreateDict(definition);
            var savedDict = await client.Insertable(dict).ExecuteReturnEntityAsync();
            dictMap[definition.DictCode] = savedDict;
            addCount++;
        }

        return new DictSeedResult(dictMap, addCount, updateCount);
    }

    private static async Task<DictItemSeedResult> EnsureDictItemsAsync(
        ISqlSugarClient client,
        IReadOnlyList<DictSeedDefinition> definitions,
        IReadOnlyDictionary<string, SysDict> dictMap)
    {
        var dictIds = dictMap.Values.Select(dict => dict.BasicId).ToArray();
        var existingItems = await client.Queryable<SysDictItem>()
            .Where(item => item.TenantId == 0 && dictIds.Contains(item.DictId))
            .ToListAsync();
        var itemMap = existingItems.ToDictionary(item => (item.DictId, item.ItemCode));

        var addList = new List<SysDictItem>();
        var updateCount = 0;
        foreach (var definition in definitions)
        {
            if (!dictMap.TryGetValue(definition.DictCode, out var dict))
            {
                continue;
            }

            foreach (var itemDefinition in definition.Items)
            {
                if (itemMap.TryGetValue((dict.BasicId, itemDefinition.ItemCode), out var existing))
                {
                    if (ApplyItemDefinition(existing, dict.BasicId, itemDefinition))
                    {
                        _ = await client.Updateable(existing).ExecuteCommandAsync();
                        updateCount++;
                    }

                    continue;
                }

                addList.Add(CreateItem(dict.BasicId, itemDefinition));
            }
        }

        if (addList.Count > 0)
        {
            await client.Insertable(addList).ExecuteReturnSnowflakeIdListAsync();
        }

        return new DictItemSeedResult(addList.Count, updateCount);
    }

    private static SysDict CreateDict(DictSeedDefinition definition)
    {
        var dict = new SysDict
        {
            DictCode = definition.DictCode
        };
        _ = ApplyDictDefinition(dict, definition);
        return dict;
    }

    private static bool ApplyDictDefinition(SysDict dict, DictSeedDefinition definition)
    {
        var changed = false;
        changed |= SetIfChanged(dict.TenantId, 0, value => dict.TenantId = value);
        changed |= SetIfChanged(dict.DictCode, definition.DictCode, value => dict.DictCode = value);
        changed |= SetIfChanged(dict.DictName, definition.DictName, value => dict.DictName = value);
        changed |= SetIfChanged(dict.DictType, "system", value => dict.DictType = value);
        changed |= SetIfChanged(dict.DictDescription, definition.DictDescription, value => dict.DictDescription = value);
        changed |= SetIfChanged(dict.IsBuiltIn, true, value => dict.IsBuiltIn = value);
        changed |= SetIfChanged(dict.Status, EnableStatus.Enabled, value => dict.Status = value);
        changed |= SetIfChanged(dict.Sort, definition.Sort, value => dict.Sort = value);
        changed |= SetIfChanged(dict.Remark, "系统初始化内置字典", value => dict.Remark = value);
        return changed;
    }

    private static SysDictItem CreateItem(long dictId, DictItemSeedDefinition definition)
    {
        var item = new SysDictItem
        {
            DictId = dictId,
            ItemCode = definition.ItemCode
        };
        _ = ApplyItemDefinition(item, dictId, definition);
        return item;
    }

    private static bool ApplyItemDefinition(SysDictItem item, long dictId, DictItemSeedDefinition definition)
    {
        var changed = false;
        changed |= SetIfChanged(item.TenantId, 0, value => item.TenantId = value);
        changed |= SetIfChanged(item.DictId, dictId, value => item.DictId = value);
        changed |= SetIfChanged(item.ParentId, null, value => item.ParentId = value);
        changed |= SetIfChanged(item.ItemCode, definition.ItemCode, value => item.ItemCode = value);
        changed |= SetIfChanged(item.ItemName, definition.ItemName, value => item.ItemName = value);
        changed |= SetIfChanged(item.ItemValue, definition.ItemValue, value => item.ItemValue = value);
        changed |= SetIfChanged(item.ItemDescription, definition.ItemDescription, value => item.ItemDescription = value);
        changed |= SetIfChanged(item.Metadata, null, value => item.Metadata = value);
        changed |= SetIfChanged(item.IsDefault, definition.IsDefault, value => item.IsDefault = value);
        changed |= SetIfChanged(item.Status, EnableStatus.Enabled, value => item.Status = value);
        changed |= SetIfChanged(item.Sort, definition.Sort, value => item.Sort = value);
        changed |= SetIfChanged(item.Remark, "系统初始化内置字典项", value => item.Remark = value);
        return changed;
    }

    private static IReadOnlyList<DictSeedDefinition> BuildDefinitions()
    {
        // 枚举绑定字典（反射生成）+ 纯业务字典（手工维护），两者合并
        return [.. BuildEnumDefinitions(), .. BuildManualDefinitions()];
    }

    /// <summary>
    /// 反射生成枚举绑定字典：字典项编码取枚举名 snake_case，名称取 [Description]，值取枚举整型值。
    /// </summary>
    private static IReadOnlyList<DictSeedDefinition> BuildEnumDefinitions()
    {
        var definitions = new List<DictSeedDefinition>(EnumDictRegistrations.Count);
        foreach (var registration in EnumDictRegistrations)
        {
            var items = new List<DictItemSeedDefinition>();
            var sort = 0;
            foreach (var value in Enum.GetValues(registration.EnumType))
            {
                var name = value.ToString() ?? string.Empty;
                var intValue = Convert.ToInt32(value);
                var displayName = GetEnumDescription(registration.EnumType, name);
                items.Add(new DictItemSeedDefinition(
                    ToSnakeCase(name),
                    displayName,
                    intValue.ToString(),
                    intValue == registration.DefaultValue,
                    displayName,
                    sort));
                sort++;
            }

            definitions.Add(new DictSeedDefinition(
                registration.DictCode,
                registration.DictName,
                registration.DictDescription,
                registration.Sort,
                items));
        }

        return definitions;
    }

    /// <summary>
    /// 纯业务字典：无对应枚举或与枚举语义存在偏差，保持手工维护。
    /// 注：resource_type 与 ResourceType 枚举、review_status 与 AuditStatus 枚举取值不一致，
    /// 暂列此处待后续单独评审是否并入枚举生成。
    /// </summary>
    private static IReadOnlyList<DictSeedDefinition> BuildManualDefinitions()
    {
        return
        [
            new("validity_status", "有效性状态", "授权、成员关系等关联数据的有效/无效状态", 20,
            [
                new("invalid", "无效", "0", false, "无效状态", 0),
                new("valid", "有效", "1", true, "有效状态", 1)
            ]),
            new("notification_priority", "通知优先级", "系统通知和消息的优先级", 160,
            [
                new("low", "低", "0", false, "低优先级通知", 0),
                new("normal", "普通", "1", true, "普通优先级通知", 1),
                new("high", "高", "2", false, "高优先级通知", 2),
                new("urgent", "紧急", "3", false, "紧急通知", 3)
            ]),
            new("resource_type", "资源类型", "权限管控的资源类型", 170,
            [
                new("api", "接口", "0", true, "API 接口资源", 0),
                new("file", "文件", "1", false, "文件资源", 1),
                new("data_table", "数据表", "2", false, "数据表资源", 2),
                new("business_object", "业务对象", "3", false, "业务对象资源", 3),
                new("other", "其他", "99", false, "其他资源类型", 99)
            ]),
            new("review_status", "审查状态", "审批流程状态", 190,
            [
                new("draft", "草稿", "0", true, "草稿状态", 0),
                new("pending", "待审批", "1", false, "等待审批", 1),
                new("approved", "已通过", "2", false, "审批通过", 2),
                new("rejected", "已驳回", "3", false, "审批驳回", 3),
                new("withdrawn", "已撤回", "4", false, "申请人撤回", 4),
                new("expired", "已过期", "5", false, "审批超时过期", 5),
                new("cancelled", "已取消", "6", false, "审批取消", 6)
            ])
        ];
    }

    /// <summary>
    /// 反射读取枚举成员的 <see cref="DescriptionAttribute"/>，无则回退成员名。
    /// </summary>
    private static string GetEnumDescription(Type enumType, string memberName)
    {
        var field = enumType.GetField(memberName);
        var attribute = field?.GetCustomAttributes(typeof(DescriptionAttribute), false);
        return attribute is { Length: > 0 } && attribute[0] is DescriptionAttribute description
            ? description.Description
            : memberName;
    }

    /// <summary>
    /// 将枚举成员名（PascalCase）转换为字典项编码（snake_case）。
    /// </summary>
    private static string ToSnakeCase(string name)
    {
        var builder = new StringBuilder(name.Length + 8);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(c));
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    private static bool SetIfChanged<T>(T current, T next, Action<T> setter)
    {
        if (EqualityComparer<T>.Default.Equals(current, next))
        {
            return false;
        }

        setter(next);
        return true;
    }

    private sealed record EnumDictRegistration(
        string DictCode,
        string DictName,
        string DictDescription,
        int Sort,
        Type EnumType,
        int DefaultValue);

    private sealed record DictSeedDefinition(
        string DictCode,
        string DictName,
        string DictDescription,
        int Sort,
        IReadOnlyList<DictItemSeedDefinition> Items);

    private sealed record DictItemSeedDefinition(
        string ItemCode,
        string ItemName,
        string ItemValue,
        bool IsDefault,
        string ItemDescription,
        int Sort);

    private sealed record DictSeedResult(
        IReadOnlyDictionary<string, SysDict> Dicts,
        int AddCount,
        int UpdateCount);

    private sealed record DictItemSeedResult(int AddCount, int UpdateCount);
}
