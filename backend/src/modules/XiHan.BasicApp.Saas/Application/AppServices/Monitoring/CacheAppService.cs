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
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Saas.Application.AppServices.Monitoring;

/// <summary>
/// 缓存管理服务（只读+删除，不暴露写入操作）
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统Saas服务")]
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
    public bool Exists(string key)
    {
        return _cacheManagementService.Exists(key);
    }

    /// <summary>
    /// 按模式获取缓存键列表
    /// </summary>
    public IReadOnlyCollection<string> GetKeys(string pattern = "*")
    {
        return _cacheManagementService.GetKeys(pattern);
    }

    /// <summary>
    /// 获取缓存字符串值
    /// </summary>
    public string? GetString(string key)
    {
        return _cacheManagementService.GetString(key);
    }

    /// <summary>
    /// 删除指定缓存键
    /// </summary>
    public void Remove(string key)
    {
        _cacheManagementService.Remove(key);
    }

    /// <summary>
    /// 按模式批量删除缓存键
    /// </summary>
    public long RemoveByPattern(string pattern = "*")
    {
        return _cacheManagementService.RemoveByPattern(pattern);
    }
}
