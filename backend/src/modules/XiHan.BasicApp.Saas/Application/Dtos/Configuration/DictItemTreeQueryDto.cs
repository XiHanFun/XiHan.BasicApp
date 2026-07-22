// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统字典项树查询 DTO
/// </summary>
public sealed class DictItemTreeQueryDto
{
    /// <summary>
    /// 字典主键
    /// </summary>
    public long DictId { get; set; }

    /// <summary>
    /// 是否只返回启用项
    /// </summary>
    public bool OnlyEnabled { get; set; } = true;

    /// <summary>
    /// 最大返回数量
    /// </summary>
    public int Limit { get; set; } = 5000;
}
