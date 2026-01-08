#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTenantDto
// Guid:b8c9d0e1-f2a3-4567-8901-234567890bcd
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Services.Dtos;

/// <summary>
/// 系统租户创建 DTO
/// </summary>
public class SysTenantCreateDto : RbacCreationDtoBase
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
    /// 数据库连接字符串
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// 配置状态
    /// </summary>
    public TenantConfigStatus ConfigStatus { get; set; } = TenantConfigStatus.Pending;

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
    public long? StorageLimit { get; set; }

    /// <summary>
    /// 租户状态
    /// </summary>
    public TenantStatus TenantStatus { get; set; } = TenantStatus.Normal;

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
/// 系统租户更新 DTO
/// </summary>
public class SysTenantUpdateDto : RbacUpdateDtoBase
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
    /// 数据库连接字符串
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// 配置状态
    /// </summary>
    public TenantConfigStatus ConfigStatus { get; set; } = TenantConfigStatus.Pending;

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
    public long? StorageLimit { get; set; }

    /// <summary>
    /// 租户状态
    /// </summary>
    public TenantStatus TenantStatus { get; set; } = TenantStatus.Normal;

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
/// 系统租户查询 DTO
/// </summary>
public class SysTenantGetDto : RbacFullAuditedDtoBase
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
    /// 数据库连接字符串
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// 配置状态
    /// </summary>
    public TenantConfigStatus ConfigStatus { get; set; } = TenantConfigStatus.Pending;

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
    public long? StorageLimit { get; set; }

    /// <summary>
    /// 租户状态
    /// </summary>
    public TenantStatus TenantStatus { get; set; } = TenantStatus.Normal;

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
