// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 更新缓存字符串值入参
/// </summary>
public sealed class CacheStringUpdateDto
{
    /// <summary>
    /// 缓存键
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 新值
    /// </summary>
    public string? Value { get; set; }
}
