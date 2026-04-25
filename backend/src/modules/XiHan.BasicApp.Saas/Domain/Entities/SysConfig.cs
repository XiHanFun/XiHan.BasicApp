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
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统配置实体
/// 集中化配置中心：承载租户级/全局级运行时参数（功能开关、SMTP、阈值等）
/// </summary>
/// <remarks>
/// 关联：
/// - 无 FK；通过 ConfigKey 被业务代码按键检索
///
/// 写入：
/// - TenantId + ConfigKey 租户内唯一（UX_TeId_CoKe）
/// - IsGlobal=true 作为平台级默认配置（TenantId = 0），租户级同键覆盖全局
/// - 敏感值（密钥/密码类）必须在应用层加密后落库
/// - 修改后应发布配置变更事件，通知相关服务刷新缓存
///
/// 查询：
/// - 单键读取：按 TenantId + ConfigKey 精确定位（走 UX）
/// - 按分组/类型分页：IX_CoGr / IX_CoTy
/// - 合并加载：先全局后租户，租户覆盖同键
///
/// 删除：
/// - 仅软删；删除前建议确认无运行时依赖
///
/// 状态：
/// - Status: Yes/No（停用配置视为不存在）
///
/// 场景：
/// - 功能开关（feature flag）
/// - 邮件/短信/OSS 等第三方连接配置
/// - 业务阈值（审批金额、并发限额）
/// </remarks>
[SugarTable("SysConfig", "系统配置表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_CoKe", nameof(TenantId), OrderByType.Asc, nameof(ConfigKey), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_CoTy", nameof(ConfigType), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_{table}_CoGr", nameof(ConfigGroup), OrderByType.Asc)]
[SugarIndex("IX_{table}_IsGl", nameof(IsGlobal), OrderByType.Asc)]
public partial class SysConfig : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 是否平台级全局配置（全局配置对所有租户生效，TenantId = 0）
    /// </summary>
    [SugarColumn(ColumnDescription = "是否全局配置")]
    public virtual bool IsGlobal { get; set; } = false;

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
