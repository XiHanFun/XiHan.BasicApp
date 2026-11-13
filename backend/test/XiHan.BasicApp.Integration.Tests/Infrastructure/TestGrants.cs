// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Integration.Tests.Infrastructure;

/// <summary>
/// 测试直接写进授予表的授予
/// </summary>
/// <remarks>
/// 不带租户的授予只对平台调用生效，<see cref="PermissionGrant.AnyTenant"/> 只对租户调用生效（框架 ADR-0197）；
/// 测试令牌的角色名是字符串，平台令牌与租户令牌共用同一个角色名时两侧都要授予
/// </remarks>
public static class TestGrants
{
    /// <summary>
    /// 每个租户的授予
    /// </summary>
    public static PermissionGrant AnyTenant(GrantSubject subject, string permission) => new(subject, permission, PermissionGrant.AnyTenant);

    /// <summary>
    /// 平台与每个租户各一条
    /// </summary>
    public static PermissionGrant[] BothSides(GrantSubject subject, string permission) => [new(subject, permission), AnyTenant(subject, permission)];
}
