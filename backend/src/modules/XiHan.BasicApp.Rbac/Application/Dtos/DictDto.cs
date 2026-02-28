#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DictDto
// Guid:ab8db9d6-dc86-4429-bb46-8fc492bd65a0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:44:01
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Rbac.Domain.Enums;

namespace XiHan.BasicApp.Rbac.Application.Dtos;

/// <summary>
/// 字典 DTO
/// </summary>
public class DictDto : BasicAppDto
{
    /// <summary>
    /// 字典编码
    /// </summary>
    public string DictCode { get; set; } = string.Empty;

    /// <summary>
    /// 字典名称
    /// </summary>
    public string DictName { get; set; } = string.Empty;

    /// <summary>
    /// 字典类型
    /// </summary>
    public string DictType { get; set; } = string.Empty;

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;
}

/// <summary>
/// 字典项 DTO
/// </summary>
public class DictItemDto : BasicAppDto
{
    /// <summary>
    /// 字典ID
    /// </summary>
    public long DictId { get; set; }

    /// <summary>
    /// 字典编码
    /// </summary>
    public string DictCode { get; set; } = string.Empty;

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
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;
}

/// <summary>
/// 创建字典 DTO
/// </summary>
public class DictCreateDto : BasicAppCDto
{
    /// <summary>
    /// 字典编码
    /// </summary>
    [Required(ErrorMessage = "字典编码不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "字典编码长度必须在 1～100 之间")]
    public string DictCode { get; set; } = string.Empty;

    /// <summary>
    /// 字典名称
    /// </summary>
    [Required(ErrorMessage = "字典名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "字典名称长度必须在 1～100 之间")]
    public string DictName { get; set; } = string.Empty;

    /// <summary>
    /// 字典类型
    /// </summary>
    [Required(ErrorMessage = "字典类型不能为空")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "字典类型长度必须在 1～50 之间")]
    public string DictType { get; set; } = string.Empty;

    /// <summary>
    /// 字典描述
    /// </summary>
    [StringLength(500, ErrorMessage = "字典描述长度不能超过 500")]
    public string? DictDescription { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }
}

/// <summary>
/// 更新字典 DTO
/// </summary>
public class DictUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 字典编码
    /// </summary>
    [Required(ErrorMessage = "字典编码不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "字典编码长度必须在 1～100 之间")]
    public string DictCode { get; set; } = string.Empty;

    /// <summary>
    /// 字典名称
    /// </summary>
    [Required(ErrorMessage = "字典名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "字典名称长度必须在 1～100 之间")]
    public string DictName { get; set; } = string.Empty;

    /// <summary>
    /// 字典类型
    /// </summary>
    [Required(ErrorMessage = "字典类型不能为空")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "字典类型长度必须在 1～50 之间")]
    public string DictType { get; set; } = string.Empty;

    /// <summary>
    /// 字典描述
    /// </summary>
    [StringLength(500, ErrorMessage = "字典描述长度不能超过 500")]
    public string? DictDescription { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}

/// <summary>
/// 创建字典项 DTO
/// </summary>
public class DictItemCreateDto : BasicAppCDto
{
    /// <summary>
    /// 字典ID
    /// </summary>
    [Range(1, long.MaxValue, ErrorMessage = "字典 ID 无效")]
    public long DictId { get; set; }

    /// <summary>
    /// 字典编码
    /// </summary>
    [Required(ErrorMessage = "字典编码不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "字典编码长度必须在 1～100 之间")]
    public string DictCode { get; set; } = string.Empty;

    /// <summary>
    /// 父级字典项ID
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 字典项编码
    /// </summary>
    [Required(ErrorMessage = "字典项编码不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "字典项编码长度必须在 1～100 之间")]
    public string ItemCode { get; set; } = string.Empty;

    /// <summary>
    /// 字典项名称
    /// </summary>
    [Required(ErrorMessage = "字典项名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "字典项名称长度必须在 1～100 之间")]
    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// 字典项值
    /// </summary>
    [StringLength(200, ErrorMessage = "字典项值长度不能超过 200")]
    public string? ItemValue { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }
}

/// <summary>
/// 更新字典项 DTO
/// </summary>
public class DictItemUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 字典ID
    /// </summary>
    [Range(1, long.MaxValue, ErrorMessage = "字典 ID 无效")]
    public long DictId { get; set; }

    /// <summary>
    /// 字典编码
    /// </summary>
    [Required(ErrorMessage = "字典编码不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "字典编码长度必须在 1～100 之间")]
    public string DictCode { get; set; } = string.Empty;

    /// <summary>
    /// 父级字典项ID
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 字典项编码
    /// </summary>
    [Required(ErrorMessage = "字典项编码不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "字典项编码长度必须在 1～100 之间")]
    public string ItemCode { get; set; } = string.Empty;

    /// <summary>
    /// 字典项名称
    /// </summary>
    [Required(ErrorMessage = "字典项名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "字典项名称长度必须在 1～100 之间")]
    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// 字典项值
    /// </summary>
    [StringLength(200, ErrorMessage = "字典项值长度不能超过 200")]
    public string? ItemValue { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}
