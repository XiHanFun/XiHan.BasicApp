#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CacheDtos
// Guid:5d8f3a26-9b1c-4e7d-a0f5-2c6e4b8d7a93
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
