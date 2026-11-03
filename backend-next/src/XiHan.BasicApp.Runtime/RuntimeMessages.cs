// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Localization.Abstractions.Messages;

namespace XiHan.BasicApp.Runtime;

/// <summary>
/// 运行时的消息目录
/// </summary>
/// <remarks>
/// 各区域文案在 <c>Localization/&lt;区域&gt;.json</c>；除下面的消息外是权限显示名（约定键 <c>permissions.&lt;权限码&gt;.name</c>）
/// </remarks>
public static class RuntimeMessages
{
    /// <summary>
    /// 消息目录名
    /// </summary>
    public const string CatalogName = "basicapp.runtime";

    /// <summary>
    /// 后台逐侧遍历有一处没有处理成功
    /// </summary>
    public const string SweepIncomplete = "runtime.sweep.incomplete";

    private static readonly Lazy<MessageCatalog> _catalog = new(static () => MessageCatalog.FromEmbeddedResources(CatalogName, typeof(RuntimeMessages)));

    /// <summary>
    /// 运行时的消息目录（嵌入的 10 种区域文件）
    /// </summary>
    public static MessageCatalog Catalog => _catalog.Value;
}
