#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConfigPageQueryDto
// Guid:6643fec2-5190-42f6-9b28-c481196e49a4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统配置分页查询 DTO
/// </summary>
public sealed class ConfigPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 是否全局配置
    /// </summary>
    public bool? IsGlobal { get; set; }

    /// <summary>
    /// 配置分组
    /// </summary>
    public string? ConfigGroup { get; set; }

    /// <summary>
    /// 配置类型
    /// </summary>
    public ConfigType? ConfigType { get; set; }

    /// <summary>
    /// 数据类型
    /// </summary>
    public ConfigDataType? DataType { get; set; }

    /// <summary>
    /// 是否内置
    /// </summary>
    public bool? IsBuiltIn { get; set; }

    /// <summary>
    /// 是否加密
    /// </summary>
    public bool? IsEncrypted { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus? Status { get; set; }
}
