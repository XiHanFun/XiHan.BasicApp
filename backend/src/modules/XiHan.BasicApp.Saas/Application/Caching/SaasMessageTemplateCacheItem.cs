#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasMessageTemplateCacheItem
// Guid:bd603e58-9cf7-42a3-b045-7ecd9b6cbf80
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/11 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Caching.Attributes;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Application.Caching;

/// <summary>
/// SaaS 消息模板缓存项（键已含租户维度，禁用框架级租户前缀避免双重隔离）。
/// </summary>
[IgnoreMultiTenancy]
[CacheName(SaasCacheNames.MessageTemplate)]
public sealed class SaasMessageTemplateCacheItem
{
    /// <summary>
    /// 主题模板（Scriban 源码）。
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// 内容模板（Scriban 源码）。
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 内容是否 HTML。
    /// </summary>
    public bool IsHtml { get; set; }

    /// <summary>
    /// 缓存时间。
    /// </summary>
    public DateTimeOffset CachedAt { get; set; }
}
