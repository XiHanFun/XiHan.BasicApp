// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.AI.Domain.Entities;

/// <summary>
/// 提示词
/// </summary>
/// <remarks>
/// 平台库实体、只在平台一侧，助手引用它当系统提示词。正文原样使用、没有变量渲染，版本只是标签、同一编码只有一行（方案 A4）
/// 编码唯一，建后不可改；软删除后可以重建
/// </remarks>
[ActiveUniqueIndex(nameof(Code))]
public sealed class SysAiPrompt : AppAuditedEntity
{
    /// <summary>
    /// 编码
    /// </summary>
    [StringLength(100)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 名称
    /// </summary>
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 分类
    /// </summary>
    [StringLength(100)]
    public string? Category { get; set; }

    /// <summary>
    /// 版本标签
    /// </summary>
    [StringLength(100)]
    public string? Version { get; set; }

    /// <summary>
    /// 正文
    /// </summary>
    [LongText]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 状态
    /// </summary>
    public AiStatus Status { get; set; } = AiStatus.Active;

    /// <summary>
    /// 排序号
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500)]
    public string? Remark { get; set; }
}
