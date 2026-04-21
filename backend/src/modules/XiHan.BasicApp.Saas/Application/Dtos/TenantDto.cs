#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantDto
// Guid:70bfef33-a72d-4b12-b98f-78b0683da17b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:43:44
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 租户 DTO
/// </summary>
public class TenantDto : BasicAppDto
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
    /// 版本/套餐ID
    /// </summary>
    public long? EditionId { get; set; }

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
    /// 数据库 Schema
    /// </summary>
    public string? DatabaseSchema { get; set; }

    /// <summary>
    /// 配置状态
    /// </summary>
    public TenantConfigStatus ConfigStatus { get; set; } = TenantConfigStatus.Pending;

    /// <summary>
    /// 租户状态
    /// </summary>
    public TenantStatus TenantStatus { get; set; } = TenantStatus.Normal;

    /// <summary>
    /// 用户上限
    /// </summary>
    public int? UserLimit { get; set; }

    /// <summary>
    /// 存储上限(MB)
    /// </summary>
    public long? StorageLimit { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset? ExpireTime { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}

/// <summary>
/// 创建租户 DTO
/// </summary>
public class TenantCreateDto : BasicAppCDto
{
    /// <summary>
    /// 租户编码
    /// </summary>
    [Required(ErrorMessage = "租户编码不能为空")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "租户编码长度必须在 1～50 之间")]
    public string TenantCode { get; set; } = string.Empty;

    /// <summary>
    /// 租户名称
    /// </summary>
    [Required(ErrorMessage = "租户名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "租户名称长度必须在 1～100 之间")]
    public string TenantName { get; set; } = string.Empty;

    /// <summary>
    /// 版本/套餐ID
    /// </summary>
    public long? EditionId { get; set; }

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
    /// 数据库 Schema
    /// </summary>
    public string? DatabaseSchema { get; set; }

    /// <summary>
    /// 配置状态
    /// </summary>
    public TenantConfigStatus ConfigStatus { get; set; } = TenantConfigStatus.Pending;

    /// <summary>
    /// 用户上限
    /// </summary>
    public int? UserLimit { get; set; }

    /// <summary>
    /// 存储上限(MB)
    /// </summary>
    public long? StorageLimit { get; set; }

    /// <summary>
    /// 租户状态
    /// </summary>
    public TenantStatus TenantStatus { get; set; } = TenantStatus.Normal;

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset? ExpireTime { get; set; }

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
/// 更新租户 DTO
/// </summary>
public class TenantUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 版本/套餐ID
    /// </summary>
    public long? EditionId { get; set; }

    /// <summary>
    /// 租户名称
    /// </summary>
    [Required(ErrorMessage = "租户名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "租户名称长度必须在 1～100 之间")]
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
    /// 数据库类型
    /// </summary>
    public TenantDatabaseType? DatabaseType { get; set; }

    /// <summary>
    /// 数据库 Schema
    /// </summary>
    public string? DatabaseSchema { get; set; }

    /// <summary>
    /// 配置状态
    /// </summary>
    public TenantConfigStatus ConfigStatus { get; set; } = TenantConfigStatus.Pending;

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset? ExpireTime { get; set; }

    /// <summary>
    /// 租户状态
    /// </summary>
    public TenantStatus TenantStatus { get; set; } = TenantStatus.Normal;

    /// <summary>
    /// 用户上限
    /// </summary>
    public int? UserLimit { get; set; }

    /// <summary>
    /// 存储上限(MB)
    /// </summary>
    public long? StorageLimit { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
