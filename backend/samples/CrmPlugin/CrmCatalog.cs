// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Plugins.Protocol.V1;

namespace CrmPlugin;

/// <summary>
/// 随握手交给宿主的消息目录
/// </summary>
/// <remarks>
/// 宿主按它翻译插件的失败、权限码的显示名与接口文档的标题；接口文档的分组名与资源标签名缺了宿主不加载插件。源语言 en-US 必须有
/// </remarks>
internal static class CrmCatalog
{
    /// <summary>
    /// 目录名
    /// </summary>
    public const string Name = "crm";

    /// <summary>
    /// 英文模板
    /// </summary>
    private static readonly Dictionary<string, string> _english = new(StringComparer.Ordinal)
    {
        ["crm.customer.name_required"] = "The customer name is required.",
        ["crm.customer.not_found"] = "Customer '{id}' was not found.",
        ["crm_customers.list.title"] = "List customers",
        ["crm_customers.create.title"] = "Create a customer",
        ["crm_customers.update.title"] = "Update a customer",
        ["crm_customers.delete.title"] = "Delete a customer",
        ["openapi.groups.crm.name"] = "CRM",
        ["openapi.groups.crm.description"] = "Customers of the sample CRM plugin, kept per tenant.",
        ["openapi.tags.crm_customers.name"] = "Customers",
        ["openapi.tags.crm_customers.description"] = "Customers of the current tenant.",
        ["permissions.crm.crm_customers.read.name"] = "Read customers",
        ["permissions.crm.crm_customers.read.description"] = "View the customers of the current tenant.",
        ["permissions.crm.crm_customers.create.name"] = "Create customers",
        ["permissions.crm.crm_customers.create.description"] = "Add customers to the current tenant.",
        ["permissions.crm.crm_customers.update.name"] = "Update customers",
        ["permissions.crm.crm_customers.update.description"] = "Change the name and email of customers in the current tenant.",
        ["permissions.crm.crm_customers.delete.name"] = "Delete customers",
        ["permissions.crm.crm_customers.delete.description"] = "Remove customers from the current tenant.",
    };

    /// <summary>
    /// 简体中文模板
    /// </summary>
    private static readonly Dictionary<string, string> _chinese = new(StringComparer.Ordinal)
    {
        ["crm.customer.name_required"] = "客户名称不能为空。",
        ["crm.customer.not_found"] = "客户 '{id}' 不存在。",
        ["crm_customers.list.title"] = "列出客户",
        ["crm_customers.create.title"] = "新建客户",
        ["crm_customers.update.title"] = "修改客户",
        ["crm_customers.delete.title"] = "删除客户",
        ["openapi.groups.crm.name"] = "客户管理",
        ["openapi.groups.crm.description"] = "示例客户管理插件的客户，按租户分开。",
        ["openapi.tags.crm_customers.name"] = "客户",
        ["openapi.tags.crm_customers.description"] = "当前租户的客户。",
        ["permissions.crm.crm_customers.read.name"] = "查看客户",
        ["permissions.crm.crm_customers.read.description"] = "查看当前租户的客户。",
        ["permissions.crm.crm_customers.create.name"] = "新建客户",
        ["permissions.crm.crm_customers.create.description"] = "给当前租户添加客户。",
        ["permissions.crm.crm_customers.update.name"] = "修改客户",
        ["permissions.crm.crm_customers.update.description"] = "修改当前租户客户的名称与邮箱。",
        ["permissions.crm.crm_customers.delete.name"] = "删除客户",
        ["permissions.crm.crm_customers.delete.description"] = "从当前租户删除客户。",
    };

    /// <summary>
    /// 建出目录
    /// </summary>
    public static MessageCatalog Create()
    {
        var catalog = new MessageCatalog { Name = Name };
        catalog.Locales.Add(Locale("en-US", _english));
        catalog.Locales.Add(Locale("zh-CN", _chinese));
        return catalog;
    }

    /// <summary>
    /// 一种区域的模板
    /// </summary>
    private static LocaleTemplates Locale(string locale, Dictionary<string, string> templates)
    {
        var result = new LocaleTemplates { Locale = locale };
        result.Templates.Add(templates);
        return result;
    }
}
