// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Tenancy.Domain.Entities;

/// <summary>
/// 租户状态。
/// </summary>
/// <remarks>
/// 与框架租户目录的 <c>IsEnabled</c> 同步（Active ⇔ 启用）。
/// </remarks>
public enum TenantStatus
{
    /// <summary>
    /// 生效：租户用户可登录。
    /// </summary>
    Active = 0,

    /// <summary>
    /// 停用：目录标记为禁用，租户用户不能登录、带该租户声明的调用被拒。
    /// </summary>
    Suspended = 1,
}
