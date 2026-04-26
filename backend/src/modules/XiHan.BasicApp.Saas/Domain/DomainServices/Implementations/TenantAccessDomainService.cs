#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantAccessDomainService
// Guid:cb351a42-7ffc-4614-903a-0605dce6e17b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.ValueObjects;

namespace XiHan.BasicApp.Saas.Domain.DomainServices.Implementations;

/// <summary>
/// 租户访问领域服务
/// </summary>
public sealed class TenantAccessDomainService : ITenantAccessDomainService
{
    /// <inheritdoc />
    public bool CanAccess(TenantMemberSnapshot member, DateTimeOffset now)
    {
        return member.InviteStatus == TenantMemberInviteStatus.Accepted
               && member.Status == ValidityStatus.Valid
               && member.Period.IsActive(now);
    }

    /// <inheritdoc />
    public bool IsPlatformAdmin(TenantMemberSnapshot member, DateTimeOffset now)
    {
        return CanAccess(member, now) && member.MemberType == TenantMemberType.PlatformAdmin;
    }
}
