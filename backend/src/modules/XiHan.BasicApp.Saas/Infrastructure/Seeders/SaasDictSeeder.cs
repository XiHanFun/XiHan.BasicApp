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
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

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
        return
        [
            new("enable_status", "启用状态", "通用启用/禁用状态", 10,
            [
                new("disabled", "禁用", "0", false, "禁用状态", 0),
                new("enabled", "启用", "1", true, "启用状态", 1)
            ]),
            new("validity_status", "有效性状态", "授权、成员关系等关联数据的有效/无效状态", 20,
            [
                new("invalid", "无效", "0", false, "无效状态", 0),
                new("valid", "有效", "1", true, "有效状态", 1)
            ]),
            new("tenant_status", "租户状态", "租户生命周期状态", 30,
            [
                new("normal", "正常", "0", true, "正常运营", 0),
                new("suspended", "暂停", "1", false, "管理员手动暂停", 1),
                new("expired", "过期", "2", false, "订阅到期", 2),
                new("disabled", "禁用", "3", false, "停用归档", 3)
            ]),
            new("tenant_config_status", "租户配置状态", "租户数据库和运行环境配置状态", 40,
            [
                new("pending", "待配置", "0", true, "等待配置", 0),
                new("configuring", "配置中", "1", false, "正在配置", 1),
                new("configured", "已配置", "2", false, "配置完成", 2),
                new("failed", "配置失败", "3", false, "配置失败", 3),
                new("disabled", "已停用", "4", false, "配置停用", 4)
            ]),
            new("tenant_isolation_mode", "租户隔离模式", "租户数据隔离策略", 50,
            [
                new("field", "字段隔离", "0", true, "通过 TenantId 字段隔离", 0),
                new("database", "数据库隔离", "1", false, "每租户独立数据库", 1),
                new("schema", "Schema 隔离", "2", false, "同库不同 Schema", 2)
            ]),
            new("tenant_member_type", "租户成员类型", "用户在租户内的成员身份", 60,
            [
                new("owner", "租户所有者", "0", true, "租户最高权限成员", 0),
                new("admin", "租户管理员", "1", false, "租户日常管理员", 1),
                new("member", "普通成员", "2", false, "租户普通成员", 2),
                new("external", "外部协作者", "3", false, "外部组织成员", 3),
                new("guest", "访客", "4", false, "短期临时访问", 4),
                new("consultant", "顾问", "5", false, "咨询或审计访问", 5),
                new("platform_admin", "平台管理员", "99", false, "平台运营身份", 99)
            ]),
            new("tenant_member_invite_status", "租户成员邀请状态", "租户成员邀请和响应生命周期", 70,
            [
                new("pending", "待接受", "0", true, "邀请等待响应", 0),
                new("accepted", "已接受", "1", false, "成员身份生效", 1),
                new("rejected", "已拒绝", "2", false, "用户拒绝邀请", 2),
                new("revoked", "已撤销", "3", false, "管理员撤销成员身份", 3),
                new("expired", "已过期", "4", false, "邀请或成员身份过期", 4)
            ]),
            new("role_type", "角色类型", "角色定义类型", 80,
            [
                new("system", "系统", "0", true, "系统内置角色", 0),
                new("business", "业务", "1", false, "业务角色", 1),
                new("custom", "自定义", "2", false, "租户自定义角色", 2)
            ]),
            new("data_permission_scope", "数据权限范围", "角色或用户授权的数据访问范围", 90,
            [
                new("self_only", "本人数据", "0", true, "仅本人数据", 0),
                new("department_only", "本部门", "1", false, "仅本部门数据", 1),
                new("department_and_children", "本部门及下级", "2", false, "本部门及子部门数据", 2),
                new("all", "全部数据", "3", false, "全部数据", 3),
                new("custom", "自定义", "99", false, "自定义数据范围", 99)
            ]),
            new("permission_type", "权限类型", "权限定义类型", 100,
            [
                new("resource_based", "资源操作", "0", false, "绑定资源和操作", 0),
                new("functional", "功能", "1", true, "功能权限", 1),
                new("data_scope", "数据范围", "2", false, "数据范围权限", 2)
            ]),
            new("user_gender", "用户性别", "用户资料性别选项", 110,
            [
                new("unknown", "未知", "0", true, "未知或未设置", 0),
                new("male", "男", "1", false, "男性", 1),
                new("female", "女", "2", false, "女性", 2)
            ]),
            new("config_type", "配置类型", "系统参数配置类型", 120,
            [
                new("system", "系统配置", "0", true, "系统配置", 0),
                new("user", "用户配置", "1", false, "用户配置", 1),
                new("application", "应用配置", "2", false, "应用配置", 2),
                new("business", "业务配置", "3", false, "业务配置", 3)
            ]),
            new("config_data_type", "配置数据类型", "系统参数值数据类型", 130,
            [
                new("string", "字符串", "0", true, "字符串", 0),
                new("number", "数字", "1", false, "数字", 1),
                new("boolean", "布尔值", "2", false, "布尔值", 2),
                new("json", "JSON对象", "3", false, "JSON 对象", 3),
                new("array", "数组", "4", false, "数组", 4)
            ]),
            new("department_type", "部门类型", "组织架构部门分类", 140,
            [
                new("corporation", "集团", "0", false, "集团公司", 0),
                new("headquarters", "总部", "1", false, "集团总部", 1),
                new("company", "公司", "2", false, "独立法人公司", 2),
                new("branch", "分公司", "3", false, "分支机构", 3),
                new("division", "事业部", "4", false, "事业部门", 4),
                new("center", "中心", "5", false, "职能中心", 5),
                new("department", "部门", "6", true, "标准部门", 6),
                new("section", "科室", "7", false, "科室", 7),
                new("team", "小组", "8", false, "工作小组", 8),
                new("group", "组", "9", false, "组", 9),
                new("project", "项目组", "10", false, "临时项目组", 10),
                new("workgroup", "工作组", "11", false, "工作组", 11),
                new("virtual", "虚拟", "12", false, "虚拟组织", 12),
                new("office", "办公室", "13", false, "办公室", 13),
                new("subsidiary", "子公司", "14", false, "子公司", 14),
                new("other", "其他", "99", false, "其他类型", 99)
            ]),
            new("message_channel", "消息发送渠道", "系统通知和消息的发送渠道", 150,
            [
                new("email", "邮件", "0", true, "电子邮件渠道", 0),
                new("sms", "短信", "1", false, "短信渠道", 1),
                new("push", "推送", "2", false, "App 推送渠道", 2),
                new("inbox", "站内信", "3", false, "系统站内信渠道", 3),
                new("wechat", "微信", "4", false, "微信公众号渠道", 4),
                new("webhook", "Webhook", "99", false, "自定义回调渠道", 99)
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
            new("task_status", "任务状态", "系统任务生命周期状态", 180,
            [
                new("pending", "待执行", "0", true, "等待调度执行", 0),
                new("running", "运行中", "1", false, "正在执行", 1),
                new("paused", "已暂停", "2", false, "已暂停", 2),
                new("completed", "已完成", "3", false, "执行完成", 3),
                new("failed", "已失败", "4", false, "执行失败", 4),
                new("cancelled", "已取消", "5", false, "已被取消", 5)
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

    private static bool SetIfChanged<T>(T current, T next, Action<T> setter)
    {
        if (EqualityComparer<T>.Default.Equals(current, next))
        {
            return false;
        }

        setter(next);
        return true;
    }

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
