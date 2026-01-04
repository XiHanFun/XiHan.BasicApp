#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DictItemDto
// Guid:k1l2m3n4-o5p6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 16:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Services.DictItems.Dtos;

/// <summary>
/// 字典项 DTO
/// </summary>
public class DictItemDto : RbacFullAuditedDtoBase
{
    /// <summary>
    /// 字典ID
    /// </summary>
    public long DictId { get; set; }

    /// <summary>
    /// 字典编码（冗余字段，便于查询）
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
/// 创建字典项 DTO
/// </summary>
public class CreateDictItemDto : RbacCreationDtoBase
{
    /// <summary>
    /// 字典ID
    /// </summary>
    public long DictId { get; set; }

    /// <summary>
    /// 字典编码（冗余字段，便于查询）
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
    /// 排序
    /// </summary>
    public int Sort { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 更新字典项 DTO
/// </summary>
public class UpdateDictItemDto : RbacUpdateDtoBase
{
    /// <summary>
    /// 父级字典项ID
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 字典项名称
    /// </summary>
    public string? ItemName { get; set; }

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
    public bool? IsDefault { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo? Status { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int? Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
