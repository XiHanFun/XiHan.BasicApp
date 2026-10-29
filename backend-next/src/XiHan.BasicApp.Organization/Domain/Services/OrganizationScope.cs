// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Organization.Domain.Services;

/// <summary>
/// 组织只在租户侧：取当前租户 ID。
/// </summary>
/// <remarks>
/// 没有租户上下文即拒绝（<c>ORGANIZATION_DENIED_001</c>）。权限码本身也只在租户侧，这里是第二道闸。
/// </remarks>
public static class OrganizationScope
{
    /// <summary>
    /// 当前租户 ID，或拒绝。
    /// </summary>
    public static Result<string> RequireTenant(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.GetSecurity().Tenant?.Id is { } tenantId
            ? tenantId
            : Error.Denied(OrganizationErrors.TenantOnly, OrganizationMessages.TenantOnly);
    }
}
