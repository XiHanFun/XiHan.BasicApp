// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// 授权申请候选的授权策略
/// </summary>
/// <remarks>
/// 持有 <c>access.permission-requests.create</c> 或 <c>access.permission-requests.update</c> 其一即可：
/// 申请人不必能读角色列表与权限目录。判定写法照框架的权限策略：匿名先按未认证拒绝，检查器失败原样返回、不当作没有权限
/// </remarks>
/// <param name="permissions">权限判定</param>
public sealed class PermissionRequestCandidatesPolicy(IPermissionChecker permissions) : IPolicy
{
    private static readonly string[] _anyOf = [AccessPermissions.PermissionRequestsCreate, AccessPermissions.PermissionRequestsUpdate];

    /// <inheritdoc />
    public string Name => AccessAuthorizationPolicies.PermissionRequestCandidates;

    /// <inheritdoc />
    public async ValueTask<Result<AuthorizationResult>> EvaluateAsync(AuthorizationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.Security.Principal.IsAuthenticated)
        {
            return AuthorizationResult.Deny(Error.Denied(SecurityErrors.NotAuthenticated, SecurityMessages.AuthenticationRequired));
        }

        foreach (var permission in _anyOf)
        {
            var held = await permissions.HasPermissionAsync(request.Security, permission, cancellationToken).ConfigureAwait(false);
            if (!held.TryGetValue(out var granted))
            {
                return held.Error!;
            }

            if (granted)
            {
                return AuthorizationResult.Allow();
            }
        }

        return AuthorizationResult.Deny(Error.Denied(SecurityErrors.PermissionMissing, SecurityMessages.PermissionMissing, MessageArguments.Of(("permission", AccessPermissions.PermissionRequestsCreate))));
    }
}
