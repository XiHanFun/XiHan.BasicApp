// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Enums;

using XiHan.BasicApp.Saas.Domain.DomainServices;

using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统字典项列表项 DTO
/// </summary>
public class DictItemListItemDto : BasicAppDto
{
    /// <summary>编辑快照版本，使用字符串保证长整型精度。</summary>
    public string EditVersion { get; set; } = "0";
    /// <summary>字典项的标签显示样式，不包含其他扩展元数据。</summary>
    [FieldSecuritySource(nameof(SysDictItem.Metadata))]
    public DictTagStyle? TagStyle { get; set; }

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
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}
