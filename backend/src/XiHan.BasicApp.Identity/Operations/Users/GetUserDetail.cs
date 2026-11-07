// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Queries;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Users;

/// <summary>
/// <c>users.detail</c> 的请求
/// </summary>
/// <param name="Id">用户 ID</param>
/// <param name="TimeZone">今日统计按哪个时区分天（IANA 名称，如 <c>Asia/Shanghai</c>；缺省 UTC）</param>
public sealed record UserDetailRequest(Guid Id, [StringLength(64)] string? TimeZone = null);

/// <summary>
/// 管理员看到的账号安全状态
/// </summary>
/// <param name="EmailVerified">邮箱是否已验证</param>
/// <param name="PhoneVerified">手机号是否已验证</param>
/// <param name="TwoFactorEnabled">是否开启了双因素</param>
/// <param name="TwoFactorMethods">开启的双因素方式</param>
/// <param name="IsLocked">是否锁定：管理员锁定或登录失败锁定期内</param>
/// <param name="LockoutEndAt">登录失败锁定的截止时间；没在锁定期为空</param>
/// <param name="FailedAccessCount">自上次成功登录以来的连续失败次数</param>
public sealed record UserSecurityDetailResponse(
    bool EmailVerified,
    bool PhoneVerified,
    bool TwoFactorEnabled,
    IReadOnlyList<TwoFactorMethod> TwoFactorMethods,
    bool IsLocked,
    DateTimeOffset? LockoutEndAt,
    int FailedAccessCount);

/// <summary>
/// 今日统计
/// </summary>
/// <param name="Logins">登录次数（成功的）</param>
/// <param name="Accesses">访问次数</param>
/// <param name="OnlineSeconds">在线时长（秒）</param>
public sealed record UserTodayResponse(int Logins, int Accesses, long OnlineSeconds);

/// <summary>
/// 当前会话
/// </summary>
/// <param name="Device">设备类型：Desktop、Mobile、Tablet、Bot；认不出为空</param>
/// <param name="Browser">浏览器</param>
/// <param name="Os">操作系统</param>
/// <param name="IpAddressMasked">登录 IP（IPv4 遮住末段、IPv6 只留前四组）</param>
/// <param name="Location">登录地点</param>
/// <param name="LoginAt">登录时间（会话签发）</param>
/// <param name="LastActiveAt">最后活动时间（最近一次续签，没续签过即登录时间）</param>
public sealed record UserSessionDetailResponse(string? Device, string? Browser, string? Os, string? IpAddressMasked, string? Location, DateTimeOffset LoginAt, DateTimeOffset LastActiveAt);

/// <summary>
/// 用户管理的详情
/// </summary>
/// <param name="User">用户资料</param>
/// <param name="Security">账号安全；外部成员为空（账号域数据由其注册地维护）</param>
/// <param name="Today">今日统计（只算当前一侧）；用户不在当前一侧（平台看租户的用户）或没装日志包为空</param>
/// <param name="CurrentSession">这一侧最近活动的会话（模仿会话不算）；没有有效会话为空</param>
public sealed record UserDetailResponse(UserResponse User, UserSecurityDetailResponse? Security, UserTodayResponse? Today, UserSessionDetailResponse? CurrentSession);

/// <summary>
/// 用户详情：资料、账号安全、今日统计与当前会话
/// </summary>
/// <remarks>
/// <c>GET /api/users/{id}:detail</c>（只要读用户的权限）。可见范围同 <c>users.get</c>：租户主体看本租户的用户与成员，受数据范围约束，范围外按不存在回答
/// 统计与会话都只看调用所在的一侧：外部成员在别的租户的活动不算。今日统计按原始日志现算，按请求的时区分天
/// </remarks>
/// <param name="users">用户仓储</param>
/// <param name="sideUsers">这一侧的用户</param>
/// <param name="accounts">账号域服务</param>
/// <param name="sessions">会话查询</param>
/// <param name="activity">今日统计（Auditing 提供）</param>
/// <param name="clock">时钟</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = IdentityPermissions.UsersRead)]
public sealed class GetUserDetail(IRepository<SysUser, Guid> users, SideUsers sideUsers, UserAccountService accounts, SessionQueries sessions, IEnumerable<IUserActivityReader> activity, TimeProvider clock) : IOperation<UserDetailRequest, UserDetailResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "users.detail";

    /// <inheritdoc />
    public async ValueTask<Result<UserDetailResponse>> HandleAsync(UserDetailRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var zone = TimeZoneInfo.Utc;
        if (!string.IsNullOrWhiteSpace(request.TimeZone) && !TimeZoneInfo.TryFindSystemTimeZoneById(request.TimeZone.Trim(), out zone))
        {
            return Error.Validation(IdentityErrors.TimeZoneInvalid, IdentityMessages.TimeZoneInvalid, MessageArguments.Of(("timeZone", request.TimeZone)));
        }

        var tenantId = context.GetSecurity().Tenant?.Id;
        var found = tenantId is null
            ? await users.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false)
            : await sideUsers.FindWithSupportAsync(request.Id, tenantId, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var user) || !(await DataScopeQuery.ResolveAsync(context).ConfigureAwait(false)).Allows(user.Id))
        {
            return Error.NotFound(IdentityErrors.UserNotFound, IdentityMessages.UserNotFound, MessageArguments.Of(("id", request.Id)));
        }

        var response = UserResponse.From(user, tenantId, tenantId is not null && await sideUsers.IsSupportMemberAsync(user.Id, tenantId, context.CancellationToken).ConfigureAwait(false));
        UserSecurityDetailResponse? security = null;
        if (!response.IsExternalMember)
        {
            var row = await accounts.SecurityOfAsync(user.Id, context.CancellationToken).ConfigureAwait(false);
            if (!row.TryGetValue(out var account))
            {
                return row.Error!;
            }

            var lockedUntil = account.LockoutEndAt is { } end && end > clock.GetUtcNow() ? end : (DateTimeOffset?)null;
            security = new UserSecurityDetailResponse(
                user.EmailVerified,
                user.PhoneVerified,
                account.TwoFactorEnabled,
                TwoFactorSettings.MethodsOf(account.TwoFactorMethods),
                user.Status == UserStatus.Locked || lockedUntil is not null,
                lockedUntil,
                account.FailedAccessCount);
        }

        // 平台看租户的用户：这个人的日志与会话都在租户那一侧
        var inSide = response.IsExternalMember || string.Equals(user.HomeTenantId, tenantId ?? TenantIds.Platform, StringComparison.Ordinal);
        if (!inSide)
        {
            return new UserDetailResponse(response, security, null, null);
        }

        UserTodayResponse? today = null;
        if (activity.FirstOrDefault() is { } reader)
        {
            var counts = await reader.TodayAsync(context, user.Id, zone).ConfigureAwait(false);
            today = new UserTodayResponse(counts.Logins, counts.Accesses, counts.OnlineSeconds);
        }

        return new UserDetailResponse(response, security, today, await sessions.LatestOfAsync(user.Id, context).ConfigureAwait(false));
    }
}
