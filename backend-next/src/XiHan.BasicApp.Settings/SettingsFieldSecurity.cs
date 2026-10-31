// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Operations.Configs;
using XiHan.BasicApp.Settings.Operations.DictItems;
using XiHan.BasicApp.Settings.Operations.Dicts;
using XiHan.BasicApp.Settings.Operations.NumberingRules;

namespace XiHan.BasicApp.Settings;

/// <summary>
/// 设置包登记的字段安全实体。
/// </summary>
/// <remarks>
/// 4.x 同五个实体：参数、字典、字典项、业务编号规则与编号分配记录。字段由响应类型生成，规则由 Access 的字段安全中间件在读、建、改处统一套用；
/// 字典项的修改按字典项本身比对（层级深度不参与）。
/// </remarks>
public static class SettingsFieldSecurity
{
    /// <summary>
    /// 登记本包的实体与操作。
    /// </summary>
    /// <param name="services">服务集合。</param>
    public static void Register(IServiceCollection services)
        => services.AddFieldSecurity("settings", SettingsJsonContext.Default, fields => fields
            .Entity<ConfigResponse>("settings.configs", "settings.configs.title", "settings.configs.fields.")
                .Reads(ListConfigs.Id, GetConfig.Id)
                .Creates(CreateConfig.Id)
                .Updates<UpdateConfigRequest>(UpdateConfig.Id, static (request, context) => FieldSecurityOriginal.FindAsync<SysConfig>(context, request.Id, ConfigResponse.From))
                .Keyword("key", "name", "group")
            .Entity<DictResponse>("settings.dicts", "settings.dicts.title", "settings.dicts.fields.")
                .Reads(ListDicts.Id, GetDict.Id)
                .Creates(CreateDict.Id)
                .Updates<UpdateDictRequest>(UpdateDict.Id, static (request, context) => FieldSecurityOriginal.FindAsync<SysDict>(context, request.Id, DictResponse.From))
                .Keyword("code", "name", "category")
            .Entity<DictItemResponse>("settings.dict-items", "settings.dicts.items_title", "settings.dicts.item_fields.")
                .ReadsAt(ListDictItems.Id, "items")
                .Reads(GetDictItem.Id)
                .Creates(CreateDictItem.Id)
                .Updates<UpdateDictItemRequest>(UpdateDictItem.Id, static (request, context) => FieldSecurityOriginal.FindAsync<SysDictItem>(context, request.Id, static item => DictItemResponse.From(item, 0)))
            .Entity<NumberingRuleResponse>("settings.numbering-rules", "settings.numbering_rules.title", "settings.numbering_rules.fields.")
                .Reads(ListNumberingRules.Id, GetNumberingRule.Id)
                .Creates(CreateNumberingRule.Id)
                .Updates<UpdateNumberingRuleRequest>(UpdateNumberingRule.Id, static (request, context) => FieldSecurityOriginal.FindAsync<SysNumberingRule>(context, request.Id, NumberingRuleResponse.From))
                .Keyword("code", "name")
            .Entity<NumberingAllocationResponse>("settings.numbering-allocations", "settings.numbering_rules.allocations_title", "settings.numbering_rules.allocation_fields.")
                .Reads(ListNumberingAllocations.Id));
}
