#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ICacheManagementService
// Guid:d27ab46c-5515-4eee-9df2-ffb2f102c6a9
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 缓存管理应用服务
/// </summary>
public interface ICacheManagementService
{
    /// <summary>
    /// 判断缓存键是否存在
    /// </summary>
    bool Exists(string key);

    /// <summary>
    /// 按模式获取缓存键列表
    /// </summary>
    IReadOnlyCollection<string> GetKeys(string pattern = "*");

    /// <summary>
    /// 获取缓存字符串值
    /// </summary>
    string? GetString(string key);

    /// <summary>
    /// 删除指定缓存键
    /// </summary>
    void Remove(string key);

    /// <summary>
    /// 按模式批量删除缓存键
    /// </summary>
    long RemoveByPattern(string pattern = "*");
}
