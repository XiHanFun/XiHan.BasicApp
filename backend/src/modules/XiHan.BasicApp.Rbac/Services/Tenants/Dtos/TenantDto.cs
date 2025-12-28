#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantDto
// Guid:7a2b3c4d-5e6f-7890-abcd-ef1234567896
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 4:30:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Services.Base.Dtos;

namespace XiHan.BasicApp.Rbac.Services.Tenants.Dtos;

/// <summary>
/// 租户 DTO
/// </summary>
public class TenantDto : RbacFullAuditedDtoBase
{
    /// <summary>
    /// 租户编码
    /// </summary>
    public string TenantCode { get; set; } = string.Empty;

    /// <summary>
    /// 租户名称
    /// </summary>
    public string TenantName { get; set; } = string.Empty;

    /// <summary>
    /// 租户简称
    /// </summary>
    public string? TenantShortName { get; set; }

    /// <summary>
    /// 联系人
    /// </summary>
    public string? ContactPerson { get; set; }

    /// <summary>
    /// 联系电话
    /// </summary>
    public string? ContactPhone { get; set; }

    /// <summary>
    /// 联系邮箱
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// 地址
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Logo
    /// </summary>
    public string? Logo { get; set; }

    /// <summary>
    /// 域名
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// 隔离模式
    /// </summary>
    public TenantIsolationMode IsolationMode { get; set; }

    /// <summary>
    /// 配置状态
    /// </summary>
    public TenantConfigStatus ConfigStatus { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset? ExpireTime { get; set; }

    /// <summary>
    /// 用户数限制
    /// </summary>
    public int? UserLimit { get; set; }

    /// <summary>
    /// 存储空间限制(MB)
    /// </summary>
    public XiHanBasicAppIdType? StorageLimit { get; set; }

    /// <summary>
    /// 租户状态
    /// </summary>
    public TenantStatus TenantStatus { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 租户详情 DTO
/// </summary>
public class TenantDetailDto : TenantDto
{
    /// <summary>
    /// 数据库类型
    /// </summary>
    public TenantDatabaseType? DatabaseType { get; set; }

    /// <summary>
    /// 数据库服务器地址
    /// </summary>
    public string? DatabaseHost { get; set; }

    /// <summary>
    /// 数据库端口
    /// </summary>
    public int? DatabasePort { get; set; }

    /// <summary>
    /// 数据库名称
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// 数据库Schema
    /// </summary>
    public string? DatabaseSchema { get; set; }

    /// <summary>
    /// 用户数量
    /// </summary>
    public int UserCount { get; set; }

    /// <summary>
    /// 已使用存储空间(MB)
    /// </summary>
    public XiHanBasicAppIdType UsedStorage { get; set; }
}

/// <summary>
/// 创建租户 DTO
/// </summary>
public class CreateTenantDto : RbacCreationDtoBase
{
    /// <summary>
    /// 租户编码
    /// </summary>
    public string TenantCode { get; set; } = string.Empty;

    /// <summary>
    /// 租户名称
    /// </summary>
    public string TenantName { get; set; } = string.Empty;

    /// <summary>
    /// 租户简称
    /// </summary>
    public string? TenantShortName { get; set; }

    /// <summary>
    /// 联系人
    /// </summary>
    public string? ContactPerson { get; set; }

    /// <summary>
    /// 联系电话
    /// </summary>
    public string? ContactPhone { get; set; }

    /// <summary>
    /// 联系邮箱
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// 地址
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Logo
    /// </summary>
    public string? Logo { get; set; }

    /// <summary>
    /// 域名
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// 隔离模式
    /// </summary>
    public TenantIsolationMode IsolationMode { get; set; } = TenantIsolationMode.Field;

    /// <summary>
    /// 数据库类型
    /// </summary>
    public TenantDatabaseType? DatabaseType { get; set; }

    /// <summary>
    /// 数据库服务器地址
    /// </summary>
    public string? DatabaseHost { get; set; }

    /// <summary>
    /// 数据库端口
    /// </summary>
    public int? DatabasePort { get; set; }

    /// <summary>
    /// 数据库名称
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// 数据库Schema
    /// </summary>
    public string? DatabaseSchema { get; set; }

    /// <summary>
    /// 数据库用户名
    /// </summary>
    public string? DatabaseUser { get; set; }

    /// <summary>
    /// 数据库密码
    /// </summary>
    public string? DatabasePassword { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset? ExpireTime { get; set; }

    /// <summary>
    /// 用户数限制
    /// </summary>
    public int? UserLimit { get; set; }

    /// <summary>
    /// 存储空间限制(MB)
    /// </summary>
    public XiHanBasicAppIdType? StorageLimit { get; set; }

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
/// 更新租户 DTO
/// </summary>
public class UpdateTenantDto : RbacUpdateDtoBase
{
    /// <summary>
    /// 租户名称
    /// </summary>
    public string? TenantName { get; set; }

    /// <summary>
    /// 租户简称
    /// </summary>
    public string? TenantShortName { get; set; }

    /// <summary>
    /// 联系人
    /// </summary>
    public string? ContactPerson { get; set; }

    /// <summary>
    /// 联系电话
    /// </summary>
    public string? ContactPhone { get; set; }

    /// <summary>
    /// 联系邮箱
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// 地址
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Logo
    /// </summary>
    public string? Logo { get; set; }

    /// <summary>
    /// 域名
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset? ExpireTime { get; set; }

    /// <summary>
    /// 用户数限制
    /// </summary>
    public int? UserLimit { get; set; }

    /// <summary>
    /// 存储空间限制(MB)
    /// </summary>
    public XiHanBasicAppIdType? StorageLimit { get; set; }

    /// <summary>
    /// 租户状态
    /// </summary>
    public TenantStatus? TenantStatus { get; set; }

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

/// <summary>
/// 配置租户数据库 DTO
/// </summary>
public class ConfigureTenantDatabaseDto
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public XiHanBasicAppIdType TenantId { get; set; }

    /// <summary>
    /// 数据库类型
    /// </summary>
    public TenantDatabaseType DatabaseType { get; set; }

    /// <summary>
    /// 数据库服务器地址
    /// </summary>
    public string DatabaseHost { get; set; } = string.Empty;

    /// <summary>
    /// 数据库端口
    /// </summary>
    public int DatabasePort { get; set; }

    /// <summary>
    /// 数据库名称
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// 数据库Schema
    /// </summary>
    public string? DatabaseSchema { get; set; }

    /// <summary>
    /// 数据库用户名
    /// </summary>
    public string DatabaseUser { get; set; } = string.Empty;

    /// <summary>
    /// 数据库密码
    /// </summary>
    public string DatabasePassword { get; set; } = string.Empty;
}
