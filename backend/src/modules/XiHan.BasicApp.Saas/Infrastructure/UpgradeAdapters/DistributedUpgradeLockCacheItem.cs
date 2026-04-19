#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DistributedUpgradeLockCacheItem
// Guid:a1b2c3d4-5e6f-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.UpgradeAdapters;

/// <summary>
/// 分布式升级锁缓存项
/// </summary>
[IgnoreMultiTenancy]
public class DistributedUpgradeLockCacheItem
{
    /// <summary>
    /// 锁值
    /// </summary>
    public string Value { get; set; } = string.Empty;
}
