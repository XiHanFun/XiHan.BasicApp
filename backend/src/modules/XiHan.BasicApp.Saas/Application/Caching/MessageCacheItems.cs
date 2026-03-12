#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MessageCacheItems
// Guid:79c57f2e-e8f4-4102-a841-5fc9957e81d8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 13:44:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Caching.Attributes;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Application.Caching;

/// <summary>
/// 消息缓存版本项
/// </summary>
[CacheName("RbacMessageVersion")]
[IgnoreMultiTenancy]
public class MessageCacheVersionItem
{
    /// <summary>
    /// 版本号
    /// </summary>
    public long Version { get; set; } = 1;
}

/// <summary>
/// 消息未读数缓存项
/// </summary>
[CacheName("RbacMessageUnreadCount")]
[IgnoreMultiTenancy]
public class MessageUnreadCountCacheItem
{
    /// <summary>
    /// 未读数量
    /// </summary>
    public int Count { get; set; }
}
