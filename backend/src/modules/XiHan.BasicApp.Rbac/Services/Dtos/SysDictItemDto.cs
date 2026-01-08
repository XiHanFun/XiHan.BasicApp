#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysDictItemDto
// Guid:e1f2a3b4-c5d6-7890-1234-567890ef0123
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Services.Dtos;

/// <summary>
/// 系统字典项创建 DTO
/// </summary>
public class SysDictItemCreateDto : RbacCreationDtoBase
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
    /// 父级字典项ID
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
    /// 扩展属性1
    /// </summary>
    public string? ExtendField1 { get; set; }

    /// <summary>
    /// 扩展属性2
    /// </summary>
    public string? ExtendField2 { get; set; }

    /// <summary>
    /// 扩展属性3
    /// </summary>
    public string? ExtendField3 { get; set; }

    /// <summary>
    /// 是否默认
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 系统字典项更新 DTO
/// </summary>
public class SysDictItemUpdateDto : RbacUpdateDtoBase
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
    /// 父级字典项ID
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
    /// 扩展属性1
    /// </summary>
    public string? ExtendField1 { get; set; }

    /// <summary>
    /// 扩展属性2
    /// </summary>
    public string? ExtendField2 { get; set; }

    /// <summary>
    /// 扩展属性3
    /// </summary>
    public string? ExtendField3 { get; set; }

    /// <summary>
    /// 是否默认
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 系统字典项查询 DTO
/// </summary>
public class SysDictItemGetDto : RbacFullAuditedDtoBase
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
    /// 父级字典项ID
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
    /// 扩展属性1
    /// </summary>
    public string? ExtendField1 { get; set; }

    /// <summary>
    /// 扩展属性2
    /// </summary>
    public string? ExtendField2 { get; set; }

    /// <summary>
    /// 扩展属性3
    /// </summary>
    public string? ExtendField3 { get; set; }

    /// <summary>
    /// 是否默认
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
