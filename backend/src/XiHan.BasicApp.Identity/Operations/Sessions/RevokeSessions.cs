// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.BasicApp.Identity.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Sessions;

/// <summary>
/// 强制下线一批会话
/// </summary>
/// <remarks>
/// <c>POST /api/sessions:revoke</c>。吊销一批有效会话（只限当前一侧、数据范围内的），提交后经实时连接通知客户端退出登录并断开它们的连接；
/// 不存在或看不到的会话逐项 <c>IDENTITY_NOTFOUND_002</c>，调用方自己当前的会话逐项 <c>IDENTITY_CONFLICT_005</c>（用退出登录）
/// 下线的是别人的会话时，逐个给会话的主人发一条「登录会话被强制下线」的安全通知（带设备与原因）；模仿会话与调用方自己的会话不发
/// </remarks>
/// <param name="queries">会话查询（取当前一侧、数据范围内的有效会话）</param>
/// <param name="accounts">账号域服务（吊销与推送）</param>
/// <param name="notices">发给本人的安全通知</param>
[Operation(Id, Expose = Expose.Http, Permission = IdentityPermissions.SessionsRevoke)]
public sealed class RevokeSessions(SessionQueries queries, UserAccountService accounts, SecurityNotices notices) : IOperation<RevokeSessionsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "sessions.revoke";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(RevokeSessionsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var ids = request.Ids.Distinct().ToList();
        var visible = await queries.ActiveAsync(context).ConfigureAwait(false);
        var found = (await visible.Where(s => ids.Contains(s.Id)).ToListAsync(context.CancellationToken).ConfigureAwait(false)).ToDictionary(static s => s.Id);
        var current = SessionQueries.CurrentSessionOf(context);
        var failed = new List<BatchFailure>();
        var targets = new List<Domain.Entities.SysUserSession>(ids.Count);
        foreach (var id in ids)
        {
            if (!found.TryGetValue(id, out var session))
            {
                failed.Add(new BatchFailure(id, IdentityErrors.SessionNotFound, IdentityMessages.SessionNotFound.L(("id", id))));
            }
            else if (id == current)
            {
                failed.Add(new BatchFailure(id, IdentityErrors.CurrentSessionNotRevocable, IdentityMessages.CurrentSessionNotRevocable.L()));
            }
            else
            {
                targets.Add(session);
            }
        }

        if (targets.Count == 0)
        {
            return new BatchResult([], failed);
        }

        var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        var revoked = await accounts.RevokeAsync(targets, "revoked", reason, context.CancellationToken).ConfigureAwait(false);
        if (revoked.IsFailure)
        {
            return revoked.Error!;
        }

        // 排在强制下线之后：被下线的端先断开，提醒只留在收件箱、推给同一侧别的端
        var operatorId = context.GetSecurity().Principal.Id;
        foreach (var session in targets.Where(s => !s.ImpersonatorUserId.HasValue && !string.Equals(s.UserId.ToString(), operatorId, StringComparison.OrdinalIgnoreCase)))
        {
            await notices.SessionRevokedAsync(session, reason, context).ConfigureAwait(false);
        }

        return new BatchResult(targets.Select(static s => s.Id).ToList(), failed);
    }
}
