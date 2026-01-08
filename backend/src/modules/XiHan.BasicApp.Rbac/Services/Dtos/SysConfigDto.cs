#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysConfigDto
// Guid:c9d0e1f2-a3b4-5678-9012-34567890cdef
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Services.Dtos;

/// <summary>
/// 系统配置创建 DTO
/// </summary>
public class SysConfigCreateDto : RbacCreationDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

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
/// 系统配置更新 DTO
/// </summary>
public class SysConfigUpdateDto : RbacUpdateDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

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
/// 系统配置查询 DTO
/// </summary>
public class SysConfigGetDto : RbacFullAuditedDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

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
