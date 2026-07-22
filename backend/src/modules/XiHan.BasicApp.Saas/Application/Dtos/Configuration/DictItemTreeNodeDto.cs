// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统字典项树节点 DTO
/// </summary>
public sealed class DictItemTreeNodeDto : BasicAppDto
{
    /// <summary>
    /// 字典主键
    /// </summary>
    public long DictId { get; set; }

    /// <summary>
    /// 父级字典项主键
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 字典项编码
    /// </summary>
    public string ItemCode { get; set; } = string.Empty;

    /// <summary>
    /// 字典项名称
    /// </summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// 字典项值
    /// </summary>
    public string? ItemValue { get; set; }

    /// <summary>
    /// 字典项描述
    /// </summary>
    public string? ItemDescription { get; set; }

    /// <summary>
    /// 是否默认
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 子项
    /// </summary>
    public List<DictItemTreeNodeDto> Children { get; } = [];
}
