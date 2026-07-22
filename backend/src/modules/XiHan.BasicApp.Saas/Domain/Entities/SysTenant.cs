// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统租户实体
/// </summary>
/// <remarks>
/// 租户生命周期由 TenantStatus 唯一控制：
/// - Normal：正常运营
/// - Suspended：管理员手动暂停（如违规）
/// - Expired：订阅到期，自动或手动标记
/// - Disabled：已停用/归档，等效于逻辑删除
/// 服务层判断"租户是否可用"只需：TenantStatus == Normal
/// </remarks>
[SugarTable(TableName = "Sys_Tenant", TableDescription = "系统租户表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeCo", nameof(TenantCode), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_TeNa", nameof(TenantName), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeSt", nameof(TenantStatus), OrderByType.Asc)]
[SugarIndex("IX_{table}_CoSt", nameof(ConfigStatus), OrderByType.Asc)]
[SugarIndex("IX_{table}_ExTi", nameof(ExpirationTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_EdId", nameof(EditionId), OrderByType.Asc)]
public partial class SysTenant : BasicAppAggregateRoot
{
    /// <summary>
    /// 租户编码
    /// </summary>
    [SugarColumn(ColumnName = "Tenant_Code", ColumnDescription = "租户编码", Length = 50, IsNullable = false)]
    public virtual string TenantCode { get; set; } = string.Empty;

    /// <summary>
    /// 版本/套餐ID（关联 SysTenantEdition 表，控制租户可用功能范围；null 表示使用 IsDefault=true 的默认版本）
    /// </summary>
    [SugarColumn(ColumnName = "Edition_Id", ColumnDescription = "版本ID", IsNullable = true)]
    public virtual long? EditionId { get; set; }

    /// <summary>
    /// 租户名称
    /// </summary>
    [SugarColumn(ColumnName = "Tenant_Name", ColumnDescription = "租户名称", Length = 100, IsNullable = false)]
    public virtual string TenantName { get; set; } = string.Empty;

    /// <summary>
    /// 租户简称
    /// </summary>
    [SugarColumn(ColumnName = "Tenant_Short_Name", ColumnDescription = "租户简称", Length = 50, IsNullable = true)]
    public virtual string? TenantShortName { get; set; }

    /// <summary>
    /// 联系人
    /// </summary>
    [SugarColumn(ColumnName = "Contact_Person", ColumnDescription = "联系人", Length = 50, IsNullable = true)]
    public virtual string? ContactPerson { get; set; }

    /// <summary>
    /// 联系电话
    /// </summary>
    [SugarColumn(ColumnName = "Contact_Phone", ColumnDescription = "联系电话", Length = 20, IsNullable = true)]
    public virtual string? ContactPhone { get; set; }

    /// <summary>
    /// 联系邮箱
    /// </summary>
    [SugarColumn(ColumnName = "Contact_Email", ColumnDescription = "联系邮箱", Length = 100, IsNullable = true)]
    public virtual string? ContactEmail { get; set; }

    /// <summary>
    /// 地址
    /// </summary>
    [SugarColumn(ColumnName = "Address", ColumnDescription = "地址", Length = 500, IsNullable = true)]
    public virtual string? Address { get; set; }

    /// <summary>
    /// Logo
    /// </summary>
    [SugarColumn(ColumnName = "Logo", ColumnDescription = "Logo", Length = 500, IsNullable = true)]
    public virtual string? Logo { get; set; }

    /// <summary>
    /// 域名
    /// </summary>
    [SugarColumn(ColumnName = "Domain", ColumnDescription = "域名", Length = 100, IsNullable = true)]
    public virtual string? Domain { get; set; }

    /// <summary>
    /// 隔离模式
    /// </summary>
    [SugarColumn(ColumnName = "Isolation_Mode", ColumnDescription = "隔离模式")]
    public virtual TenantIsolationMode IsolationMode { get; set; } = TenantIsolationMode.Field;

    /// <summary>
    /// 数据库类型
    /// </summary>
    [SugarColumn(ColumnName = "Database_Type", ColumnDescription = "数据库类型", IsNullable = true)]
    public virtual TenantDatabaseType? DatabaseType { get; set; }

    /// <summary>
    /// 数据库Schema
    /// </summary>
    [SugarColumn(ColumnName = "Database_Schema", ColumnDescription = "数据库Schema", Length = 100, IsNullable = true)]
    public virtual string? DatabaseSchema { get; set; }

    /// <summary>
    /// 数据库连接字符串（敏感信息加密存储）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(ColumnName = "Connection_String", ColumnDescription = "数据库连接字符串", Length = 1000, IsNullable = true)]
    public virtual string? ConnectionString { get; set; }

    /// <summary>
    /// 连接字符串是否已加密（用于密钥轮换和加密状态审计）
    /// </summary>
    [SugarColumn(ColumnName = "Is_Connection_String_Encrypted", ColumnDescription = "连接字符串是否已加密")]
    public virtual bool IsConnectionStringEncrypted { get; set; } = false;

    /// <summary>
    /// 配置状态
    /// </summary>
    [SugarColumn(ColumnName = "Config_Status", ColumnDescription = "配置状态")]
    public virtual TenantConfigStatus ConfigStatus { get; set; } = TenantConfigStatus.Pending;

    /// <summary>
    /// 过期时间
    /// </summary>
    [SugarColumn(ColumnName = "Expiration_Time", ColumnDescription = "过期时间", IsNullable = true)]
    public virtual DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// 用户数限制（租户级覆盖值，为空时取 SysTenantEdition 的默认值）
    /// </summary>
    [SugarColumn(ColumnName = "User_Limit", ColumnDescription = "用户数限制", IsNullable = true)]
    public virtual int? UserLimit { get; set; }

    /// <summary>
    /// 存储空间限制(MB)（租户级覆盖值，为空时取 SysTenantEdition 的默认值）
    /// </summary>
    [SugarColumn(ColumnName = "Storage_Limit", ColumnDescription = "存储空间限制(MB)", IsNullable = true)]
    public virtual long? StorageLimit { get; set; }

    /// <summary>
    /// 租户状态
    /// </summary>
    [SugarColumn(ColumnName = "Tenant_Status", ColumnDescription = "租户状态")]
    public virtual TenantStatus TenantStatus { get; set; } = TenantStatus.Normal;

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnName = "Sort", ColumnDescription = "排序")]
    public virtual int Sort { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
