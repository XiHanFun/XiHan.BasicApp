// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.DomainServices;

using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 字典选项 DTO（业务表单下拉的选项来源）
/// </summary>
/// <remarks>
/// 只带下拉用得到的字段：业务数据按字典项编码存，界面显示字典项名称。
/// 树形字典按深度优先展平，靠 <see cref="ParentValue"/> 还原层级。
/// </remarks>
public sealed class DictOptionDto : IFieldSecurityMultiSourceDto
{
    Type? IFieldSecurityMultiSourceDto.FieldSecurityEntity => typeof(SysDictItem);
    string? IFieldSecurityMultiSourceDto.FieldSecuritySourceOf(string property) => property switch
    {
        nameof(TagStyle) => nameof(SysDictItem.Metadata),
        nameof(Description) => nameof(SysDictItem.ItemDescription),
        nameof(Label) => nameof(SysDictItem.ItemName),
        nameof(Value) => nameof(SysDictItem.ItemCode),
        _ => null
    };

    /// <summary>字典项的标签显示样式，不包含其他扩展元数据。</summary>
    [FieldSecuritySource(nameof(SysDictItem.Metadata))]
    public DictTagStyle? TagStyle { get; set; }

    /// <summary>
    /// 选项值（字典项编码，业务数据按它存）
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 选项文本（字典项名称）
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>可维护的中文说明，作为标签提示；不包含其他扩展元数据。</summary>
    public string? Description { get; set; }

    /// <summary>
    /// 父级选项值（树形字典的上级项编码；顶层为 null）
    /// </summary>
    public string? ParentValue { get; set; }

    /// <summary>
    /// 是否默认项
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 是否停用（停用项只用于回显引用了它的历史数据，不能再选）
    /// </summary>
    public bool Disabled { get; set; }
}
