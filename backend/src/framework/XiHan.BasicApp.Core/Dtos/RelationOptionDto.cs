// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Core.Dtos;

/// <summary>
/// 关联记录选项 DTO（外键下拉与树形选择的选项来源）
/// </summary>
/// <remarks>
/// 值是被关联记录的主键，业务数据按它存；树形关联平铺返回，靠 <see cref="ParentValue"/> 还原层级
/// （父级在结果里找不到的节点即为根，0 或空都算根）。
/// </remarks>
public sealed class RelationOptionDto
{
    /// <summary>
    /// 选项值（被关联记录的主键）
    /// </summary>
    public long Value { get; set; }

    /// <summary>
    /// 选项文本（被关联记录的显示列）
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// 父级选项值（树形关联的上级主键；平铺关联为 null）
    /// </summary>
    public long? ParentValue { get; set; }
}
