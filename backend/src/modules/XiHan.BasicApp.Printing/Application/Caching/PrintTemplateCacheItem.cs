// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Printing.Domain.Enums;
using XiHan.Framework.Caching.Attributes;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Printing.Application.Caching;

/// <summary>
/// 打印模板解析缓存项；业务键已经包含请求租户、作用域和编码，故禁用框架额外租户前缀。
/// </summary>
[IgnoreMultiTenancy]
[CacheName(PrintingCacheNames.PrintTemplate)]
public sealed class PrintTemplateCacheItem
{
    /// <summary>是否命中模板；false 是短期负缓存哨兵。</summary>
    public bool Found { get; set; }

    /// <summary>模板主键。</summary>
    public long BasicId { get; set; }

    /// <summary>模板编码。</summary>
    public string TemplateCode { get; set; } = string.Empty;

    /// <summary>可选数据源编码；为空表示自由模板。</summary>
    public string? DataSourceCode { get; set; }

    /// <summary>模板名称。</summary>
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>hiprint 模板 JSON。</summary>
    public string TemplateJson { get; set; } = string.Empty;

    /// <summary>hiprint 引擎版本。</summary>
    public string EngineVersion { get; set; } = string.Empty;

    /// <summary>最终命中的明确作用域。</summary>
    public PrintTemplateScope ResolvedScope { get; set; }

    /// <summary>数据库行版本。</summary>
    public long RowVersion { get; set; }

    /// <summary>缓存创建时间。</summary>
    public DateTimeOffset CachedAt { get; set; }
}
