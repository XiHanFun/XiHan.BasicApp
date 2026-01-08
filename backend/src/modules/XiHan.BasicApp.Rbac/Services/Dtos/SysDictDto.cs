#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysDictDto
// Guid:d0e1f2a3-b4c5-6789-0123-4567890def01
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Services.Dtos;

/// <summary>
/// 系统字典创建 DTO
/// </summary>
public class SysDictCreateDto : RbacCreationDtoBase
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
    /// 字典描述
    /// </summary>
    public string? DictDescription { get; set; }

    /// <summary>
    /// 是否内置
    /// </summary>
    public bool IsBuiltIn { get; set; } = false;

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
/// 系统字典更新 DTO
/// </summary>
public class SysDictUpdateDto : RbacUpdateDtoBase
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
    /// 字典描述
    /// </summary>
    public string? DictDescription { get; set; }

    /// <summary>
    /// 是否内置
    /// </summary>
    public bool IsBuiltIn { get; set; } = false;

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
/// 系统字典查询 DTO
/// </summary>
public class SysDictGetDto : RbacFullAuditedDtoBase
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
    /// 字典描述
    /// </summary>
    public string? DictDescription { get; set; }

    /// <summary>
    /// 是否内置
    /// </summary>
    public bool IsBuiltIn { get; set; } = false;

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
