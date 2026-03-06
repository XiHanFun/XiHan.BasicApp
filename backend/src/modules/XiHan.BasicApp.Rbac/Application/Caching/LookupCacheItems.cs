#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:LookupCacheItems
// Guid:debcf95b-5df5-43da-8fed-26cb52f1d8a2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 13:36:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.Framework.Caching.Attributes;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Rbac.Application.Caching;

/// <summary>
/// 查找缓存版本项
/// </summary>
[CacheName("RbacLookupVersion")]
[IgnoreMultiTenancy]
public class LookupCacheVersionItem
{
    /// <summary>
    /// 版本号
    /// </summary>
    public long Version { get; set; } = 1;
}

/// <summary>
/// 文件查找缓存项
/// </summary>
[CacheName("RbacFileLookup")]
[IgnoreMultiTenancy]
public class FileLookupCacheItem
{
    /// <summary>
    /// 文件
    /// </summary>
    public FileDto? Item { get; set; }
}

/// <summary>
/// 任务查找缓存项
/// </summary>
[CacheName("RbacTaskLookup")]
[IgnoreMultiTenancy]
public class TaskLookupCacheItem
{
    /// <summary>
    /// 任务
    /// </summary>
    public TaskDto? Item { get; set; }
}

/// <summary>
/// OAuth 应用查找缓存项
/// </summary>
[CacheName("RbacOAuthAppLookup")]
[IgnoreMultiTenancy]
public class OAuthAppLookupCacheItem
{
    /// <summary>
    /// OAuth 应用
    /// </summary>
    public OAuthAppDto? Item { get; set; }
}

/// <summary>
/// 查找缓存版本快照
/// </summary>
public class LookupCacheVersionSnapshot
{
    /// <summary>
    /// 文件查找版本
    /// </summary>
    public long FileLookupVersion { get; set; }

    /// <summary>
    /// 任务查找版本
    /// </summary>
    public long TaskLookupVersion { get; set; }

    /// <summary>
    /// OAuth 应用查找版本
    /// </summary>
    public long OAuthAppLookupVersion { get; set; }
}
