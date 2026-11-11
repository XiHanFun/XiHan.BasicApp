// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Access;

/// <summary>
/// Access 登记的授权策略名
/// </summary>
public static class AccessAuthorizationPolicies
{
    /// <summary>
    /// 取授权申请的候选
    /// </summary>
    /// <remarks>
    /// 持有提交或重新提交授权申请的权限其一即可：两条路径用同一个申请弹窗，不另造权限码，由 <c>Infrastructure/PermissionRequestCandidatesPolicy</c> 实现
    /// </remarks>
    public const string PermissionRequestCandidates = "access.permission-request-candidates";
}
