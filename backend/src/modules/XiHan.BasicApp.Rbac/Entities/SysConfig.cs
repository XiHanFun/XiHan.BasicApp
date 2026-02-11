#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysConfig
// Guid:1c28152c-d6e9-4396-addb-b479254bad25
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 05:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统配置实体
/// </summary>
[SugarTable("Sys_Config", "系统配置表")]
[SugarIndex("IX_SysConfig_ConfigKey", nameof(ConfigKey), OrderByType.Asc, true)]
[SugarIndex("IX_SysConfig_ConfigType", nameof(ConfigType), OrderByType.Asc)]
[SugarIndex("IX_SysConfig_TenantId", nameof(TenantId), OrderByType.Asc)]
[SugarIndex("IX_SysConfig_TenantId_ConfigKey", nameof(TenantId), OrderByType.Asc, nameof(ConfigKey), OrderByType.Asc)]
[SugarIndex("IX_SysConfig_Status", nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_SysConfig_ConfigGroup", nameof(ConfigGroup), OrderByType.Asc)]
public partial class SysConfig : RbacAggregateRoot<long>
{
    /// <summary>
    /// 配置名称
    /// </summary>
    [SugarColumn(ColumnDescription = "配置名称", Length = 100, IsNullable = false)]
    public virtual string ConfigName { get; set; } = string.Empty;

    /// <summary>
    /// 配置分组
    /// </summary>
    [SugarColumn(ColumnDescription = "配置分组", Length = 100, IsNullable = true)]
    public virtual string? ConfigGroup { get; set; }

    /// <summary>
    /// 配置键
    /// </summary>
    [SugarColumn(ColumnDescription = "配置键", Length = 100, IsNullable = false)]
    public virtual string ConfigKey { get; set; } = string.Empty;

    /// <summary>
    /// 配置值
    /// </summary>
    [SugarColumn(ColumnDescription = "配置值", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ConfigValue { get; set; }

    /// <summary>
    /// 默认值
    /// </summary>
    [SugarColumn(ColumnDescription = "默认值", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? DefaultValue { get; set; }

    /// <summary>
    /// 配置类型
    /// </summary>
    [SugarColumn(ColumnDescription = "配置类型")]
    public virtual ConfigType ConfigType { get; set; } = ConfigType.System;

    /// <summary>
    /// 数据类型
    /// </summary>
    [SugarColumn(ColumnDescription = "数据类型")]
    public virtual ConfigDataType DataType { get; set; } = ConfigDataType.String;

    /// <summary>
    /// 配置描述
    /// </summary>
    [SugarColumn(ColumnDescription = "配置描述", Length = 500, IsNullable = true)]
    public virtual string? ConfigDescription { get; set; }

    /// <summary>
    /// 是否内置
    /// </summary>
    [SugarColumn(ColumnDescription = "是否内置")]
    public virtual bool IsBuiltIn { get; set; } = false;

    /// <summary>
    /// 是否加密
    /// </summary>
    [SugarColumn(ColumnDescription = "是否加密")]
    public virtual bool IsEncrypted { get; set; } = false;

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public virtual YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnDescription = "排序")]
    public virtual int Sort { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
