// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 租户创建 DTO
/// </summary>
public sealed class TenantCreateDto : BasicAppCDto
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
    /// Logo
    /// </summary>
    public string? Logo { get; set; }

    /// <summary>
    /// 域名
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// 版本/套餐主键
    /// </summary>
    public long? EditionId { get; set; }

    /// <summary>
    /// 租户管理员用户名（与 AdminEmail/AdminPassword 同时提供时，开通后自动创建管理员 + Owner 角色 + 按版本授权）
    /// </summary>
    public string? AdminUserName { get; set; }

    /// <summary>
    /// 租户管理员邮箱（登录身份标识，全平台唯一；开通管理员时必填）
    /// </summary>
    public string? AdminEmail { get; set; }

    /// <summary>
    /// 租户管理员初始密码（与 AdminUserName/AdminEmail 同时提供时生效）
    /// </summary>
    public string? AdminPassword { get; set; }

    /// <summary>
    /// 隔离模式
    /// </summary>
    public TenantIsolationMode IsolationMode { get; set; } = TenantIsolationMode.Field;

    /// <summary>
    /// 数据库类型（隔离模式为 Database 时必填）
    /// </summary>
    public TenantDatabaseType? DatabaseType { get; set; }

    /// <summary>
    /// 数据库连接字符串（隔离模式为 Database 时必填；加密落库、绝不回显）
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// 用户数限制
    /// </summary>
    public int? UserLimit { get; set; }

    /// <summary>
    /// 存储空间限制(MB)
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
