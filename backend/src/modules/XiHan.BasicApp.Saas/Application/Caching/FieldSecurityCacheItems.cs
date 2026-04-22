#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FieldSecurityCacheItems
// Guid:4d7577e0-2528-4f17-a91a-a0e22b4ac301
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/22 22:10:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Caching.Attributes;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Application.Caching;

[CacheName("FieldSecurityVersion")]
[IgnoreMultiTenancy]
public class FieldSecurityVersionCacheItem
{
    public long Version { get; set; } = 1;
}

[CacheName("FieldSecurityDecision")]
[IgnoreMultiTenancy]
public class FieldSecurityDecisionCacheItem
{
    public FieldSecurityDecisionFieldCacheItem[] Fields { get; set; } = [];
}

public class FieldSecurityDecisionFieldCacheItem
{
    public string FieldName { get; set; } = string.Empty;

    public bool IsReadable { get; set; } = true;

    public bool IsEditable { get; set; } = true;

    public int MaskStrategy { get; set; }

    public string? MaskPattern { get; set; }

    public int Priority { get; set; }
}
