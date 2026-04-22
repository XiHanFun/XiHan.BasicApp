#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FieldLevelSecurityDto
// Guid:f0f1f2f3-a1b2-c3d4-e5f6-112233445566
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/22 18:30:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 字段级安全 DTO
/// </summary>
public class FieldLevelSecurityDto : BasicAppDto
{
    /// <summary>
    /// 目标类型
    /// </summary>
    public FieldSecurityTargetType TargetType { get; set; } = FieldSecurityTargetType.Role;

    /// <summary>
    /// 目标 ID
    /// </summary>
    public long TargetId { get; set; }

    /// <summary>
    /// 资源 ID
    /// </summary>
    public long ResourceId { get; set; }

    /// <summary>
    /// 字段名
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// 是否可读
    /// </summary>
    public bool IsReadable { get; set; } = true;

    /// <summary>
    /// 是否可编辑
    /// </summary>
    public bool IsEditable { get; set; } = true;

    /// <summary>
    /// 脱敏策略
    /// </summary>
    public FieldMaskStrategy MaskStrategy { get; set; } = FieldMaskStrategy.None;

    /// <summary>
    /// 脱敏模式
    /// </summary>
    public string? MaskPattern { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 租户 ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}

/// <summary>
/// 创建字段级安全 DTO
/// </summary>
public class FieldLevelSecurityCreateDto : BasicAppCDto
{
    /// <summary>
    /// 目标类型
    /// </summary>
    public FieldSecurityTargetType TargetType { get; set; } = FieldSecurityTargetType.Role;

    /// <summary>
    /// 目标 ID
    /// </summary>
    [Range(1, long.MaxValue, ErrorMessage = "目标 ID 无效")]
    public long TargetId { get; set; }

    /// <summary>
    /// 资源 ID
    /// </summary>
    [Range(1, long.MaxValue, ErrorMessage = "资源 ID 无效")]
    public long ResourceId { get; set; }

    /// <summary>
    /// 字段名
    /// </summary>
    [Required(ErrorMessage = "字段名不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "字段名长度必须在 1～100 之间")]
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// 是否可读
    /// </summary>
    public bool IsReadable { get; set; } = true;

    /// <summary>
    /// 是否可编辑
    /// </summary>
    public bool IsEditable { get; set; } = true;

    /// <summary>
    /// 脱敏策略
    /// </summary>
    public FieldMaskStrategy MaskStrategy { get; set; } = FieldMaskStrategy.None;

    /// <summary>
    /// 脱敏模式
    /// </summary>
    [StringLength(500, ErrorMessage = "脱敏模式长度不能超过 500")]
    public string? MaskPattern { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    [StringLength(500, ErrorMessage = "描述长度不能超过 500")]
    public string? Description { get; set; }

    /// <summary>
    /// 租户 ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}

/// <summary>
/// 更新字段级安全 DTO
/// </summary>
public class FieldLevelSecurityUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 目标类型
    /// </summary>
    public FieldSecurityTargetType TargetType { get; set; } = FieldSecurityTargetType.Role;

    /// <summary>
    /// 目标 ID
    /// </summary>
    [Range(1, long.MaxValue, ErrorMessage = "目标 ID 无效")]
    public long TargetId { get; set; }

    /// <summary>
    /// 资源 ID
    /// </summary>
    [Range(1, long.MaxValue, ErrorMessage = "资源 ID 无效")]
    public long ResourceId { get; set; }

    /// <summary>
    /// 字段名
    /// </summary>
    [Required(ErrorMessage = "字段名不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "字段名长度必须在 1～100 之间")]
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// 是否可读
    /// </summary>
    public bool IsReadable { get; set; } = true;

    /// <summary>
    /// 是否可编辑
    /// </summary>
    public bool IsEditable { get; set; } = true;

    /// <summary>
    /// 脱敏策略
    /// </summary>
    public FieldMaskStrategy MaskStrategy { get; set; } = FieldMaskStrategy.None;

    /// <summary>
    /// 脱敏模式
    /// </summary>
    [StringLength(500, ErrorMessage = "脱敏模式长度不能超过 500")]
    public string? MaskPattern { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    [StringLength(500, ErrorMessage = "描述长度不能超过 500")]
    public string? Description { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}
