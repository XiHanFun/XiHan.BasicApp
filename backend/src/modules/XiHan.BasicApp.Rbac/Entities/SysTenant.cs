#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTenant
// Guid:2c28152c-d6e9-4396-addb-b479254bad26
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 5:05:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Entities.Base;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 租户实体
/// </summary>
[SugarTable("sys_tenant", "租户表")]
[SugarIndex("IX_SysTenant_TenantCode", nameof(TenantCode), OrderByType.Asc, true)]
[SugarIndex("IX_SysTenant_TenantName", nameof(TenantName), OrderByType.Asc)]
public partial class SysTenant : RbacFullAuditedEntity<RbacIdType>
{
    /// <summary>
    /// 租户编码
    /// </summary>
    [SugarColumn(ColumnDescription = "租户编码", Length = 50, IsNullable = false)]
    public virtual string TenantCode { get; set; } = string.Empty;

    /// <summary>
    /// 租户名称
    /// </summary>
    [SugarColumn(ColumnDescription = "租户名称", Length = 100, IsNullable = false)]
    public virtual string TenantName { get; set; } = string.Empty;

    /// <summary>
    /// 租户简称
    /// </summary>
    [SugarColumn(ColumnDescription = "租户简称", Length = 50, IsNullable = true)]
    public virtual string? TenantShortName { get; set; }

    /// <summary>
    /// 联系人
    /// </summary>
    [SugarColumn(ColumnDescription = "联系人", Length = 50, IsNullable = true)]
    public virtual string? ContactPerson { get; set; }

    /// <summary>
    /// 联系电话
    /// </summary>
    [SugarColumn(ColumnDescription = "联系电话", Length = 20, IsNullable = true)]
    public virtual string? ContactPhone { get; set; }

    /// <summary>
    /// 联系邮箱
    /// </summary>
    [SugarColumn(ColumnDescription = "联系邮箱", Length = 100, IsNullable = true)]
    public virtual string? ContactEmail { get; set; }

    /// <summary>
    /// 地址
    /// </summary>
    [SugarColumn(ColumnDescription = "地址", Length = 500, IsNullable = true)]
    public virtual string? Address { get; set; }

    /// <summary>
    /// Logo
    /// </summary>
    [SugarColumn(ColumnDescription = "Logo", Length = 500, IsNullable = true)]
    public virtual string? Logo { get; set; }

    /// <summary>
    /// 域名
    /// </summary>
    [SugarColumn(ColumnDescription = "域名", Length = 100, IsNullable = true)]
    public virtual string? Domain { get; set; }

    /// <summary>
    /// 隔离模式
    /// </summary>
    [SugarColumn(ColumnDescription = "隔离模式")]
    public virtual TenantIsolationMode IsolationMode { get; set; } = TenantIsolationMode.Field;

    /// <summary>
    /// 数据库类型
    /// </summary>
    [SugarColumn(ColumnDescription = "数据库类型", IsNullable = true)]
    public virtual TenantDatabaseType? DatabaseType { get; set; }

    /// <summary>
    /// 数据库服务器地址
    /// </summary>
    [SugarColumn(ColumnDescription = "数据库服务器地址", Length = 200, IsNullable = true)]
    public virtual string? DatabaseHost { get; set; }

    /// <summary>
    /// 数据库端口
    /// </summary>
    [SugarColumn(ColumnDescription = "数据库端口", IsNullable = true)]
    public virtual int? DatabasePort { get; set; }

    /// <summary>
    /// 数据库名称
    /// </summary>
    [SugarColumn(ColumnDescription = "数据库名称", Length = 100, IsNullable = true)]
    public virtual string? DatabaseName { get; set; }

    /// <summary>
    /// 数据库Schema
    /// </summary>
    [SugarColumn(ColumnDescription = "数据库Schema", Length = 100, IsNullable = true)]
    public virtual string? DatabaseSchema { get; set; }

    /// <summary>
    /// 数据库用户名
    /// </summary>
    [SugarColumn(ColumnDescription = "数据库用户名", Length = 100, IsNullable = true)]
    public virtual string? DatabaseUser { get; set; }

    /// <summary>
    /// 数据库密码（加密存储）
    /// </summary>
    [SugarColumn(ColumnDescription = "数据库密码", Length = 200, IsNullable = true)]
    public virtual string? DatabasePassword { get; set; }

    /// <summary>
    /// 数据库连接字符串
    /// </summary>
    [SugarColumn(ColumnDescription = "数据库连接字符串", Length = 1000, IsNullable = true)]
    public virtual string? ConnectionString { get; set; }

    /// <summary>
    /// 配置状态
    /// </summary>
    [SugarColumn(ColumnDescription = "配置状态")]
    public virtual TenantConfigStatus ConfigStatus { get; set; } = TenantConfigStatus.Pending;

    /// <summary>
    /// 过期时间
    /// </summary>
    [SugarColumn(ColumnDescription = "过期时间", IsNullable = true)]
    public virtual DateTimeOffset? ExpireTime { get; set; }

    /// <summary>
    /// 用户数限制
    /// </summary>
    [SugarColumn(ColumnDescription = "用户数限制", IsNullable = true)]
    public virtual int? UserLimit { get; set; }

    /// <summary>
    /// 存储空间限制(MB)
    /// </summary>
    [SugarColumn(ColumnDescription = "存储空间限制(MB)", IsNullable = true)]
    public virtual long? StorageLimit { get; set; }

    /// <summary>
    /// 租户状态
    /// </summary>
    [SugarColumn(ColumnDescription = "租户状态")]
    public virtual TenantStatus TenantStatus { get; set; } = TenantStatus.Normal;

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
