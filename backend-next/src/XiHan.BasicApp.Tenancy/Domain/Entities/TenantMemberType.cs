// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Tenancy.Domain.Entities;

/// <summary>
/// 租户成员的类型。
/// </summary>
/// <remarks>
/// 所有者可转移所有权与管理成员（随后续批次），普通成员只是能进入该租户。
/// 权限仍由 Access 的角色决定，成员类型不带任何权限。
/// </remarks>
public enum TenantMemberType
{
    /// <summary>
    /// 普通成员。
    /// </summary>
    Member = 0,

    /// <summary>
    /// 所有者。
    /// </summary>
    Owner = 1,
}
