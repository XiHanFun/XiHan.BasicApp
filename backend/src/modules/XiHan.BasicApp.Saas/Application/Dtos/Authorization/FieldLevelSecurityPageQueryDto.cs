#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FieldLevelSecurityPageQueryDto
// Guid:fe5ac8b5-2976-4244-a963-c50f6b0ad0a7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 字段级安全分页查询 DTO
/// </summary>
public sealed class FieldLevelSecurityPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字（字段名、策略描述、脱敏模式、备注）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 目标类型
    /// </summary>
    public FieldSecurityTargetType? TargetType { get; set; }

    /// <summary>
    /// 目标主键
    /// </summary>
    public long? TargetId { get; set; }

    /// <summary>
    /// 资源主键
    /// </summary>
    public long? ResourceId { get; set; }

    /// <summary>
    /// 脱敏策略
    /// </summary>
    public FieldMaskStrategy? MaskStrategy { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus? Status { get; set; }
}
