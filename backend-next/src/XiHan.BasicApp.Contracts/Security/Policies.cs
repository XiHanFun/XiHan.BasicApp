// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Security;

/// <summary>
/// 操作可引用的策略名。
/// </summary>
/// <remarks>
/// 自助操作用 <see cref="Authenticated"/>（框架内置），不写权限码；匿名操作既不写策略也不写权限码，并登记在架构测试的白名单里。
/// </remarks>
public static class Policies
{
    /// <summary>
    /// 只要求已认证（框架 <c>AuthenticatedPolicy</c>）。
    /// </summary>
    public const string Authenticated = "authenticated";
}
