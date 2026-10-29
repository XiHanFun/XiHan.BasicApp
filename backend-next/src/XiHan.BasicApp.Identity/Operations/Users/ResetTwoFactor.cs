// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Users;

/// <summary>
/// 批量重置用户的双因素。
/// </summary>
/// <remarks>
/// <c>POST /api/users:reset-two-factor</c>。清掉全部方式与认证器密钥，用户下次只凭密码登录、再自行重新绑定；
/// 给丢了认证器的用户用。不能对自己操作（本人走个人中心）。每个被重置的账号记一条 <see cref="LoginEvent.TwoFactorReset"/> 登录日志。
/// </remarks>
/// <param name="users">用户仓储。</param>
/// <param name="securities">账号安全仓储。</param>
/// <param name="accounts">账号域服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = IdentityPermissions.UsersResetTwoFactor)]
public sealed class ResetTwoFactor(IRepository<SysUser, Guid> users, IRepository<SysUserSecurity, Guid> securities, UserAccountService accounts) : IOperation<UserIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "users.reset-two-factor";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(UserIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var (targets, failed) = await UserBatch.ResolveAsync(users, request.Ids, context, rejectSelf: true).ConfigureAwait(false);
        var succeeded = new List<Guid>(targets.Count);
        foreach (var user in targets)
        {
            var securityResult = await accounts.SecurityOfAsync(user.Id, context.CancellationToken).ConfigureAwait(false);
            if (!securityResult.TryGetValue(out var security))
            {
                failed.Add(new BatchFailure(user.Id, securityResult.Error!.Code));
                continue;
            }

            security.TwoFactorEnabled = false;
            security.TwoFactorMethods = TwoFactorMethods.None;
            security.TwoFactorSecret = null;
            security.TwoFactorLastStep = null;
            security.SecurityStamp = UserAccountService.NewStamp();
            var updated = await securities.UpdateAsync(security, context.CancellationToken).ConfigureAwait(false);
            if (updated.IsFailure)
            {
                failed.Add(new BatchFailure(user.Id, updated.Error!.Code));
                continue;
            }

            succeeded.Add(user.Id);
            context.AddAuditLog(new LoginLogEntry(LoginEvent.TwoFactorReset) { TenantId = user.HomeTenantId, AccountTenantId = user.HomeTenantId, UserId = user.Id, UserName = user.UserName });
        }

        return new BatchResult(succeeded, failed);
    }
}
