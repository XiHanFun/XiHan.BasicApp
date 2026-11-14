// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.AI.Domain.Entities;

/// <summary>
/// 提示词
/// </summary>
/// <remarks>
/// 提示词单独成表、只在平台一侧，助手按编码引用它当系统提示词：几个助手可以共用一段提示词，改一处即生效
/// 正文原样使用、没有变量渲染，版本只是标签、同一编码只有一行（方案 A4），不留历史版本
/// 编码建后不可改（助手按编码引用），被助手引用即不能删；唯一是部分唯一索引，软删除后同码可重建
/// 引用它的助手在它停用后回复即失败、不回落到缺省提示词：配错要暴露出来，不悄悄换成别的提示词
/// </remarks>
[SugarTable(TableName = "sys_ai_prompt", TableDescription = "提示词")]
[ActiveUniqueIndex(nameof(Code), Name = "ux_sys_ai_prompt_code")]
public sealed class SysAiPrompt : AppAuditedEntity
{
    /// <summary>
    /// 编码
    /// </summary>
    [SugarColumn(ColumnName = "code", Length = 100, IsNullable = false, ColumnDescription = "编码")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnName = "name", Length = 200, IsNullable = false, ColumnDescription = "名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 分类
    /// </summary>
    [SugarColumn(ColumnName = "category", Length = 100, IsNullable = true, ColumnDescription = "分类")]
    public string? Category { get; set; }

    /// <summary>
    /// 版本标签
    /// </summary>
    [SugarColumn(ColumnName = "version", Length = 100, IsNullable = true, ColumnDescription = "版本标签")]
    public string? Version { get; set; }

    /// <summary>
    /// 正文
    /// </summary>
    [SugarColumn(ColumnName = "content", IsNullable = false, ColumnDescription = "正文")]
    [LongText]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "状态")]
    public AiStatus Status { get; set; } = AiStatus.Active;

    /// <summary>
    /// 排序号
    /// </summary>
    [SugarColumn(ColumnName = "order", IsNullable = false, ColumnDescription = "排序号")]
    public int Order { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDescription = "备注")]
    public string? Remark { get; set; }
}
