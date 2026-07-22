// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.ValueObjects;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 租户访问领域服务
/// </summary>
public interface ITenantAccessDomainService
{
    /// <summary>
    /// 判断成员是否可进入租户
    /// </summary>
    /// <param name="member">成员快照</param>
    /// <param name="now">当前时间</param>
    /// <returns>是否可访问</returns>
    bool CanAccess(TenantMemberSnapshot member, DateTimeOffset now);

    /// <summary>
    /// 判断成员是否为平台管理员身份
    /// </summary>
    /// <param name="member">成员快照</param>
    /// <param name="now">当前时间</param>
    /// <returns>是否平台管理员</returns>
    bool IsPlatformAdmin(TenantMemberSnapshot member, DateTimeOffset now);
}
