// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Localization.Abstractions.Messages;

namespace XiHan.BasicApp.Printing;

/// <summary>
/// 打印模块的消息键。
/// </summary>
/// <remarks>
/// 各区域文案在 <c>Localization/&lt;区域&gt;.json</c>。操作说明用约定键 <c>&lt;操作 ID&gt;.description</c>（资源段的 <c>-</c> 换成 <c>_</c>）。
/// </remarks>
public static class PrintingMessages
{
    /// <summary>
    /// 消息目录名。
    /// </summary>
    public const string CatalogName = "basicapp.printing";

    /// <summary>
    /// 操作 <c>print-templates.list</c> 的说明。
    /// </summary>
    public const string ListPrintTemplatesDescription = "print_templates.list.description";

    /// <summary>
    /// 操作 <c>print-templates.get</c> 的说明。
    /// </summary>
    public const string GetPrintTemplateDescription = "print_templates.get.description";

    /// <summary>
    /// 操作 <c>print-templates.resolve</c> 的说明。
    /// </summary>
    public const string ResolvePrintTemplateDescription = "print_templates.resolve.description";

    /// <summary>
    /// 操作 <c>print-templates.create</c> 的说明。
    /// </summary>
    public const string CreatePrintTemplateDescription = "print_templates.create.description";

    /// <summary>
    /// 操作 <c>print-templates.update</c> 的说明。
    /// </summary>
    public const string UpdatePrintTemplateDescription = "print_templates.update.description";

    /// <summary>
    /// 操作 <c>print-templates.set-status</c> 的说明。
    /// </summary>
    public const string SetPrintTemplateStatusDescription = "print_templates.set_status.description";

    /// <summary>
    /// 操作 <c>print-templates.delete</c> 的说明。
    /// </summary>
    public const string DeletePrintTemplatesDescription = "print_templates.delete.description";

    /// <summary>
    /// 操作 <c>print-data-sources.list</c> 的说明。
    /// </summary>
    public const string ListPrintDataSourcesDescription = "print_data_sources.list.description";

    /// <summary>
    /// 模板不存在。
    /// </summary>
    public const string TemplateNotFound = "printing.template.not_found";

    /// <summary>
    /// 解析不到可用的模板。
    /// </summary>
    public const string NoUsableTemplate = "printing.template.no_usable";

    /// <summary>
    /// 本侧已有同编码的模板。
    /// </summary>
    public const string TemplateCodeTaken = "printing.template.code_taken";

    /// <summary>
    /// 模板仍在启用。
    /// </summary>
    public const string TemplateEnabled = "printing.template.enabled";

    /// <summary>
    /// 缺少全局管理权限。
    /// </summary>
    public const string GlobalManageRequired = "printing.template.global_manage_required";

    /// <summary>
    /// 全局模板没有向租户开放。
    /// </summary>
    public const string GlobalNotOpen = "printing.template.global_not_open";

    /// <summary>
    /// 模板 JSON 不是合法的 JSON。
    /// </summary>
    public const string TemplateJsonSyntax = "printing.template_json.syntax";

    /// <summary>
    /// 模板 JSON 的根不是对象。
    /// </summary>
    public const string TemplateJsonRoot = "printing.template_json.root";

    /// <summary>
    /// 模板格式版本不受支持。
    /// </summary>
    public const string TemplateJsonSchemaVersion = "printing.template_json.schema_version";

    /// <summary>
    /// 模板没有页面。
    /// </summary>
    public const string TemplateJsonPages = "printing.template_json.pages";

    /// <summary>
    /// 某一页不是对象。
    /// </summary>
    public const string TemplateJsonPage = "printing.template_json.page";

    /// <summary>
    /// 数据源没有登记。
    /// </summary>
    public const string DataSourceUnregistered = "printing.data_source.unregistered";

    /// <summary>
    /// 平台一侧不能用租户作用域。
    /// </summary>
    public const string ScopeInvalid = "printing.scope.invalid";

    /// <summary>
    /// 不支持的筛选。
    /// </summary>
    public const string FilterUnsupported = "printing.query.filter_unsupported";

    /// <summary>
    /// 不支持的排序。
    /// </summary>
    public const string SortUnsupported = "printing.query.sort_unsupported";

    private static readonly Lazy<MessageCatalog> _catalog = new(static () => MessageCatalog.FromEmbeddedResources(CatalogName, typeof(PrintingMessages)));

    /// <summary>
    /// 本模块的消息目录（嵌入的 10 种区域文件）。
    /// </summary>
    public static MessageCatalog Catalog => _catalog.Value;
}
