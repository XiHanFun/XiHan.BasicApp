// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 操作选择器查询 DTO
/// </summary>
public sealed class OperationSelectQueryDto
{
    /// <summary>
    /// 关键字（操作编码、名称、描述）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 操作类型代码
    /// </summary>
    public OperationTypeCode? OperationTypeCode { get; set; }

    /// <summary>
    /// 操作分类
    /// </summary>
    public OperationCategory? Category { get; set; }

    /// <summary>
    /// 返回数量上限
    /// </summary>
    public int Limit { get; set; } = 100;
}
