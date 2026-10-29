// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Net;
using System.Net.Sockets;
using SqlSugar;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Contracts.Realtime;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.BasicApp.Identity.Operations.Sessions;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Queries;

/// <summary>
/// 在线用户（有效会话）的查询。
/// </summary>
/// <remarks>
/// 只看当前一侧（会话当前所在的租户，平台为没有租户的会话），再按数据范围收窄到看得到的用户；
/// 实时在线取自 <see cref="IRealtimePresence"/>（没装 Realtime 即全部不在线）。
/// </remarks>
/// <param name="unitOfWork">当前调用的工作单元。</param>
/// <param name="presence">在线状态（可能没有）。</param>
/// <param name="clock">时钟。</param>
public sealed class SessionQueries(SqlSugarUnitOfWork unitOfWork, IEnumerable<IRealtimePresence> presence, TimeProvider clock)
{
    private const string FieldDevice = "device";
    private const string FieldBrowser = "browser";
    private const string FieldOs = "os";
    private const string FieldUserId = "userId";
    private const string FieldLoginAt = "loginAt";
    private const string FieldLastActiveAt = "lastActiveAt";

    private readonly IRealtimePresence? _presence = presence.FirstOrDefault();

    /// <summary>
    /// 分页列出有效会话。
    /// </summary>
    /// <remarks>
    /// 关键字匹配用户的登录名与显示名；筛选接受 <c>device / browser / os</c> 的 <c>eq / in</c> 与 <c>userId</c> 的 <c>eq</c>；
    /// 排序接受 <c>loginAt / lastActiveAt</c>，缺省按登录时间倒序。
    /// </remarks>
    public async ValueTask<Result<PageResult<OnlineSessionResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var query = await ActiveAsync(context).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(s => SqlFunc.Subqueryable<SysUser>().Where(u => u.Id == s.UserId && (u.UserName.Contains(keyword) || u.DisplayName.Contains(keyword))).Any());
        }

        foreach (var filter in request.Filters)
        {
            var filtered = Apply(query, filter);
            if (!filtered.TryGetValue(out query))
            {
                return filtered.Error!;
            }
        }

        if (request.Sorts.Count == 0)
        {
            query = query.OrderBy(s => s.IssuedAt, OrderByType.Desc);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            var sorted = sort.Field switch
            {
                FieldLoginAt => query.OrderBy(s => s.IssuedAt, direction),
                // 没续签过的会话最后活动即登录时间
                FieldLastActiveAt => query.OrderBy(s => SqlFunc.IsNull(s.LastSeenAt, s.IssuedAt), direction),
                _ => null,
            };
            if (sorted is null)
            {
                return Error.Validation(IdentityErrors.QueryUnsupported, IdentityMessages.SortUnsupported, MessageArguments.Of(("field", sort.Field)));
            }

            query = sorted;
        }

        var total = new RefAsync<int>();
        var rows = await query.ToPageListAsync(request.Page.Index, request.Page.Size, total, context.CancellationToken).ConfigureAwait(false);
        var userIds = rows.Select(static s => s.UserId).Distinct().ToList();
        var users = userIds.Count == 0
            ? []
            : (await unitOfWork.ConnectionFor(typeof(SysUser)).Queryable<SysUser>().Where(u => userIds.Contains(u.Id)).ToListAsync(context.CancellationToken).ConfigureAwait(false)).ToDictionary(static u => u.Id);
        var online = _presence?.ConnectedSessions(rows.Select(static s => s.Id)) ?? new HashSet<Guid>();
        var current = CurrentSessionOf(context);
        var items = rows.Select(s =>
        {
            users.TryGetValue(s.UserId, out var user);
            return new OnlineSessionResponse(
                s.Id,
                s.UserId,
                user?.UserName ?? string.Empty,
                user?.DisplayName ?? string.Empty,
                s.Device,
                s.Browser,
                s.Os,
                Mask(s.IpAddress),
                s.Location,
                s.IssuedAt,
                s.LastSeenAt ?? s.IssuedAt,
                online.Contains(s.Id),
                s.Id == current,
                s.ImpersonatorUserId);
        }).ToList();
        return new PageResult<OnlineSessionResponse>(items, total.Value, request.Page);
    }

    /// <summary>
    /// 在线概况。
    /// </summary>
    public async ValueTask<SessionSummaryResponse> SummaryAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var query = await ActiveAsync(context).ConfigureAwait(false);
        var owners = await query.Select(static s => s.UserId).ToListAsync(context.CancellationToken).ConfigureAwait(false);
        var side = context.GetSecurity().Tenant?.Id ?? TenantIds.Platform;
        return new SessionSummaryResponse(owners.Count, owners.Distinct().Count(), _presence?.ConnectedUsers(side) ?? 0);
    }

    /// <summary>
    /// 取调用方自己当前的会话。
    /// </summary>
    /// <remarks>
    /// 即访问令牌的 <c>sid</c>。
    /// </remarks>
    /// <returns>会话 ID；令牌不带会话即为 <see langword="null"/>。</returns>
    public static Guid? CurrentSessionOf(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Guid.TryParse(context.GetSecurity().Principal.ClaimValues(IdentityClaims.Session).FirstOrDefault(), out var id) ? id : null;
    }

    /// <summary>
    /// 当前一侧、数据范围内的有效会话（未吊销、未过期）。
    /// </summary>
    public async ValueTask<ISugarQueryable<SysUserSession>> ActiveAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var now = clock.GetUtcNow();
        // 可空值类型的判空写 HasValue（见 UserAccountService.RevokeSessionsAsync）
        var query = unitOfWork.ConnectionFor(typeof(SysUserSession)).Queryable<SysUserSession>().Where(s => !s.RevokedAt.HasValue && s.RefreshExpiresAt > now);
        query = context.GetSecurity().Tenant?.Id is { } tenantId
            ? query.Where(s => s.ActiveTenantId == tenantId)
            : query.Where(s => s.ActiveTenantId == null);
        // 数据范围（方案 5.6）：受限主体只看得到自己与可见部门里的用户的会话
        return query.ApplyDataScope(await DataScopeQuery.ResolveAsync(context).ConfigureAwait(false), static s => s.UserId);
    }

    private static Result<ISugarQueryable<SysUserSession>> Apply(ISugarQueryable<SysUserSession> query, PageFilter filter)
    {
        var value = filter.Value ?? string.Empty;
        var values = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        return (filter.Field, filter.Operator) switch
        {
            (FieldDevice, "eq") => Result.Success(query.Where(s => s.Device == value)),
            (FieldDevice, "in") => Result.Success(query.Where(s => values.Contains(s.Device!))),
            (FieldBrowser, "eq") => Result.Success(query.Where(s => s.Browser == value)),
            (FieldBrowser, "in") => Result.Success(query.Where(s => values.Contains(s.Browser!))),
            (FieldOs, "eq") => Result.Success(query.Where(s => s.Os == value)),
            (FieldOs, "in") => Result.Success(query.Where(s => values.Contains(s.Os!))),
            (FieldUserId, "eq") when Guid.TryParse(value, out var userId) => Result.Success(query.Where(s => s.UserId == userId)),
            _ => Error.Validation(IdentityErrors.QueryUnsupported, IdentityMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator))),
        };
    }

    private static string? Mask(string? address)
    {
        if (!IPAddress.TryParse(address, out var ip))
        {
            return address is null ? null : "*";
        }

        if (ip.IsIPv4MappedToIPv6)
        {
            ip = ip.MapToIPv4();
        }

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = ip.GetAddressBytes();
            return $"{bytes[0]}.{bytes[1]}.{bytes[2]}.*";
        }

        // 按字节取前四组（压缩写法 ::1 拆分后组数不定，会把整个地址露出来）
        var groups = ip.GetAddressBytes();
        return string.Create(CultureInfo.InvariantCulture, $"{groups[0] << 8 | groups[1]:x}:{groups[2] << 8 | groups[3]:x}:{groups[4] << 8 | groups[5]:x}:{groups[6] << 8 | groups[7]:x}:*");
    }
}
