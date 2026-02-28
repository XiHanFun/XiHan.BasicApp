#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConfigDto
// Guid:4164103b-5cab-41b7-a200-d8ddaeb63dd9
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
/// 配置 DTO
/// </summary>
public class ConfigDto : BasicAppDto
{
    /// <summary>
    /// 配置名称
    /// </summary>
    public string ConfigName { get; set; } = string.Empty;

    /// <summary>
    /// 配置键
    /// </summary>
    public string ConfigKey { get; set; } = string.Empty;

    /// <summary>
    /// 配置值
    /// </summary>
    public string? ConfigValue { get; set; }

    /// <summary>
    /// 配置类型
    /// </summary>
    public ConfigType ConfigType { get; set; } = ConfigType.System;

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;
}

/// <summary>
/// 创建配置 DTO
/// </summary>
public class ConfigCreateDto : BasicAppCDto
{
    /// <summary>
    /// 配置名称
    /// </summary>
    [Required(ErrorMessage = "配置名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "配置名称长度必须在 1～100 之间")]
    public string ConfigName { get; set; } = string.Empty;

    /// <summary>
    /// 配置键
    /// </summary>
    [Required(ErrorMessage = "配置键不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "配置键长度必须在 1～100 之间")]
    public string ConfigKey { get; set; } = string.Empty;

    /// <summary>
    /// 配置值
    /// </summary>
    public string? ConfigValue { get; set; }

    /// <summary>
    /// 配置类型
    /// </summary>
    public ConfigType ConfigType { get; set; } = ConfigType.System;

    /// <summary>
    /// 数据类型
    /// </summary>
    public ConfigDataType DataType { get; set; } = ConfigDataType.String;

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}

/// <summary>
/// 更新配置 DTO
/// </summary>
public class ConfigUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 配置名称
    /// </summary>
    [Required(ErrorMessage = "配置名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "配置名称长度必须在 1～100 之间")]
    public string ConfigName { get; set; } = string.Empty;

    /// <summary>
    /// 配置键
    /// </summary>
    [Required(ErrorMessage = "配置键不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "配置键长度必须在 1～100 之间")]
    public string ConfigKey { get; set; } = string.Empty;

    /// <summary>
    /// 配置值
    /// </summary>
    public string? ConfigValue { get; set; }

    /// <summary>
    /// 配置类型
    /// </summary>
    public ConfigType ConfigType { get; set; } = ConfigType.System;

    /// <summary>
    /// 数据类型
    /// </summary>
    public ConfigDataType DataType { get; set; } = ConfigDataType.String;

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
