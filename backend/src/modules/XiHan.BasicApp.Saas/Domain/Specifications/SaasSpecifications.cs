#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasSpecifications
// Guid:7f05f42e-f7cc-4c19-88cf-77e052b88be2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Linq.Expressions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Domain.Specifications;

namespace XiHan.BasicApp.Saas.Domain.Specifications;

/// <summary>
/// 启用用户规约
/// </summary>
public sealed class ActiveUserSpecification : Specification<SysUser>
{
    /// <inheritdoc />
    public override Expression<Func<SysUser, bool>> ToExpression()
    {
        return user => !user.IsDeleted && user.Status == EnableStatus.Enabled;
    }
}

/// <summary>
/// 可用租户规约
/// </summary>
public sealed class AvailableTenantSpecification(DateTimeOffset now) : Specification<SysTenant>
{
    /// <inheritdoc />
    public override Expression<Func<SysTenant, bool>> ToExpression()
    {
        return tenant => !tenant.IsDeleted
                         && tenant.TenantStatus == TenantStatus.Normal
                         && (!tenant.ExpireTime.HasValue || tenant.ExpireTime.Value > now);
    }
}

/// <summary>
/// 有效租户成员规约
/// </summary>
public sealed class ActiveTenantUserSpecification(DateTimeOffset now) : Specification<SysTenantUser>
{
    /// <inheritdoc />
    public override Expression<Func<SysTenantUser, bool>> ToExpression()
    {
        return member => !member.IsDeleted
                         && member.InviteStatus == TenantMemberInviteStatus.Accepted
                         && member.Status == ValidityStatus.Valid
                         && (!member.EffectiveTime.HasValue || member.EffectiveTime.Value <= now)
                         && (!member.ExpirationTime.HasValue || member.ExpirationTime.Value > now);
    }
}

/// <summary>
/// 启用角色规约
/// </summary>
public sealed class EnabledRoleSpecification : Specification<SysRole>
{
    /// <inheritdoc />
    public override Expression<Func<SysRole, bool>> ToExpression()
    {
        return role => !role.IsDeleted && role.Status == EnableStatus.Enabled;
    }
}

/// <summary>
/// 启用权限规约
/// </summary>
public sealed class EnabledPermissionSpecification : Specification<SysPermission>
{
    /// <inheritdoc />
    public override Expression<Func<SysPermission, bool>> ToExpression()
    {
        return permission => !permission.IsDeleted && permission.Status == EnableStatus.Enabled;
    }
}

/// <summary>
/// 有效用户角色规约
/// </summary>
public sealed class ValidUserRoleSpecification(DateTimeOffset now) : Specification<SysUserRole>
{
    /// <inheritdoc />
    public override Expression<Func<SysUserRole, bool>> ToExpression()
    {
        return userRole => userRole.Status == ValidityStatus.Valid
                           && (!userRole.EffectiveTime.HasValue || userRole.EffectiveTime.Value <= now)
                           && (!userRole.ExpirationTime.HasValue || userRole.ExpirationTime.Value > now);
    }
}

/// <summary>
/// 有效角色权限规约
/// </summary>
public sealed class ValidRolePermissionSpecification(DateTimeOffset now) : Specification<SysRolePermission>
{
    /// <inheritdoc />
    public override Expression<Func<SysRolePermission, bool>> ToExpression()
    {
        return rolePermission => rolePermission.Status == ValidityStatus.Valid
                                 && (!rolePermission.EffectiveTime.HasValue || rolePermission.EffectiveTime.Value <= now)
                                 && (!rolePermission.ExpirationTime.HasValue || rolePermission.ExpirationTime.Value > now);
    }
}

/// <summary>
/// 有效用户权限规约
/// </summary>
public sealed class ValidUserPermissionSpecification(DateTimeOffset now) : Specification<SysUserPermission>
{
    /// <inheritdoc />
    public override Expression<Func<SysUserPermission, bool>> ToExpression()
    {
        return userPermission => userPermission.Status == ValidityStatus.Valid
                                 && (!userPermission.EffectiveTime.HasValue || userPermission.EffectiveTime.Value <= now)
                                 && (!userPermission.ExpirationTime.HasValue || userPermission.ExpirationTime.Value > now);
    }
}
