// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Authorization.Permissions;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 按实际发生的操作校验权限的守卫实现：与 <c>[PermissionAuthorize]</c> 同走 <see cref="IPermissionChecker"/>（授权快照），口径一致。
/// </summary>
public sealed class OperationPermissionGuard : IOperationPermissionGuard
{
    private static readonly IReadOnlyDictionary<string, string> PermissionNames = SaasPermissionDefinitions.All
        .ToDictionary(definition => definition.PermissionCode, definition => definition.PermissionName, StringComparer.OrdinalIgnoreCase);

    private readonly ICurrentUser _currentUser;

    private readonly IPermissionChecker _permissionChecker;

    /// <summary>
    /// 构造函数
    /// </summary>
    public OperationPermissionGuard(ICurrentUser currentUser, IPermissionChecker permissionChecker)
    {
        _currentUser = currentUser;
        _permissionChecker = permissionChecker;
    }

    /// <inheritdoc />
    public async Task EnsureGrantedAsync(string permissionCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);

        var userId = _currentUser.UserId ?? throw new UserFriendlyException("当前用户未登录。");
        if (!await _permissionChecker.IsGrantedAsync(userId.ToString(), permissionCode, cancellationToken))
        {
            var name = PermissionNames.TryGetValue(permissionCode, out var permissionName) ? permissionName : permissionCode;
            throw new UserFriendlyException($"缺少「{name}」权限。");
        }
    }
}
