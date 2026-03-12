#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ICacheAppService
// Guid:feeb119e-8c9a-48ec-aac6-7fdcf6d63f84
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 14:13:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 缓存应用服务
/// </summary>
public interface ICacheAppService : IApplicationService
{
    /// <summary>
    /// 获取缓存字符串
    /// </summary>
    string? GetString(string key);

    /// <summary>
    /// 设置缓存字符串
    /// </summary>
    void SetString(string key, string value, int expireSeconds = 300);

    /// <summary>
    /// 删除缓存项
    /// </summary>
    void Remove(string key);

    /// <summary>
    /// 批量删除缓存项
    /// </summary>
    void RemoveMany(IReadOnlyCollection<string> keys);

    /// <summary>
    /// 判断缓存键是否存在
    /// </summary>
    bool Exists(string key);

    /// <summary>
    /// 按模式获取缓存键
    /// </summary>
    IReadOnlyCollection<string> GetKeys(string pattern = "*");

    /// <summary>
    /// 按模式删除缓存项
    /// </summary>
    long RemoveByPattern(string pattern = "*");
}
