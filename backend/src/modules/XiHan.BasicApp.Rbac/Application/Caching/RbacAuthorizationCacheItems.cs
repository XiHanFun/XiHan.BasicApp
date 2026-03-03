#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacAuthorizationCacheItems
// Guid:df8fc670-6b95-4d4c-a39f-faf0463516ec
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/03 17:40:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Caching;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Rbac.Application.Caching;

/// <summary>
/// 授权缓存版本项
/// </summary>
[CacheName("RbacAuthorizationVersion")]
[IgnoreMultiTenancy]
public class AuthorizationCacheVersionItem
{
    /// <summary>
    /// 版本号
    /// </summary>
    public long Version { get; set; } = 1;
}

/// <summary>
/// 用户权限编码缓存项
/// </summary>
[CacheName("RbacUserPermissionCodes")]
[IgnoreMultiTenancy]
public class UserPermissionCodesCacheItem
{
    /// <summary>
    /// 权限编码集合
    /// </summary>
    public string[] PermissionCodes { get; set; } = [];
}

/// <summary>
/// 用户数据范围缓存项
/// </summary>
[CacheName("RbacUserDataScopeDepartmentIds")]
[IgnoreMultiTenancy]
public class UserDataScopeDepartmentIdsCacheItem
{
    /// <summary>
    /// 部门ID集合
    /// </summary>
    public long[] DepartmentIds { get; set; } = [];
}

/// <summary>
/// 授权缓存版本快照
/// </summary>
public class AuthorizationCacheVersionSnapshot
{
    /// <summary>
    /// 权限缓存版本
    /// </summary>
    public long PermissionVersion { get; set; }

    /// <summary>
    /// 数据范围缓存版本
    /// </summary>
    public long DataScopeVersion { get; set; }
}
