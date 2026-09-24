// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 库隔离租户初始化管理员 DTO
/// </summary>
public sealed class TenantAdminInitializeDto
{
    /// <summary>
    /// 租户主键
    /// </summary>
    public long TenantId { get; set; }

    /// <summary>
    /// 租户管理员用户名（租户内唯一）
    /// </summary>
    public string AdminUserName { get; set; } = string.Empty;

    /// <summary>
    /// 租户管理员邮箱（登录身份标识，全平台唯一）
    /// </summary>
    public string AdminEmail { get; set; } = string.Empty;

    /// <summary>
    /// 租户管理员初始密码（须满足密码策略）
    /// </summary>
    public string AdminPassword { get; set; } = string.Empty;
}
