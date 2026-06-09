#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FieldLevelSecurityDetailDto
// Guid:c50ed543-36b3-44a0-bcae-7c54e47bd0e6
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
/// 字段级安全详情 DTO
/// </summary>
public sealed class FieldLevelSecurityDetailDto : BasicAppDto
{
    /// <summary>
    /// 目标类型
    /// </summary>
    public FieldSecurityTargetType TargetType { get; set; }

    /// <summary>
    /// 目标主键
    /// </summary>
    public long TargetId { get; set; }

    /// <summary>
    /// 目标编码
    /// </summary>
    public string? TargetCode { get; set; }

    /// <summary>
    /// 目标名称
    /// </summary>
    public string? TargetName { get; set; }

    /// <summary>
    /// 资源主键
    /// </summary>
    public long ResourceId { get; set; }

    /// <summary>
    /// 资源编码
    /// </summary>
    public string? ResourceCode { get; set; }

    /// <summary>
    /// 资源名称
    /// </summary>
    public string? ResourceName { get; set; }

    /// <summary>
    /// 资源类型
    /// </summary>
    public ResourceType? ResourceType { get; set; }

    /// <summary>
    /// 字段名
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// 是否可读
    /// </summary>
    public bool IsReadable { get; set; }

    /// <summary>
    /// 是否可编辑
    /// </summary>
    public bool IsEditable { get; set; }

    /// <summary>
    /// 脱敏策略
    /// </summary>
    public FieldMaskStrategy MaskStrategy { get; set; }

    /// <summary>
    /// 脱敏模式
    /// </summary>
    public string? MaskPattern { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 策略描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 创建人主键
    /// </summary>
    public long? CreatedId { get; set; }

    /// <summary>
    /// 创建人
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }

    /// <summary>
    /// 修改人主键
    /// </summary>
    public long? ModifiedId { get; set; }

    /// <summary>
    /// 修改人
    /// </summary>
    public string? ModifiedBy { get; set; }
}
