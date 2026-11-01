// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Printing.Infrastructure;

/// <summary>
/// 数据源目录的授权策略。
/// </summary>
/// <remarks>
/// 持有 <c>printing.print-templates.read</c> 或 <c>printing.print-templates.use</c> 其一即可（4.x 的数据源目录在方法里同样判定）：
/// 目录既服务模板管理也服务业务打印，不另造权限码。判定写法照框架的权限策略：匿名先按未认证拒绝，检查器失败原样返回、不当作没有权限。
/// </remarks>
/// <param name="permissions">权限判定。</param>
public sealed class PrintDataSourceCatalogPolicy(IPermissionChecker permissions) : IPolicy
{
    private static readonly string[] _anyOf = [PrintingPermissions.PrintTemplatesRead, PrintingPermissions.PrintTemplatesUse];

    /// <inheritdoc />
    public string Name => PrintingPolicies.DataSourceCatalog;

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

        return AuthorizationResult.Deny(Error.Denied(SecurityErrors.PermissionMissing, SecurityMessages.PermissionMissing, MessageArguments.Of(("permission", PrintingPermissions.PrintTemplatesRead))));
    }
}
