// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Localization.Abstractions.Messages;

namespace XiHan.BasicApp.Workbench;

/// <summary>
/// 工作台模块的消息键。
/// </summary>
public static class WorkbenchMessages
{
    /// <summary>
    /// 消息目录名。
    /// </summary>
    public const string CatalogName = "basicapp.workbench";

    /// <summary>
    /// 操作 <c>workbench.widgets</c> 的说明。
    /// </summary>
    public const string ListWidgetsDescription = "workbench.widgets.description";

    /// <summary>
    /// 操作 <c>workbench.widget-data</c> 的说明。
    /// </summary>
    public const string GetWidgetDataDescription = "workbench.widget_data.description";

    /// <summary>
    /// 没有这个小组件。
    /// </summary>
    /// <remarks>
    /// 参数 <c>key</c>。
    /// </remarks>
    public const string WidgetNotFound = "workbench.widget.not_found";

    /// <summary>
    /// 没有看这个小组件的权限。
    /// </summary>
    /// <remarks>
    /// 参数 <c>key</c>。
    /// </remarks>
    public const string WidgetDenied = "workbench.widget.denied";

    private static readonly Lazy<MessageCatalog> _catalog = new(static () => MessageCatalog.FromEmbeddedResources(CatalogName, typeof(WorkbenchMessages)));

    /// <summary>
    /// 本包的消息目录（嵌入的 10 种区域文件）。
    /// </summary>
    public static MessageCatalog Catalog => _catalog.Value;
}
