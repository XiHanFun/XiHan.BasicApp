#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConfigListItemDto
// Guid:35f6fc93-5f79-4261-b515-3e36a3d790d0
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
/// 系统配置列表项 DTO
/// </summary>
public class ConfigListItemDto : BasicAppDto
{
    /// <summary>
    /// 是否全局配置
    /// </summary>
    public bool IsGlobal { get; set; }

    /// <summary>
    /// 配置名称
    /// </summary>
    public string ConfigName { get; set; } = string.Empty;

    /// <summary>
    /// 配置分组
    /// </summary>
    public string? ConfigGroup { get; set; }

    /// <summary>
    /// 配置键
    /// </summary>
    public string ConfigKey { get; set; } = string.Empty;

    /// <summary>
    /// 配置类型
    /// </summary>
    public ConfigType ConfigType { get; set; }

    /// <summary>
    /// 数据类型
    /// </summary>
    public ConfigDataType DataType { get; set; }

    /// <summary>
    /// 配置描述
    /// </summary>
    public string? ConfigDescription { get; set; }

    /// <summary>
    /// 是否内置
    /// </summary>
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// 是否加密
    /// </summary>
    public bool IsEncrypted { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 是否存在当前值
    /// </summary>
    public bool HasCurrentValue { get; set; }

    /// <summary>
    /// 是否存在默认值
    /// </summary>
    public bool HasFallbackValue { get; set; }

    /// <summary>
    /// 是否包含备注
    /// </summary>
    public bool HasNote { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}
