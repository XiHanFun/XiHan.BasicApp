// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Queries;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Profile;

/// <summary>
/// 本人的一台登录设备（一个有效会话）。
/// </summary>
/// <param name="Id">会话 ID。</param>
/// <param name="Device">设备类型：Desktop、Mobile、Tablet、Bot；认不出为空。</param>
/// <param name="Browser">浏览器。</param>
/// <param name="Os">操作系统。</param>
/// <param name="IpAddress">登录 IP（本人的设备不遮掩）。</param>
/// <param name="Location">登录地点。</param>
/// <param name="LoginAt">登录时间（会话签发）。</param>
/// <param name="LastActiveAt">最后活动时间（最近一次续签，没续签过即登录时间）。</param>
/// <param name="IsCurrent">是否当前这个会话（不能在这里下线，用退出登录）。</param>
public sealed record MySessionResponse(Guid Id, string? Device, string? Browser, string? Os, string? IpAddress, string? Location, DateTimeOffset LoginAt, DateTimeOffset LastActiveAt, bool IsCurrent);

/// <summary>
/// 本人的登录设备：当前会话在最前，其余按最后活动倒序。
/// </summary>
/// <param name="Items">设备。</param>
public sealed record MySessionsResponse(IReadOnlyList<MySessionResponse> Items);

/// <summary>
/// <c>profile.revoke-session</c> 的请求。
/// </summary>
/// <param name="SessionId">要下线的会话。</param>
public sealed record RevokeMySessionRequest(Guid SessionId);

/// <summary>
/// 本人的有效会话（个人中心「登录设备」）：只按本人，不分平台与租户一侧、不套数据范围；4.x 同样列出别人模仿本人开的会话（会话的用户是本人）。
/// </summary>
internal static class MySessions
{
    /// <summary>
    /// 本人未吊销、未过期的会话。
    /// </summary>
    public static async Task<List<SysUserSession>> ActiveAsync(IRepository<SysUserSession, Guid> sessions, TimeProvider clock, Guid userId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        return [.. await sessions.ListAsync(EntityQuery.All<SysUserSession>().Where(new Specification<SysUserSession>(s => s.UserId == userId && !s.RevokedAt.HasValue && s.RefreshExpiresAt > now)), cancellationToken).ConfigureAwait(false)];
    }

    /// <summary>
    /// 本人发起、还没结束的模仿会话（会话的用户是被模仿的人）。
    /// </summary>
    public static async Task<List<SysUserSession>> ImpersonatingAsync(IRepository<SysUserSession, Guid> sessions, TimeProvider clock, Guid userId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        return [.. await sessions.ListAsync(EntityQuery.All<SysUserSession>().Where(new Specification<SysUserSession>(s => s.ImpersonatorUserId == userId && !s.RevokedAt.HasValue && s.RefreshExpiresAt > now)), cancellationToken).ConfigureAwait(false)];
    }

    /// <summary>
    /// 投影成设备列表（当前会话在最前）。
    /// </summary>
    public static MySessionsResponse Project(IEnumerable<SysUserSession> rows, Guid? current)
        => new([.. rows
            .OrderByDescending(s => s.Id == current)
            .ThenByDescending(static s => s.LastSeenAt ?? s.IssuedAt)
            .Select(s => new MySessionResponse(s.Id, s.Device, s.Browser, s.Os, s.IpAddress, s.Location, s.IssuedAt, s.LastSeenAt ?? s.IssuedAt, s.Id == current))]);
}

/// <summary>
/// 本人的登录设备：<c>GET /api/profile:sessions</c>（4.x <c>Profile/Sessions</c>）。已登录即可；模仿会话里看到的是被模仿的人的设备（4.x 同）。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="sessions">会话仓储。</param>
/// <param name="clock">时钟。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class ListMySessions(IRepository<SysUser, Guid> users, UserAccountService accounts, IRepository<SysUserSession, Guid> sessions, TimeProvider clock) : IOperation<ProfileRequest, MySessionsResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "profile.sessions";

    /// <inheritdoc />
    public async ValueTask<Result<MySessionsResponse>> HandleAsync(ProfileRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: false).ConfigureAwait(false);
        if (!subject.TryGetValue(out var found))
        {
            return subject.Error!;
        }

        var rows = await MySessions.ActiveAsync(sessions, clock, found.User.Id, context.CancellationToken).ConfigureAwait(false);
        return MySessions.Project(rows, SessionQueries.CurrentSessionOf(context));
    }
}

/// <summary>
/// 下线本人的一台设备：<c>POST /api/profile:revoke-session</c>（4.x <c>Profile/RevokeSession</c>）。不是本人的有效会话即 <c>IDENTITY_NOTFOUND_002</c>，
/// 当前会话即 <c>IDENTITY_CONFLICT_005</c>（用退出登录）；提交后经实时连接让那台设备退出登录。返回剩下的设备。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务（吊销与推送）。</param>
/// <param name="sessions">会话仓储。</param>
/// <param name="clock">时钟。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class RevokeMySession(IRepository<SysUser, Guid> users, UserAccountService accounts, IRepository<SysUserSession, Guid> sessions, TimeProvider clock) : IOperation<RevokeMySessionRequest, MySessionsResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "profile.revoke-session";

    /// <inheritdoc />
    public async ValueTask<Result<MySessionsResponse>> HandleAsync(RevokeMySessionRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: false).ConfigureAwait(false);
        if (!subject.TryGetValue(out var found))
        {
            return subject.Error!;
        }

        var current = SessionQueries.CurrentSessionOf(context);
        var rows = await MySessions.ActiveAsync(sessions, clock, found.User.Id, context.CancellationToken).ConfigureAwait(false);
        var target = rows.Find(s => s.Id == request.SessionId);
        if (target is null)
        {
            return Error.NotFound(IdentityErrors.SessionNotFound, IdentityMessages.SessionNotFound);
        }

        if (target.Id == current)
        {
            return Error.Conflict(IdentityErrors.CurrentSessionNotRevocable, IdentityMessages.CurrentSessionNotRevocable);
        }

        var revoked = await accounts.RevokeAsync([target], "self-revoked", null, context.CancellationToken).ConfigureAwait(false);
        return revoked.IsFailure ? revoked.Error! : MySessions.Project(rows.Where(s => s.Id != target.Id), current);
    }
}

/// <summary>
/// 下线本人的其他全部设备：<c>POST /api/profile:revoke-other-sessions</c>（4.x <c>Profile/RevokeOtherSessions</c>）。除当前会话外本人的全部有效会话，
/// 连同本人发起、还没结束的模仿会话（4.x 同）；提交后经实时连接让它们退出登录。返回剩下的设备（只剩当前会话）。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务（吊销与推送）。</param>
/// <param name="sessions">会话仓储。</param>
/// <param name="clock">时钟。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class RevokeMyOtherSessions(IRepository<SysUser, Guid> users, UserAccountService accounts, IRepository<SysUserSession, Guid> sessions, TimeProvider clock) : IOperation<ProfileRequest, MySessionsResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "profile.revoke-other-sessions";

    /// <inheritdoc />
    public async ValueTask<Result<MySessionsResponse>> HandleAsync(ProfileRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: false).ConfigureAwait(false);
        if (!subject.TryGetValue(out var found))
        {
            return subject.Error!;
        }

        var current = SessionQueries.CurrentSessionOf(context);
        var rows = await MySessions.ActiveAsync(sessions, clock, found.User.Id, context.CancellationToken).ConfigureAwait(false);
        var started = await MySessions.ImpersonatingAsync(sessions, clock, found.User.Id, context.CancellationToken).ConfigureAwait(false);
        var targets = rows.Concat(started).Where(s => s.Id != current).DistinctBy(static s => s.Id).ToList();
        if (targets.Count > 0)
        {
            var revoked = await accounts.RevokeAsync(targets, "self-revoked", null, context.CancellationToken).ConfigureAwait(false);
            if (revoked.IsFailure)
            {
                return revoked.Error!;
            }
        }

        return MySessions.Project(rows.Where(s => s.Id == current), current);
    }
}
