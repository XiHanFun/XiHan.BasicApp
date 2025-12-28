#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConfigDto
// Guid:n1o2p3q4-r5s6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 17:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Services.Base.Dtos;

namespace XiHan.BasicApp.Rbac.Services.Configs.Dtos;

/// <summary>
/// 配置 DTO
/// </summary>
public class ConfigDto : RbacFullAuditedDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public XiHanBasicAppIdType? TenantId { get; set; }

    /// <summary>
    /// 配置键
    /// </summary>
    public string ConfigKey { get; set; } = string.Empty;

    /// <summary>
    /// 配置名称
    /// </summary>
    public string ConfigName { get; set; } = string.Empty;

    /// <summary>
    /// 配置值
    /// </summary>
    public string? ConfigValue { get; set; }

    /// <summary>
    /// 默认值
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 配置类型
    /// </summary>
    public ConfigType ConfigType { get; set; } = ConfigType.System;

    /// <summary>
    /// 数据类型
    /// </summary>
    public ConfigDataType DataType { get; set; } = ConfigDataType.String;

    /// <summary>
    /// 配置描述
    /// </summary>
    public string? ConfigDescription { get; set; }

    /// <summary>
    /// 是否内置
    /// </summary>
    public bool IsBuiltIn { get; set; } = false;

    /// <summary>
    /// 是否加密
    /// </summary>
    public bool IsEncrypted { get; set; } = false;

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
/// 创建配置 DTO
/// </summary>
public class CreateConfigDto : RbacCreationDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public XiHanBasicAppIdType? TenantId { get; set; }

    /// <summary>
    /// 配置键
    /// </summary>
    public string ConfigKey { get; set; } = string.Empty;

    /// <summary>
    /// 配置名称
    /// </summary>
    public string ConfigName { get; set; } = string.Empty;

    /// <summary>
    /// 配置值
    /// </summary>
    public string? ConfigValue { get; set; }

    /// <summary>
    /// 默认值
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 配置类型
    /// </summary>
    public ConfigType ConfigType { get; set; } = ConfigType.System;

    /// <summary>
    /// 数据类型
    /// </summary>
    public ConfigDataType DataType { get; set; } = ConfigDataType.String;

    /// <summary>
    /// 配置描述
    /// </summary>
    public string? ConfigDescription { get; set; }

    /// <summary>
    /// 是否加密
    /// </summary>
    public bool IsEncrypted { get; set; } = false;

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
/// 更新配置 DTO
/// </summary>
public class UpdateConfigDto : RbacUpdateDtoBase
{
    /// <summary>
    /// 配置名称
    /// </summary>
    public string? ConfigName { get; set; }

    /// <summary>
    /// 配置值
    /// </summary>
    public string? ConfigValue { get; set; }

    /// <summary>
    /// 默认值
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 配置类型
    /// </summary>
    public ConfigType? ConfigType { get; set; }

    /// <summary>
    /// 数据类型
    /// </summary>
    public ConfigDataType? DataType { get; set; }

    /// <summary>
    /// 配置描述
    /// </summary>
    public string? ConfigDescription { get; set; }

    /// <summary>
    /// 是否加密
    /// </summary>
    public bool? IsEncrypted { get; set; }

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

