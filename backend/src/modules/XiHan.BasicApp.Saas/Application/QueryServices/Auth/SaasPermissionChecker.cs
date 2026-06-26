#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasPermissionChecker
// Guid:2e1b8c44-5a9d-4f3b-9c7a-1d6e3f8a0b21
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/08 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authorization.Permissions;
using XiHan.Framework.Security.Claims;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 基于授权快照的权限检查器（请求期实时鉴权，替换框架内存版 DefaultPermissionChecker）
/// </summary>
/// <remarks>
/// 框架默认的 <c>DefaultPermissionChecker</c> 走内存版 <c>IPermissionStore/IRoleStore</c>，对真实用户恒返回 false，
/// 导致请求期 <c>IsGrantedAsync</c> 失效、鉴权只能依赖登录时写入 JWT 的权限声明 —— 授权变更须重新登录才生效。
/// 本实现改为读取按用户缓存、且在授权写路径失效的 <see cref="AuthorizationSnapshot"/>，使授权变更无需重新登录即可生效；
/// 并在鉴权前校验「当前请求所属会话」是否仍有效（登出/强制下线/禁用→吊销会话 后即时拒绝）。
/// </remarks>
public sealed class SaasPermissionChecker : IPermissionChecker
{
    private const string WildcardPermission = "*";

    private readonly IAuthorizationSnapshotQueryService _authorizationSnapshotQueryService;

    private readonly IUserSessionRepository _userSessionRepository;

    private readonly ICurrentUser _currentUser;

    // 请求级（Scoped 实例生命周期=单次请求）记忆化：一个请求内多次鉴权只构建/取一次快照、只校验一次会话
    private readonly Dictionary<long, AuthorizationSnapshot> _requestSnapshots = [];

    private bool? _currentSessionValid;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SaasPermissionChecker(
        IAuthorizationSnapshotQueryService authorizationSnapshotQueryService,
        IUserSessionRepository userSessionRepository,
        ICurrentUser currentUser)
    {
        _authorizationSnapshotQueryService = authorizationSnapshotQueryService;
        _userSessionRepository = userSessionRepository;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<bool> IsGrantedAsync(string userId, string permissionName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(permissionName) || !TryResolveUserId(userId, out var id))
        {
            return false;
        }

        if (!await IsCurrentSessionValidAsync(cancellationToken))
        {
            return false;
        }

        var snapshot = await BuildSnapshotAsync(id, cancellationToken);
        return HasPermission(snapshot, permissionName);
    }

    /// <inheritdoc />
    public async Task<bool> IsAnyGrantedAsync(string userId, List<string> permissionNames, CancellationToken cancellationToken = default)
    {
        if (permissionNames is not { Count: > 0 } || !TryResolveUserId(userId, out var id))
        {
            return false;
        }

        if (!await IsCurrentSessionValidAsync(cancellationToken))
        {
            return false;
        }

        var snapshot = await BuildSnapshotAsync(id, cancellationToken);
        return permissionNames.Any(name => HasPermission(snapshot, name));
    }

    /// <inheritdoc />
    public async Task<bool> IsAllGrantedAsync(string userId, List<string> permissionNames, CancellationToken cancellationToken = default)
    {
        if (permissionNames is not { Count: > 0 } || !TryResolveUserId(userId, out var id))
        {
            return false;
        }

        if (!await IsCurrentSessionValidAsync(cancellationToken))
        {
            return false;
        }

        var snapshot = await BuildSnapshotAsync(id, cancellationToken);
        return permissionNames.All(name => HasPermission(snapshot, name));
    }

    /// <inheritdoc />
    public async Task<List<string>> GetGrantedPermissionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (!TryResolveUserId(userId, out var id) || !await IsCurrentSessionValidAsync(cancellationToken))
        {
            return [];
        }

        var snapshot = await BuildSnapshotAsync(id, cancellationToken);
        return [.. snapshot.Permissions];
    }

    /// <inheritdoc />
    public Task<bool> PermissionExistsAsync(string permissionName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(!string.IsNullOrWhiteSpace(permissionName));
    }

    private static bool HasPermission(AuthorizationSnapshot snapshot, string permissionName)
    {
        return snapshot.Permissions.Contains(WildcardPermission, StringComparer.OrdinalIgnoreCase)
            || snapshot.Permissions.Contains(permissionName.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    private static bool TryResolveUserId(string? userId, out long id)
    {
        id = 0;
        return !string.IsNullOrWhiteSpace(userId) && long.TryParse(userId, out id) && id > 0;
    }

    private async Task<AuthorizationSnapshot> BuildSnapshotAsync(long userId, CancellationToken cancellationToken)
    {
        if (_requestSnapshots.TryGetValue(userId, out var cached))
        {
            return cached;
        }

        var snapshot = await _authorizationSnapshotQueryService.BuildAsync(userId, DateTimeOffset.UtcNow, cancellationToken);
        _requestSnapshots[userId] = snapshot;
        return snapshot;
    }

    /// <summary>
    /// 校验当前请求所属会话是否仍有效（请求级记忆化）。
    /// 失败安全：仅当「按会话标识确实查到该会话且其非活跃（已登出/下线/过期）」时才拒绝；
    /// 查不到 / 异常 / 无会话标识 一律放行，避免误锁。禁用用户走「吊销会话」链路，亦命中此处。
    /// </summary>
    private async Task<bool> IsCurrentSessionValidAsync(CancellationToken cancellationToken)
    {
        if (_currentSessionValid.HasValue)
        {
            return _currentSessionValid.Value;
        }

        _currentSessionValid = await ResolveCurrentSessionValidAsync(cancellationToken);
        return _currentSessionValid.Value;
    }

    private async Task<bool> ResolveCurrentSessionValidAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var sessionId = _currentUser.FindClaim(XiHanClaimTypes.SessionId)?.Value;
        // 无会话标识（系统/后台上下文、或非会话型令牌）：不拦截
        if (userId is not > 0 || string.IsNullOrWhiteSpace(sessionId))
        {
            return true;
        }

        try
        {
            var session = await _userSessionRepository.GetByUserSessionIdAsync(sessionId, cancellationToken);
            // 查不到会话：放行（避免历史令牌/边界情况误锁）
            if (session is null)
            {
                return true;
            }

            // 查到会话：仅当属于当前用户且非活跃时拒绝（已登出/强制下线/过期/禁用吊销）
            return session.UserId != userId || session.Status == SessionStatus.Active;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // 会话校验异常（瞬时故障等）放行，避免误锁全员
            return true;
        }
    }
}
