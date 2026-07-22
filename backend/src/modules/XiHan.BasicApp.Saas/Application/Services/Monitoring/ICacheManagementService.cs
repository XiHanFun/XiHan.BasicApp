// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 缓存管理应用服务
/// 仅基于分布式缓存抽象（IDistributedCache）提供「查询 / 改字符串值 / 删除」三类运维能力，不直连 Redis 原生类型。
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
    /// 更新缓存字符串值（鉴权关键命名空间禁止改写）
    /// </summary>
    void SetString(string key, string? value);

    /// <summary>
    /// 删除指定缓存键
    /// </summary>
    void Remove(string key);

    /// <summary>
    /// 按模式批量删除缓存键
    /// </summary>
    long RemoveByPattern(string pattern = "*");
}
