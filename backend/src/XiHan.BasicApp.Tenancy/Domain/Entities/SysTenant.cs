// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Tenancy.Domain.Entities;

/// <summary>
/// 租户（平台库业务实体）
/// </summary>
/// <remarks>
/// 套餐、到期、配额、状态归这里；租户目录（ID、显示名、是否启用、分片连接）是框架的 <c>TenantRecord</c>，开通 / 启停 / 改名 / 配置独立库时在同一工作单元里同步
/// <see cref="TenantId"/> 是目录键（登录与令牌里的 <c>tenant</c>），创建后不可改
/// </remarks>
[SugarTable(TableName = "sys_tenant", TableDescription = "租户（平台库业务实体）")]
[ActiveUniqueIndex(nameof(TenantId), Name = "ux_sys_tenant_tenant_id")]
[SugarIndex("ix_sys_tenant_edition_id", nameof(EditionId), OrderByType.Asc)]
[SugarIndex("ix_sys_tenant_admin_user_id", nameof(AdminUserId), OrderByType.Asc)]
public sealed class SysTenant : AppAuditedEntity
{
    /// <summary>
    /// 租户 ID（目录键）：字母或数字开头，只含字母、数字、<c>.</c>、<c>_</c>、<c>-</c>，最长 64
    /// </summary>
    [SugarColumn(ColumnName = "tenant_id", Length = 512, IsNullable = false, ColumnDescription = "租户 ID（目录键）：字母或数字开头，只含字母、数字、.、_、-，最长 64")]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnName = "name", Length = 512, IsNullable = false, ColumnDescription = "名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 说明
    /// </summary>
    [SugarColumn(ColumnName = "description", Length = 512, IsNullable = true, ColumnDescription = "说明")]
    public string? Description { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "状态")]
    public TenantStatus Status { get; set; } = TenantStatus.Active;

    /// <summary>
    /// 到期时间（UTC）
    /// </summary>
    /// <remarks>
    /// 为空即不限。随目录属性同步，到了这一刻 Identity 即拒绝登录、刷新与切入；到期作业把仍生效的租户标为 <see cref="TenantStatus.Expired"/>
    /// </remarks>
    [SugarColumn(ColumnName = "expires_at", IsNullable = true, ColumnDescription = "到期时间（UTC）")]
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// 租户管理员（<c>tenants.initialize-admin</c> 写入）
    /// </summary>
    /// <remarks>
    /// 为空即未初始化
    /// </remarks>
    [SugarColumn(ColumnName = "admin_user_id", IsNullable = true, ColumnDescription = "租户管理员（tenants.initialize-admin 写入）")]
    public Guid? AdminUserId { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    [SugarColumn(ColumnName = "order", IsNullable = false, ColumnDescription = "排序号")]
    public int Order { get; set; }

    /// <summary>
    /// 租户绑定的套餐（<see cref="SysEdition"/>）
    /// </summary>
    /// <remarks>
    /// 有效权限与它的白名单求交。为空即不受套餐限制（只能经 <c>tenants.change-edition</c> 显式选择）；开通时不指定即用缺省套餐
    /// </remarks>
    [SugarColumn(ColumnName = "edition_id", IsNullable = true, ColumnDescription = "租户绑定的套餐（SysEdition）")]
    public Guid? EditionId { get; set; }

    /// <summary>
    /// 租户自己的席位上限，覆盖套餐的
    /// </summary>
    /// <remarks>
    /// 为空即沿用套餐
    /// </remarks>
    [SugarColumn(ColumnName = "user_limit", IsNullable = true, ColumnDescription = "租户自己的席位上限，覆盖套餐的")]
    public int? UserLimit { get; set; }

    /// <summary>
    /// 租户自己的存储空间上限（MB），覆盖套餐的
    /// </summary>
    /// <remarks>
    /// 为空即沿用套餐
    /// </remarks>
    [SugarColumn(ColumnName = "storage_limit_mb", IsNullable = true, ColumnDescription = "租户自己的存储空间上限（MB），覆盖套餐的")]
    public long? StorageLimitMb { get; set; }
    /// <summary>
    /// 隔离方式
    /// </summary>
    [SugarColumn(ColumnName = "isolation_mode", IsNullable = false, ColumnDescription = "隔离方式")]
    public TenantIsolationMode IsolationMode { get; set; }

    /// <summary>
    /// 独立库的库类型
    /// </summary>
    /// <remarks>
    /// <c>postgresql</c>、<c>sqlite</c>、<c>mysql</c> 或 <c>sqlserver</c>；为空即与平台库相同
    /// </remarks>
    [SugarColumn(ColumnName = "database_provider", Length = 32, IsNullable = true, ColumnDescription = "独立库的库类型")]
    public string? DatabaseProvider { get; set; }

    /// <summary>
    /// 独立库的连接串（密文）
    /// </summary>
    /// <remarks>
    /// 经 <c>ISecretProtector</c> 加密存放，同步进租户目录时仍是密文，由 Runtime 的分片解析器解密；不进任何响应
    /// </remarks>
    [SugarColumn(ColumnName = "connection_string", Length = 2048, IsNullable = true, ColumnDescription = "独立库的连接串（密文）")]
    public string? ConnectionString { get; set; }

    /// <summary>
    /// 独立库的状态
    /// </summary>
    [SugarColumn(ColumnName = "database_status", IsNullable = false, ColumnDescription = "独立库的状态")]
    public TenantDatabaseStatus DatabaseStatus { get; set; }

    /// <summary>
    /// 最近一次初始化失败的原因
    /// </summary>
    [SugarColumn(ColumnName = "database_error", Length = 1024, IsNullable = true, ColumnDescription = "最近一次初始化失败的原因")]
    public string? DatabaseError { get; set; }

    /// <summary>
    /// 独立库就绪的时间（UTC）
    /// </summary>
    [SugarColumn(ColumnName = "database_initialized_at", IsNullable = true, ColumnDescription = "独立库就绪的时间（UTC）")]
    public DateTimeOffset? DatabaseInitializedAt { get; set; }
}

/// <summary>
/// 租户独立库的状态
/// </summary>
/// <remarks>
/// 配好连接即待初始化，<c>tenants.initialize-database</c> 成功即就绪、失败记原因可再试
/// </remarks>
public enum TenantDatabaseStatus
{
    /// <summary>
    /// 共库，不需要独立库
    /// </summary>
    NotRequired = 0,

    /// <summary>
    /// 待初始化
    /// </summary>
    Pending = 1,

    /// <summary>
    /// 已就绪
    /// </summary>
    Ready = 2,

    /// <summary>
    /// 初始化失败
    /// </summary>
    Failed = 3,
}

/// <summary>
/// 租户数据的隔离方式
/// </summary>
/// <remarks>
/// 方案 5.10：Field（共库、按租户列过滤，缺省）与 Database（落租户库的实体在租户自己的库里）
/// </remarks>
public enum TenantIsolationMode
{
    /// <summary>
    /// 共库，按租户列隔离
    /// </summary>
    Field = 0,

    /// <summary>
    /// 独立库
    /// </summary>
    Database = 1,
}

/// <summary>
/// 租户状态
/// </summary>
/// <remarks>
/// 与框架租户目录的 <c>IsEnabled</c> 同步（Active ⇔ 启用）
/// </remarks>
public enum TenantStatus
{
    /// <summary>
    /// 生效：租户用户可登录
    /// </summary>
    Active = 0,

    /// <summary>
    /// 停用：目录标记为禁用，租户用户不能登录、带该租户声明的调用被拒
    /// </summary>
    Suspended = 1,

    /// <summary>
    /// 到期：到期作业把过了到期时间的生效租户标为到期，目录随之禁用
    /// </summary>
    /// <remarks>
    /// 延长到期时间之后用 <c>tenants.resume</c> 恢复；停用照常可用
    /// </remarks>
    Expired = 2,
}
