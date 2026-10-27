// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Localization.Abstractions.Messages;

namespace XiHan.BasicApp.Host;

/// <summary>
/// 宿主的消息键（命令说明与命令错误）；各区域文案在 <c>Localization/&lt;区域&gt;.json</c>。
/// </summary>
public static class HostMessages
{
    /// <summary>
    /// 消息目录名。
    /// </summary>
    public const string CatalogName = "basicapp.host";

    /// <summary>
    /// 命令 <c>seed</c> 的说明。
    /// </summary>
    public const string SeedDescription = "host.commands.seed_description";

    /// <summary>
    /// <c>--only</c> 指定了不存在的种子；参数 <c>name</c>。
    /// </summary>
    public const string SeederUnknown = "host.commands.seeder_unknown";

    private static readonly Lazy<MessageCatalog> _catalog = new(static () => MessageCatalog.FromEmbeddedResources(CatalogName, typeof(HostMessages)));

    /// <summary>
    /// 宿主的消息目录（嵌入的 10 种区域文件）。
    /// </summary>
    public static MessageCatalog Catalog => _catalog.Value;
}
