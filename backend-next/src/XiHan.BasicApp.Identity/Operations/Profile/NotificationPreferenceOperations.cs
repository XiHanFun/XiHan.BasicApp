// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Profile;

/// <summary>
/// 本人的通知偏好（没保存过即缺省，全收）。强制阅读与紧急通知不受偏好影响。
/// </summary>
/// <param name="ChannelInApp">收站内信。</param>
/// <param name="ChannelEmail">收邮件。</param>
/// <param name="ChannelSms">收短信。</param>
/// <param name="TypeAnnouncement">收公告（系统、业务等）。</param>
/// <param name="TypeTask">收待办。</param>
/// <param name="TypeSecurity">收安全通知。</param>
public sealed record NotificationPreferenceResponse(bool ChannelInApp, bool ChannelEmail, bool ChannelSms, bool TypeAnnouncement, bool TypeTask, bool TypeSecurity)
{
    /// <summary>
    /// 一行偏好的投影；没有行即缺省。
    /// </summary>
    public static NotificationPreferenceResponse From(SysUserNotificationPreference? preference)
    {
        var row = preference ?? new SysUserNotificationPreference();
        return new(row.ChannelInApp, row.ChannelEmail, row.ChannelSms, row.TypeAnnouncement, row.TypeTask, row.TypeSecurity);
    }
}

/// <summary>
/// <c>profile.update-notification-preference</c> 的请求（整体覆盖，4.x 同）。
/// </summary>
/// <param name="ChannelInApp">收站内信。</param>
/// <param name="ChannelEmail">收邮件。</param>
/// <param name="ChannelSms">收短信。</param>
/// <param name="TypeAnnouncement">收公告（系统、业务等）。</param>
/// <param name="TypeTask">收待办。</param>
/// <param name="TypeSecurity">收安全通知。</param>
public sealed record UpdateNotificationPreferenceRequest(bool ChannelInApp, bool ChannelEmail, bool ChannelSms, bool TypeAnnouncement, bool TypeTask, bool TypeSecurity);

/// <summary>
/// 本人的通知偏好：<c>GET /api/profile:notification-preference</c>（4.x <c>Profile/NotificationPreference</c>）。已登录即可。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="preferences">偏好仓储。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class GetNotificationPreference(IRepository<SysUser, Guid> users, UserAccountService accounts, IRepository<SysUserNotificationPreference, Guid> preferences) : IOperation<ProfileRequest, NotificationPreferenceResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "profile.notification-preference";

    /// <inheritdoc />
    public async ValueTask<Result<NotificationPreferenceResponse>> HandleAsync(ProfileRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: false).ConfigureAwait(false);
        if (!subject.TryGetValue(out var found))
        {
            return subject.Error!;
        }

        return NotificationPreferenceResponse.From(await NotificationPreferenceRows.FindAsync(preferences, found.User.Id, context.CancellationToken).ConfigureAwait(false));
    }
}

/// <summary>
/// 保存本人的通知偏好：<c>POST /api/profile:update-notification-preference</c>（4.x 同，整体覆盖，第一次保存时建行）。已登录即可；模仿会话不拒绝（4.x 同）。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="preferences">偏好仓储。</param>
/// <param name="ids">标识生成器。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class UpdateNotificationPreference(IRepository<SysUser, Guid> users, UserAccountService accounts, IRepository<SysUserNotificationPreference, Guid> preferences, IIdGenerator<Guid> ids) : IOperation<UpdateNotificationPreferenceRequest, NotificationPreferenceResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "profile.update-notification-preference";

    /// <inheritdoc />
    public async ValueTask<Result<NotificationPreferenceResponse>> HandleAsync(UpdateNotificationPreferenceRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: false).ConfigureAwait(false);
        if (!subject.TryGetValue(out var found))
        {
            return subject.Error!;
        }

        var existing = await NotificationPreferenceRows.FindAsync(preferences, found.User.Id, context.CancellationToken).ConfigureAwait(false);
        var row = existing ?? new SysUserNotificationPreference { Id = ids.NewId(), UserId = found.User.Id };
        row.ChannelInApp = request.ChannelInApp;
        row.ChannelEmail = request.ChannelEmail;
        row.ChannelSms = request.ChannelSms;
        row.TypeAnnouncement = request.TypeAnnouncement;
        row.TypeTask = request.TypeTask;
        row.TypeSecurity = request.TypeSecurity;
        var saved = existing is null
            ? await preferences.AddAsync(row, context.CancellationToken).ConfigureAwait(false)
            : await preferences.UpdateAsync(row, context.CancellationToken).ConfigureAwait(false);
        return saved.IsFailure ? saved.Error! : NotificationPreferenceResponse.From(row);
    }
}

/// <summary>
/// 按用户取偏好行。
/// </summary>
internal static class NotificationPreferenceRows
{
    /// <summary>
    /// 本人的偏好行；没保存过为空。
    /// </summary>
    public static async Task<SysUserNotificationPreference?> FindAsync(IRepository<SysUserNotificationPreference, Guid> preferences, Guid userId, CancellationToken cancellationToken)
    {
        var rows = await preferences.ListAsync(EntityQuery.All<SysUserNotificationPreference>().Where(new Specification<SysUserNotificationPreference>(p => p.UserId == userId)).Page(0, 1), cancellationToken).ConfigureAwait(false);
        return rows.Count > 0 ? rows[0] : null;
    }
}
