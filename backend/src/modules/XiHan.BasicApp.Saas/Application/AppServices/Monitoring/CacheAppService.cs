#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CacheAppService
// Guid:9a67eaf4-3eca-4072-b5c3-0ae47f3b66ab
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 14:17:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Authorization.AspNetCore;

namespace XiHan.BasicApp.Saas.Application.AppServices.Monitoring;

/// <summary>
/// 缓存管理服务（查询 / 改字符串值 / 删除）
/// 读取走 saas:cache:read，写入与删除走 saas:cache:clear。
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统Saas服务", Tag = "缓存管理")]
public class CacheAppService : ApplicationServiceBase
{
    private readonly ICacheManagementService _cacheManagementService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public CacheAppService(ICacheManagementService cacheManagementService)
    {
        _cacheManagementService = cacheManagementService;
    }

    /// <summary>
    /// 判断缓存键是否存在
    /// </summary>
    [PermissionAuthorize(SaasPermissionCodes.Cache.Read)]
    public bool Exists(string key)
    {
        return _cacheManagementService.Exists(key);
    }

    /// <summary>
    /// 按模式获取缓存键列表
    /// </summary>
    [PermissionAuthorize(SaasPermissionCodes.Cache.Read)]
    public IReadOnlyCollection<string> GetKeys(string pattern = "*")
    {
        return _cacheManagementService.GetKeys(pattern);
    }

    /// <summary>
    /// 获取缓存字符串值
    /// </summary>
    [PermissionAuthorize(SaasPermissionCodes.Cache.Read)]
    public string? GetString(string key)
    {
        return _cacheManagementService.GetString(key);
    }

    /// <summary>
    /// 更新缓存字符串值（鉴权关键命名空间禁止改写）
    /// </summary>
    [PermissionAuthorize(SaasPermissionCodes.Cache.Clear)]
    public void UpdateString(CacheStringUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);
        _cacheManagementService.SetString(input.Key, input.Value);
    }

    /// <summary>
    /// 删除指定缓存键
    /// </summary>
    [PermissionAuthorize(SaasPermissionCodes.Cache.Clear)]
    public void Remove(string key)
    {
        _cacheManagementService.Remove(key);
    }

    /// <summary>
    /// 按模式批量删除缓存键
    /// </summary>
    [PermissionAuthorize(SaasPermissionCodes.Cache.Clear)]
    public long RemoveByPattern(string pattern = "*")
    {
        return _cacheManagementService.RemoveByPattern(pattern);
    }
}
